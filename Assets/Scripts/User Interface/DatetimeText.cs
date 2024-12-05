using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DatetimeText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI datetimeText;

    private Clock clock;

    private void Start()
    {
        clock = Clock.Instance;
    }

    void Update()
    {
        datetimeText.text = clock.GetFormattedDatetime("dd MMM yyyy HH:mm:ss");
    }
}
