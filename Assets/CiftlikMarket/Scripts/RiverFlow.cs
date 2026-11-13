using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Akarsu yüzeyindeki dalga çizgilerini akış yönünde kaydırır (sonsuz döngü).</summary>
    public class RiverFlow : MonoBehaviour
    {
        public Transform[] ripples;
        public float speed = 1.4f;
        public float zMin = -26f, zMax = 34f;

        void Update()
        {
            float len = zMax - zMin;
            for (int i = 0; i < ripples.Length; i++)
            {
                var r = ripples[i];
                if (!r) continue;
                var lp = r.localPosition;
                float wz = r.position.z - speed * Time.deltaTime;
                if (wz < zMin) wz += len;
                // akış yönünde kay + hafif sağa-sola salınım
                lp.z += wz - r.position.z;
                lp.x = Mathf.Sin(wz * 0.6f + i) * 0.35f;
                r.localPosition = lp;
            }
        }
    }
}
