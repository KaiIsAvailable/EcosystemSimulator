using UnityEngine;

public class GasParticle : MonoBehaviour
{
    [Header("Gas Type")]
    public GasType gasType = GasType.Oxygen;

    [Header("Movement")]
    public float floatSpeed = 0.5f;
    public float wobbleAmount = 0.2f;
    public float wobbleSpeed = 2f;

    [Header("Fade Out")]
    public float fadeStartTime = 3f;
    public float fadeDuration = 2f;

    private SpriteRenderer spriteRenderer;
    private float spawnTime;
    private Vector3 startPos;
    private float wobbleOffset;
    private bool absorbed = false;

    public enum GasType
    {
        Oxygen,
        CarbonDioxide
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spawnTime = Time.time;
        startPos = transform.position;
        wobbleOffset = Random.Range(0f, 100f); // Random phase offset
    }

    void Update()
    {
        // Float upward with wobble
        float wobble = Mathf.Sin((Time.time + wobbleOffset) * wobbleSpeed) * wobbleAmount;
        transform.position += new Vector3(wobble * Time.deltaTime, floatSpeed * Time.deltaTime, 0f);

        // Fade out after a while
        if (spriteRenderer && Time.time > spawnTime + fadeStartTime)
        {
            float fadeProgress = (Time.time - (spawnTime + fadeStartTime)) / fadeDuration;
            Color c = spriteRenderer.color;
            c.a = Mathf.Lerp(1f, 0f, fadeProgress);
            spriteRenderer.color = c;

            // When fully faded, absorb into atmosphere
            if (fadeProgress >= 1f && !absorbed)
            {
                AbsorbIntoAtmosphere();
            }
        }
    }

    void AbsorbIntoAtmosphere()
    {
        absorbed = true;

        // Add this gas to the atmosphere manager
        if (AtmosphereManager.Instance != null)
        {
            if (gasType == GasType.Oxygen)
            {
                AtmosphereManager.Instance.AddOxygen();
            }
            else if (gasType == GasType.CarbonDioxide)
            {
                AtmosphereManager.Instance.AddCarbonDioxide();
            }
        }
    }
}
