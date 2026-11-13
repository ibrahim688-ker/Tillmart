using UnityEngine;

namespace CiftlikMarket
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 23f, -15f);
        public float smooth = 6f;
        [Tooltip("Mesafe çarpanı: 1 normal, küçük değer yakınlaşır (ör. yeni çalışan girişi)")]
        public float zoom = 1f;
        float zoomCur = 1f;

        void Start()
        {
            transform.rotation = Quaternion.LookRotation(-offset);
            if (target) transform.position = target.position + offset;
        }

        void LateUpdate()
        {
            if (!target) return;
            zoomCur = Mathf.Lerp(zoomCur, zoom, 1f - Mathf.Exp(-4f * Time.deltaTime));
            transform.position = Vector3.Lerp(transform.position, target.position + offset * zoomCur, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        }
    }
}
