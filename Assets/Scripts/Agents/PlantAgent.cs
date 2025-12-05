using UnityEngine;

public class PlantAgent : MonoBehaviour
{
    [Header("Plant Type")]
    public PlantType plantType = PlantType.Tree;

    [Header("Physiological Parameters")]
    [Tooltip("Base respiration rate at 20°C (mol/s per biomass unit)")]
    public float R_base = 0.0001f;  // 树木默认维持成本，草会在InitializeParameters中覆盖

    [Tooltip("Temperature sensitivity factor Q10")]
    public float Q10_factor = 2.5f;  // 树木默认温度敏感度

    [Tooltip("Light compensation point (ratio of L_global_max)")]
    [Range(0f, 1f)]
    public float L_comp_ratio = 0.10f;

    [Tooltip("Light saturation point (ratio of L_global_max)")]
    [Range(0f, 1f)]
    public float L_sat_ratio = 0.90f;

    [Tooltip("Maximum photosynthetic efficiency (mol/s per biomass unit)")]
    public float P_max = 0.0012f;  // 树木默认产出效率，草会在InitializeParameters中覆盖

    [Header("Metabolism Scale")]
    [Tooltip("Global metabolism rate scaling factor (calibrated to atmospheric mole scale)")]
    public float metabolismScale = 0.1f;  // Dramatic visible changes: day ~36 mol, night ~3 mol

    [Header("Current State")]
    [Tooltip("Current biomass (energy reserve)")]
    public float biomass = 10.0f;

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

    // Accumulators to fix float precision loss (small changes to large base values)
    private float o2Accumulator = 0f;
    private float co2Accumulator = 0f;
    private const float ACCUMULATOR_THRESHOLD = 0.01f;  // Apply when accumulated change >= 0.01 mol

    [Tooltip("Net energy accumulation (mol/s)")]
    public float P_net = 0f;

    [Header("Death Settings")]
    [Tooltip("Minimum biomass before death")]
    public float minBiomass = 1.0f;  // Die when biomass drops to 1kg or below

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

        if (!sunMoon)
        {
            //Debug.LogError("[PlantAgent] No SunMoonController found in scene!");
        }

        if (!atmosphere)
        {
            //Debug.LogError("[PlantAgent] No AtmosphereManager found in scene!");
        }
        else
        {
            // 注册到AtmosphereManager用于统计
            atmosphere.RegisterPlantAgent(this);
        }

        InitializeParameters();
    }

    void InitializeParameters()
    {
        if (plantType == PlantType.Tree)
        {
            // Sun-loving species: high metabolism, needs strong light
            // 真实校准参数（方案D最终版）
            R_base = 0.0001f;       // 基础呼吸速率 (树木维持成本高)
            Q10_factor = 2.5f;      // 温度敏感系数（树木更敏感）
            L_comp_ratio = 0.10f;   // 光补偿点 10%
            L_sat_ratio = 0.90f;    // 光饱和点 90%
            P_max = 0.0012f;        // 最大光合速率 (树木产出效率高)
            verticalLayerH = 0.5f;  // 冠层位置
        }
        else  // Grass
        {
            // Shade-tolerant species: low metabolism, efficient in low light
            // 真实校准参数（方案D最终版）
            R_base = 0.00005f;      // 基础呼吸速率 (草维持成本低，是树的一半)
            Q10_factor = 2.0f;      // 温度敏感系数（草更耐受）
            L_comp_ratio = 0.01f;   // 光补偿点 1%（耐荫）
            L_sat_ratio = 0.15f;    // 光饱和点 15%（低光饱和）
            P_max = 0.0008f;        // 最大光合速率 (草产出效率低)
            verticalLayerH = 0.0f;  // 地面层
        }
    }

    void Update()
    {
        if (!sunMoon || !atmosphere) return;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Step 1: Calculate local temperature and respiration cost
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        float globalTemp = sunMoon.currentTemperature;
        float verticalOffset = GetVerticalTemperatureOffset(verticalLayerH);
        localTemperature = globalTemp + verticalOffset;

        // Temperature correction factor using Q10 model
        float deltaT = localTemperature - 20f;  // T_ref = 20°C
        float C_R = Mathf.Pow(Q10_factor, deltaT / 10f);
        
        // Total respiration scales with biomass
        R_total = R_base * C_R * biomass;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Step 2: Calculate local light and gross photosynthesis
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        
        // 使用SunMoonController的光合作用效率系统
        // GetPhotosynthesisEfficiency() 已经处理了昼夜循环和光强变化
        float photoEfficiency = sunMoon.GetPhotosynthesisEfficiency();  // 0-1
        
        // 获取当前光强用于显示（TODO: 未来用遮蔽系统替代）
        if (sunMoon.globalLight2D != null)
        {
            localLight = sunMoon.globalLight2D.intensity;
        }

        // 光合作用速率 = 最大速率 × 光合效率 × 生物量
        // photoEfficiency在夜晚=0，正午=1，其他时间根据时段变化
        P_gross = P_max * photoEfficiency * biomass;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Step 3: Calculate net energy accumulation
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        P_net = P_gross - R_total;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Step 4: Update biomass
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        biomass += P_net * Time.deltaTime;

        // Death condition
        if (enableDeath && biomass <= minBiomass)
        {
            Die();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Step 5: Update atmosphere (gas exchange)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Positive P_net → produce O₂, consume CO₂
        // Negative P_net → consume O₂, produce CO₂
        
        // Apply metabolismScale calibration factor to real ecological rates
        float o2Change = P_net * Time.deltaTime * metabolismScale;
        float co2Change = -P_net * Time.deltaTime * metabolismScale;

        // Accumulate changes to avoid float precision loss
        // (adding 0.000007 to 209500 is below float precision)
        o2Accumulator += o2Change;
        co2Accumulator += co2Change;

        // Apply accumulated changes when threshold is reached
        if (Mathf.Abs(o2Accumulator) >= ACCUMULATOR_THRESHOLD)
        {
            atmosphere.oxygenMoles += o2Accumulator;
            if (Time.frameCount % 60 == 0)
            {
                //Debug.Log($"[O₂ Applied] {o2Accumulator:F4} mol | New O₂: {atmosphere.oxygenMoles:F2} mol");
            }
            o2Accumulator = 0f;
        }
        
        if (Mathf.Abs(co2Accumulator) >= ACCUMULATOR_THRESHOLD)
        {
            atmosphere.carbonDioxideMoles += co2Accumulator;
            co2Accumulator = 0f;
        }
        
        // Debug log: output every ~2 seconds with more details
        if (plantType == PlantType.Tree && Time.frameCount % 120 == 0)
        {
            string nightMarker = sunMoon.GetPhotosynthesisEfficiency() == 0f ? "[NIGHT]" : "";
            //Debug.Log($"[PlantAgent {nightMarker}] {sunMoon.currentTimeOfDay} {sunMoon.hours:00}:{sunMoon.minutes:00} | " +
            //          $"Temp={localTemperature:F1}°C | PhotoEff={sunMoon.GetPhotosynthesisEfficiency():F3} | " +
            //          $"P_net={P_net:F8} | metabolismScale={metabolismScale:F6} | " +
            //          $"Time.deltaTime={Time.deltaTime:F6} | " +
            //          $"O2Δ={o2Change:F10} mol/frame | CO2Δ={co2Change:F10} mol/frame | " +
            //          $"Atmosphere CO2={atmosphere.carbonDioxideMoles:F1} mol");
        }

        atmosphere.oxygenMoles = Mathf.Max(0f, atmosphere.oxygenMoles);
        atmosphere.carbonDioxideMoles = Mathf.Max(0f, atmosphere.carbonDioxideMoles);
    }

    float GetVerticalTemperatureOffset(float height)
    {
        // Ground layer (0): +0°C
        // Canopy layer (1): +2°C
        return height * 2f;
    }

    void Die()
    {
        //Debug.Log($"[PlantAgent] {gameObject.name} died due to energy depletion (biomass: {biomass:F2})");
        
        // TODO: Play death animation
        // TODO: Drop resources
        // TODO: Notify ecosystem manager
        
        Destroy(gameObject);
    }
    
    void OnDestroy()
    {
        // Apply any remaining accumulated changes before destruction
        if (atmosphere != null)
        {
            if (o2Accumulator != 0f)
            {
                atmosphere.oxygenMoles += o2Accumulator;
                //Debug.Log($"[Plant Destroyed] Applied remaining O₂: {o2Accumulator:F6} mol");
            }
            if (co2Accumulator != 0f)
            {
                atmosphere.carbonDioxideMoles += co2Accumulator;
            }
            
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
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            $"Biomass: {biomass:F1}\n" +
            $"P_net: {P_net:F3}\n" +
            $"Light: {localLight:F2}"
        );
        #endif
    }
}
