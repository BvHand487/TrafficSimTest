using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : SingletonMonobehaviour<UIManager>
{
    private OptionsMenu optionsMenu;
    private Dictionary<string, int> sliderValues = new Dictionary<string, int>();  // tracks all sliders in the scene

    public override void Awake()
    {
        base.Awake();

        optionsMenu = GetComponentInChildren<OptionsMenu>();
        optionsMenu?.gameObject.SetActive(false);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && optionsMenu != null)
        {
            optionsMenu.gameObject.SetActive(!optionsMenu.gameObject.activeSelf);
        }
    }

    private void Start()
    {
        foreach (CustomSlider slider in FindObjectsByType(typeof(CustomSlider), FindObjectsSortMode.None))
            slider.UpdateSliderValue(slider.minValue);
    }

    public void UpdateSliderValues(string name, int value)
    {
        sliderValues[name] = value;
    }

    public void Generate()
    {
        PlayerPrefs.SetString("Load method", "generate");

        // pass generation settings using PlayerPrefs API
        foreach (var (name, val) in sliderValues)
            PlayerPrefs.SetInt(name, val);

        SceneManager.LoadSceneAsync(1);
    }

    public void LoadSimulation()
    {
        PlayerPrefs.SetString("Load method", "file");

        SceneManager.LoadSceneAsync(1);
    }
}
