using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Eklendiği nesneyi küçükten "pop" ile büyütür, sonra kendini siler.</summary>
    public class PopAnim : MonoBehaviour
    {
        public float duration = 0.35f;
        Vector3 target;
        float t;

        void OnEnable()
        {
            target = transform.localScale;
            if (target == Vector3.zero) target = Vector3.one;
            transform.localScale = target * 0.01f;
            t = 0f;
        }

        void Update()
        {
            t += Time.deltaTime / duration;
            float k = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float s = 1f + c3 * Mathf.Pow(k - 1f, 3) + c1 * Mathf.Pow(k - 1f, 2);
            transform.localScale = target * Mathf.Max(0.01f, s);
            if (k >= 1f)
            {
                transform.localScale = target;
                Destroy(this);
            }
        }
    }
}
