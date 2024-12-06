using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private Dictionary<string, int> sliderValues = new Dictionary<string, int>();

    private void Start()
    {
        PlayerPrefs.DeleteAll();

        foreach (CustomSlider slider in FindObjectsOfType<CustomSlider>())
            slider.UpdateSliderValue(slider.minValue);
    }

    public void UpdateSliderValues(string name, int value)
    {
        sliderValues[name] = value;
    }

    // Pass data to the generator script using the PlayerRrefs API
    public void Generate()
    {
        foreach (var (name, val) in sliderValues)
            PlayerPrefs.SetInt(name, val);

        SceneManager.LoadSceneAsync(1);
    }
}
