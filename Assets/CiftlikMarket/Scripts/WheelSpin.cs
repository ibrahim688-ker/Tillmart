using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Araç hareket ettikçe tekerleri gidilen yola göre döndürür; duran araçta tekerler durur.</summary>
    public class WheelSpin : MonoBehaviour
    {
        public Transform[] wheels;
        public float radius = 0.29f;

        Vector3 last;

        void OnEnable() { last = transform.position; }

        void LateUpdate()
        {
            Vector3 d = transform.position - last;
            last = transform.position;
            if (d.sqrMagnitude > 25f) return;                 // ışınlanma
            float dist = Vector3.Dot(d, transform.forward);   // geri gidişte ters döner
            if (Mathf.Abs(dist) < 1e-5f || wheels == null) return;
            float deg = dist / Mathf.Max(0.05f, radius) * Mathf.Rad2Deg;
            foreach (var w in wheels) if (w) w.Rotate(Vector3.right, deg, Space.Self);
        }
    }
}
