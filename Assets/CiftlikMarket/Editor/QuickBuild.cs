using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CiftlikMarket.EditorTools
{
    /// <summary>
    /// Telefonda hızlı deneme için Android build ayarları ve tek tıkla APK alma.
    /// Mağaza (yayın) build'i için ayrıca "Release" ayarlarına dönülmeli.
    /// </summary>
    public static class QuickBuild
    {
        const string OutPath = "Builds/CiftlikMarket_test.apk";

        [MenuItem("CiftlikMarket/Build/1 - Hızlı Test Ayarlarını Uygula")]
        public static void ApplyFastSettings()
        {
            var t = NamedBuildTarget.Android;
            PlayerSettings.SetScriptingBackend(t, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;          // tek mimari (ARMv7 yok) → yaklaşık yarı süre
            PlayerSettings.SetIl2CppCodeGeneration(t, Il2CppCodeGeneration.OptimizeSize);    // "Faster (smaller) builds"
            PlayerSettings.SetIl2CppCompilerConfiguration(t, Il2CppCompilerConfiguration.Debug); // C++ derleme en hızlı
            PlayerSettings.SetManagedStrippingLevel(t, ManagedStrippingLevel.Minimal);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = true;
            EditorUserBuildSettings.buildAppBundle = false;   // .aab değil doğrudan .apk
            EditorUserBuildSettings.development = false;
            Debug.Log("[CiftlikMarket] Hızlı test build ayarları uygulandı: IL2CPP + ARM64 + Faster builds + C++ Debug + APK.");
        }

        [MenuItem("CiftlikMarket/Build/2 - Test APK Al")]
        public static void BuildApk() { Build(false); }

        [MenuItem("CiftlikMarket/Build/3 - Test APK Al ve Telefonda Çalıştır (USB)")]
        public static void BuildAndRun() { Build(true); }

        static void Build(bool run)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.LogError("[CiftlikMarket] Aktif platform Android değil. File > Build Profiles'tan Android'e geç.");
                return;
            }
            ApplyFastSettings();
            Directory.CreateDirectory("Builds");
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutPath,
                target = BuildTarget.Android,
                options = BuildOptions.CompressWithLz4 | (run ? BuildOptions.AutoRunPlayer : BuildOptions.None),
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            if (s.result == BuildResult.Succeeded)
                Debug.Log("[CiftlikMarket] Build tamam: " + OutPath + "  (" + (s.totalSize / (1024f * 1024f)).ToString("0.0") + " MB, " + s.totalTime.TotalMinutes.ToString("0.0") + " dk)");
            else
                Debug.LogError("[CiftlikMarket] Build başarısız: " + s.result + " (" + s.totalErrors + " hata)");
        }
    }
}
