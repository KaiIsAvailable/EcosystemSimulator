using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AnimalWander : MonoBehaviour
{
    [Header("Movement")]
    public bool canMove = false; // Toggle to enable/disable movement
    public float speed = 2f;
    public float retargetInterval = 2f;
    public float arriveDistance = 0.1f;

    [Header("Avoidance")]
    public float avoidRadius = 0.4f;
    public LayerMask obstacleMask; // set to "Obstacle" in Inspector

    [Header("Facing / Head Direction")]
    public FacingMode facingMode = FacingMode.Rotate;   // Rotate or FlipX
    [Tooltip("How fast to turn (degrees / sec) when Rotate mode is used")]
    public float turnSpeed = 360f;
    [Tooltip("If your sprite's 'forward' is not to the RIGHT, add an offset. (e.g., faces UP -> 90)")]
    public float spriteAngleOffset = 0f;
    [Tooltip("Below this speed we keep the last facing (prevents jitter when nearly idle).")]
    public float facingIdleThreshold = 0.01f;

    [HideInInspector] public Vector3 areaCenter;       // set by WorldLogic
    [HideInInspector] public Vector2 areaHalfExtents;  // set by WorldLogic

    [Header("Breathing Animation")]
    public bool showBreathing = true;
    public float breathingSpeed = 1.5f;      // How fast they breathe
    public float breathingAmount = 0.05f;    // How much they expand/contract (5%)

    private Rigidbody2D rb;
    private Vector3 target;
    private Vector2 lastMoveDir = Vector2.right;
    private SpriteRenderer sr; // optional if you want FlipX
    private Vector3 originalScale;
    private float breathingTimer = 0f;

    public enum FacingMode { Rotate, FlipX }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>(); // ok if null when Rotate mode
        originalScale = transform.localScale;
        
        // Disable gravity for top-down view
        rb.gravityScale = 0f;
        
        // Randomize breathing phase so they don't all breathe in sync
        breathingTimer = Random.Range(0f, Mathf.PI * 2f);
        
        if (canMove)
        {
            PickNewTarget();
            InvokeRepeating(nameof(PickNewTarget), retargetInterval, retargetInterval);
        }
    }

    void PickNewTarget()
    {
        float x = Random.Range(-areaHalfExtents.x, areaHalfExtents.x);
        float y = Random.Range(-areaHalfExtents.y, areaHalfExtents.y);
        target = areaCenter + new Vector3(x, y, 0f);
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
        
        // If near target, pick a new one
        if ((target - transform.position).sqrMagnitude < arriveDistance * arriveDistance)
            PickNewTarget();

        // If touching a tree/obstacle, immediately retarget
        if (Physics2D.OverlapCircle(transform.position, avoidRadius, obstacleMask))
            PickNewTarget();

        // Move toward target, clamped to area
        Vector3 toTarget = (target - transform.position);
        Vector2 dir = toTarget.sqrMagnitude > 0.0001f ? ((Vector2)toTarget).normalized : Vector2.zero;

        // keep inside area for next position
        Vector3 next = transform.position + (Vector3)(dir * speed * Time.fixedDeltaTime);
        next.x = Mathf.Clamp(next.x, areaCenter.x - areaHalfExtents.x, areaCenter.x + areaHalfExtents.x);
        next.y = Mathf.Clamp(next.y, areaCenter.y - areaHalfExtents.y, areaCenter.y + areaHalfExtents.y);

        // apply movement
        rb.MovePosition(next);

        // compute current velocity approximation for facing
        Vector2 vel = dir * speed; // since we control speed directly
        if (vel.sqrMagnitude >= facingIdleThreshold * facingIdleThreshold)
            lastMoveDir = vel.normalized; // remember last valid move direction

        // apply facing
        if (facingMode == FacingMode.Rotate)
        {
            // Angle using RIGHT as forward. Add offset if your sprite faces UP, etc.
            float desiredAngle = Mathf.Atan2(lastMoveDir.y, lastMoveDir.x) * Mathf.Rad2Deg + spriteAngleOffset;
            float newAngle = Mathf.MoveTowardsAngle(rb.rotation, desiredAngle, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }
        else // FlipX
        {
            // Keep body upright; just flip left/right by x direction
            if (sr != null && Mathf.Abs(lastMoveDir.x) > 0.0001f)
                sr.flipX = (lastMoveDir.x < 0f);
            // Optional: tiny tilt if你想要些许朝向感，可加：transform.rotation = Quaternion.identity;
        }
    }

    // (optional) visualize avoidance radius
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, avoidRadius);
    }
}