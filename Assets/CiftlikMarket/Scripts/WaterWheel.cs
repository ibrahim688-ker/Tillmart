using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Değirmen çarkı: değirmen un üretirken döner, girdisi bitince yavaşlayıp durur.</summary>
    public class WaterWheel : MonoBehaviour
    {
        public Converter mill;
        public float maxSpeed = 70f;   // derece/sn
        public float idleSpeed = 6f;   // su akarken boşta hafifçe döner
        float speed;

        void Update()
        {
            bool working = mill != null && mill.isActiveAndEnabled && mill.IsWorking;
            float target = working ? maxSpeed : (mill != null && mill.isActiveAndEnabled ? idleSpeed : 0f);
            speed = Mathf.Lerp(speed, target, 1f - Mathf.Exp(-1.5f * Time.deltaTime));
            transform.Rotate(-speed * Time.deltaTime, 0f, 0f, Space.Self);
        }
    }
}
