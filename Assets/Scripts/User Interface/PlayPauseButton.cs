using UnityEngine;
using UnityEngine.UI;

public class PlayPauseButton : MonoBehaviour
{
    [SerializeField] private Sprite imagePaused;
    [SerializeField] private Sprite imageUnpaused;

    private Image image;
    private Clock clock;
    private bool isPaused = false;

    private void Start()
    {
        image = GetComponent<Image>();
        clock = Clock.Instance;
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
