using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class CustomSlider : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI title;

        private TextMeshProUGUI sliderValueComp = null;
        private Slider sliderComp;
        
        [SerializeField] private Image background;
        [SerializeField] private Image fillArea;
        [SerializeField] private Image handle;
        [SerializeField] public Color disabledBackgroundColor;
        [SerializeField] public Color disabledFillAreaColor;
        [SerializeField] public Color disabledHandleColor;
        private Color enabledBackgroundColor;
        private Color enabledFillAreaColor;
        private Color enabledHandleColor;

        public float minValue => sliderComp.minValue;
        public float maxValue => sliderComp.maxValue;

        private void Awake()
        {
            sliderValueComp = transform.GetChild(transform.childCount - 1).GetComponent<TextMeshProUGUI>();
            sliderComp = GetComponent<Slider>();
        }

        private void Start()
        {
            enabledBackgroundColor = background.color;
            enabledFillAreaColor = fillArea.color;
            enabledHandleColor = handle.color;
            sliderComp.interactable = true;
            
            UpdateSliderValue(sliderComp.value);
        }

        public void Enable()
        {
            sliderComp.enabled = true;
            background.color = enabledBackgroundColor;
            fillArea.color = enabledFillAreaColor;
            handle.color = enabledHandleColor;
        }

        public void Disable()
        {
            sliderComp.enabled = false;
            background.color = disabledBackgroundColor;
            fillArea.color = disabledFillAreaColor;
            handle.color = disabledHandleColor;
        }
        
        public void UpdateSliderValue(float value)
        {
            int roundedValue = (int) value;
            sliderValueComp.text = roundedValue.ToString("0");
            UIManager.Instance.UpdateSliderValues(this, roundedValue);
        }
    }
}
