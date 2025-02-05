using SFB;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class LoadModel : MonoBehaviour
{
    public Button loadButton;

    public void Awake()
    {
        loadButton = GetComponent<Button>();

        loadButton.onClick.AddListener(() => LoadModelFromFileSystem());
    }


    // Load .onnx model into the agents brain
    public void LoadModelFromFileSystem()
    {
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open File", Path.Combine(Application.dataPath, ".."), "onnx", false);

        if (paths.Length == 0)
            return;
    }
}
