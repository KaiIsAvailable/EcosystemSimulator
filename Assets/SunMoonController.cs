using UnityEngine;

public class SunMoonController : MonoBehaviour
{
    [Header("References (set by WorldLogic)")]
    public Transform sun;     // instance transform
    public Transform moon;    // instance transform

    [Header("Optional Lights (fade by time)")]
    public Light sunLight;
    public Light moonLight;
    [Tooltip("Sun max intensity at local noon; moon at local midnight.")]
    public float sunMaxIntensity = 1.0f;
    public float moonMaxIntensity = 0.6f;
    public float lightLerpSpeed = 2f;

    [Header("Cycle")]
    [Tooltip("Seconds for a full 24h cycle (day+night).")]
    public float fullDaySeconds = 120f;

    [Header("Sunrise / Sunset (local time)")]
    [Range(0,23)] public int sunriseHour = 6;
    [Range(0,59)] public int sunriseMin  = 58; // 06:58
    [Range(0,23)] public int sunsetHour  = 19;
    [Range(0,59)] public int sunsetMin   = 2;  // 19:02

    [Header("Path settings (relative to camera top)")]
    [Tooltip("Keep sun/moon this far away from the very top edge (world units).")]
    public float topMargin = 1f;
    [Tooltip("How high the arc bulges at noon/midnight (world units).")]
    public float arcHeight = 1.5f;
    [Tooltip("Keep some space from left/right edges (world units).")]
    public float horizontalPadding = 0.5f;
    [Tooltip("Extra distance beyond the visible bounds so bodies start off-screen.")]
    public float offScreenMargin = 1.3f;

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

    void Awake()
    {
        RecomputeDayNight();
    }
    
    void Start()
    {
        // Initialize time to sunrise (6:58 AM) so simulation starts at daytime
        float startTimeHours = sunriseHour + sunriseMin / 60f;  // 6.9667 hours (6:58 AM)
        time01 = startTimeHours / 24f;  // Convert to 0..1 range
        
        hours = sunriseHour;
        minutes = sunriseMin;
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

        bool isDay = (clockH >= sunriseH) && (clockH < sunsetH);
        
        // Debug log (only occasionally to avoid spam)
        if (Random.value < 0.005f)
        {
            //Debug.Log($"[SunMoon] Time: {hours:00}:{minutes:00} (clockH={clockH:F2}), sunriseH={sunriseH:F2}, sunsetH={sunsetH:F2}, isDay={isDay}");
        }

        if (sun)  sun.gameObject.SetActive(isDay);
        if (moon) moon.gameObject.SetActive(!isDay);

        if (isDay)
        {
            // Map [sunrise..sunset) -> t∈[0..1] across the sky (sun)
            float t = Mathf.InverseLerp(sunriseH, sunsetH, clockH);
            Vector3 pos = GetArcPosition(t);
            if (sun) sun.position = pos;

            // Optional light fading: noon brightest
            if (sunLight)
            {
                float noonCurve = 1f - Mathf.Abs(t - 0.5f) * 2f; // 0..1..0
                float target = Mathf.Lerp(0.05f, sunMaxIntensity, noonCurve);
                sunLight.intensity = Mathf.Lerp(sunLight.intensity, target, Time.deltaTime * lightLerpSpeed);
            }
            if (moonLight)
            {
                moonLight.intensity = Mathf.Lerp(moonLight.intensity, 0f, Time.deltaTime * lightLerpSpeed);
            }
        }
        else
        {
            // Night has two segments: [sunset..24) and [0..sunrise)
            float nightClock;
            if (clockH >= sunsetH)
                nightClock = clockH - sunsetH;      // after sunset to midnight
            else
                nightClock = (24f - sunsetH) + clockH; // after midnight to sunrise

            float t = Mathf.Clamp01(nightClock / nightLenH); // 0..1 across the sky (moon)
            Vector3 pos = GetArcPosition(t);
            if (moon) moon.position = pos;

            // Optional light fading: midnight brightest
            if (moonLight)
            {
                float midnightCurve = 1f - Mathf.Abs(t - 0.5f) * 2f; // 0..1..0
                float target = Mathf.Lerp(0.05f, moonMaxIntensity, midnightCurve);
                moonLight.intensity = Mathf.Lerp(moonLight.intensity, target, Time.deltaTime * lightLerpSpeed);
            }
            if (sunLight)
            {
                sunLight.intensity = Mathf.Lerp(sunLight.intensity, 0f, Time.deltaTime * lightLerpSpeed);
            }
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
