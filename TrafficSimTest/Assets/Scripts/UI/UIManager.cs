using System.Collections.Generic;
using Core;
using ML;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class UIManager : SingletonMonobehaviour<UIManager>
    {
        private OptionsMenu optionsMenu;
        public Dictionary<string, int> sliderValues = new Dictionary<string, int>();  // tracks all sliders in the scene
        public Dictionary<string, bool> checkmarkValues = new Dictionary<string, bool>();  // tracks all checkmarks in the scene

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

            foreach (CustomCheckmark checkmark in FindObjectsByType(typeof(CustomCheckmark), FindObjectsSortMode.None))
            {
                checkmark.isToggled = false;
                checkmarkValues[checkmark.title.text] = false;
            }
        }

        public void UpdateSliderValues(string name, int value)
        {
            sliderValues[name] = value;

            switch (name)
            {
                case "Vehicle multiplier (%)":
                    GameManager.Instance.simulation.vehicleManager.vehicleMultiplier = (float) UIManager.Instance.sliderValues[name] / 100f;
                    GameManager.Instance.simulation.vehicleManager.UpdateMaxVehicleCount();
                    break;
            }
        }

        public void UpdateCheckmarkValues(string name, bool value)
        {
            checkmarkValues[name] = value;

            switch (name)
            {
                case "Time-dependent traffic":
                    TrainingManager.Instance.timeDependentTraffic = UIManager.Instance.checkmarkValues[name];
                    break;
                case "Two mode junctions":
                    TrainingManager.Instance.twoModeJunctions = UIManager.Instance.checkmarkValues[name];
                    break;
                case "Vehicle collisions":
                    GameManager.Instance.simulation.vehicleManager.vehicleCollisions = UIManager.Instance.checkmarkValues[name];
                    break;
            }
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
