using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Makine çalışırken hareketli parçayı döndürür (yayık fıçısı, pres çarkı, karıştırıcı)
    /// ve gövdeye hafif bir "çalışıyor" titreşimi verir. Durunca yavaşça durur.
    /// </summary>
    public class MachineAnimator : MonoBehaviour
    {
        public Transform body;
        public Transform spin;
        public Vector3 spinAxis = Vector3.up;
        public float spinSpeed = 180f;          // derece/sn
        public float pulse = 0.02f;

        Converter station;
        float speed01, t;
        Vector3 baseScale;

        void Awake()
        {
            station = GetComponentInParent<Converter>();
            if (body) baseScale = body.localScale;
        }

        void Update()
        {
            bool working = station != null && station.IsWorking;
            speed01 = Mathf.MoveTowards(speed01, working ? 1f : 0f, Time.deltaTime * 1.5f);
            if (spin && speed01 > 0f) spin.Rotate(spinAxis, spinSpeed * speed01 * Time.deltaTime, Space.Self);
            if (body)
            {
                t += Time.deltaTime * 14f;
                float k = 1f + Mathf.Sin(t) * pulse * speed01;
                body.localScale = new Vector3(baseScale.x * (2f - k), baseScale.y * k, baseScale.z * (2f - k));
            }
        }
    }
}
