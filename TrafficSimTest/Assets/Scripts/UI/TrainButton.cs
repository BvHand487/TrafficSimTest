using ML;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TrainButton : MonoBehaviour
    {
        public string trainString = "Train";
        public string stopTrainString = "Stop";

        private Button button;
        private TextMeshProUGUI trainText;

        private bool isTraining = false;

        void Start()
        {
            button = GetComponent<Button>();
            trainText = GetComponentInChildren<TextMeshProUGUI>();

            button.onClick.AddListener(() => ToggleTraining());
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
                ToggleTraining();
        }

        public void ToggleTraining()
        {
            isTraining = !isTraining;

            if (isTraining)
            {
                trainText.text = stopTrainString;
                TrainingManager.Instance.StartTraining();
            }
            else
            {
                trainText.text = trainString;
                TrainingManager.Instance.StopTraining();
            }
        }
    }
}
