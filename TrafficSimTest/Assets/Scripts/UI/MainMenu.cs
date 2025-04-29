using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MainMenu : MonoBehaviour
    {
        private Dictionary<string, int> sliderValues = new Dictionary<string, int>();

        private void Start()
        {
            PlayerPrefs.DeleteAll();

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
}
