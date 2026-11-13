using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Daldan sarkan ürün için hafif sallanma (mısır koçanı vb.). Pivot asılma noktasıdır.</summary>
    public class Sway : MonoBehaviour
    {
        public float angleZ = 7f;
        public float angleX = 4f;
        public float speed = 1.6f;

        Quaternion rest;
        float phase;

        void Awake()
        {
            rest = transform.localRotation;
            phase = Random.value * 10f;
        }

        void Update()
        {
            float t = Time.time * speed + phase;
            transform.localRotation = rest * Quaternion.Euler(Mathf.Sin(t * 0.7f) * angleX, 0f, Mathf.Sin(t) * angleZ);
        }
    }
}
