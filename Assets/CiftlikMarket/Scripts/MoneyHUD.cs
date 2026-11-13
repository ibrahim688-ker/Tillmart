using UnityEngine;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>
    /// Sağ üstteki para göstergesi: krem renkli tombul hap + solda taşan büyük altın para.
    /// Sayı hedefe doğru sayarak akar; para artınca altın zıplar.
    /// </summary>
    public class MoneyHUD : MonoBehaviour
    {
        public static RectTransform CoinRect { get; private set; }

        Text text;
        UIPunch coinPunch, textPunch;
        float shown = -1f;
        int last = -1;

        void Awake()
        {
            var old = transform.Find("MoneyPanel");
            if (old) old.gameObject.SetActive(false);     // eski gösterge saklı kalsın
            if (GameManager.I && GameManager.I.moneyText) GameManager.I.moneyText = null;
            Build();
        }

        void Build()
        {
            var root = UIKit.Node("MoneyHUD", transform);
            UIKit.Place(root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -34), new Vector2(290, 84));

            var pill = UIKit.MakeChunk("Pill", root, UIKit.Cream, UIKit.CreamLip, 42f, 5f, 8f);
            UIKit.Stretch(pill.Rect, 38, 0, 0, 0);

            text = UIKit.Label("Amount", pill.Face.transform, "0", 46, UIKit.TextDark, TextAnchor.MiddleRight);
            UIKit.Stretch(text.rectTransform, 68, -4, 20, 0);
            textPunch = text.gameObject.AddComponent<UIPunch>();

            // altın para (gölgesiyle)
            var shadow = UIKit.Img("CoinShadow", root, UIKit.Circle, new Color(0, 0, 0, 0.22f));
            UIKit.Place(shadow.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(52, -7), new Vector2(96, 96));
            var coin = UIKit.Img("Coin", root, UIKit.Coin, Color.white);
            UIKit.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(48, -1), new Vector2(104, 104));
            var lira = UIKit.Label("Lira", coin.transform, "₺", 50, UIKit.Hex("B5700E"));
            UIKit.Stretch(lira.rectTransform, 0, 4, 0, 0);
            CoinRect = coin.rectTransform;
            coinPunch = coin.gameObject.AddComponent<UIPunch>();

            // ses / müzik aç-kapa (tablet butonunun altında)
            // ses/müzik ayarları tabletteki AYARLAR sayfasında
        }

        Toggle musicBtn, sfxBtn;

        class Toggle { public Chunk chunk; public Image icon, slash; }

        Toggle MakeToggle(string name, Sprite sprite, Vector2 pos, System.Action onClick)
        {
            var t = new Toggle();
            t.chunk = UIKit.MakeButton(name, transform, UIKit.Cream, UIKit.CreamLip, 40f, onClick, 5f, 8f);
            UIKit.Place(t.chunk.Rect, new Vector2(1, 1), new Vector2(0.5f, 0.5f), pos, new Vector2(78, 82));
            t.icon = UIKit.Img("Icon", t.chunk.Face.transform, sprite, UIKit.TextDark);
            UIKit.Stretch(t.icon.rectTransform, 13, 12, 13, 12);
            t.slash = UIKit.Img("Slash", t.chunk.Face.transform, UIKit.Round, UIKit.Red, 62f / 5f);
            UIKit.Place(t.slash.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 10));
            t.slash.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
            UIKit.HudBlockers.Add(t.chunk.Rect);
            return t;
        }

        void RefreshToggles()
        {
            Set(musicBtn, AudioFx.MusicOn);
            Set(sfxBtn, AudioFx.SfxOn);
        }

        static void Set(Toggle t, bool on)
        {
            if (t == null) return;
            UIKit.SetChunkColor(t.chunk, on ? UIKit.Cream : UIKit.Gray, on ? UIKit.CreamLip : UIKit.GrayLip);
            t.icon.color = on ? UIKit.TextDark : UIKit.Hex("5B6270");
            t.slash.gameObject.SetActive(!on);
        }

        void OnDestroy()
        {
            if (musicBtn != null) UIKit.HudBlockers.Remove(musicBtn.chunk.Rect);
            if (sfxBtn != null) UIKit.HudBlockers.Remove(sfxBtn.chunk.Rect);
        }

        void Update()
        {
            if (GameManager.I == null) return;
            int money = GameManager.I.Money;
            if (shown < 0f) shown = money;
            if (last >= 0 && money > last) { coinPunch.Punch(0.18f); textPunch.Punch(0.08f); }
            last = money;

            // sayarak akış
            float diff = money - shown;
            if (Mathf.Abs(diff) < 1f) shown = money;
            else shown += diff * Mathf.Min(1f, Time.unscaledDeltaTime * 9f) + Mathf.Sign(diff);
            if ((diff > 0 && shown > money) || (diff < 0 && shown < money)) shown = money;
            text.text = UIKit.Money(Mathf.RoundToInt(shown));
        }
    }
}
