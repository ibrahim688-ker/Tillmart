using System.Collections;
using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Yeni çalışan alınınca: çalışan dışarıdan (kaldırımdan) gelir, market kapısından girer ve işine başlar.
    /// Bu sırada kamera ona kısa bir yakınlaştırmayla odaklanır, sonra oyuncuya geri döner.
    /// Tablet açıksa kapanır (tablet açıkken dünya dondurulduğu için giriş görünmezdi).
    /// </summary>
    public class HireIntro : MonoBehaviour
    {
        static HireIntro I;
        static bool active;

        /// <summary>Kamera çalışanı gösterirken joystick çalışmasın.</summary>
        public static bool Blocking => active;

        const float Zoom = 0.72f;        // kamera mesafesi çarpanı (yakınlaşma)
        const float MaxFocus = 6f;      // en fazla bu kadar sn odaklan
        const float Hold = 0.7f;         // kapıdan girdikten sonra bir an daha göster

        /// <summary>Çalışanı kapıdan içeri yürüt; kamera boştaysa ona odaklan.</summary>
        public static void Play(Worker w)
        {
            if (w == null) return;
            var sp = FindAnyObjectByType<CustomerSpawner>();
            if (sp == null || sp.approachPoint == null || sp.entryPoint == null) return;   // kapı bilinmiyorsa eski davranış

            var outside = sp.approachPoint.position;
            var inside = sp.entryPoint.position;
            var start = outside + new Vector3(0f, 0f, -7f);       // kaldırımda, kapının güneyinden yürüyerek gelir
            var dir = inside - outside; dir.y = 0f;
            var further = inside + dir.normalized * 1.8f;          // kapıdan girip biraz içeri (tente altında kalmasın)
            w.BeginArrival(start, new[] { outside, inside, further });

            var ui = FindAnyObjectByType<OfficeUI>();
            if (ui != null && OfficeUI.IsOpen) ui.Close();

            if (I == null) I = new GameObject("HireIntro").AddComponent<HireIntro>();
            if (!active) I.StartCoroutine(I.Focus(w));
        }

        IEnumerator Focus(Worker w)
        {
            var cam = Camera.main;
            var cf = cam ? cam.GetComponent<CameraFollow>() : null;
            if (cf == null) yield break;
            active = true;
            var prev = cf.target;
            cf.target = w.transform;
            cf.zoom = Zoom;

            float t = 0f;
            while (w != null && w.Arriving && t < MaxFocus) { t += Time.deltaTime; yield return null; }
            float h = 0f;
            while (h < Hold) { h += Time.deltaTime; yield return null; }

            cf.target = prev;
            cf.zoom = 1f;
            active = false;
        }

        void OnDestroy() { if (I == this) { I = null; active = false; } }
    }
}
