using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayPauseButton : MonoBehaviour
{
    [SerializeField] private Sprite imagePaused;
    [SerializeField] private Sprite imageUnpaused;

    private bool isPaused = false;
    private Clock clock;

    private void Start()
    {
        clock = Clock.Instance;
    }

    public void TogglePaused(Image buttonImage)
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            clock.SetPaused(true);
            buttonImage.sprite = imagePaused;
        }
        else
        {
            clock.SetPaused(false);
            buttonImage.sprite = imageUnpaused;
        }
    }
}
