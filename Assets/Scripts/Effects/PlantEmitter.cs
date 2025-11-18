using UnityEngine;

public class PlantEmitter : MonoBehaviour
{
    [Header("Emission Settings")]
    public GameObject oxygenPrefab;
    public GameObject carbonDioxidePrefab;
    
    [Tooltip("Time between emissions (seconds)")]
    public float emissionInterval = 3f;
    
    [Tooltip("How far particles spawn from plant")]
    public float emissionRadius = 0.3f;
    
    [Tooltip("Upward velocity for particles")]
    public float emissionForce = 0.5f;

    private SunMoonController timeController;
    private float timeSinceLastEmit;
    private bool wasDay;

    void Start()
    {
        // Find the time controller in the scene
        timeController = FindAnyObjectByType<SunMoonController>();
        if (!timeController)
        {
            Debug.LogWarning($"[PlantEmitter] No SunMoonController found. {gameObject.name} won't emit particles.");
        }

        // Randomize start time so all plants don't emit at once
        timeSinceLastEmit = Random.Range(0f, emissionInterval);
    }

    void Update()
    {
        if (!timeController) return;
        if (!oxygenPrefab && !carbonDioxidePrefab) return;

        // Determine if it's day or night
        bool isDay = IsDayTime();

        // Track state change for optional effects
        if (isDay != wasDay)
        {
            wasDay = isDay;
            // Could add visual feedback here (e.g., change tint)
        }

        // Emit particles at intervals
        timeSinceLastEmit += Time.deltaTime;
        if (timeSinceLastEmit >= emissionInterval)
        {
            timeSinceLastEmit = 0f;
            EmitParticle(isDay);
        }
    }

    bool IsDayTime()
    {
        float sunriseH = timeController.sunriseHour + timeController.sunriseMin / 60f;
        float sunsetH = timeController.sunsetHour + timeController.sunsetMin / 60f;
        float clockH = timeController.time01 * 24f;

        return (clockH >= sunriseH) && (clockH < sunsetH);
    }

    void EmitParticle(bool isDay)
    {
        GameObject prefab = isDay ? oxygenPrefab : carbonDioxidePrefab;
        if (!prefab) return;

        // Random position around the plant
        Vector2 randomOffset = Random.insideUnitCircle * emissionRadius;
        Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

        // Instantiate particle
        GameObject particle = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Set the gas type
        GasParticle gasParticle = particle.GetComponent<GasParticle>();
        if (gasParticle)
        {
            gasParticle.gasType = isDay ? GasParticle.GasType.Oxygen : GasParticle.GasType.CarbonDioxide;
        }

        // Add upward movement (optional - requires Rigidbody2D on particle prefab)
        Rigidbody2D rb = particle.GetComponent<Rigidbody2D>();
        if (rb)
        {
            Vector2 upwardForce = new Vector2(
                Random.Range(-0.1f, 0.1f), 
                emissionForce
            );
            rb.linearVelocity = upwardForce;
        }

        // Auto-destroy particle after a few seconds
        Destroy(particle, 5f);
    }

    // Visualize emission radius in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, emissionRadius);
    }
}
