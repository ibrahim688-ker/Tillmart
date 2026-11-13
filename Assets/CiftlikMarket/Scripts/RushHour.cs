using UnityEngine;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>
    /// Yoğun saat (GDD 3.6): ilk dalga 3. dakikada, sonra 4–5 dakikada bir 60 saniye.
    /// Dalga boyunca yaya müşteri iki kat sık gelir, arabalar art arda gelir. Üstte uyarı + geri sayım.
    /// </summary>
    public class RushHour : MonoBehaviour
    {
        public static RushHour I;
        public static bool Active { get; private set; }

        public float firstDelay = 180f;
        public float minGap = 240f, maxGap = 300f;
        public float duration = 60f;

        float timer, left;
        GameObject banner;
        Text bannerText;

        void Awake()
        {
            I = this;
            Active = false;
            timer = firstDelay;
            BuildUI();
            banner.SetActive(false);
        }

        void OnDestroy() { Active = false; }

        void Update()
        {
            if (Active)
            {
                left -= Time.deltaTime;
                bannerText.text = "YOĞUN SAAT! " + Mathf.CeilToInt(Mathf.Max(0f, left));
                banner.transform.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.time * 5f)) * 0.03f);
                if (clockRt) clockRt.localEulerAngles = new Vector3(0, 0, Mathf.Sin(Time.time * 14f) * 12f);
                if (left <= 0f) End();
                return;
            }
            if (Shelf.All.Count < 2) return;   // en az iki ürün satılmaya başlayınca
            timer -= Time.deltaTime;
            if (timer <= 0f) Begin();
        }

        public void Begin()
        {
            Active = true;
            left = duration;
            banner.SetActive(true);
            AudioFx.Play(Sfx.Rush, 0.8f);
            Haptics.Medium();
            foreach (var dt in FindObjectsByType<DriveThru>(FindObjectsSortMode.None)) dt.OnRushStart();
        }

        void End()
        {
            Active = false;
            banner.SetActive(false);
            timer = Random.Range(minGap, maxGap);
        }

        void BuildUI()
        {
            var canvas = UIKit.MakeCanvas("RushCanvas", transform, 15, false);
            var c = UIKit.MakeChunk("Banner", canvas.transform, UIKit.Hex("FF6B4A"), UIKit.Hex("C9402A"), 40f, 5f, 9f);
            UIKit.Place(c.Rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(62, -130), new Vector2(330, 76));
            banner = c.Root.gameObject;
            var clock = UIKit.Img("ClockBg", c.Rect, UIKit.Circle, UIKit.Ink);
            UIKit.Place(clock.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(2, 3), new Vector2(80, 80));
            var cf = UIKit.Img("Fill", clock.transform, UIKit.Circle, UIKit.Yellow);
            UIKit.Stretch(cf.rectTransform, 5, 5, 5, 5);
            var ci = UIKit.Img("Clock", cf.transform, UIKit.Clock, UIKit.Hex("C9402A"));
            UIKit.Stretch(ci.rectTransform, 8, 8, 8, 8);
            clockRt = ci.rectTransform;
            bannerText = UIKit.Label("Text", c.Face.transform, "", 34, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(bannerText.rectTransform, 52, -3, 6, 0);
        }

        RectTransform clockRt;
    }
}
