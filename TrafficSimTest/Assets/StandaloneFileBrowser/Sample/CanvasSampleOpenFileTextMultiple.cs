using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SFB;
using UnityEngine.Networking;

[RequireComponent(typeof(Button))]
public class CanvasSampleOpenFileTextMultiple : MonoBehaviour, IPointerDownHandler {
    public Text output;

#if UNITY_WEBGL && !UNITY_EDITOR
    //
    // WebGL
    //
    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

    public void OnPointerDown(PointerEventData eventData) {
        UploadFile(gameObject.name, "OnFileUpload", ".txt", true);
    }

    // Called from browser
    public void OnFileUpload(string urls) {
        StartCoroutine(OutputRoutine(urls.Split(',')));
    }
#else
    //
    // Standalone platforms & editor
    //
    public void OnPointerDown(PointerEventData eventData) { }

    void Start() {
        var button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick() {
        // var paths = StandaloneFileBrowser.OpenFilePanel("Title", "", "txt", true);
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", "", true);
        if (paths.Length > 0) {
            var urlArr = new List<string>(paths.Length);
            for (int i = 0; i < paths.Length; i++) {
                urlArr.Add(new System.Uri(paths[i]).AbsoluteUri);
            }
            StartCoroutine(OutputRoutine(urlArr.ToArray()));
        }
    }
#endif

    // Changed from WWW to UnityWebRequest
    private IEnumerator OutputRoutine(string[] urlArr) {
        // var outputText = "";
        // for (int i = 0; i < urlArr.Length; i++) {
        //     var loader = new WWW(urlArr[i]);
        //     yield return loader;
        //     outputText += loader.text;
        // }
        // output.text = outputText;
        
        string outputText = "";

        for (int i = 0; i < urlArr.Length; i++)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(urlArr[i]))
            {
                // Start the request and wait for it to complete
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || 
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Request error for URL {urlArr[i]}: {request.error}");
                }
                else
                {
                    // Append the successful response to outputText
                    outputText += request.downloadHandler.text;
                }
            }
        }

        // Once all URLs have been processed, update the output text
        output.text = outputText;
    }
}