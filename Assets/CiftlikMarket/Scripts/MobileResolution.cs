using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Telefonda ekran çözünürlüğünü 720 piksel genişliğe indirir (en-boy oranı korunur).
    /// Telefon testinde ekran kartı süresi ~31 ms çıktı (60 FPS için 16 ms gerekir): oyun piksel boyama
    /// gücüne takılıyordu. 1080×2400 yerine 720×1600 çizmek boyanan pikselleri yarıdan fazla azaltır;
    /// arayüz de bu çözünürlükte çizilir. Editörde ve zaten küçük ekranlarda hiçbir şey yapmaz.
    /// </summary>
    public static class MobileResolution
    {
        const int TargetWidth = 720;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            if (!Application.isMobilePlatform) return;
            int w = Display.main.systemWidth, h = Display.main.systemHeight;
            if (w <= 0 || h <= 0) { w = Screen.width; h = Screen.height; }
            int shortSide = Mathf.Min(w, h);
            if (shortSide <= TargetWidth) return;
            float k = (float)TargetWidth / shortSide;
            Screen.SetResolution(Mathf.RoundToInt(w * k), Mathf.RoundToInt(h * k), FullScreenMode.FullScreenWindow);
        }
    }
}
