using Core;
using TMPro;
using UnityEngine;

namespace UI
{
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
            datetimeText.text = string.Format("{0:dd MMM yyyy HH:mm:ss}", clock.datetime);
        }
    }
}
