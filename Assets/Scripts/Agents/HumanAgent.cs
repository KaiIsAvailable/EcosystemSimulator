using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HumanAgent : MonoBehaviour
{
    [Header("Movement Settings")]
    public bool canMove = false; // Toggle to enable/disable movement
    public float speed = 2f;
    public float retargetInterval = 3f;   // seconds between choosing new direction
    public float arriveDistance = 0.2f;   // how close to target before re-choosing

    [Header("Area Boundaries (set by WorldLogic)")]
    [HideInInspector] public Vector3 areaCenter;
    [HideInInspector] public Vector2 areaHalfExtents;

    [Header("Breathing Animation")]
    public bool showBreathing = true;
    public float breathingSpeed = 1.2f;      // Humans breathe slightly slower than animals
    public float breathingAmount = 0.06f;    // Slightly more visible breathing (6%)
    
    [Header("Breeding Behavior")]
    public bool seekMateForBreeding = true;  // Enable mate-seeking behavior
    public float mateSeekDistance = 8f;      // How far to search for mates
    
    [Header("Hunting Behavior")]
    public float huntingSearchRadius = 5f;   // How far to search for prey
    public float eatingRange = 0.6f;         // Distance to eat prey
    public float trophicEfficiency = 0.15f;  // Biomass gain from eating

    private Rigidbody2D rb;
    private Vector3 target;
    private Vector3 originalScale;
    private float breathingTimer = 0f;
    private HumanMetabolism targetMate = null;  // Current mate being sought
    private AnimalMetabolism targetPrey = null;  // Current prey being hunted
    private HumanMetabolism myMetabolism;
    private bool isSeekingMate = false;  // Commanded to seek mate
    private HumanAgent assignedMate = null;  // Specific mate to seek

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myMetabolism = GetComponent<HumanMetabolism>();
        originalScale = transform.localScale;
        
        // Disable gravity for top-down view
        rb.gravityScale = 0f;
        
        // Randomize breathing phase so they don't all breathe in sync
        breathingTimer = Random.Range(0f, Mathf.PI * 2f);
        
        if (canMove)
        {
            PickNewTarget();
            // Re-choose target every few seconds
            InvokeRepeating(nameof(PickNewTarget), retargetInterval, retargetInterval);
        }
    }

    void FixedUpdate()
    {
        // Breathing animation (always active)
        if (showBreathing)
        {
            breathingTimer += Time.fixedDeltaTime * breathingSpeed;
            float breathScale = 1f + Mathf.Sin(breathingTimer) * breathingAmount;
            transform.localScale = originalScale * breathScale;
        }
        
        // Movement (only if enabled)
        if (!canMove) return;
        
        // PRIORITY 1: Hunt if hungry
        bool isHungry = myMetabolism != null && (myMetabolism.hunger / myMetabolism.maxHunger) < (myMetabolism.hungerSearchThreshold / 100f);
        
        if (isHungry)
        {
            // Cancel mate seeking if hungry
            if (isSeekingMate)
            {
                StopSeeking();
            }
            
            // Set activity to hunting
            if (myMetabolism != null)
                myMetabolism.currentActivity = HumanMetabolism.ActivityState.Hunting;
            
            // Find and chase prey
            if (targetPrey == null || !targetPrey.isAlive)
            {
                FindPrey();
            }
            
            if (targetPrey != null && targetPrey.isAlive)
            {
                // Chase prey
                target = targetPrey.transform.position;
                
                // Check if in eating range
                float distanceToPrey = Vector3.Distance(transform.position, targetPrey.transform.position);
                if (distanceToPrey <= eatingRange)
                {
                    EatPrey(targetPrey);
                    targetPrey = null;
                }
            }
        }
        // PRIORITY 2: Seek assigned mate (if commanded)
        else if (isSeekingMate && assignedMate != null)
        {
            if (myMetabolism != null)
                myMetabolism.currentActivity = HumanMetabolism.ActivityState.Walking;
            
            // Move toward assigned mate
            target = assignedMate.transform.position;
        }
        // PRIORITY 3: Random wandering
        else
        {
            if (myMetabolism != null)
                myMetabolism.currentActivity = HumanMetabolism.ActivityState.Walking;
        }
        
        // Move toward target
        Vector3 dir = (target - transform.position);
        if (dir.magnitude < arriveDistance)
        {
            // Arrived at target - pick new random target
            PickNewTarget();
        }

        dir.Normalize();
        Vector3 nextPos = transform.position + dir * speed * Time.fixedDeltaTime;

        // Clamp inside area
        nextPos.x = Mathf.Clamp(nextPos.x, areaCenter.x - areaHalfExtents.x, areaCenter.x + areaHalfExtents.x);
        nextPos.y = Mathf.Clamp(nextPos.y, areaCenter.y - areaHalfExtents.y, areaCenter.y + areaHalfExtents.y);

        rb.MovePosition(nextPos);
    }
    
    /// <summary>
    /// Command this human to seek a specific mate for breeding
    /// </summary>
    public void SeekMate(HumanAgent mate)
    {
        assignedMate = mate;
        isSeekingMate = true;
        Debug.Log($"[HumanAgent] {gameObject.name} now seeking {mate.gameObject.name}");
    }
    
    /// <summary>
    /// Stop seeking mate and return to normal wandering
    /// </summary>
    public void StopSeeking()
    {
        isSeekingMate = false;
        assignedMate = null;
        PickNewTarget();
    }
    
    void FindPrey()
    {
        // Find nearest animal within hunting radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, huntingSearchRadius);
        AnimalMetabolism nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D col in colliders)
        {
            AnimalMetabolism animal = col.GetComponent<AnimalMetabolism>();
            if (animal != null && animal.isAlive)
            {
                float distance = Vector2.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = animal;
                }
            }
        }

        targetPrey = nearest;
        if (targetPrey != null)
        {
            Debug.Log($"[HumanAgent] {gameObject.name} hunting {targetPrey.gameObject.name} (distance: {nearestDistance:F2})");
        }
    }
    
    void EatPrey(AnimalMetabolism prey)
    {
        if (prey == null || myMetabolism == null) return;
        
        string animalName = prey.gameObject.name;
        float biomassTaken = prey.biomass;

        // Kill the animal
        prey.Die();

        // Gain biomass and restore hunger
        float biomassGained = biomassTaken * trophicEfficiency;
        myMetabolism.biomass += biomassGained;
        myMetabolism.biomass = Mathf.Clamp(myMetabolism.biomass, 0f, myMetabolism.maxBiomass);
        
        myMetabolism.hunger += myMetabolism.hungerGainPerAnimal;
        myMetabolism.hunger = Mathf.Clamp(myMetabolism.hunger, 0f, myMetabolism.maxHunger);

        // Notify about hunting
        if (EventNotificationUI.Instance != null)
        {
            EventNotificationUI.Instance.NotifyHumanHunt(gameObject.name, animalName);
        }

        // Trigger animal reproduction after delay
        StartCoroutine(DelayedAnimalReproduction(10f));

        Debug.Log($"[HumanAgent] {gameObject.name} ate {animalName}, gained {biomassGained:F1} kg biomass, hunger now {myMetabolism.hunger:F0}");
    }
    
    System.Collections.IEnumerator DelayedAnimalReproduction(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (AnimalRespawnManager.Instance != null)
        {
            AnimalRespawnManager.Instance.TriggerAnimalReproduction();
        }
    }
    
    void FindMate()
    {
        // Find opposite gender within seeking distance
        HumanMetabolism[] allHumans = FindObjectsOfType<HumanMetabolism>();
        
        if (myMetabolism == null || !myMetabolism.isAlive) return;
        
        // Determine my gender based on sprite color
        SpriteRenderer mySr = GetComponent<SpriteRenderer>();
        if (mySr == null) return;
        
        bool iAmMale = IsYellowColor(mySr.color);
        
        HumanMetabolism closestMate = null;
        float closestDistance = float.MaxValue;
        
        foreach (var human in allHumans)
        {
            if (human == myMetabolism) continue;  // Skip self
            if (!human.isAlive) continue;
            
            SpriteRenderer theirSr = human.GetComponent<SpriteRenderer>();
            if (theirSr == null) continue;
            
            bool theyAreMale = IsYellowColor(theirSr.color);
            
            // Only seek opposite gender
            if (iAmMale == theyAreMale) continue;
            
            float distance = Vector3.Distance(transform.position, human.transform.position);
            if (distance <= mateSeekDistance && distance < closestDistance)
            {
                closestMate = human;
                closestDistance = distance;
            }
        }
        
        if (closestMate != null)
        {
            targetMate = closestMate;
            //Debug.Log($"[HumanAgent] {gameObject.name} seeking mate: {targetMate.gameObject.name} (distance: {closestDistance:F2})");
        }
    }
    
    void PickTargetNearMate()
    {
        // Pick a position near the mate (stay close for breeding)
        if (targetMate != null)
        {
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0f);
            target = targetMate.transform.position + offset;
        }
        else
        {
            PickNewTarget();
        }
    }
    
    bool IsYellowColor(Color color)
    {
        // Yellow = more red+green than blue
        return (color.r > color.b && color.g > color.b);
    }

    void PickNewTarget()
    {
        float x = Random.Range(-areaHalfExtents.x, areaHalfExtents.x);
        float y = Random.Range(-areaHalfExtents.y, areaHalfExtents.y);
        target = areaCenter + new Vector3(x, y, 0f);
    }

    // Optional: visualize wander area in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(areaCenter, new Vector3(areaHalfExtents.x * 2f, areaHalfExtents.y * 2f, 0));
    }
}
