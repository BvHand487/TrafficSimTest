using SFB;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadSimulation : MonoBehaviour
{
    public Button loadButton;
    public bool forcesReload = false;

    public void Awake()
    {
        loadButton = GetComponent<Button>();

        loadButton.onClick.AddListener(LoadSimulationFromFileSystem);
    }

    // Load .tsf
    public void LoadSimulationFromFileSystem()
    {
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open File", Application.persistentDataPath, "tsf", false);

        if (paths.Length == 0)
            return;

        PersistenceManager.Instance.LoadSimulationData(paths[0]);

        if (forcesReload == true)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetString("Load method", "file");

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
