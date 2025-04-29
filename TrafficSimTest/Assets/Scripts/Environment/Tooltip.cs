using TMPro;
using UnityEngine;

namespace Environment
{
    public class Tooltip : MonoBehaviour
    {
        private Camera mainCamera;

        private TextMeshPro textComponent;

        private Transform follow;
        private Vector3 followOffset;

        private void Awake()
        {
            mainCamera = Camera.main;

            textComponent = GetComponentInChildren<TextMeshPro>();
        }

        public void Initialize(Transform transformToFollow, Vector3? offset=null, bool isStatic=false)
        {
            this.follow = transformToFollow;

            if (offset == null)
                offset = new Vector3(0f, 5f, 5f);
        
            followOffset = (Vector3) offset;

            Update();

            if (isStatic)
                this.enabled = false;
        }

        public void SetText(string text)
        {
            this.textComponent.text = text;
        }

        public void SetVisible(bool visible)
        {
            this.gameObject.SetActive(visible);
        }

        void Update()
        {
            if (follow == null) return;

            transform.position = follow.position + followOffset;
            transform.forward = mainCamera.transform.forward;
        }
    }
}
