using SFB;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SaveModel : MonoBehaviour
{
    public Button saveButton;

    public void Awake()
    {
        saveButton = GetComponent<Button>();

        saveButton.onClick.AddListener(SaveModelToFileSystem);
    }


    // save .onnx model to file system
    public void SaveModelToFileSystem()
    {
        Debug.Log("not implemented");
    }
}
