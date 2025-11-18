using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SunMoonController : MonoBehaviour
{
    [Header("References (set by WorldLogic)")]
    public Transform sun;     // instance transform
    public Transform moon;    // instance transform

    [Header("2D Lighting (UPR)")]
    public Light2D globalLight2D;

    [Tooltip("Sun max intensity at local noon; moon at local midnight.")]
    public float sunMaxIntensity = 1.0f;
    public float moonMaxIntensity = 0.6f;
    public float lightLerpSpeed = 2f;

    [Header("Cycle")]
    [Tooltip("Seconds for a full 24h cycle (day+night).")]
    public float fullDaySeconds = 120f;

    [Header("Sunrise / Sunset (local time)")]
    [Range(0,23)] public int dawnHour = 5;
    [Range(0,59)] public int dawnMin  = 30; // 0530
    [Range(0,23)] public int sunriseHour = 6;
    [Range(0,59)] public int sunriseMin  = 0; // 0600
    [Range(0,23)] public int sunsetHour  = 18;
    [Range(0,59)] public int sunsetMin   = 0;  // 1800
    [Range(0,23)] public int duskHour = 19;
    [Range(0,59)] public int duskMin  = 0; // 1900

    [Header("Path settings (relative to camera top)")]
    [Tooltip("Keep sun/moon this far away from the very top edge (world units).")]
    public float topMargin = 1f;
    [Tooltip("How high the arc bulges at noon/midnight (world units).")]
    public float arcHeight = 1.5f;
    [Tooltip("Keep some space from left/right edges (world units).")]
    public float horizontalPadding = 0.5f;
    [Tooltip("Extra distance beyond the visible bounds so bodies start off-screen.")]
    public float offScreenMargin = 1.3f;

    // Temperature
    [Header("Temperature Settings")]
    [Tooltip("Minimum temperature at dawn (°C).")]
    public float minTemperature = 21f;  // 热带雨林夜间最低温（潮湿环境）

    [Tooltip("Maximum temperature at noon (°C).")]
    public float maxTemperature = 34f;  // 热带雨林日间最高温（接近上限）

    [Tooltip("Hour when  temperature peaks (24 h  format).")]
    [Range(12f, 18f)]
    public float temperaturePeakHour = 15f;  // 下午3点温度峰值（滞后于正午）

    [Tooltip("Hour when temperature is lowest (24 h format).")]
    [Range(3f, 7f)]
    public float temperatureMinHour = 5.5f;

    [Tooltip("Current global temperature (°C).")]
    public float currentTemperature = 24f;

    // Bounds (set by WorldLogic)
    [HideInInspector] public Vector3 areaCenter;
    [HideInInspector] public Vector2 areaHalfExtents;

    // Time state
    [Tooltip("0..1 over a full 24h cycle")]
    public float time01;  // 0..1 over a full 24h cycle
    public int hours;
    public int minutes;
    public int day = 0;

    float sunriseH;
    float sunsetH;
    float dayLenH;
    float nightLenH;

    public enum TimeOfDay
    {
        Night,      //1900-0530
        Dawn,       //0530-0600
        Morning,    //0600-1200
        Noon,       //1200
        Afternoon,  //1200-1800
        Sunset,     //1800
        Dusk,       //1800-1900
    }

    public TimeOfDay currentTimeOfDay;

    void Awake()
    {
        RecomputeDayNight();
    }
    
    void Start()
    {
        // Initialize time to sunrise (6:58 AM) so simulation starts at daytime
        float startTimeHours = sunriseHour + sunriseMin / 60f;  // 6.9667 hours (6:58 AM)
        time01 = startTimeHours / 24f;  // Convert to 0..1 range

        day = 0;
        
        //Debug.Log($"[SunMoon] Simulation started at {hours:00}:{minutes:00} (sunrise, daytime)");
    }

    void OnValidate()
    {
        RecomputeDayNight();
    }

    void RecomputeDayNight()
    {
        sunriseH = sunriseHour + sunriseMin / 60f;
        sunsetH  = sunsetHour  + sunsetMin  / 60f;

        // handle (very unlikely here) if sunrise occurs after sunset due to input
        if (sunsetH <= sunriseH)
        {
            // force a minimal gap
            sunsetH = Mathf.Min(sunriseH + 0.1f, 23.999f);
        }

        dayLenH   = sunsetH - sunriseH;
        nightLenH = 24f - dayLenH;
    }

    void Update()
    {
        if (fullDaySeconds <= 0f) return;

        // Advance time (0..1)
        time01 += Time.deltaTime / fullDaySeconds;
        if (time01 >= 1f)
        {
            time01 -= 1f;
            day++;
        }
        // Convert to 24h clock
        float clockH = time01 * 24f;
        hours = Mathf.FloorToInt(clockH);
        minutes = Mathf.FloorToInt((clockH - hours) * 60f);
        
        float dawnH = dawnHour + dawnMin / 60f;
        float sunriseH = sunriseHour + sunriseMin / 60f;
        float noonH = 12.0f;
        float sunsetH = sunsetHour + sunsetMin / 60f;
        float duskH = duskHour + duskMin / 60f;

        if (clockH >= duskH || clockH < dawnH)
        {
            currentTimeOfDay = TimeOfDay.Night;
        }
        else if (clockH >= dawnH && clockH < sunriseH)
        {
            currentTimeOfDay = TimeOfDay.Dawn;
        }
        else if (clockH >= sunriseH && clockH < noonH)
        {
            currentTimeOfDay = TimeOfDay.Morning;
        }
        else if (Mathf.Abs(clockH - noonH) < 0.1f)
        {
            currentTimeOfDay = TimeOfDay.Noon;
        }
        else if (clockH >= noonH && clockH < sunsetH)
        {
            currentTimeOfDay = TimeOfDay.Afternoon;
        }
        else if (Mathf.Abs(clockH - sunsetH) < 0.1f)
        {
            currentTimeOfDay = TimeOfDay.Sunset;
        }
        else
        {
            currentTimeOfDay = TimeOfDay.Dusk;
        }

        currentTemperature = CalculateTemperature(clockH);

        UpdateSunMoonPosition(clockH);
        UpdateLighting();
    }

    void UpdateSunMoonPosition(float clockH)
    {
        float sunriseH = sunriseHour + sunriseMin / 60f;
        float sunsetH  = sunsetHour  + sunsetMin  / 60f;

        if (clockH >= sunriseH && clockH < sunsetH)
        {
            if (sun != null)
            {
                float dayProgress = (clockH - sunriseH) / (sunsetH - sunriseH); // 0..1
                sun.position = GetArcPosition(dayProgress);
                sun.gameObject.SetActive(true);
            }
            if (moon != null)
            {
                moon.gameObject.SetActive(false);   
            }
        }
        else
        {
            if (moon != null)
            {
                float nightStart = sunsetH;
                float nightEnd = sunriseH + 24f; // next day
                
                float currentNightTime = clockH < sunsetH ? clockH + 24f : clockH; // adjust for after midnight
                float nightProgress = (currentNightTime - nightStart) / (nightEnd - nightStart); // 0..1

                moon.position = GetArcPosition(nightProgress);
                moon.gameObject.SetActive(true);
            }
            if (sun != null)
            {
                sun.gameObject.SetActive(false);
            }
        }
    }

    float CalculateTemperature(float clockH)
    {
        // 使用非对称温度曲线：快速升温 (5:30-15:00)，缓慢降温 (15:00-19:00)，夜晚稳定 (19:00-5:30)
        
        // Night: 7:00 PM (19:00) → 5:30 AM (稳定低温)
        if (clockH >= 19f || clockH < temperatureMinHour)
        {
            return minTemperature;  // 夜晚保持最低温度 21°C
        }
        // Dawn to Peak: 5:30 AM → 3:00 PM (快速升温)
        else if (clockH >= temperatureMinHour && clockH <= temperaturePeakHour)
        {
            float progress = (clockH - temperatureMinHour) / (temperaturePeakHour - temperatureMinHour);
            // 使用正弦曲线加速升温
            float curve = Mathf.Sin(progress * Mathf.PI * 0.5f);  // 0→1 (加速)
            return Mathf.Lerp(minTemperature, maxTemperature, curve);
        }
        // Peak to Evening: 3:00 PM → 7:00 PM (快速降温到夜间温度)
        else // clockH > temperaturePeakHour && clockH < 19f
        {
            float progress = (clockH - temperaturePeakHour) / (19f - temperaturePeakHour);
            // 使用余弦曲线快速降温
            float curve = Mathf.Cos(progress * Mathf.PI * 0.5f);  // 1→0 (快速)
            return Mathf.Lerp(minTemperature, maxTemperature, curve);
        }
    }

    public float GetRespirationMultiplier()
    {
        float baseTemp = 20f;
        float Q10 = 2.0f;

        float tempDiff = currentTemperature - baseTemp;
        float multiplier = Mathf.Pow(Q10, tempDiff / 10f);
        return multiplier;
    }

    void UpdateLighting()
    {
        float targetIntensity = GetLightIntensity();
        Color ambientColor = GetAmbientColor();

        if (globalLight2D != null) 
        {
            globalLight2D.intensity = Mathf.Lerp(
                globalLight2D.intensity, 
                targetIntensity, 
                Time.deltaTime * lightLerpSpeed
            );
            globalLight2D.color = GetAmbientColor();
        }
    }

    float GetLightIntensity()
    {
        switch(currentTimeOfDay)
        {
            case TimeOfDay.Night:
                return moonMaxIntensity;
            case TimeOfDay.Dawn:
                float dawnH = dawnHour + dawnMin / 60f;
                float sunriseH = sunriseHour + sunriseMin / 60f;
                float clockH = time01 * 24f;
                float dawnProgress = Mathf.InverseLerp(dawnH, sunriseH, clockH);
                return Mathf.Lerp(moonMaxIntensity, sunMaxIntensity * 0.5f, dawnProgress);
            case TimeOfDay.Morning:
                float morningProgress = Mathf.InverseLerp(6f, 12f, time01 * 24f);
                return Mathf.Lerp(sunMaxIntensity * 0.5f, sunMaxIntensity, morningProgress);
            case TimeOfDay.Noon:
                return sunMaxIntensity;
            case TimeOfDay.Afternoon:
                float  afternoonProgress = Mathf.InverseLerp(12f, 18f, time01 * 24f);
                return Mathf.Lerp(sunMaxIntensity, sunMaxIntensity * 0.5f, afternoonProgress);
            case TimeOfDay.Sunset:
                return sunMaxIntensity * 0.3f;
            case TimeOfDay.Dusk:
                float duskProgress = Mathf.InverseLerp(18f, 19f, time01 * 24f);
                return Mathf.Lerp(sunMaxIntensity * 0.3f, moonMaxIntensity, duskProgress);
            default:
                return moonMaxIntensity;
        }
    }

    Color GetAmbientColor()
    {
        switch(currentTimeOfDay)
        {
            case TimeOfDay.Night:
                return new Color(0.1f, 0.1f, 0.2f);
            case TimeOfDay.Dawn:
                return new Color(0.9f, 0.6f, 0.4f);
            case TimeOfDay.Morning:
                return new Color(1f, 1f, 0.9f);
            case TimeOfDay.Noon:
                return new Color(1f, 1f, 1f);
            case TimeOfDay.Afternoon:
                return new Color(1f, 0.95f, 0.85f);
            case TimeOfDay.Sunset:
                return new Color(1f, 0.7f, 0.5f);
            case TimeOfDay.Dusk:
                return new Color(0.4f, 0.3f, 0.5f);
            default:
                return Color.white;
        }
    }

    public bool CanPhotosynthesize()
    {
        return currentTimeOfDay == TimeOfDay.Morning || 
               currentTimeOfDay == TimeOfDay.Noon || 
               currentTimeOfDay == TimeOfDay.Afternoon;
    }

    public float GetPhotosynthesisEfficiency()
    {
        switch(currentTimeOfDay)
        {
            case TimeOfDay.Night:
                return 0f;
            case TimeOfDay.Dawn:
                float dawnProgress = Mathf.InverseLerp(5.5f, 6f, time01 * 24f);
                return Mathf.Lerp(0f, 0.5f, dawnProgress);
            case TimeOfDay.Morning:
                float morningProgress = Mathf.InverseLerp(6f, 12f, time01 * 24f); // 0→1
                return Mathf.Lerp(0.5f, 1f, morningProgress); 
            case TimeOfDay.Noon:
                return 1f;
            case TimeOfDay.Afternoon:
                float afternoonProgress = Mathf.InverseLerp(12f, 18f, time01 * 24f);
                return Mathf.Lerp(1f, 0.5f, afternoonProgress);
            case TimeOfDay.Sunset:
                return 0.3f;
            case TimeOfDay.Dusk:
                float duskProgress = Mathf.InverseLerp(18f, 19f, time01 * 24f);
                return Mathf.Lerp(0.3f, 0f, duskProgress);  
            default:
                return 0f;
        }
    }

    // Calculate a nice arc near the top of the visible area from left→right
    Vector3 GetArcPosition(float t) // t in [0..1]
    {
        float left  = areaCenter.x - areaHalfExtents.x - offScreenMargin;
        float right = areaCenter.x + areaHalfExtents.x + offScreenMargin;
        float x = Mathf.Lerp(left, right, t);

        float topBandBase = areaCenter.y + areaHalfExtents.y - topMargin; // near top
        float arc = Mathf.Sin(t * Mathf.PI); // 0→1→0
        float y = topBandBase + arcHeight * arc; // peak at t=0.5

        return new Vector3(x, y, 0f);
    }

    // Optional: show the path in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        int steps = 24;
        for (int i = 0; i < steps; i++)
        {
            float t0 = i / (float)steps;
            float t1 = (i + 1) / (float)steps;
            Vector3 p0 = GetArcPositionSafe(t0);
            Vector3 p1 = GetArcPositionSafe(t1);
            Gizmos.DrawLine(p0, p1);
        }
    }

    // Safe version for gizmos before bounds are injected
    Vector3 GetArcPositionSafe(float t)
    {
        if (areaHalfExtents == Vector2.zero)
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.zero;

            float h = cam.orthographicSize * 2f;
            float w = h * cam.aspect;
            areaCenter = Vector3.zero;
            areaHalfExtents = new Vector2(w * 0.5f, h * 0.5f);
        }
        return GetArcPosition(t);
    }
}
