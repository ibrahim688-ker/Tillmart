using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>Şubeler arası geçiş: ekran kararır, gidilen şubenin adı görünür, yeni sahne yüklenince açılır.</summary>
    public class TravelFade : MonoBehaviour
    {
        CanvasGroup group;
        Action load;
        bool loaded;

        public static void Go(string title, Action load)
        {
            var go = new GameObject("TravelFade");
            DontDestroyOnLoad(go);
            var f = go.AddComponent<TravelFade>();
            f.load = load;
            f.Build(title);
            f.StartCoroutine(f.Run());
        }

        void Build(string title)
        {
            var canvas = UIKit.MakeCanvas("Canvas", transform, 300);
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = true;
            var bg = UIKit.Img("Bg", canvas.transform, UIKit.Gradient(UIKit.Hex("2E4A45"), UIKit.Hex("16201F")), Color.white);
            UIKit.Stretch(bg.rectTransform);
            bg.raycastTarget = true;
            var pin = UIKit.Img("Pin", canvas.transform, UIKit.Pin, UIKit.Hex("FFD84D"));
            UIKit.Place(pin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(120, 120));
            pin.gameObject.AddComponent<UIPunch>();
            var t = UIKit.Label("Title", canvas.transform, title, 64, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1000, 120));
            var s = UIKit.Label("Sub", canvas.transform, "yola çıkılıyor...", 38, UIKit.Hex("BFD8D2"), TextAnchor.MiddleCenter, false, true);
            UIKit.Place(s.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(800, 60));
        }

        IEnumerator Run()
        {
            AudioFx.Play(Sfx.Popup, 0.8f);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.35f) { group.alpha = t; yield return null; }
            group.alpha = 1f;
            GetComponentInChildren<UIPunch>()?.Punch(0.2f);
            yield return new WaitForSecondsRealtime(0.25f);
            SceneManager.sceneLoaded += OnLoaded;
            load?.Invoke();
            while (!loaded) yield return null;
            yield return null; yield return null;          // yeni sahne kurulsun
            yield return new WaitForSecondsRealtime(0.3f);
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.5f) { group.alpha = t; yield return null; }
            Destroy(gameObject);
        }

        void OnLoaded(Scene s, LoadSceneMode m) { loaded = true; SceneManager.sceneLoaded -= OnLoaded; }
    }
}
