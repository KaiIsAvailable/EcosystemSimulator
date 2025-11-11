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

    private Rigidbody2D rb;
    private Vector3 target;
    private Vector3 originalScale;
    private float breathingTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
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
        
        // Move toward target
        Vector3 dir = (target - transform.position);
        if (dir.magnitude < arriveDistance)
        {
            PickNewTarget();
        }

        dir.Normalize();
        Vector3 nextPos = transform.position + dir * speed * Time.fixedDeltaTime;

        // Clamp inside area
        nextPos.x = Mathf.Clamp(nextPos.x, areaCenter.x - areaHalfExtents.x, areaCenter.x + areaHalfExtents.x);
        nextPos.y = Mathf.Clamp(nextPos.y, areaCenter.y - areaHalfExtents.y, areaCenter.y + areaHalfExtents.y);

        rb.MovePosition(nextPos);
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
