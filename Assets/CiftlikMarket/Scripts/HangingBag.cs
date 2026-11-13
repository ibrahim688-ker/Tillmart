using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Elde taşınan poşet: tutma noktası (bu objenin pivotu) kolla birlikte hareket eder,
    /// poşet ise kendi ağırlığıyla aşağı sarkar ve yürürken sarkaç gibi hafifçe sallanır.
    /// Karakter animasyonundan sonra çalışır (kolun son konumunu kullanır).
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class HangingBag : MonoBehaviour
    {
        public Transform root;              // karakter kökü (yön buradan alınır)
        public float stiffness = 60f;
        public float damping = 6f;
        public float response = 0.8f;       // ivmeye tepki (derece / (m/s²))
        public float maxTilt = 28f;
        public float popTime = 0.22f;

        Vector3 lastPos, lastVel;
        Vector2 tilt, tiltVel;              // x: sağ-sol, y: ileri-geri (derece)
        float age;
        Vector3 baseScale;
        bool started;

        void Start()
        {
            if (root == null) root = transform.root;
            lastPos = transform.position;
            baseScale = transform.localScale;
            transform.localScale = baseScale * 0.01f;
            started = true;
        }

        void LateUpdate()
        {
            if (!started) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // ortaya çıkış: hafif taşan büyüme
            if (age < popTime)
            {
                age += dt;
                float t = Mathf.Clamp01(age / popTime);
                float s = 1f + 0.18f * Mathf.Sin(t * Mathf.PI);
                transform.localScale = baseScale * Mathf.Lerp(0.01f, 1f, t) * s;
                if (t >= 1f) transform.localScale = baseScale;
            }

            var pos = transform.position;
            var vel = (pos - lastPos) / dt;
            if ((pos - lastPos).sqrMagnitude > 1f) vel = lastVel;     // ışınlanma
            var acc = Vector3.ClampMagnitude((vel - lastVel) / dt, 40f);
            lastPos = pos; lastVel = vel;

            // ivmeyi karakterin yerel eksenlerine çevir
            var fwd = root.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();
            var right = Vector3.Cross(Vector3.up, fwd);
            var target = new Vector2(Vector3.Dot(acc, right), Vector3.Dot(acc, fwd)) * response;

            // yay: hedef eğim ivmenin tersine (ağırlık geride kalır)
            var f = (-target - tilt) * stiffness - tiltVel * damping;
            tiltVel += f * dt;
            tilt += tiltVel * dt;
            if (tilt.magnitude > maxTilt) { tilt = tilt.normalized * maxTilt; tiltVel *= 0.5f; }

            // dünyada dik dur, karakterin yönüne bak, sallanma eğimini ekle
            var yaw = Quaternion.LookRotation(fwd, Vector3.up);
            transform.rotation = yaw * Quaternion.Euler(-tilt.y, 0f, tilt.x);
        }
    }
}
