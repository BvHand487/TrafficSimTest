using System;
using ML;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class CustomCheckmark : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI title;

        public bool isToggled = false;

        public GameObject checkmarkObject;
        
        [SerializeField] private Image background;
        [SerializeField] public Color disabledColor;
        private Color enabledColor;
        private bool isEnabled = true;

        private void Awake()
        {
            title = GetComponentInChildren<TextMeshProUGUI>();

            checkmarkObject = transform.GetChild(0).GetChild(0).gameObject;
        }

        private void Start()
        {
            enabledColor = background.color;
            isEnabled = true;
            
            UIManager.Instance.UpdateCheckmarkValues(this, isToggled);
        }

        public void Enable()
        {
            isEnabled = true;
            background.color = enabledColor;
        }

        public void Disable()
        {
            isEnabled = false;
            background.color = disabledColor;
        }
        
        public void SwapState()
        {
            if (isEnabled == true)
            {
                isToggled = !isToggled;
                checkmarkObject.SetActive(isToggled);
                UIManager.Instance.UpdateCheckmarkValues(this, isToggled);
            }
        }
    }
}
