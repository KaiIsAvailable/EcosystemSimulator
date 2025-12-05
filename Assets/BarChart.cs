using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BarChart : MonoBehaviour
{
    public RectTransform barContainer;
    public GameObject barPrefab;

    [Header("Tooltip UI")]
    public GameObject tooltipObject;
    public Text tooltipText;

    void Start()
    {
        GenerateBars(new List<float> { 3, 10, 5, 8, 2 });
    }

    public void GenerateBars(List<float> values)
    {
        foreach (Transform child in barContainer)
            Destroy(child.gameObject);

        float max = Mathf.Max(values.ToArray());

        foreach (float v in values)
        {
            GameObject bar = Instantiate(barPrefab, barContainer);
            RectTransform rt = bar.GetComponent<RectTransform>();

            float height = (v / max) * 250f;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);

            // Add tooltip component
            HoverTooltip tooltip = bar.AddComponent<HoverTooltip>();
            tooltip.message = $"Value: {v}";
            tooltip.tooltipObject = tooltipObject;
            tooltip.tooltipText = tooltipText;
        }
    }
}
