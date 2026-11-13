using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Kare hızı hedefi (60). Tek yerden ayarlanır; ileride telefon ısınınca otomatik düşürme buraya eklenecek.
    /// </summary>
    public static class FramePacing
    {
        const string Key = "cmg_battery";

        public static bool BatterySaver
        {
            get => PlayerPrefs.GetInt(Key, 0) == 1;
            set { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); Apply(); }
        }

        public static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;   // ayarlardaki pil tasarrufu kaldırıldı; ısınma ileride otomatik ele alınacak
        }
    }
}
