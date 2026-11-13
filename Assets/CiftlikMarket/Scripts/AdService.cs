using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>
    /// Ödüllü reklam için yer tutucu. Şimdilik 2 saniyelik "REKLAM (TEST)" ekranı gösterip ödülü verir.
    /// Gerçek reklam (Unity LevelPlay / AdMob) bağlanınca sadece ShowRewarded'ın içi değişecek;
    /// çağıran kodlar aynı kalacak. Yerleşim adları analitik için kullanılacak (ör. "offline_x2").
    /// </summary>
    public static class AdService
    {
        public static bool IsShowing { get; private set; }

        public static void ShowRewarded(string placement, Action<bool> onDone)
        {
            if (IsShowing) return;
            IsShowing = true;
            var go = new GameObject("AdTestOverlay");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<AdTestOverlay>().Run(placement, ok =>
            {
                IsShowing = false;
                onDone?.Invoke(ok);
            });
        }

        class AdTestOverlay : MonoBehaviour
        {
            public void Run(string placement, Action<bool> done) { StartCoroutine(Show(placement, done)); }

            IEnumerator Show(string placement, Action<bool> done)
            {
                var canvas = UIKit.MakeCanvas("Canvas", transform, 100);
                var bg = UIKit.Img("Bg", canvas.transform, UIKit.Gradient(UIKit.Hex("2E3440"), UIKit.Hex("14171D")), Color.white);
                bg.raycastTarget = true;
                UIKit.Stretch(bg.rectTransform);
                var card = UIKit.MakeChunk("Card", bg.transform, UIKit.Hex("3B4252"), UIKit.Hex("232832"), 60f, 7f, 14f, false);
                UIKit.Place(card.Rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 520));
                var t = UIKit.Label("Text", card.Face.transform, "", 56, Color.white, TextAnchor.MiddleCenter, true);
                UIKit.Stretch(t.rectTransform, 30, 0, 30, 0);
                for (int s = 2; s > 0; s--)
                {
                    t.text = "REKLAM (TEST)\n<size=34><color=#AAB2C0>" + placement + "</color></size>\n<size=110>" + s + "</size>";
                    yield return new WaitForSecondsRealtime(1f);
                }
                done(true);
                Destroy(gameObject);
            }
        }
    }
}
