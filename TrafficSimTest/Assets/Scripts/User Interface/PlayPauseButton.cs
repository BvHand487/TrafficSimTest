using UnityEngine;
using UnityEngine.UI;

public class PlayPauseButton : MonoBehaviour
{
    [SerializeField] private Sprite imagePaused;
    [SerializeField] private Sprite imageUnpaused;

    private Button button;
    private Image image;
    private Clock clock;
    private bool isPaused = false;

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
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            TogglePaused();
    }

    public void TogglePaused()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            clock.SetPaused(true);
            image.sprite = imagePaused;
        }
        else
        {
            clock.SetPaused(false);
            image.sprite = imageUnpaused;
        }
    }
}
