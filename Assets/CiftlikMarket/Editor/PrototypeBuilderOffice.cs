using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CiftlikMarket.EditorTools
{
    public static partial class PrototypeBuilder
    {
        /// <summary>
        /// Açık sahneye yükseltme menüsünü (HUD'daki bilgisayar butonu) ve yükseltmeleri uygular.
        /// Eski ofis odası varsa kaldırır. Sahneyi baştan kurmaz; elle yapılmış düzenlemelere dokunmaz.
        /// Birden fazla çalıştırmak güvenlidir.
        /// </summary>
        [MenuItem("CiftlikMarket/Apply Upgrade Menu (sahneye uygula)")]
        public static void ApplyUpgradeMenu()
        {
            mats.Clear();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            AddOfficeToScene();
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CiftlikMarket] Yükseltme menüsü sahneye uygulandı.");
        }

        static bool AddOfficeToScene()
        {
            var world = FindInScene("World");
            if (world == null) { Debug.LogError("[CiftlikMarket] 'World' bulunamadı."); return false; }
            var market = world.Find("Market");
            var farm = world.Find("Farm");

            // Eski ofis odası artık yok (market sağa büyüdüğü için)
            var oldOffice = market.Find("Ofis");
            if (oldOffice != null) Object.DestroyImmediate(oldOffice.gameObject);

            // Yükseltme menüsü (HUD butonu + monitör ekranı çalışma anında kurulur)
            var ui = world.Find("OfficeUI");
            if (ui == null) { ui = new GameObject("OfficeUI").transform; ui.SetParent(world, false); }
            if (ui.GetComponent<OfficeUI>() == null) ui.gameObject.AddComponent<OfficeUI>();

            // Yükseltmeler
            var upRoot = world.Find("Upgrades");
            if (upRoot == null) { upRoot = new GameObject("Upgrades").transform; upRoot.SetParent(world, false); }
            Up(upRoot, "oyuncu_kapasite", "KAPASİTE", UpgradeKind.PlayerCapacity, 50, 1.25f, 8, 10);
            Up(upRoot, "oyuncu_hiz", "HIZ", UpgradeKind.PlayerSpeed, 80, 1.3f, 5, 20);
            int order = 30;
            foreach (var pair in new[] { new[] { "Inek_1", "İNEK 1 · YEM SLOTU" }, new[] { "Inek_2", "İNEK 2 · YEM SLOTU" }, new[] { "Kumes_1", "KÜMES · YEM SLOTU" } })
            {
                var a = farm != null ? farm.Find(pair[0]) : null;
                if (a == null) continue;
                Up(upRoot, "slot_" + pair[0].ToLowerInvariant(), pair[1], UpgradeKind.StationSlot, 120, 2.2f, 3, order++).station = a.GetComponent<Converter>();
            }
            var bakici = world.Find("Calisan_Bakici");
            if (bakici != null)
            {
                var w = bakici.GetComponent<Worker>();
                Up(upRoot, "bakici_kapasite", "BAKICI · KAPASİTE", UpgradeKind.WorkerCapacity, 200, 1.6f, 3, 40).workerTemplate = w;
                Up(upRoot, "bakici_hiz", "BAKICI · HIZ", UpgradeKind.WorkerSpeed, 150, 1.6f, 3, 41).workerTemplate = w;
                Up(upRoot, "bakici_sayi", "+1 BAKICI", UpgradeKind.WorkerCount, 600, 2f, 4, 42).workerTemplate = w;
            }

            Up(upRoot, "depo", "DEPO", UpgradeKind.OfflineCapacity, 1000, 2f, 3, 50);

            // Market genişletme bilgisayardan satın alınır (zeminde görünmez)
            var uzExp = world.Find("UnlockZones/Unlock_genisleme_1");
            if (uzExp != null)
            {
                var uz = uzExp.GetComponent<UnlockZone>();
                uz.officeOnly = true;
                uz.title = "MARKET GENİŞLETME";
                uz.description = "Market sağa doğru büyür, domates rafı ve bahçesi açılır.";
                foreach (var r in uzExp.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                EditorUtility.SetDirty(uz);
            }

            // Çöp kutusu: genişleme yönünden uzakta, arka duvarın sol tarafında
            var cop = market.Find("Cop");
            if (cop != null) cop.localPosition = new Vector3(-4.6f, 0, 15.3f);

            return true;
        }

        static Upgrade Up(Transform parent, string id, string title, UpgradeKind kind, int baseCost, float growth, int maxLevel, int order)
        {
            var existing = parent.Find("Up_" + id);
            var go = existing != null ? existing.gameObject : Empty("Up_" + id, parent, Vector3.zero);
            var u = go.GetComponent<Upgrade>();
            if (u == null) u = go.AddComponent<Upgrade>();
            u.id = id;
            u.title = title;
            u.kind = kind;
            u.baseCost = baseCost;
            u.growth = growth;
            u.maxLevel = maxLevel;
            u.sortOrder = order;
            return u;
        }

        static Transform FindInScene(string rootName)
        {
            foreach (var go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == rootName) return go.transform;
            return null;
        }
    }
}
