using Core;
using Persistence;
using SFB;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SaveSimulation : MonoBehaviour
    {
        public Button saveButton;

        public void Awake()
        {
            saveButton = GetComponent<Button>();

            saveButton.onClick.AddListener(SaveSimulationToFileSystem);
        }

        public void SaveSimulationToFileSystem()
        {
            Simulation simulation = GameManager.Instance.simulation;
            string path = StandaloneFileBrowser.SaveFilePanel("Save File", Application.persistentDataPath, "simulation", "tsf");

            PersistenceManager.Instance.SaveSimulationData(path, simulation);
        }
    }
}
