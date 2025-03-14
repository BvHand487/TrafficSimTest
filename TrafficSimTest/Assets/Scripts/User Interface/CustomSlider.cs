using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomSlider : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;

    private TextMeshProUGUI sliderValueComp = null;
    private Slider sliderComp;

    public float minValue => sliderComp.minValue;
    public float maxValue => sliderComp.maxValue;

    private void Awake()
    {
        sliderValueComp = transform.GetChild(transform.childCount - 1).GetComponent<TextMeshProUGUI>();
        sliderComp = GetComponent<Slider>();
    }

    private void Start()
    {
        UpdateSliderValue(sliderComp.value);
    }

    public void UpdateSliderValue(float value)
    {
        int roundedValue = (int) value;
        sliderValueComp.text = roundedValue.ToString("0");
        UIManager.Instance.UpdateSliderValues(title.text, roundedValue);
    }
}
