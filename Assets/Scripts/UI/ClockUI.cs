using UnityEngine;
using UnityEngine.UI;

public class ClockUI : MonoBehaviour
{
    private Text txt;
    private SunMoonController ctrl;
    private bool warned;
    private float lastTime = -1f;

    void Awake()
    {
        txt = GetComponent<Text>();
        if (!txt) 
        {
            Debug.LogError("[ClockUI] ❌ No Text component!");
            return;
        }

        ctrl = FindAnyObjectByType<SunMoonController>();
        if (!ctrl)
        {
            Debug.LogWarning("[ClockUI] ⚠️ No SunMoonController found!");
        }
        else
        {
            Debug.Log($"[ClockUI] ✅ Found SunMoonController: fullDaySeconds={ctrl.fullDaySeconds}");
        }
    }

    void Update()
    {
        if (!txt) return;

        if (ctrl)
        {
            // 显示时间
            txt.text = $"Day {ctrl.day}\nTime {ctrl.hours:00}:{ctrl.minutes:00}\n({ctrl.currentTimeOfDay})";
            
            // ✅ 每秒检查一次
            if (Time.time - lastTime >= 1f)
            {
                lastTime = Time.time;
                
                Debug.Log($"[ClockUI] time01={ctrl.time01:F4}, hours={ctrl.hours:D2}:{ctrl.minutes:D2}, " +
                          $"timeOfDay={ctrl.currentTimeOfDay}, fullDaySeconds={ctrl.fullDaySeconds}");
                
                // ✅ 检查引用
                if (ctrl.sun == null)
                    Debug.LogWarning("[ClockUI] ⚠️ Sun is null! Drag Sun GameObject to SunMoonController.");
                if (ctrl.moon == null)
                    Debug.LogWarning("[ClockUI] ⚠️ Moon is null! Drag Moon GameObject to SunMoonController.");
                if (ctrl.globalLight2D == null)
                    Debug.LogWarning("[ClockUI] ⚠️ Global Light 2D is null! Drag Light to SunMoonController.");
            }
        }
        else
        {
            var now = System.DateTime.Now;
            txt.text = $"System\n{now.Hour:00}:{now.Minute:00}";
        }
    }
}
