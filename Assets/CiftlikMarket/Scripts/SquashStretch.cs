using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Oyuncu modeli için kalkış/duruş esnemesi: harekete başlarken hafifçe uzar, dururken basıp
    /// sıkışır, sonra yayla eski boyuna döner. Hacim korunur (boy uzarken en incelir).
    /// </summary>
    public class SquashStretch : MonoBehaviour
    {
        public Transform root;            // hızı ölçülen kök (oyuncu)
        public float stiffness = 170f;
        public float damping = 11f;
        public float kick = 0.022f;       // hız değişimine tepki
        public float maxAmount = 0.11f;

        Vector3 baseScale, lastPos;
        float lastSpeed, s, sv;
        bool init;

        void Awake() { baseScale = transform.localScale; }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (root == null || dt <= 0f) return;
            var pos = root.position;
            if (!init) { lastPos = pos; init = true; return; }
            var d = pos - lastPos; d.y = 0f; lastPos = pos;
            float speed = d.magnitude / dt;
            if (speed > 20f) speed = lastSpeed;                     // ışınlanma
            float dv = speed - lastSpeed; lastSpeed = speed;
            sv += dv * kick * stiffness * 0.1f;                     // hızlanınca uza (+), yavaşlayınca sıkış (−)
            sv += (-stiffness * s - damping * sv) * dt;
            s += sv * dt;
            s = Mathf.Clamp(s, -maxAmount, maxAmount);
            float side = 1f / Mathf.Sqrt(1f + s);                   // hacmi koru
            transform.localScale = new Vector3(baseScale.x * side, baseScale.y * (1f + s), baseScale.z * side);
        }
    }
}
