using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>
    /// Günlük giriş ödülü (GDD 3.11): 7 günlük takvim. Her gün bir kez alınır; bir gün kaçırılırsa
    /// seri 1. güne döner, 7. günden sonra baştan başlar. Ödül, oyuncunun kazanç hızıyla büyür
    /// (geç oyunda da anlamlı kalsın). Kayıt bütün şubeler için ortaktır (cmg_ anahtarları).
    /// Oyun açılınca (öğretici bittiyse ve hoş geldin penceresi kapandıysa) kendiliğinden açılır;
    /// sağdaki hediye butonundan da açılır.
    /// </summary>
    public class DailyReward : MonoBehaviour
    {
        public static DailyReward I;
        public static bool IsOpen { get; private set; }

        const string LastKey = "cmg_daily_last";     // en son alınan günün tarihi (yyyyMMdd, yerel saat)
        const string StreakKey = "cmg_daily_day";    // sıradaki gün (0..6)

        static readonly int[] BaseReward = { 100, 150, 250, 350, 500, 700, 1500 };
        static readonly float[] Minutes = { 2f, 3f, 4f, 5f, 6f, 8f, 15f };   // kazanç hızı × dakika

        GameObject panel, hudRoot;
        RectTransform hudBadge;
        readonly Cell[] cells = new Cell[7];
        Chunk claimBtn, doubleBtn;
        Text claimText, infoText;
        bool autoShown;

        class Cell { public Chunk chunk; public Text day, amount; public Image check, coin; public UIPunch punch; }

        static string Today => DateTime.Now.ToString("yyyyMMdd");
        static string Yesterday => DateTime.Now.AddDays(-1).ToString("yyyyMMdd");

        /// <summary>Bugünün ödülü alınabilir mi?</summary>
        public static bool CanClaim => PlayerPrefs.GetString(LastKey, "") != Today;

        /// <summary>Bugün alınacak (ya da en son alınan) günün sırası 0..6.</summary>
        static int CurrentDay
        {
            get
            {
                int day = PlayerPrefs.GetInt(StreakKey, 0);
                string last = PlayerPrefs.GetString(LastKey, "");
                if (last == Today) return (day + 6) % 7;                               // bugün alındı: en son alınan gün
                if (last != Yesterday) return 0;                                       // seri koptu (ya da ilk kez)
                return day % 7;
            }
        }

        static bool TutorialDone => PlayerPrefs.GetInt("cmg_tut_done", 0) == 1;

        public static int RewardFor(int day)
        {
            float rate = OfflineEarnings.I != null ? OfflineEarnings.I.RatePerMinute : 0f;
            int v = Mathf.Max(BaseReward[day], Mathf.RoundToInt(rate * Minutes[day]));
            return v < 1000 ? Mathf.RoundToInt(v / 10f) * 10 : Mathf.RoundToInt(v / 50f) * 50;
        }

        void Awake()
        {
            I = this;
            BuildUI();
            panel.SetActive(false);
        }

        void Start() { StartCoroutine(AutoShow()); }

        IEnumerator AutoShow()
        {
            // yükseltmeler ve kazanç hızı yüklensin, hoş geldin penceresi önce gelsin
            for (int i = 0; i < 4; i++) yield return null;
            while (OfflineEarnings.IsOpen || OfficeUI.IsOpen || AdService.IsShowing || BranchManager.IsConfirmOpen || !TutorialDone)
                yield return new WaitForSecondsRealtime(0.5f);
            yield return new WaitForSecondsRealtime(0.6f);
            if (CanClaim && !autoShown && !Quests.IsOpen) { autoShown = true; Open(); }
        }

        void Update()
        {
            bool show = TutorialDone && !OfficeUI.IsOpen;
            if (hudRoot.activeSelf != show) hudRoot.SetActive(show);
            if (!show) return;
            bool can = CanClaim;
            if (hudBadge.gameObject.activeSelf != can) hudBadge.gameObject.SetActive(can);
            if (can) hudBadge.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 0.14f);
        }

        public void Open()
        {
            if (IsOpen || Quests.IsOpen) return;
            Refresh();
            panel.SetActive(true);
            IsOpen = true;
            AudioFx.Play(Sfx.Popup, 0.8f);
            var card = panel.transform.Find("Card");
            if (card) card.gameObject.AddComponent<PopAnim>();
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            IsOpen = false;
        }

        void Refresh()
        {
            int cur = CurrentDay;
            bool can = CanClaim;
            for (int i = 0; i < 7; i++)
            {
                var c = cells[i];
                bool done = i < cur || (i == cur && !can);
                bool today = i == cur && can;
                Color fill = today ? UIKit.Gold : (done ? UIKit.Hex("D7F5D2") : Color.white);
                Color lip = today ? UIKit.GoldLip : (done ? UIKit.Hex("8FD487") : UIKit.CreamLip);
                UIKit.SetChunkColor(c.chunk, fill, lip);
                c.day.text = (i + 1) + ". GÜN";
                c.day.color = today ? Color.white : UIKit.TextSoft;
                c.amount.text = UIKit.Money(RewardFor(i));
                c.amount.color = today ? Color.white : UIKit.TextDark;
                c.check.gameObject.SetActive(done);
                c.coin.color = done ? new Color(1, 1, 1, 0.45f) : Color.white;
                if (today) c.punch.Punch(0.08f);
            }
            claimBtn.Root.gameObject.SetActive(can);
            doubleBtn.Root.gameObject.SetActive(can);
            if (can) claimText.text = "AL  +" + UIKit.Money(RewardFor(cur));
            infoText.text = can ? "Her gün gir, ödül büyüsün! Bir gün kaçırırsan seri baştan başlar."
                                : "Bugünkü ödülünü aldın. Yarın yine gel!";
        }

        void Claim(int multiplier)
        {
            if (!CanClaim) return;
            int cur = CurrentDay;
            int amount = RewardFor(cur) * multiplier;
            PlayerPrefs.SetString(LastKey, Today);
            PlayerPrefs.SetInt(StreakKey, (cur + 1) % 7);
            GameManager.I.AddMoney(amount);
            GameManager.I.Save();
            AudioFx.Play(Sfx.Coin);
            AudioFx.Play(Sfx.Unlock, 0.6f);
            Haptics.Medium();
            Refresh();
            cells[cur].punch.Punch(0.25f);
            StartCoroutine(CloseSoon());
        }

        IEnumerator CloseSoon()
        {
            yield return new WaitForSecondsRealtime(1.1f);
            Close();
        }

        // ---------------- Arayüz ----------------

        void BuildUI()
        {
            var canvas = UIKit.MakeCanvas("DailyCanvas", transform, 45);          // pencere: tabletin ve hoş geldin penceresinin üstünde
            var hudCanvas = UIKit.MakeCanvas("DailyHud", transform, 19);         // HUD butonu: pencerelerin altında

            // HUD butonu: tablet butonunun altında, mor, hediye kutusu
            var hud = UIKit.MakeButton("DailyButton", hudCanvas.transform, UIKit.Hex("A77BFF"), UIKit.Hex("7A4FD6"), 56f, () => { if (IsOpen) Close(); else Open(); }, 5f, 9f);
            UIKit.Place(hud.Rect, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-82, -316), new Vector2(112, 116));
            var gi = UIKit.Img("Gift", hud.Face.transform, UIKit.Icon("hud_hediye"), Color.white);
            gi.preserveAspect = true;
            UIKit.Stretch(gi.rectTransform, 14, 12, 14, 12);
            hudBadge = MakeBadge(hud.Rect);
            UIKit.HudBlockers.Add(hud.Rect);
            hudRoot = hud.Root.gameObject;

            var p = UIKit.MakePopup(canvas.transform, "Dim", "GÜNLÜK ÖDÜL", UIKit.Hex("A77BFF"), UIKit.Hex("7A4FD6"), new Vector2(860, 980), 10f);
            panel = p.Dim.gameObject;
            p.Card.name = "Card";
            p.Dim.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            MakeCloseButton(p.Card, Close);

            // 7 gün: üstte 4, altta 3 (7. gün geniş ve büyük ödül)
            float cw = 172f, ch = 236f, gap = 16f;
            for (int i = 0; i < 7; i++)
            {
                bool big = i == 6;
                var c = new Cell();
                c.chunk = UIKit.MakeChunk("Day" + (i + 1), p.Body, Color.white, UIKit.CreamLip, 40f, 6f, 10f, false);
                Vector2 pos, size;
                if (i < 4) { pos = new Vector2((i - 1.5f) * (cw + gap), -150); size = new Vector2(cw, ch); }
                else if (!big) { pos = new Vector2((i - 4 - 1.5f) * (cw + gap), -150 - ch - gap); size = new Vector2(cw, ch); }
                else { pos = new Vector2(cw + gap, -150 - ch - gap); size = new Vector2(cw * 2 + gap, ch); }   // son iki sütunu kaplar
                UIKit.Place(c.chunk.Rect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), pos, size);
                var face = c.chunk.Face.transform;
                c.day = UIKit.Label("Day", face, "", 30, UIKit.TextSoft, TextAnchor.MiddleCenter, false, true);
                UIKit.Place(c.day.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(size.x, 44));
                c.coin = UIKit.Img("Coin", face, big ? UIKit.Icon("hud_hediye") : UIKit.Coin, Color.white);
                c.coin.preserveAspect = true;
                UIKit.Place(c.coin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), big ? new Vector2(120, 120) : new Vector2(92, 92));
                c.amount = UIKit.Label("Amount", face, "", 38, UIKit.TextDark, TextAnchor.MiddleCenter);
                UIKit.Place(c.amount.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(size.x, 52));
                c.check = UIKit.Img("Check", c.chunk.Rect, UIKit.Circle, UIKit.Ink);
                UIKit.Place(c.check.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-14, -14), new Vector2(60, 60));
                var cIn = UIKit.Img("Fill", c.check.transform, UIKit.Circle, UIKit.Green);
                UIKit.Stretch(cIn.rectTransform, 6, 6, 6, 6);
                var tick = UIKit.Img("T", cIn.transform, UIKit.Check, Color.white);
                UIKit.Stretch(tick.rectTransform, 9, 9, 9, 9);
                c.punch = c.chunk.Root.gameObject.AddComponent<UIPunch>();
                cells[i] = c;
            }

            infoText = UIKit.Label("Info", p.Body, "", 32, UIKit.TextSoft, TextAnchor.MiddleCenter, false, true);
            var irt = infoText.rectTransform;
            irt.anchorMin = new Vector2(0, 0); irt.anchorMax = new Vector2(1, 0); irt.pivot = new Vector2(0.5f, 0);
            irt.offsetMin = new Vector2(50, 196); irt.offsetMax = new Vector2(-50, 326);   // günlerin hemen altı

            claimBtn = UIKit.MakeButton("Claim", p.Body, UIKit.Yellow, UIKit.YellowLip, 62f, () => Claim(1), 6f, 12f);
            UIKit.Place(claimBtn.Rect, new Vector2(0.27f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(360, 140));
            claimText = UIKit.Label("T", claimBtn.Face.transform, "AL", 44, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(claimText.rectTransform, 6, -4, 6, 0);
            claimText.resizeTextForBestFit = true; claimText.resizeTextMinSize = 28; claimText.resizeTextMaxSize = 44;

            doubleBtn = MakeAdButton(p.Body, "2x AL", () => AdService.ShowRewarded("daily_x2", ok => { if (ok) Claim(2); }));
            UIKit.Place(doubleBtn.Rect, new Vector2(0.73f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(360, 140));
        }

        /// <summary>Köşede zıplayan kırmızı "!" rozeti (alınacak bir şey var).</summary>
        public static RectTransform MakeBadge(RectTransform parent)
        {
            var badge = UIKit.Img("Badge", parent, UIKit.Circle, UIKit.Ink);
            UIKit.Place(badge.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-8, -10), new Vector2(48, 48));
            var bIn = UIKit.Img("Fill", badge.transform, UIKit.Circle, UIKit.Red);
            UIKit.Stretch(bIn.rectTransform, 6, 6, 6, 6);
            var t = UIKit.Label("T", bIn.transform, "!", 30, Color.white);
            UIKit.Stretch(t.rectTransform, 0, 2, 0, 0);
            return badge.rectTransform;
        }

        /// <summary>Kartın sağ üst köşesinde kırmızı X.</summary>
        public static void MakeCloseButton(RectTransform card, Action onClick)
        {
            var x = UIKit.MakeButton("Close", card, UIKit.Red, UIKit.RedLip, 50f, onClick, 6f, 10f);
            UIKit.Place(x.Rect, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-20, -10), new Vector2(104, 108));
            var ci = UIKit.Img("X", x.Face.transform, UIKit.Cross, Color.white);
            UIKit.Stretch(ci.rectTransform, 24, 22, 24, 22);
        }

        /// <summary>Yeşil "reklam izle" butonu (oynat simgesi + kırmızı REKLAM etiketi).</summary>
        public static Chunk MakeAdButton(RectTransform parent, string text, Action onClick)
        {
            var b = UIKit.MakeButton("Ad", parent, UIKit.Green, UIKit.GreenLip, 62f, onClick, 6f, 12f);
            var play = UIKit.Img("Play", b.Face.transform, UIKit.Circle, new Color(1, 1, 1, 0.95f));
            UIKit.Place(play.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(70, 70));
            var tri = UIKit.Img("Tri", play.transform, UIKit.PlayTri, UIKit.GreenLip);
            UIKit.Stretch(tri.rectTransform, 14, 14, 12, 14);
            var t = UIKit.Label("T", b.Face.transform, text, 48, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(t.rectTransform, 84, -4, 8, 0);
            var badge = UIKit.MakeChunk("Tag", b.Rect, UIKit.Red, UIKit.RedLip, 34f, 5f, 6f);
            UIKit.Place(badge.Rect, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-24, -6), new Vector2(110, 68));
            UIKit.Stretch(UIKit.Label("T", badge.Face.transform, "REKLAM", 24, Color.white, TextAnchor.MiddleCenter, true).rectTransform, 0, -2, 0, 0);
            return b;
        }
    }
}
