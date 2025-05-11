using Core;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayPauseButton : MonoBehaviour
    {
        [SerializeField] private Sprite imagePaused;
        [SerializeField] private Sprite imageUnpaused;
        [SerializeField] private Color disabledColor;

        private Color enabledColor;

        private Button button;
        private Image image;
        private Clock clock;
        private bool isPaused = false;
        private bool isEnabled = true;

        private void Start()
        {
            button = GetComponent<Button>();
            image = GetComponent<Image>();

            clock = Clock.Instance;
            button.onClick.AddListener(() => TogglePaused());

            if (clock.isPaused)
                image.sprite = imagePaused;
            else
                image.sprite = imageUnpaused;
            
            enabledColor = image.color;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
                TogglePaused();
        }

        public void Enable()
        {
            isEnabled = true;
            image.color = enabledColor;
        }

        public void Disable()
        {
            Unpause();
            isEnabled = false;
            image.color = disabledColor;
        }

        private void Pause()
        {
            isPaused = true;
            clock.SetPaused(true);
            image.sprite = imagePaused;
        }
        
        private void Unpause()
        {
            isPaused = false;
            clock.SetPaused(false);
            image.sprite = imageUnpaused;
        }
        
        public void TogglePaused()
        {
            if (isEnabled)
            {
                isPaused = !isPaused;

                if (isPaused)
                    Pause();
                else
                    Unpause();
            }
        }
    }
}
