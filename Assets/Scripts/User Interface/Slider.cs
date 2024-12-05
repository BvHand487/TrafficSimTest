using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Slider : MonoBehaviour
{
    [SerializeField] private MainMenu menu;
    [SerializeField] private TextMeshProUGUI title;

    private TextMeshProUGUI sliderValueComp = null;

    private void Start()
    {
        sliderValueComp = transform.GetChild(transform.childCount - 1).GetComponent<TextMeshProUGUI>();
    }

    public void UpdateSliderValue(float value)
    {
        int roundedValue = (int) value;
        sliderValueComp.text = roundedValue.ToString("0");
        menu.UpdateSliderValues(title.text, roundedValue);
    }
}
