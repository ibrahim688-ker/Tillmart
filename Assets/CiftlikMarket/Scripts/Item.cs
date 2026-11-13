using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Taşınabilir ürün. Bir hedefe yay çizerek uçabilir.</summary>
    public class Item : MonoBehaviour
    {
        public ItemType type;

        Vector3 startLocal, targetLocal;
        float t, duration = 0.25f, arc = 0.8f;
        bool flying;
        public bool Flying => flying;
        System.Action onDone;

        public void FlyTo(Transform parent, Vector3 localPos, float dur = 0.25f, System.Action done = null)
        {
            transform.SetParent(parent, true);
            transform.localScale = Vector3.one;
            startLocal = transform.localPosition;
            targetLocal = localPos;
            transform.localRotation = Quaternion.identity;
            t = 0f;
            duration = Mathf.Max(0.01f, dur);
            flying = true;
            onDone = done;
        }

        /// <summary>Kap içindeki yeri değiştiğinde çağrılır.</summary>
        public void SetTarget(Vector3 localPos)
        {
            if (flying) targetLocal = localPos;
            else transform.localPosition = localPos;
        }

        void Update()
        {
            if (!flying) return;
            t += Time.deltaTime / duration;
            float k = Mathf.Clamp01(t);
            var p = Vector3.Lerp(startLocal, targetLocal, k);
            p.y += Mathf.Sin(k * Mathf.PI) * arc;
            transform.localPosition = p;
            if (k >= 1f)
            {
                flying = false;
                var d = onDone;
                onDone = null;
                d?.Invoke();
            }
        }
    }
}
