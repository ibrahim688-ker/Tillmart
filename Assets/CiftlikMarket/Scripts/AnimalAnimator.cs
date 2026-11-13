using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Hayvanın sadece kafasını oynatarak "yemek yiyor" hissi verir.
    /// İnek: kafayı yavaşça yemliğe indirir, aşağıdayken çiğner, sonra kaldırıp etrafa bakar.
    /// Tavuk: hızlı gagalamalar ve ara sıra kafa çevirme.
    /// Bağlı olduğu istasyon üretim yapıyorsa daha sık yer.
    /// </summary>
    public class AnimalAnimator : MonoBehaviour
    {
        public enum Kind { Cow, Chicken }
        public Kind kind = Kind.Cow;
        public Transform head;
        [Tooltip("Model yerel -Z yönüne bakıyorsa işaretle (Blender'dan gelen hayvan modelleri)")]
        public bool faceMinusZ = true;

        Quaternion rest;
        Converter station;
        float timer, t;
        int phase;            // 0 bekle, 1 in, 2 ye/çiğne, 3 kalk, 4 etrafa bak
        float pitch, yaw, roll;
        float tPitch, tYaw;
        int pecksLeft;

        void Awake()
        {
            if (!head) head = transform.Find("Head");
            if (head) rest = head.localRotation;
            station = GetComponentInParent<Converter>();
            timer = Random.Range(0.2f, 2.5f);
        }

        bool Hungry => station != null && (station.IsWorking || station.inputPile.Count > 0);

        void Update()
        {
            if (!head) return;
            float dt = Time.deltaTime;
            t += dt;
            timer -= dt;
            if (kind == Kind.Cow) Cow(dt); else Chicken(dt);
            float s = faceMinusZ ? -1f : 1f;      // model -Z'ye bakıyorsa "aşağı" eğim ters işaretli
            head.localRotation = Quaternion.Euler(pitch * s, yaw, roll * s) * rest;
        }

        void Cow(float dt)
        {
            switch (phase)
            {
                case 0: // kafa yukarıda, ara sıra kulak/kafa salınımı
                    tPitch = Mathf.Sin(t * 0.8f) * 3f;
                    if (timer <= 0f) { phase = 1; timer = 0.7f; tYaw = Random.Range(-8f, 8f); }
                    break;
                case 1: // yemliğe in
                    tPitch = 34f;
                    if (timer <= 0f) { phase = 2; timer = Hungry ? Random.Range(2.2f, 3.5f) : Random.Range(1.2f, 2f); }
                    break;
                case 2: // çiğne: küçük hızlı inip kalkma + yana kayma
                    tPitch = 30f + Mathf.Sin(t * 9f) * 4f;
                    roll = Mathf.Sin(t * 9f) * 2.5f;
                    if (timer <= 0f) { phase = 3; timer = 0.8f; roll = 0f; }
                    break;
                case 3: // kalk, çiğnemeye devam
                    tPitch = -4f + Mathf.Sin(t * 7f) * 2f;
                    if (timer <= 0f) { phase = 4; timer = Random.Range(0.8f, 1.6f); tYaw = Random.Range(-22f, 22f); }
                    break;
                case 4: // etrafa bak
                    tPitch = -2f;
                    if (timer <= 0f) { phase = 0; timer = Hungry ? Random.Range(0.3f, 1.2f) : Random.Range(1.5f, 4f); tYaw = 0f; }
                    break;
            }
            pitch = Mathf.Lerp(pitch, tPitch, 1f - Mathf.Exp(-5f * dt));
            yaw = Mathf.Lerp(yaw, tYaw, 1f - Mathf.Exp(-3f * dt));
        }

        void Chicken(float dt)
        {
            switch (phase)
            {
                case 0: // bekle / etrafa bak (tavuklar kafayı kesik kesik çevirir)
                    tPitch = 0f;
                    if (timer <= 0f)
                    {
                        if (Random.value < (Hungry ? 0.8f : 0.5f)) { phase = 1; pecksLeft = Random.Range(2, 5); timer = 0.09f; }
                        else { tYaw = Random.Range(-40f, 40f); timer = Random.Range(0.4f, 1.0f); }
                    }
                    break;
                case 1: // gagala: aşağı
                    tPitch = 60f;
                    if (timer <= 0f) { phase = 2; timer = 0.1f; }
                    break;
                case 2: // yukarı
                    tPitch = 8f;
                    if (timer <= 0f)
                    {
                        pecksLeft--;
                        if (pecksLeft > 0) { phase = 1; timer = 0.09f; }
                        else { phase = 0; timer = Hungry ? Random.Range(0.3f, 0.9f) : Random.Range(0.8f, 2f); tYaw = Random.Range(-15f, 15f); }
                    }
                    break;
            }
            // tavuk hareketleri keskin
            pitch = Mathf.Lerp(pitch, tPitch, 1f - Mathf.Exp(-28f * dt));
            yaw = Mathf.Lerp(yaw, tYaw, 1f - Mathf.Exp(-18f * dt));
        }
    }
}
