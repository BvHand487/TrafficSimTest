using System.Collections.Generic;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TimeScaleButtons : MonoBehaviour
    {
        [SerializeField] private Color normalColor;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Color disabledColor;

        private static Clock clock;
        private List<TimeScaleButton> buttons = new List<TimeScaleButton>();
        private int currentButtonIndex = 0;

        private void Start()
        {
            clock = Clock.Instance;

            TimeScaleButton.normalColor = normalColor;
            TimeScaleButton.selectedColor = selectedColor;
            TimeScaleButton.disabledColor = disabledColor;

            foreach (Button button in GetComponentsInChildren<Button>())
            {
                buttons.Add(new TimeScaleButton(button));

                button.onClick.AddListener(() =>
                {
                    buttons[currentButtonIndex].SetSelected(false);
                    currentButtonIndex = buttons.FindIndex(tsb => tsb.button == button);
                    buttons[currentButtonIndex].SetSelected(true);
                });
            }

            currentButtonIndex = buttons.FindIndex(tsb => tsb.timeScale == clock.timeScale);
            buttons[currentButtonIndex].SetSelected(true);
        
            // Applications running in the editor don't support timeScales > 100, so make a check and disable buttons if needed.
            foreach (TimeScaleButton button in buttons)
                if (button.timeScale > 100 && Application.isEditor)
                    button.SetDisabled(true);
        }

        private void Update()
        {
            // speed up time
            if (Input.GetKeyDown(KeyCode.Period))
            {
                if (currentButtonIndex < buttons.Count - 1)
                {
                    buttons[currentButtonIndex].SetSelected(false);

                    // find first next button which is enabled, aka button.interactible is true
                    int nextButtonIndex = buttons.FindIndex(currentButtonIndex + 1, tsb => tsb.button.interactable);
                    if (nextButtonIndex != -1)
                        currentButtonIndex = nextButtonIndex;
                }

                buttons[currentButtonIndex].SetSelected(true);
            }

            // slow up time
            if (Input.GetKeyDown(KeyCode.Comma))
            {
                if (currentButtonIndex > 0)
                {
                    buttons[currentButtonIndex].SetSelected(false);

                    // find first previous button which is enabled, aka button.interactible is true
                    int previousbuttonIndex = buttons.FindLastIndex(currentButtonIndex - 1, tsb => tsb.button.interactable);
                    if (previousbuttonIndex != -1)
                        currentButtonIndex = previousbuttonIndex;
                }

                buttons[currentButtonIndex].SetSelected(true);
            }
        }

        private struct TimeScaleButton
        {
            public Button button;
            public Image image;
            public int timeScale;
            public static Color normalColor;
            public static Color selectedColor;
            public static Color disabledColor;

            public TimeScaleButton(Button button)
            {
                this.button = button;
                this.image = button.GetComponent<Image>();

                this.timeScale = int.Parse(button.name);

                button.GetComponentInChildren<TextMeshProUGUI>().text = $"x{timeScale}";
            }

            public void SetSelected(bool isSelected)
            {
                if (isSelected)
                {
                    image.color = selectedColor;
                    clock.timeScale = timeScale;
                }
                else
                    image.color = normalColor;
            }

            public void SetDisabled(bool isDisabled)
            {
                button.interactable = !isDisabled;

                if (isDisabled)
                    image.color = disabledColor;
                else
                    image.color = normalColor;
            }
        }
    }
}
