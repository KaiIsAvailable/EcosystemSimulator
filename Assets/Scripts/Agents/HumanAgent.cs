using UnityEngine;
using System.Collections;
using System.Linq; // Use System.Linq for queries

[RequireComponent(typeof(Rigidbody2D))]
public class HumanAgent : MonoBehaviour
{
    [Header("Movement Settings")]
    public bool canMove = false; // Toggle to enable/disable movement
    public float speed = 2f;
    public float retargetInterval = 3f;   
    public float arriveDistance = 0.2f;   

    [Header("Identity")]
    [Tooltip("True if this agent is male (used for gender-based counting)")]
    public bool isMale = true; // NEW: Reliable gender tracking

    [Header("Area Boundaries (set by WorldLogic)")]
    // Removed HideInInspector as these are unused after switching to WorldBounds.ClampToLand
    public Vector3 areaCenter;
    public Vector2 areaHalfExtents;

    [Header("Breathing Animation")]
    public bool showBreathing = true;
    public float breathingSpeed = 1.2f;
    public float breathingAmount = 0.06f; 
    
    [Header("Breeding Behavior")]
    public bool seekMateForBreeding = true;
    public float mateSeekDistance = 8f;
    
    private Rigidbody2D rb;
    private Vector3 target;
    private Vector3 originalScale;
    private float breathingTimer = 0f;
    private HumanMetabolism targetMate = null;
    private AnimalMetabolism targetPrey = null;
    private HumanMetabolism myMetabolism;
    private bool isSeekingMate = false;
    private HumanAgent assignedMate = null;
    private SunMoonController sunMoon;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myMetabolism = GetComponent<HumanMetabolism>();
        sunMoon = FindAnyObjectByType<SunMoonController>();
        originalScale = transform.localScale;
        
        rb.gravityScale = 0f;
        breathingTimer = Random.Range(0f, Mathf.PI * 2f);
        
        if (canMove)
        {
            PickNewTarget();
            InvokeRepeating(nameof(PickNewTarget), retargetInterval, retargetInterval);
        }
    }

    void FixedUpdate()
    {
        // Check if it's nighttime - sleep system
        bool isSleeping = (sunMoon != null && sunMoon.currentTimeOfDay == SunMoonController.TimeOfDay.Night);
        
        if (isSleeping)
        {
            // Stop all movement during night
            rb.linearVelocity = Vector2.zero;
            
            // Set metabolism to resting state
            if (myMetabolism != null)
            {
                myMetabolism.currentActivity = HumanMetabolism.ActivityState.Resting;
            }
            
            // Breathing animation (slower during sleep)
            if (showBreathing)
            {
                breathingTimer += Time.fixedDeltaTime * breathingSpeed * 0.5f; // Half speed breathing
                float breathScale = 1f + Mathf.Sin(breathingTimer) * breathingAmount * 0.5f; // Less movement
                transform.localScale = originalScale * breathScale;
            }
            return; // Skip all movement logic
        }
        
        // Breathing animation (normal during day)
        if (showBreathing)
        {
            breathingTimer += Time.fixedDeltaTime * breathingSpeed;
            float breathScale = 1f + Mathf.Sin(breathingTimer) * breathingAmount;
            transform.localScale = originalScale * breathScale;
        }
        
        // Movement (only if enabled and awake)
        if (!canMove || myMetabolism == null || !myMetabolism.isAlive) return;
        
        // PRIORITY 1: Hunt if hungry
        bool isHungry = (myMetabolism.hunger / myMetabolism.maxHunger) < (myMetabolism.hungerSearchThreshold / 100f);
        
        if (isHungry)
        {
            if (isSeekingMate) StopSeeking();
            
            myMetabolism.currentActivity = HumanMetabolism.ActivityState.Hunting;
            
            if (targetPrey == null || !targetPrey.isAlive)
            {
                FindPrey();
            }
            
            if (targetPrey != null && targetPrey.isAlive)
            {
                // Chase prey
                target = targetPrey.transform.position;
                
                // Check if in eating range (use metabolism's eatingRange)
                float distanceToPrey = Vector3.Distance(transform.position, targetPrey.transform.position);
                if (distanceToPrey <= myMetabolism.eatingRange) // Using corrected range from Metabolism
                {
                    EatPrey(targetPrey);
                    targetPrey = null;
                }
            }
        }
        // PRIORITY 2: Seek assigned mate (if commanded)
        else if (isSeekingMate && assignedMate != null)
        {
            myMetabolism.currentActivity = HumanMetabolism.ActivityState.Walking;
            target = assignedMate.transform.position;
        }
        // PRIORITY 3: Random wandering
        else
        {
            myMetabolism.currentActivity = HumanMetabolism.ActivityState.Walking;
        }
        
        // Dynamic Speed Calculation based on Activity State
        float activityMultiplier = GetActivitySpeedMultiplier(myMetabolism.currentActivity);
        
        // Move toward target
        Vector3 dir = (target - transform.position);
        if (dir.magnitude < arriveDistance)
        {
            PickNewTarget();
        }

        dir.Normalize();
        Vector3 nextPos = transform.position + dir * speed * activityMultiplier * Time.fixedDeltaTime;

        // CRITICAL FIX: Clamp using WorldBounds helper function for land safety
        nextPos = WorldBounds.ClampToLand(nextPos);

        rb.MovePosition(nextPos);
    }
    
    float GetActivitySpeedMultiplier(HumanMetabolism.ActivityState state)
    {
        // Use Hunting multiplier for movement only when actively hunting
        if (state == HumanMetabolism.ActivityState.Hunting)
        {
            return 3.5f; 
        }
        // Use 1.0f for all other states (Walking, Resting, Working) for simpler movement.
        // We avoid tying speed to BMR multipliers directly for movement control.
        return 1.0f; 
    }
    
    public void SeekMate(HumanAgent mate)
    {
        assignedMate = mate;
        isSeekingMate = true;
        Debug.Log($"[HumanAgent] {gameObject.name} now seeking {mate.gameObject.name}");
    }
    
    public void StopSeeking()
    {
        isSeekingMate = false;
        assignedMate = null;
        PickNewTarget();
    }
    
    void FindPrey()
    {
        // Use the HumanMetabolism's search radius
        float searchRadius = myMetabolism.searchRadius; 
        
        AnimalMetabolism nearest = Physics2D.OverlapCircleAll(transform.position, searchRadius)
            .Select(c => c.GetComponent<AnimalMetabolism>())
            .Where(a => a != null && a.isAlive)
            .OrderBy(a => Vector3.Distance(transform.position, a.transform.position))
            .FirstOrDefault();

        targetPrey = nearest;
        if (targetPrey != null)
        {
            Debug.Log($"[HumanAgent] {gameObject.name} hunting {targetPrey.gameObject.name}");
        }
    }
    
    void EatPrey(AnimalMetabolism prey)
    {
        // Delegate eating fully back to HumanMetabolism to centralize hunger/biomass logic
        myMetabolism.EatAnimal(prey);
        
        // Reproduction trigger is handled inside HumanMetabolism.EatAnimal
        
        // After eating, the agent is no longer hungry, so it reverts to wandering/seeking.
    }
    
    // Simplified target picking using WorldBounds initialized area
    void PickNewTarget()
    {
        // Safety check: If WorldBounds not initialized yet, use current position
        if (!WorldBounds.IsInitialized)
        {
            target = transform.position;
            return;
        }
        
        target = WorldBounds.GetRandomLandPosition();
    }
    
    // Removed redundant gender identification functions (IsYellowColor, FindMate, PickTargetNearMate)
    // as gender is now a public variable and mating logic is handled by HumanRespawnManager
    
    void OnDrawGizmosSelected()
    {
        if (WorldBounds.IsInitialized)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(WorldBounds.areaCenter, new Vector3(WorldBounds.areaHalfExtents.x * 2f, WorldBounds.areaHalfExtents.y * 2f, 0));
        }
        
        if (targetPrey != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetPrey.transform.position);
            Gizmos.DrawWireSphere(targetPrey.transform.position, 0.5f);
        }
        else if (assignedMate != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, assignedMate.transform.position);
        }
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target, 0.1f);
    }
}