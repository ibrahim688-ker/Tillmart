using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>
    /// Çevrimdışı kazanç (GDD 3.9): oyuncu yokken çalışanlar kazanmaya devam eder.
    /// Kazanç = son oturumlardaki gelir hızı (dk başına, hareketli ortalama) × süre × %50.
    /// Kasiyer yoksa kazanç yok; bakıcı yoksa hız düşük sayılır. Süre üst sınırı DEPO yükseltmesiyle artar.
    /// </summary>
    public class OfflineEarnings : MonoBehaviour
    {
        public static OfflineEarnings I;
        public static bool IsOpen { get; private set; }
        public static float CapHours = 1f;   // DEPO yükseltmesi ayarlar

        const float Factor = 0.2f;   // telefonda test: 0,5 çok fazlaydı, dönünce her şey tek seferde açılıyordu
        const float MinSeconds = 60f;

        float ema, windowIncome, windowTimer;

        /// <summary>Son oturumlardaki gelir hızı (para / dk). Günlük ödül ve görev ödülleri bununla ölçeklenir.</summary>
        public float RatePerMinute => ema;
        int pending;
        GameObject panel;
        Text timeText, amountText;

        void Awake()
        {
            I = this;
            ema = PlayerPrefs.GetFloat(SaveKeys.P("cm_rate"), 0f);
            BuildUI();
            panel.SetActive(false);
        }

        void Start() { StartCoroutine(CheckNextFrame()); }

        IEnumerator CheckNextFrame()
        {
            yield return null; // yükseltmeler (DEPO) uygulanmış olsun
            yield return null;
            CheckOffline();
        }

        /// <summary>Para yığınına düşen her gelir buraya bildirilir (gelir hızını ölçmek için).</summary>
        public void ReportIncome(int v) { windowIncome += v; }

        void Update()
        {
            windowTimer += Time.unscaledDeltaTime;
            if (windowTimer < 30f) return;
            float perMin = windowIncome * (60f / windowTimer);
            ema = ema <= 0f ? perMin : Mathf.Lerp(ema, perMin, 0.3f);
            windowIncome = 0f;
            windowTimer = 0f;
            PlayerPrefs.SetFloat(SaveKeys.P("cm_rate"), ema);
        }

        public void MarkTime()
        {
            PlayerPrefs.SetString(SaveKeys.P("cm_last_time"), DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.SetFloat(SaveKeys.P("cm_rate"), ema);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) { MarkTime(); PlayerPrefs.Save(); }
            else CheckOffline();
        }

        void OnApplicationQuit() { MarkTime(); }

        void CheckOffline()
        {
            if (IsOpen) return;
            var s = PlayerPrefs.GetString(SaveKeys.P("cm_last_time"), "");
            MarkTime();
            if (!long.TryParse(s, out long ticks)) return;
            double sec = (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds;
            if (sec < MinSeconds) return;                               // saat geri alındıysa da negatif çıkar
            if (GameManager.I == null || !GameManager.I.IsUnlocked("kasiyer")) return;

            double capped = Math.Min(sec, CapHours * 3600.0);
            float automation = GameManager.I.IsUnlocked("bakici") ? 1f : 0.3f;
            int gain = (int)(ema / 60.0 * capped * Factor * automation);
            if (gain <= 0) return;

            pending = gain;
            timeText.text = "<b>" + BranchManager.BranchName + "</b>\nSen yokken çalışanların\n<color=#2A78CF><b>" + FormatTime(capped) + "</b></color> boyunca çalıştı" +
                            (sec > capped ? "\n<size=30><color=#9A8B76>(Depo dolu: en fazla " + CapHours.ToString("0") + " saat)</color></size>" : "");
            amountText.text = "+" + UIKit.Money(pending);
            panel.SetActive(true);
            IsOpen = true;
            AudioFx.Play(Sfx.Popup, 0.8f);
            var card = panel.transform.Find("Card");
            if (card && card.GetComponent<PopAnim>() == null) card.gameObject.AddComponent<PopAnim>();
        }

        void Collect(int multiplier)
        {
            if (!IsOpen) return;
            GameManager.I.AddMoney(pending * multiplier);
            AudioFx.Play(Sfx.Coin);
            AudioFx.Play(Sfx.Unlock, 0.6f);
            Haptics.Medium();
            pending = 0;
            panel.SetActive(false);
            IsOpen = false;
            GameManager.I.Save();
        }

        /// <summary>
        /// Başka bir şubedeyken o şubede biriken para (tahmini) ve kasanın doluluğu (0..1).
        /// Hesap, şubeye varınca gösterilen offline kazançla aynıdır.
        /// </summary>
        public static int Estimate(int branch, out float fill)
        {
            fill = 0f;
            if (!long.TryParse(PlayerPrefs.GetString(SaveKeys.For(branch, "cm_last_time"), ""), out long ticks)) return 0;
            double sec = (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalSeconds;
            if (sec < MinSeconds) return 0;
            string unlocked = "," + PlayerPrefs.GetString(SaveKeys.For(branch, "cm_unlocked"), "") + ",";
            if (!unlocked.Contains(",kasiyer,")) return 0;
            float cap = 1f + PlayerPrefs.GetInt(SaveKeys.For(branch, "cm_up_depo"), 0);
            double capped = Math.Min(sec, cap * 3600.0);
            fill = (float)(capped / (cap * 3600.0));
            float automation = unlocked.Contains(",bakici,") ? 1f : 0.3f;
            float rate = PlayerPrefs.GetFloat(SaveKeys.For(branch, "cm_rate"), 0f);
            return Mathf.Max(0, (int)(rate / 60.0 * capped * Factor * automation));
        }

        static string FormatTime(double sec)
        {
            int m = (int)(sec / 60);
            int h = m / 60;
            m %= 60;
            return h > 0 ? h + " sa " + m + " dk" : m + " dk";
        }

        // ---------------- Arayüz ----------------

        void BuildUI()
        {
            var canvas = UIKit.MakeCanvas("OfflineCanvas", transform, 40);
            var p = UIKit.MakePopup(canvas.transform, "Dim", "HOŞ GELDİN!", UIKit.Green, UIKit.GreenLip, new Vector2(920, 1000), 20f);
            panel = p.Dim.gameObject;
            p.Card.name = "Card";

            // kasiyer: "senin yerine çalıştık"
            var disc = UIKit.MakeChunk("Disc", p.Body, UIKit.Hex("4DA6FF"), UIKit.Hex("2A78CF"), 96f, 7f, 10f);
            UIKit.Place(disc.Rect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(192, 202));
            var di = UIKit.Img("Inner", disc.Face.transform, UIKit.Circle, new Color(1, 1, 1, 0.95f));
            UIKit.Stretch(di.rectTransform, 12, 12, 12, 12);
            var icon = UIKit.Img("Icon", di.transform, UIKit.Icon("calisan_kasiyer"), Color.white);
            icon.preserveAspect = true;
            UIKit.Stretch(icon.rectTransform, 6, 4, 6, 8);
            var zz = UIKit.Label("Zz", disc.Rect, "z<size=34>z</size><size=26>z</size>", 46, UIKit.Hex("5C7CFA"), TextAnchor.MiddleLeft, false);
            UIKit.Place(zz.rectTransform, new Vector2(1, 1), new Vector2(0, 1), new Vector2(-6, -4), new Vector2(120, 80));

            timeText = UIKit.Label("Time", p.Body, "", 40, UIKit.TextDark, TextAnchor.MiddleCenter, false, true);
            var ttr = timeText.rectTransform;
            ttr.anchorMin = new Vector2(0, 1); ttr.anchorMax = new Vector2(1, 1); ttr.pivot = new Vector2(0.5f, 1);
            ttr.offsetMin = new Vector2(40, -490); ttr.offsetMax = new Vector2(-40, -306);

            // kazanç: büyük krem hap + altın
            var money = UIKit.Node("Money", p.Body);
            UIKit.Place(money, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(20, 236), new Vector2(560, 150));
            var mp = UIKit.MakeChunk("Pill", money, Color.white, UIKit.CreamLip, 75f, 7f, 12f);
            UIKit.Stretch(mp.Rect, 60, 0, 0, 0);
            amountText = UIKit.Label("Amount", mp.Face.transform, "0", 84, UIKit.Hex("2F9E3A"), TextAnchor.MiddleCenter);
            UIKit.Stretch(amountText.rectTransform, 90, -6, 24, 0);
            var coin = UIKit.Img("Coin", money, UIKit.Coin, Color.white);
            UIKit.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60, 0), new Vector2(178, 178));
            var lira = UIKit.Label("Lira", coin.transform, "₺", 86, UIKit.Hex("B5700E"));
            UIKit.Stretch(lira.rectTransform, 0, 8, 0, 0);
            amountPunch = amountText.gameObject.AddComponent<UIPunch>();

            var collect = UIKit.MakeButton("Collect", p.Body, UIKit.Yellow, UIKit.YellowLip, 62f, () => Collect(1), 6f, 12f);
            UIKit.Place(collect.Rect, new Vector2(0.27f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(370, 140));
            UIKit.Stretch(UIKit.Label("T", collect.Face.transform, "TOPLA", 52, Color.white, TextAnchor.MiddleCenter, true).rectTransform, 0, -4, 0, 0);

            var dbl = UIKit.MakeButton("Double", p.Body, UIKit.Green, UIKit.GreenLip, 62f, () =>
                AdService.ShowRewarded("offline_x2", ok => { if (ok) Collect(2); }), 6f, 12f);
            UIKit.Place(dbl.Rect, new Vector2(0.73f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(370, 140));
            var play = UIKit.Img("Play", dbl.Face.transform, UIKit.Circle, new Color(1, 1, 1, 0.95f));
            UIKit.Place(play.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(70, 70));
            var tri = UIKit.Img("Tri", play.transform, UIKit.PlayTri, UIKit.GreenLip);
            UIKit.Stretch(tri.rectTransform, 14, 14, 12, 14);
            var dt = UIKit.Label("T", dbl.Face.transform, "2x TOPLA", 50, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(dt.rectTransform, 84, -4, 8, 0);
            var badge = UIKit.MakeChunk("X2", dbl.Rect, UIKit.Red, UIKit.RedLip, 34f, 5f, 6f);
            UIKit.Place(badge.Rect, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-24, -6), new Vector2(110, 68));
            UIKit.Stretch(UIKit.Label("T", badge.Face.transform, "REKLAM", 24, Color.white, TextAnchor.MiddleCenter, true).rectTransform, 0, -2, 0, 0);
        }

        UIPunch amountPunch;
    }
}
