//这个脚本负责让植物（树/草）发射气体粒子，模拟光合作用和呼吸作用的视觉效果。
using UnityEngine;

public class GasExchanger : MonoBehaviour
{
    public enum EntityType { Tree, Grass, Human, Animal, Ocean }
    
    [Header("Entity Type")]
    public EntityType entityType;
    
    [Header("Gas Exchange Rates (mol/day)")]
    [Tooltip("O₂ produced (+) or consumed (-) per day")]
    public float oxygenRate = 0f;
    
    [Tooltip("CO₂ consumed (-) or produced (+) per day")]
    public float co2Rate = 0f;
    
    [Header("Metabolism Scale")]
    [Tooltip("全局代谢速率缩放因子 (校准到与PlantAgent匹配的生态速率)")]
    public float metabolismScale = 0.00636f;  // 与PlantAgent保持一致的校准系数
    
    [Header("Day/Night Behavior")]
    [Tooltip("If true, only exchanges gas during daylight (photosynthesis)")]
    public bool onlyDuringDay = false;
    
    [Header("Status")]
    public bool isAlive = true;
    
    private SunMoonController timeController;
    
    void Start()
    {
        timeController = FindAnyObjectByType<SunMoonController>();
        
        // Set default rates based on entity type
        SetDefaultRates();
        
        // Register with AtmosphereManager
        if (AtmosphereManager.Instance != null)
        {
            AtmosphereManager.Instance.RegisterExchanger(this);
        }
    }
    
    void SetDefaultRates()
    {
        switch (entityType)
        {
            case EntityType.Tree:
                // Gross photosynthesis: +5.5 mol O₂/day (day only)
                // Respiration: -0.5 mol O₂/day (24/7)
                // Net daytime: +5.5 - 0.5 = +5.0 mol O₂/day
                oxygenRate = 5.5f;     // Gross photosynthesis
                co2Rate = -5.5f;        // Gross CO₂ consumption
                onlyDuringDay = true;
                break;
                
            case EntityType.Grass:
                // Gross photosynthesis: +1.1 mol O₂/day (day only)
                // Respiration: -0.1 mol O₂/day (24/7)
                // Net daytime: +1.1 - 0.1 = +1.0 mol O₂/day
                oxygenRate = 1.1f;     // Gross photosynthesis
                co2Rate = -1.1f;        // Gross CO₂ consumption
                onlyDuringDay = true;
                break;
                
            case EntityType.Human:
                oxygenRate = -25.0f;   // -25 mol O₂/day (respiration)
                co2Rate = 25.0f;        // +25 mol CO₂/day (exhaled)
                onlyDuringDay = false;  // Breathes 24/7
                break;
                
            case EntityType.Animal:
                oxygenRate = -2.5f;    // -2.5 mol O₂/day per animal
                co2Rate = 2.5f;         // +2.5 mol CO₂/day
                onlyDuringDay = false;
                break;
                
            case EntityType.Ocean:
                oxygenRate = 0f;        // Ocean doesn't produce O₂ in this model
                co2Rate = 0f;           // Ocean absorption handled by AtmosphereManager.oceanAbsorptionRate
                onlyDuringDay = false;
                break;
        }
    }
    
    /// <summary>
    /// Get the current O₂ exchange rate, accounting for day/night
    /// </summary>
    public float GetCurrentO2Rate()
    {
        if (!isAlive) return 0f;
        
        float rate = 0f;
        
        // Plants: photosynthesis during day + respiration 24/7
        if (entityType == EntityType.Tree || entityType == EntityType.Grass)
        {
            // Plant respiration (24/7)
            float respiration = entityType == EntityType.Tree ? -0.5f : -0.1f;
            
            var controller = FindAnyObjectByType<SunMoonController>();
            float photoEfficiency = 1f;

            if (controller != null)
            {
                photoEfficiency = controller.GetPhotosynthesisEfficiency();
            }
            float photosynthesis = oxygenRate * photoEfficiency;
            rate = photosynthesis + respiration; // Gross photosynthesis + respiration

            if (Random.value < 0.001f && entityType == EntityType.Tree)
            {
                Debug.Log($"[Tree] Time: {controller?.currentTimeOfDay}, " +
                            $"Efficiency: {photoEfficiency:F2}, " +
                            $"Photosynthesis: {photosynthesis:F1}, " +
                            $"Respiration: {respiration:F1}, " +
                            $"Net O₂ Rate: {rate:F1}");
            }
        }
        else
        {
            // Animals and humans: respiration 24/7 (apply metabolismScale for calibration)
            rate = oxygenRate * metabolismScale;
        }
        
        return rate;
    }
    
    /// <summary>
    /// Get the current CO₂ exchange rate, accounting for day/night
    /// </summary>
    public float GetCurrentCO2Rate()
    {
        if (!isAlive) return 0f;
        
        float rate = 0f;
        
        // Plants: photosynthesis during day + respiration 24/7
        if (entityType == EntityType.Tree || entityType == EntityType.Grass)
        {
            // Plant respiration produces CO₂ (24/7)
            float respirationCO2 = entityType == EntityType.Tree ? 0.5f : 0.1f;
            
            var controller = FindAnyObjectByType<SunMoonController>();
            float photoEfficiency = 1f;

            if (controller != null)
            {
                photoEfficiency = controller.GetPhotosynthesisEfficiency();
            }

            float photosynthesis = co2Rate * photoEfficiency;
            rate = photosynthesis + respirationCO2; // Gross CO₂ consumption + respiration

            // Debug log occasionally
            if (Random.value < 0.001f && entityType == EntityType.Tree)
            {
                Debug.Log($"[Tree CO₂] Time: {controller?.currentTimeOfDay}, " +
                        $"Efficiency: {photoEfficiency:F2}, " +
                        $"Photosynthesis: {photosynthesis:F1}, " +
                        $"Respiration: {respirationCO2:F1}, " +
                        $"Net CO₂: {rate:F1}");
            }
        }
        else
        {
            // Animals and humans: produce CO₂ 24/7 (apply metabolismScale for calibration)
            rate = co2Rate * metabolismScale;
        }
        
        return rate;
    }
    
    /// <summary>
    /// Kill this entity and release stored carbon
    /// </summary>
    public void Die()
    {
        if (!isAlive) return;
        
        isAlive = false;
        
        // When tree dies, release stored carbon as CO₂ spike
        if (entityType == EntityType.Tree && AtmosphereManager.Instance != null)
        {
            // Release 10x the tree's daily CO₂ consumption as a one-time spike
            float carbonRelease = Mathf.Abs(co2Rate) * 10f;
            AtmosphereManager.Instance.AddCO2Spike(carbonRelease);
            Debug.Log($"[GasExchanger] Tree died! Released {carbonRelease} mol CO₂");
        }
        
        // Unregister from atmosphere
        if (AtmosphereManager.Instance != null)
        {
            AtmosphereManager.Instance.UnregisterExchanger(this);
        }
    }
    
    void OnDestroy()
    {
        // Unregister when destroyed (but don't trigger death effects)
        if (AtmosphereManager.Instance != null && isAlive)
        {
            AtmosphereManager.Instance.UnregisterExchanger(this);
        }
    }
    
    // Visualize gas exchange in Scene view
    void OnDrawGizmosSelected()
    {
        // Green for oxygen producers, red for consumers
        Gizmos.color = oxygenRate > 0 ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
