using TMPro;
using UnityEngine;

public class CustomCheckmark : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI title;

    public bool isToggled = false;

    public GameObject checkmarkObject;

    private void Awake()
    {
        title = GetComponentInChildren<TextMeshProUGUI>();

        checkmarkObject = transform.GetChild(0).GetChild(0).gameObject;
    }

    public void SwapState()
    {
        isToggled = !isToggled;
        checkmarkObject.SetActive(isToggled);
        UIManager.Instance.UpdateCheckmarkValues(title.text, isToggled);
    }
}
