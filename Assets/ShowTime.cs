using UnityEngine;
using UnityEngine.UI;

public class ClockUI : MonoBehaviour
{
    private Text txt;
    private SunMoonController ctrl;
    private bool warned;

    void Awake()
    {
        txt = GetComponent<Text>();
        if (!txt) Debug.LogError("[ClockUI] No Text component on this object.");

        // auto-find the controller in scene
        ctrl = FindAnyObjectByType<SunMoonController>();
        if (!ctrl && !warned)
        {
            warned = true;
            Debug.LogWarning("[ClockUI] No SunMoonController found. Showing real time instead.");
        }
    }

    void Update()
    {
        if (!txt) return;

        if (ctrl)
        {
            txt.text = $"Day {ctrl.day} \nTime {ctrl.hours:00}:{ctrl.minutes:00}";
        }
        else
        {
            // Fallback to system time if controller missing
            var now = System.DateTime.Now;
            txt.text = $"{now.Hour:00}:{now.Minute:00}";
        }
    }
}
