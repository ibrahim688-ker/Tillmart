using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CiftlikMarket.EditorTools
{
    public static partial class PrototypeBuilder
    {
        /// <summary>
        /// Şube sistemi: bilgisayar menüsüne "YENİ ŞUBE AÇ" ekler. Poğaça ve salça rafları açılınca görünür.
        /// Sahneyi baştan kurmaz; tekrar çalıştırmak güvenlidir.
        /// </summary>
        [MenuItem("CiftlikMarket/Add Branch System (sahneye ekle)")]
        public static void AddBranchMenu()
        {
            mats.Clear();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bool added = AddBranchToScene();
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(added ? "[CiftlikMarket] Şube sistemi eklendi." : "[CiftlikMarket] Şube sistemi zaten var.");
        }

        static bool AddBranchToScene()
        {
            var world = FindInScene("World");
            if (world == null) return false;
            var uzRoot = world.Find("UnlockZones");
            if (uzRoot == null || uzRoot.Find("Unlock_yeni_sube") != null) return false;

            var z = Unlock(uzRoot, "yeni_sube", "YENİ ŞUBE AÇ", 20000, new Vector3(28f, 0, 4f), new Vector2(2f, 2f), new GameObject[0], null);
            z.officeOnly = true;
            z.opensNewBranch = true;
            z.requiresUnlocked = new[] { "raf_pogaca", "raf_salca" };
            z.description = "Market sıfırlanır; kalıcı +%25 gelir kazanırsın.";
            foreach (var r in z.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            z.gameObject.SetActive(false);

            foreach (var id in new[] { "raf_pogaca", "raf_salca" })
            {
                var t = uzRoot.Find("Unlock_" + id);
                if (t == null) continue;
                var u = t.GetComponent<UnlockZone>();
                var next = new List<GameObject>(u.nextZones ?? new GameObject[0]) { z.gameObject };
                u.nextZones = next.ToArray();
                EditorUtility.SetDirty(u);
            }
            return true;
        }
    }
}
