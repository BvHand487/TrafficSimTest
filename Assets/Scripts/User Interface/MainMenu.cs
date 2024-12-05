using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private Dictionary<string, int> sliderValues;

    private void Start()
    {
        PlayerPrefs.DeleteAll();
        sliderValues = new Dictionary<string, int>();

        foreach (var slider in FindObjectsOfType<Slider>())
        {
            var sliderTitle = slider.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text;
            sliderValues.Add(sliderTitle, 1);
        }
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

    public void FixedUpdate()
    {
        string msg = "";

        foreach (var (name, val) in sliderValues)
        {
            msg += $"({name} -> {val}), ";
        }
        Debug.Log(msg);
    }
}
