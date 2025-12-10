using UnityEngine;

public class PlantAgent : MonoBehaviour
{
    [Header("Plant Type")]
    public PlantType plantType = PlantType.Tree;

    [Header("Physiological Parameters")]
    [Tooltip("Base respiration rate at 20°C (mol/s per biomass unit).")]
    public float R_base = 0.00072f; 

    [Tooltip("Temperature sensitivity factor Q10")]
    public float Q10_factor = 2.5f;

    [Tooltip("Light compensation point (ratio of L_global_max)")]
    [Range(0f, 1f)]
    public float L_comp_ratio = 0.10f;

    [Tooltip("Light saturation point (ratio of L_global_max)")]
    [Range(0f, 1f)]
    public float L_sat_ratio = 0.90f;

    [Tooltip("Maximum photosynthetic efficiency (mol/s per biomass unit).")]
    public float P_max = 0.0108f; // CORRECTED RATE

    [Header("Metabolism Scale")]
    [Tooltip("Global metabolism rate scaling factor (calibrated to atmospheric mole scale)")]
    public float metabolismScale = 1.0f;

    [Header("Current State")]
    [Tooltip("Current biomass (energy reserve)")]
    public float biomass = 10.0f;

    [Tooltip("Maximum biomass before growth stops")] 
    public float maxBiomass = 500f; 

    [Tooltip("Vertical layer height(0=ground, 1=conopy)")]
    [Range(0f, 1f)]
    public float verticalLayerH = 0f;

    [Header("Environment (Runtime Calculated)")]
    [Tooltip("Local temperature at theis plant's position")]
    public float localTemperature = 20f;

    [Header("Local light intensity (0-1, calculated by shading system)")]
    [Range(0f, 1f)]
    public float localLight = 1.0f;

    [Header("Metabolism (Debug Info)")]
    [Tooltip("Total respiration cost (mol/s)")]
    public float R_total = 0f;

    [Tooltip("Gross photosynthesis production (mol/s)")]
    public float P_gross = 0f;

    // Accumulators are no longer strictly needed but kept as debug info
    private float o2Accumulator = 0f;
    private float co2Accumulator = 0f;
    private const float ACCUMULATOR_THRESHOLD = 0.001f;

    [Tooltip("Net energy accumulation (mol/s)")]
    public float P_net = 0f;

    [Header("Death Settings")]
    [Tooltip("Minimum biomass before death")]
    public float minBiomass = 1.0f;

    [Tooltip("Enable death when biomass depleted")]
    public bool enableDeath = true;

    [Header("Registration")]
    [Tooltip("Auto-register with AtmosphereManager")]
    public bool autoRegister = true;
    private SunMoonController sunMoon;
    private AtmosphereManager atmosphere;
    public enum PlantType {Tree, Grass}

    void Start()
    {
        sunMoon = FindObjectOfType<SunMoonController>();
        atmosphere = AtmosphereManager.Instance;

        if (atmosphere)
        {
            // Registering the agent allows AtmosphereManager to read P_net
            atmosphere.RegisterPlantAgent(this);
        }

        InitializeParameters();
    }

    void InitializeParameters()
    {
        // Max biomass cap setup
        if (plantType == PlantType.Tree)
        {
            verticalLayerH = 0.5f;
            maxBiomass = 500f; 
        }
        else // Grass
        {
            verticalLayerH = 0.0f;
            maxBiomass = 30f;
        }
    }

    void Update()
    {
        if (!sunMoon || !atmosphere) return;

        // --- Step 1: Calculate local temperature and respiration cost ---
        float globalTemp = sunMoon.currentTemperature;
        float verticalOffset = GetVerticalTemperatureOffset(verticalLayerH);
        localTemperature = globalTemp + verticalOffset;

        float deltaT = localTemperature - 20f;
        float C_R = Mathf.Pow(Q10_factor, deltaT / 10f);
        
        R_total = R_base * C_R * biomass;

        // --- Step 2: Calculate local light and gross photosynthesis ---
        float photoEfficiency = sunMoon.GetPhotosynthesisEfficiency();
        
        if (sunMoon.globalLight2D != null)
        {
            localLight = sunMoon.globalLight2D.intensity;
        }

        P_gross = P_max * photoEfficiency * biomass;

        // --- Step 3: Calculate net energy accumulation ---
        P_net = P_gross - R_total;

        // --- Step 4: Update biomass (Growth/Loss) ---
        biomass += P_net * Time.deltaTime;
        
        // Clamp biomass growth at the maximum limit
        biomass = Mathf.Clamp(biomass, minBiomass, maxBiomass);

        // Death condition
        if (enableDeath && biomass <= minBiomass)
        {
            Die();
        }

        // --- Step 5: Gas Exchange Accumulation (for debug/rate tracking) ---
        // VITAL: Application logic REMOVED. AtmosphereManager reads P_net directly.
        float o2Change = P_net * Time.deltaTime * metabolismScale;
        float co2Change = -P_net * Time.deltaTime * metabolismScale;

        o2Accumulator += o2Change;
        co2Accumulator += co2Change;
    }

    float GetVerticalTemperatureOffset(float height)
    {
        return height * 2f;
    }

    void Die()
    {
        Destroy(gameObject);
    }
    
    void OnDestroy()
    {
        if (atmosphere != null)
        {
            // VITAL FIX: REMOVE DIRECT MOLE MODIFICATION LOGIC HERE
            // The AtmosphereManager's central loop is responsible for applying all mole changes.
            // Leaving this commented out to show the removed code:
            /*
            if (o2Accumulator != 0f) atmosphere.oxygenMoles += o2Accumulator;
            if (co2Accumulator != 0f) atmosphere.carbonDioxideMoles += co2Accumulator;
            */
            
            atmosphere.UnregisterPlantAgent(this);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize biomass size
        Gizmos.color = biomass > 5f ? Color.green : 
                             biomass > 2f ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(biomass * 0.1f, 0.1f));

        // Visualize net growth direction
        if (P_net > 0.01f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.5f);
        }
        else if (P_net < -0.01f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 0.5f);
        }

        // Display stats
        #if UNITY_EDITOR
        // (Editor code omitted for brevity)
        #endif
    }
}