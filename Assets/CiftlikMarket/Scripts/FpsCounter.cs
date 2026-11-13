using UnityEngine;
using UnityEngine.UI;

namespace CiftlikMarket
{
    /// <summary>
    /// Ekranın sol altında FPS göstergesi (telefonda kasma testi için). Ayarlardan açılıp kapanır.
    /// Sahneye eklemeye gerek yok: oyun başlarken kendini kurar. Ortalama ve son 1 sn'deki en kötü kare gösterilir.
    /// </summary>
    public class FpsCounter : MonoBehaviour
    {
        const string Key = "cmg_fps";
        static FpsCounter inst;
        Text text;
        GameObject root;
        int frames;
        float acc, worst, timer;
        // Kare süresinin nereye gittiği (CPU ana iş parçacığı / çizim iş parçacığı / GPU). Player Settings'te
        // "Frame Timing Stats" açık olmalı; destekleyen cihazlarda gösterilir.
        readonly FrameTiming[] ft = new FrameTiming[1];
        double sumMain, sumRender, sumGpu;
        int ftCount;

        public static bool Visible
        {
            get => PlayerPrefs.GetInt(Key, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(Key, value ? 1 : 0);
                PlayerPrefs.Save();
                if (inst != null && inst.root != null) inst.root.SetActive(value);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("FpsCounter");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<FpsCounter>();
        }

        void Awake()
        {
            // Mobilde 30'a kilitli kalmasın (Android varsayılanı 30)
            QualitySettings.vSyncCount = 0;
            FramePacing.Apply();

            var canvasGo = new GameObject("FpsCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            root = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvasGo.transform, false);
            var bg = root.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            bg.raycastTarget = false;
            var rt = (RectTransform)root.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(16f, 16f);
            rt.sizeDelta = new Vector2(340f, 96f);

            var tgo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            tgo.transform.SetParent(root.transform, false);
            text = tgo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 30;
            text.supportRichText = true;
            text.lineSpacing = 0.9f;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.color = Color.white;
            var trt = (RectTransform)tgo.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
            text.text = "FPS --";
            root.SetActive(Visible);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, ft) > 0)
            {
                sumMain += ft[0].cpuMainThreadFrameTime;
                sumRender += ft[0].cpuRenderThreadFrameTime;
                sumGpu += ft[0].gpuFrameTime;
                ftCount++;
            }
            frames++; acc += dt; timer += dt;
            if (dt > worst) worst = dt;
            if (timer < 0.5f) return;
            if (root.activeSelf)
            {
                int fps = Mathf.RoundToInt(frames / acc);
                int low = Mathf.RoundToInt(1f / Mathf.Max(worst, 0.0001f));
                string col = fps >= 50 ? "#7CFF8A" : (fps >= 30 ? "#FFD54A" : "#FF6B6B");
                string line2 = "";
                if (ftCount > 0)
                    line2 = "\n<size=22>CPU " + (sumMain / ftCount).ToString("0.0") + "  Çizim " + (sumRender / ftCount).ToString("0.0")
                          + "  GPU " + (sumGpu > 0 ? (sumGpu / ftCount).ToString("0.0") : "-") + " ms</size>";
                text.text = "<color=" + col + "><b>" + fps + "</b></color> FPS  <size=22>min " + low + "</size>" + line2;
            }
            frames = 0; acc = 0f; timer = 0f; worst = 0f;
            sumMain = sumRender = sumGpu = 0; ftCount = 0;
        }
    }
}
