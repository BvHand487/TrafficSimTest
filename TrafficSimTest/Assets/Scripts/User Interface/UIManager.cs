using Unity.VisualScripting;
using UnityEngine;

public class UIManager : SingletonMonobehaviour<UIManager>
{
    public OptionsMenu optionsMenu;

    public override void Awake()
    {
        base.Awake();

        optionsMenu = GetComponentInChildren<OptionsMenu>();
        optionsMenu.gameObject.SetActive(false);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            optionsMenu.gameObject.SetActive(!optionsMenu.gameObject.activeSelf);
        }
    }
}
