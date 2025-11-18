using UnityEngine;
using UnityEngine.UI;

public class TimeSpeedUI : MonoBehaviour
{
    [Header("UI References")]
    public Text speedDisplayText;
    public Button speed1xButton;
    public Button speed2xButton;
    public Button speed4xButton;
    public Button speed8xButton;
    public Button speed12xButton;
    
    [Header("Button Colors (Optional)")]
    public Color normalColor = Color.white;
    public Color activeColor = Color.green;
    
    private TimeSpeedController timeController;
    
    void Start()
    {
        timeController = TimeSpeedController.Instance;
        
        if (!timeController)
        {
            Debug.LogError("[TimeSpeedUI] No TimeSpeedController found in scene!");
            return;
        }
        
        // Setup button listeners
        if (speed1xButton) speed1xButton.onClick.AddListener(() => SetSpeed(1));
        if (speed2xButton) speed2xButton.onClick.AddListener(() => SetSpeed(2));
        if (speed4xButton) speed4xButton.onClick.AddListener(() => SetSpeed(4));
        if (speed8xButton) speed8xButton.onClick.AddListener(() => SetSpeed(8));
        if (speed12xButton) speed12xButton.onClick.AddListener(() => SetSpeed(12));
    }
    
    void Update()
    {
        if (!timeController) return;
        
        // Update display text
        if (speedDisplayText)
        {
            speedDisplayText.text = $"Speed: {timeController.GetSpeedText()}";
        }
        
        // Highlight active button
        UpdateButtonColors();
    }
    
    void SetSpeed(int speed)
    {
        if (timeController)
        {
            timeController.SetSpeed(speed);
        }
    }
    
    void UpdateButtonColors()
    {
        if (!timeController) return;
        
        int currentSpeed = timeController.currentSpeed;
        
        // Update each button's color based on active state
        SetButtonColor(speed1xButton, currentSpeed == 1);
        SetButtonColor(speed2xButton, currentSpeed == 2);
        SetButtonColor(speed4xButton, currentSpeed == 4);
        SetButtonColor(speed8xButton, currentSpeed == 8);
        SetButtonColor(speed12xButton, currentSpeed == 12);
    }
    
    void SetButtonColor(Button button, bool isActive)
    {
        if (!button) return;
        
        ColorBlock colors = button.colors;
        colors.normalColor = isActive ? activeColor : normalColor;
        button.colors = colors;
    }
}
