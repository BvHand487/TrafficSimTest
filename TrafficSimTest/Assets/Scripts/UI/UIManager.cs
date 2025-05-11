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
        public Dictionary<CustomSlider, int> sliderValues = new Dictionary<CustomSlider, int>();  // tracks all sliders in the scene
        public Dictionary<CustomCheckmark, bool> checkmarkValues = new Dictionary<CustomCheckmark, bool>();  // tracks all checkmarks in the scene
        public PlayPauseButton playPauseButton;
        public TimeScaleButtons timeScaleButtons;

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
            playPauseButton = GetComponentInChildren<PlayPauseButton>();
            timeScaleButtons = GetComponentInChildren<TimeScaleButtons>();
            
            foreach (var o in FindObjectsByType(typeof(CustomSlider), FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var slider = (CustomSlider) o;
                slider.Initialize();
            }

            foreach (var o in FindObjectsByType(typeof(CustomCheckmark), FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var checkmark = (CustomCheckmark) o;
                checkmark.Initialize();
            }
        }

        public void UpdateSliderValues(CustomSlider slider, int value)
        {
            sliderValues[slider] = value;

            switch (slider.title.text)
            {
                case "Vehicle multiplier (%)":
                    GameManager.Instance.simulation.vehicleManager.vehicleMultiplier = (float) UIManager.Instance.sliderValues[slider] / 100f;
                    GameManager.Instance.simulation.vehicleManager.UpdateMaxVehicleCount();
                    break;
            }
        }

        public void UpdateCheckmarkValues(CustomCheckmark checkmark, bool value)
        {
            checkmarkValues[checkmark] = value;

            switch (checkmark.title.text)
            {
                case "Time-dependent traffic":
                    TrainingManager.Instance.timeDependentTraffic = UIManager.Instance.checkmarkValues[checkmark];
                    break;
                case "Two mode junctions":
                    TrainingManager.Instance.twoModeJunctions = UIManager.Instance.checkmarkValues[checkmark];
                    break;
                case "Vehicle collisions":
                    GameManager.Instance.simulation.vehicleManager.vehicleCollisions = UIManager.Instance.checkmarkValues[checkmark];
                    break;
            }
        }

        public void DisableSettings()
        {
            foreach (var checkmark in UIManager.Instance.checkmarkValues.Keys)
                checkmark.Disable();
            
            playPauseButton.Disable();
            
            // foreach (var slider in UIManager.Instance.sliderValues.Keys)
            //     slider.Disable();
        }
        
        public void EnableSettings()
        {
            foreach (var checkmark in UIManager.Instance.checkmarkValues.Keys)
                checkmark.Enable();
            
            playPauseButton.Enable();
            
            // foreach (var slider in UIManager.Instance.sliderValues.Keys)
            //     slider.Enable();
        }


        public void Generate()
        {
            PlayerPrefs.SetString("Load method", "generate");

            // pass generation settings using PlayerPrefs API
            foreach (var (slider, val) in sliderValues)
                PlayerPrefs.SetInt(slider.title.text, val);
            
            SceneManager.LoadSceneAsync(1);
        }

        public void LoadSimulation()
        {
            PlayerPrefs.SetString("Load method", "file");

            SceneManager.LoadSceneAsync(1);
        }
    }
}
