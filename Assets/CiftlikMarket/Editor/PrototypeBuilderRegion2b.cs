using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CiftlikMarket.EditorTools
{
    public static partial class PrototypeBuilder
    {
        /// <summary>
        /// Bölge 2 ikinci parça: drive-thru görevlisi, motosiklet + minibüs araçları, araç sırası aralığı,
        /// bakıcı sınırı 5. Sahneyi baştan kurmaz; tekrar çalıştırmak güvenlidir.
        /// </summary>
        [MenuItem("CiftlikMarket/Add Region 2 - Part B (sahneye ekle)")]
        public static void AddRegion2PartBMenu()
        {
            mats.Clear();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bool added = AddRegion2PartBToScene();
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(added ? "[CiftlikMarket] Bölge 2 / Part B eklendi." : "[CiftlikMarket] Part B zaten var; sadece ayarlar güncellendi.");
        }

        static bool AddRegion2PartBToScene()
        {
            var world = FindInScene("World");
            if (world == null) { Debug.LogError("[CiftlikMarket] 'World' bulunamadı."); return false; }
            var market = world.Find("Market");
            var uzRoot = world.Find("UnlockZones");

            // Bakıcı sınırı: 5 (1 + 4 seviye)
            var upSayi = world.Find("Upgrades/Up_bakici_sayi");
            if (upSayi != null) { var u = upSayi.GetComponent<Upgrade>(); u.maxLevel = 4; EditorUtility.SetDirty(u); }

            var dtTr = market.Find("DriveThru");
            var dt = dtTr != null ? dtTr.GetComponent<DriveThru>() : null;
            if (dt == null) { Debug.LogError("[CiftlikMarket] DriveThru bulunamadı."); return false; }

            // Araç sırası aralığı (minibüs uzun)
            for (int i = 0; i < dt.queuePoints.Length; i++)
            {
                var p = dt.queuePoints[i].localPosition;
                dt.queuePoints[i].localPosition = new Vector3(p.x, p.y, i * 4.2f);
            }

            if (world.Find("Calisan_Gorevli") != null) { EditorUtility.SetDirty(dt); return false; }

            // ---------- Araçlar ----------
            dt.motoPrefab = VehiclePrefab("Moto", MotoModel, 2.2f, new Vector3(0, 0.95f, -0.45f), 2, 2, 4);
            dt.vanPrefab = VehiclePrefab("Minibus", VanModel, 3.0f, new Vector3(0, 1.85f, -0.3f), 4, 3, 14);
            EditorUtility.SetDirty(dt);

            // ---------- Drive-thru görevlisi ----------
            var checkoutTr = market.Find("Kasa");
            var checkout = checkoutTr != null ? checkoutTr.GetComponent<Checkout>() : null;
            var gorevli = WorkerB("Calisan_Gorevli", world, new Vector3(-3.2f, 0, 11.2f), Worker.Role.DriveThru, checkout, Hex("E53935"), "GÖREVLİ");
            var w = gorevli.GetComponent<Worker>();
            w.driveThru = dt;
            w.baseCapacity = 4;
            w.baseSpeed = 3.5f;
            w.speed = 3.5f;
            w.carrier.stack.capacity = 4;
            w.idlePoint = new Vector3(-5.6f, 0, 12.2f);

            var zGorevli = Unlock(uzRoot, "gorevli", "DRIVE-THRU\nGÖREVLİSİ", 1200, new Vector3(-3.2f, 0, 11.2f), new Vector2(1.8f, 1.8f), new[] { gorevli }, null);
            zGorevli.gameObject.SetActive(false);

            // Mandıra bölgesi alınınca belirir
            var zExp2 = uzRoot.Find("Unlock_genisleme_2");
            if (zExp2 != null)
            {
                var u2 = zExp2.GetComponent<UnlockZone>();
                var next = new List<GameObject>(u2.nextZones ?? new GameObject[0]) { zGorevli.gameObject };
                u2.nextZones = next.ToArray();
                EditorUtility.SetDirty(u2);
            }
            return true;
        }

        /// <summary>DriveThruCar bileşenli araç prefabı (gövde + sabır barı + sipariş kasası).</summary>
        static GameObject VehiclePrefab(string name, System.Func<Transform, GameObject> model, float barY, Vector3 trunkPos, int cols, int rows, int cap)
        {
            var root = new GameObject(name);
            var car = root.AddComponent<DriveThruCar>();
            var body = model(root.transform);

            var trunkGo = Empty("Trunk", root.transform, trunkPos);
            var tc = trunkGo.AddComponent<ItemContainer>();
            tc.capacity = cap; tc.layout = StackLayout.Grid; tc.gridColumns = cols; tc.gridRows = rows; tc.spacingX = 0.36f; tc.spacingZ = 0.42f; tc.layerHeight = 0.3f;

            var bar = Empty("PatienceBar", root.transform, new Vector3(0, barY, 0));
            bar.AddComponent<Billboard>();
            Box("BarBG", bar.transform, Vector3.zero, new Vector3(2.1f, 0.3f, 0.04f), M("BarArka", Hex("263238")));
            var pivot = Empty("FillPivot", bar.transform, new Vector3(-1.0f, 0, -0.03f));
            var fill = Box("Fill", pivot.transform, new Vector3(1.0f, 0, 0), new Vector3(2.0f, 0.2f, 0.04f), M("BarDolum", Color.white));

            car.trunk = tc;
            car.body = body.GetComponent<Renderer>();
            car.barRoot = bar;
            car.barFill = pivot.transform;
            car.barRenderer = fill.GetComponent<Renderer>();
            car.label = Label(root.transform, new Vector3(0, barY + 0.5f, 0), "", 0.045f, Hex("D32F2F"));

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject MotoModel(Transform r)
        {
            var body = Box("Body", r, new Vector3(0, 0.55f, 0), new Vector3(0.45f, 0.4f, 1.3f), M("Araba", Color.white));
            var tire = M("Teker", Hex("212121"));
            Prim(PrimitiveType.Cylinder, "WheelF", r, new Vector3(0, 0.3f, 0.6f), new Vector3(0.55f, 0.06f, 0.55f), tire, new Vector3(0, 0, 90));
            Prim(PrimitiveType.Cylinder, "WheelB", r, new Vector3(0, 0.3f, -0.6f), new Vector3(0.55f, 0.06f, 0.55f), tire, new Vector3(0, 0, 90));
            Box("Handle", r, new Vector3(0, 0.95f, 0.5f), new Vector3(0.7f, 0.06f, 0.06f), M("CamCerceve", Hex("37474F")));
            Prim(PrimitiveType.Capsule, "Rider", r, new Vector3(0, 1.2f, 0.05f), new Vector3(0.45f, 0.45f, 0.45f), M("Surucu", Hex("455A64")));
            Prim(PrimitiveType.Sphere, "Helmet", r, new Vector3(0, 1.75f, 0.1f), new Vector3(0.38f, 0.38f, 0.38f), M("Kask", Hex("FDD835")));
            Box("Box", r, new Vector3(0, 0.85f, -0.45f), new Vector3(0.6f, 0.15f, 0.5f), M("MotoKasa", Hex("8D6E63")));
            return body;
        }

        static GameObject VanModel(Transform r)
        {
            var body = Box("Body", r, new Vector3(0, 1.0f, 0), new Vector3(1.6f, 1.4f, 3.4f), M("Araba", Color.white));
            var glass = M("ArabaCam", Hex("B3E5FC"));
            Box("WindowL", r, new Vector3(-0.81f, 1.3f, 0.1f), new Vector3(0.02f, 0.45f, 2.6f), glass);
            Box("WindowR", r, new Vector3(0.81f, 1.3f, 0.1f), new Vector3(0.02f, 0.45f, 2.6f), glass);
            Box("Windshield", r, new Vector3(0, 1.3f, 1.71f), new Vector3(1.4f, 0.5f, 0.02f), glass);
            var tire = M("Teker", Hex("212121"));
            foreach (var p in new[] { new Vector3(-0.82f, 0.32f, 1.1f), new Vector3(0.82f, 0.32f, 1.1f), new Vector3(-0.82f, 0.32f, -1.1f), new Vector3(0.82f, 0.32f, -1.1f) })
                Prim(PrimitiveType.Cylinder, "Wheel", r, p, new Vector3(0.6f, 0.08f, 0.6f), tire, new Vector3(0, 0, 90));
            Box("Stripe", r, new Vector3(0, 0.75f, 0), new Vector3(1.62f, 0.12f, 3.42f), M("MinibusSerit", Hex("FFC107")));
            return body;
        }
    }
}
