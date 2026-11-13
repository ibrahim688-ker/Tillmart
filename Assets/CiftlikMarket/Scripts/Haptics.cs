using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Kısa titreşim. Android'de milisaniye ayarlı (Handheld.Vibrate çok uzun ve sert),
    /// editörde hiçbir şey yapmaz.
    /// </summary>
    public static class Haptics
    {
        const string Key = "cmg_haptics";
        static int enabled = -1;

        /// <summary>Ayarlardan açılıp kapanır; tüm şubelerde ortak ve kalıcı.</summary>
        public static bool Enabled
        {
            get { if (enabled < 0) enabled = PlayerPrefs.GetInt(Key, 1); return enabled == 1; }
            set { enabled = value ? 1 : 0; PlayerPrefs.SetInt(Key, enabled); PlayerPrefs.Save(); }
        }
        static bool neverTrue;

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
#endif

        public static void Light() { Pulse(12); }
        public static void Medium() { Pulse(28); }

        static void Pulse(long ms)
        {
            if (!Enabled) return;
            // Bu satır hiç çalışmaz; Unity'nin Android VIBRATE iznini otomatik eklemesi için gerekli.
            if (neverTrue) Handheld.Vibrate();
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (vibrator == null)
                {
                    using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        var activity = up.GetStatic<AndroidJavaObject>("currentActivity");
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    }
                }
                vibrator?.Call("vibrate", ms);
            }
            catch (System.Exception) { }
#endif
        }
    }
}
