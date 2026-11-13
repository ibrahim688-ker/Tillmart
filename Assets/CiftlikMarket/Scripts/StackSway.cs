using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Taşınan ürün yığınının yaylanarak salınması: yürümeye başlayınca üst kısım geride kalır,
    /// durunca öne savrulup yerine oturur, dönerken yana yatar. Alttaki ürün neredeyse hiç kıpırdamaz,
    /// yukarı çıktıkça salınım artar. Sadece sütun dizilişli yığınlarda çalışır (oyuncu ve çalışanlar).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class StackSway : MonoBehaviour
    {
        public float stiffness = 90f;      // yay sertliği
        public float damping = 9f;         // sönümleme
        public float response = 0.045f;    // ivmeye tepki (m / (m/sn²))
        public float maxOffset = 0.38f;    // en üstteki ürünün en fazla kayması
        public float tiltDeg = 22f;        // en üstteki ürünün en fazla eğimi
        public int minItems = 4;           // bundan az ürün varken yığın sallanmaz (elde 3'ten fazla olunca başlar)

        ItemContainer box;
        Vector3 lastPos, lastVel, offset, offVel;
        bool init, swaying;

        void Awake() { box = GetComponent<ItemContainer>(); }

        void OnEnable() { init = false; }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (box == null || dt <= 0f) return;
            var pos = transform.position;
            if (!init) { lastPos = pos; lastVel = Vector3.zero; init = true; return; }

            var vel = (pos - lastPos) / dt; vel.y = 0f;
            if (vel.sqrMagnitude > 400f) vel = Vector3.zero;          // ışınlanma
            var acc = (vel - lastVel) / dt;
            lastPos = pos; lastVel = vel;
            if (acc.sqrMagnitude > 2500f) acc = acc.normalized * 50f;

            int n = box.items.Count;
            if (n < minItems)
            {
                // az ürün: salınım yok; az önce sallanıyorduysa ürünleri yerine oturt
                offset = Vector3.zero; offVel = Vector3.zero;
                if (swaying)
                {
                    swaying = false;
                    for (int i = 0; i < n; i++)
                    {
                        var it0 = box.items[i];
                        if (it0 == null || it0.Flying) continue;
                        it0.transform.localPosition = box.BasePos(i);
                        it0.transform.localRotation = Quaternion.identity;
                    }
                }
                return;
            }
            swaying = true;

            // yerel uzayda yay: ivmenin tersine savrulur
            var localAcc = transform.InverseTransformDirection(acc); localAcc.y = 0f;
            var force = -stiffness * offset - damping * offVel - localAcc * response * stiffness;
            offVel += force * dt;
            offset += offVel * dt;
            if (offset.magnitude > maxOffset) { offset = offset.normalized * maxOffset; offVel *= 0.5f; }

            float top = Mathf.Max(0.01f, box.BasePos(n - 1).y);
            for (int i = 0; i < n; i++)
            {
                var it = box.items[i];
                if (it == null || it.Flying) continue;
                var bp = box.BasePos(i);
                float t = Mathf.Pow(Mathf.Clamp01(bp.y / top), 1.6f);
                it.transform.localPosition = bp + new Vector3(offset.x, 0f, offset.z) * t;
                float k = tiltDeg / maxOffset * t;
                it.transform.localRotation = Quaternion.Euler(offset.z * k, 0f, -offset.x * k);
            }
        }
    }
}
