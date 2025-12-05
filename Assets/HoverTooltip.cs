using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string message;
    public GameObject tooltipObject;
    public Text tooltipText;

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        tooltipText.text = message;
        tooltipObject.SetActive(true);
    }

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
    {
        tooltipObject.SetActive(false);
    }
}
