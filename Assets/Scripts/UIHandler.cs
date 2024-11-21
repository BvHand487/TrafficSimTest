using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHandler : MonoBehaviour
{
    public Sprite pause;
    public Sprite play;

    private Image playPauseButtonImage;
    private int timeMultiplier = 1;
    private bool isPaused = false;
    private bool change = false;

    private void Awake()
    {
        SetTimeButtonsListeners();
        SetPlayPauseButtonListener();
    }

    private void Start()
    {
        playPauseButtonImage = transform.GetChild(1).GetComponent<Image>();
    }

    void Update()
    {
        if (change)
        {
            if(isPaused)
            {
                playPauseButtonImage.sprite = play;
                playPauseButtonImage.color = Color.red;
                Utils.Time.SetTimeScale(0);
            }
            else if (!isPaused)
            {
                playPauseButtonImage.sprite = pause;
                playPauseButtonImage.color = Color.green;
                Utils.Time.SetTimeScale(timeMultiplier);
            }
            else
                Utils.Time.SetTimeScale(timeMultiplier);

            change = false;
        }

    }

    private void SetPlayPauseButtonListener()
    {
        Button button = transform.GetChild(1).GetComponent<Button>();

        button.onClick.AddListener(() =>
        {
            isPaused = !isPaused;
            change = true;
        });
    }

    private void SetTimeButtonsListeners()
    {
        Button[] buttons = transform.GetChild(0).GetComponentsInChildren<Button>();

        foreach (Button button in buttons)
        {
            int timeMultiplier = 1;
            try
            {
                timeMultiplier = int.Parse(button.gameObject.name.Substring(1));
            }
            catch (Exception e) when (e is SystemException || e is ApplicationException)
            {
                Debug.Log(e);
                continue;
            }

            button.onClick.AddListener(() =>
            {
                this.timeMultiplier = timeMultiplier;
                change = true;
            });
        }
    }
}
