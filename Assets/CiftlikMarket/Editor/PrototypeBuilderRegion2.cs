using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace CiftlikMarket.EditorTools
{
    public static partial class PrototypeBuilder
    {
        /// <summary>
        /// Bölge 2 (Mandıra) içeriğini açık sahneye EKLER: Genişletme 2 (bölge kapısı), yoğurt/ayran/peynir/tereyağı
        /// makineleri, 4 raf, İnek 3, kilit zinciri, yükseltmeler. Sahneyi baştan kurmaz; zaten eklenmişse bir şey yapmaz.
        /// </summary>
        [MenuItem("CiftlikMarket/Add Region 2 - Mandira (sahneye ekle)")]
        public static void AddRegion2Menu()
        {
            mats.Clear();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bool added = AddRegion2ToScene();
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(added ? "[CiftlikMarket] Bölge 2 (Mandıra) eklendi." : "[CiftlikMarket] Bölge 2 zaten var, bir şey eklenmedi.");
        }

        static bool AddRegion2ToScene()
        {
            var world = FindInScene("World");
            if (world == null) { Debug.LogError("[CiftlikMarket] 'World' bulunamadı."); return false; }
            var market = world.Find("Market");
            var farm = world.Find("Farm");
            var uzRoot = world.Find("UnlockZones");
            if (market.Find("Genisleme_2") != null) return false;

            // ---------- Ürünler ----------
            var pYogurt = ItemPrefab("Yogurt", ItemType.Yogurt, r =>
            {
                Prim(PrimitiveType.Cylinder, "Cup", r, new Vector3(0, 0.1f, 0), new Vector3(0.24f, 0.1f, 0.24f), M("YogurtKap", Hex("FAFAFA")));
                Prim(PrimitiveType.Cylinder, "Lid", r, new Vector3(0, 0.21f, 0), new Vector3(0.25f, 0.01f, 0.25f), M("YogurtKapak", Hex("42A5F5")));
            });
            var pAyran = ItemPrefab("Ayran", ItemType.Ayran, r =>
            {
                Prim(PrimitiveType.Cylinder, "Cup", r, new Vector3(0, 0.14f, 0), new Vector3(0.18f, 0.14f, 0.18f), M("AyranKap", Hex("FFFFFF")));
                Prim(PrimitiveType.Cylinder, "Band", r, new Vector3(0, 0.14f, 0), new Vector3(0.185f, 0.04f, 0.185f), M("AyranBant", Hex("26C6DA")));
                Prim(PrimitiveType.Cylinder, "Lid", r, new Vector3(0, 0.29f, 0), new Vector3(0.19f, 0.01f, 0.19f), M("AyranKapak", Hex("26C6DA")));
            });
            var pPeynir = ItemPrefab("Peynir", ItemType.Peynir, r =>
            {
                Box("Block", r, new Vector3(0, 0.08f, 0), new Vector3(0.34f, 0.16f, 0.26f), M("Peynir", Hex("FFF8E1")));
                Box("Label", r, new Vector3(0, 0.08f, 0.131f), new Vector3(0.2f, 0.08f, 0.005f), M("PeynirEtiket", Hex("66BB6A")));
            });
            var pTereyag = ItemPrefab("Tereyag", ItemType.Tereyag, r =>
            {
                Box("Block", r, new Vector3(0, 0.07f, 0), new Vector3(0.3f, 0.14f, 0.2f), M("Tereyag", Hex("FFD54F")));
                Box("Paper", r, new Vector3(0, 0.07f, 0), new Vector3(0.12f, 0.145f, 0.205f), M("TereyagKagit", Hex("FFFFFF")));
            });

            var cfgGo = FindInScene("GameConfig");
            var cfg = cfgGo != null ? cfgGo.GetComponent<GameConfig>() : null;
            if (cfg != null)
            {
                AddDef(cfg, ItemType.Yogurt, "Yoğurt", 7, 0.24f, pYogurt);
                AddDef(cfg, ItemType.Ayran, "Ayran", 10, 0.3f, pAyran);
                AddDef(cfg, ItemType.Peynir, "Peynir", 14, 0.17f, pPeynir);
                AddDef(cfg, ItemType.Tereyag, "Tereyağı", 21, 0.15f, pTereyag);
                EditorUtility.SetDirty(cfg);
            }

            // ---------- Genişletme 2 (x 14..23) ----------
            var floorMat = M("Zemin", Hex("F5D7A1"));
            var wall = M("Duvar", Hex("ECEFF1"));
            var stripe = M("DuvarSerit", Hex("4A90E2"));
            var exp2 = new GameObject("Genisleme_2").transform;
            exp2.SetParent(market, false);
            Box("Floor", exp2, new Vector3(18.5f, 0.01f, 9), new Vector3(9, 0.02f, 14), floorMat);
            Obstacle(Box("WallBack", exp2, new Vector3(18.65f, 0.6f, 16.15f), new Vector3(9.3f, 1.2f, 0.3f), wall));
            Box("WallBackStripe", exp2, new Vector3(18.65f, 1.15f, 16.0f), new Vector3(9.3f, 0.12f, 0.02f), stripe);
            Obstacle(Box("WallRight", exp2, new Vector3(23.15f, 0.6f, 9f), new Vector3(0.3f, 1.2f, 14.3f), wall));
            exp2.gameObject.SetActive(false);

            // ---------- Raflar ----------
            var rYogurt = ShelfB("Raf_Yogurt", market, new Vector3(15.5f, 0, 14.4f), ItemType.Yogurt, "YOĞURT", Hex("64B5F6"));
            var rAyran = ShelfB("Raf_Ayran", market, new Vector3(18.5f, 0, 14.4f), ItemType.Ayran, "AYRAN", Hex("26C6DA"));
            var rPeynir = ShelfB("Raf_Peynir", market, new Vector3(21.5f, 0, 14.4f), ItemType.Peynir, "PEYNİR", Hex("FFF176"));
            var rTereyag = ShelfB("Raf_Tereyag", market, new Vector3(18.5f, 0, 9.2f), ItemType.Tereyag, "TEREYAĞI", Hex("FFB300"));
            foreach (var s in new[] { rYogurt, rAyran, rPeynir, rTereyag })
            {
                Obstacle(s.transform.Find("Base"));
                Obstacle(s.transform.Find("Back"));
                s.gameObject.SetActive(false);
            }

            // ---------- Makineler (çiftliğin sağı) ve İnek 3 ----------
            var mYogurt = AnimalB("Makine_Yogurt", farm, new Vector3(13f, 0, -3.8f), ItemType.Sut, ItemType.Yogurt, m => MachineModel(m, Hex("90CAF9"), Hex("1E88E5"), "YOĞURT"));
            var mAyran = AnimalB("Makine_Ayran", farm, new Vector3(17f, 0, -3.8f), ItemType.Yogurt, ItemType.Ayran, m => MachineModel(m, Hex("80DEEA"), Hex("00ACC1"), "AYRAN"));
            var mPeynir = AnimalB("Makine_Peynir", farm, new Vector3(13f, 0, -9.8f), ItemType.Sut, ItemType.Peynir, m => MachineModel(m, Hex("FFE082"), Hex("FFB300"), "MANDIRA"));
            var mTereyag = AnimalB("Makine_Tereyag", farm, new Vector3(17f, 0, -9.8f), ItemType.Sut, ItemType.Tereyag, ChurnModel);
            mAyran.unitTime = 1f;
            mPeynir.unitTime = 1.5f; mPeynir.inputPerOutput = 2; mPeynir.inputCapacity = 6; mPeynir.inputPile.capacity = 6; mPeynir.inputPile.gridColumns = 3;
            mTereyag.unitTime = 2f; mTereyag.inputPerOutput = 3; mTereyag.inputCapacity = 6; mTereyag.inputPile.capacity = 6; mTereyag.inputPile.gridColumns = 3;
            var cow3 = AnimalB("Inek_3", farm, new Vector3(4.5f, 0, -10.3f), ItemType.Saman, ItemType.Sut, CowModel);
            foreach (var c in new[] { mYogurt, mAyran, mPeynir, mTereyag, cow3 })
            {
                var body = c.transform.Find("Model/Body");
                if (body) Obstacle(body);
                c.gameObject.SetActive(false);
            }

            // ---------- Kilit zinciri ----------
            // Genişletme 2 (bilgisayar, 2.500) → (Yoğurt mak. → Yoğurt rafı → Ayran mak. → Ayran rafı),
            //                                     (Mandıra → Peynir rafı → Yayık → Tereyağı rafı), İnek 3
            var zTereyagRaf = Unlock(uzRoot, "raf_tereyag", "TEREYAĞI RAFI", 800, new Vector3(18.5f, 0, 8.8f), new Vector2(2.2f, 1.8f), new[] { rTereyag.gameObject }, null);
            var zTereyagMak = Unlock(uzRoot, "mak_tereyag", "YAYIK", 1800, new Vector3(17f, 0, -9.0f), new Vector2(2.4f, 2.4f), new[] { mTereyag.gameObject }, new[] { zTereyagRaf.gameObject });
            var zPeynirRaf = Unlock(uzRoot, "raf_peynir", "PEYNİR RAFI", 600, new Vector3(21.5f, 0, 14f), new Vector2(2.2f, 1.8f), new[] { rPeynir.gameObject }, new[] { zTereyagMak.gameObject });
            var zPeynirMak = Unlock(uzRoot, "mak_peynir", "MANDIRA", 1200, new Vector3(13f, 0, -9.0f), new Vector2(2.4f, 2.4f), new[] { mPeynir.gameObject }, new[] { zPeynirRaf.gameObject });
            var zAyranRaf = Unlock(uzRoot, "raf_ayran", "AYRAN RAFI", 500, new Vector3(18.5f, 0, 14f), new Vector2(2.2f, 1.8f), new[] { rAyran.gameObject }, null);
            var zAyranMak = Unlock(uzRoot, "mak_ayran", "AYRAN MAKİNESİ", 900, new Vector3(17f, 0, -3.0f), new Vector2(2.4f, 2.4f), new[] { mAyran.gameObject }, new[] { zAyranRaf.gameObject });
            var zYogurtRaf = Unlock(uzRoot, "raf_yogurt", "YOĞURT RAFI", 400, new Vector3(15.5f, 0, 14f), new Vector2(2.2f, 1.8f), new[] { rYogurt.gameObject }, new[] { zAyranMak.gameObject });
            var zYogurtMak = Unlock(uzRoot, "mak_yogurt", "YOĞURT MAKİNESİ", 700, new Vector3(13f, 0, -3.0f), new Vector2(2.4f, 2.4f), new[] { mYogurt.gameObject }, new[] { zYogurtRaf.gameObject });
            var zCow3 = Unlock(uzRoot, "inek3", "İNEK 3", 600, new Vector3(4.5f, 0, -9.5f), new Vector2(2.4f, 2.4f), new[] { cow3.gameObject }, null);

            var zExp2 = Unlock(uzRoot, "genisleme_2", "MANDIRA BÖLGESİ", 2500, new Vector3(16f, 0, 9f), new Vector2(2.2f, 2.2f),
                new[] { exp2.gameObject }, new[] { zYogurtMak.gameObject, zPeynirMak.gameObject, zCow3.gameObject });
            zExp2.officeOnly = true;
            zExp2.description = "Market büyür; yoğurt, ayran, peynir ve tereyağı makineleri ile İnek 3 açılır.";
            var exp1Wall = market.Find("Genisleme_1/WallRight");
            if (exp1Wall != null) zExp2.removeTargets = new[] { exp1Wall.gameObject };
            foreach (var r in zExp2.GetComponentsInChildren<Renderer>(true)) r.enabled = false;

            foreach (var z in new[] { zTereyagRaf, zTereyagMak, zPeynirRaf, zPeynirMak, zAyranRaf, zAyranMak, zYogurtRaf, zYogurtMak, zCow3, zExp2 })
                z.gameObject.SetActive(false);

            // Bölge 2 kapısı, Genişletme 1 alınınca bilgisayarda belirir
            var uzExp1 = uzRoot.Find("Unlock_genisleme_1");
            if (uzExp1 != null)
            {
                var u1 = uzExp1.GetComponent<UnlockZone>();
                var next = new List<GameObject>(u1.nextZones ?? new GameObject[0]) { zExp2.gameObject };
                u1.nextZones = next.ToArray();
                EditorUtility.SetDirty(u1);
            }

            // ---------- Yükseltmeler (yem slotu) ----------
            var upRoot = world.Find("Upgrades");
            if (upRoot != null)
            {
                Up(upRoot, "slot_inek_3", "İNEK 3 · YEM SLOTU", UpgradeKind.StationSlot, 200, 2.2f, 3, 33).station = cow3;
                Up(upRoot, "slot_mak_yogurt", "YOĞURT MAK. · SLOT", UpgradeKind.StationSlot, 300, 2.2f, 3, 34).station = mYogurt;
                Up(upRoot, "slot_mak_ayran", "AYRAN MAK. · SLOT", UpgradeKind.StationSlot, 350, 2.2f, 3, 35).station = mAyran;
                Up(upRoot, "slot_mak_peynir", "MANDIRA · SLOT", UpgradeKind.StationSlot, 450, 2.2f, 3, 36).station = mPeynir;
                Up(upRoot, "slot_mak_tereyag", "YAYIK · SLOT", UpgradeKind.StationSlot, 600, 2.2f, 3, 37).station = mTereyag;
            }

            // Oyuncu genişleyen markette sağa kadar yürüyebilsin
            var player = FindInScene("Player");
            if (player != null)
            {
                var pc = player.GetComponent<PlayerController>();
                if (pc != null) { pc.boundsMax = new Vector2(22.6f, pc.boundsMax.y); EditorUtility.SetDirty(pc); }
            }
            return true;
        }

        static void AddDef(GameConfig cfg, ItemType t, string name, int price, float height, GameObject prefab)
        {
            var d = cfg.items.Find(x => x.type == t);
            if (d == null) { d = new ItemDef { type = t }; cfg.items.Add(d); }
            d.displayName = name; d.price = price; d.height = height; d.prefab = prefab;
        }

        static void Obstacle(GameObject go) { if (go) Obstacle(go.transform); }

        static void Obstacle(Transform t)
        {
            if (t == null) return;
            if (t.GetComponent<Collider>() == null) t.gameObject.AddComponent<BoxCollider>();
            var ob = t.GetComponent<NavMeshObstacle>();
            if (ob == null) ob = t.gameObject.AddComponent<NavMeshObstacle>();
            ob.shape = NavMeshObstacleShape.Box;
            ob.center = Vector3.zero;
            ob.size = Vector3.one;
            ob.carving = true;
            ob.carveOnlyStationary = true;
        }

        static void MachineModel(Transform m, Color body, Color accent, string title)
        {
            Box("Body", m, new Vector3(0, 0.6f, -0.25f), new Vector3(1.5f, 1.2f, 1.1f), M("Makine_" + title, body));
            Box("Panel", m, new Vector3(0.35f, 0.75f, 0.31f), new Vector3(0.5f, 0.4f, 0.02f), M("MakinePanel", Hex("37474F")));
            Box("Light", m, new Vector3(0.35f, 0.75f, 0.325f), new Vector3(0.12f, 0.12f, 0.01f), M("MakineIsik", Hex("76FF03")));
            Prim(PrimitiveType.Cylinder, "Tank", m, new Vector3(-0.3f, 1.45f, -0.3f), new Vector3(0.6f, 0.3f, 0.6f), M("MakineTank_" + title, accent));
            Box("Spout", m, new Vector3(-0.3f, 0.95f, 0.38f), new Vector3(0.15f, 0.15f, 0.2f), M("MakineMetal", Hex("B0BEC5")));
        }

        static void ChurnModel(Transform m)
        {
            var wood = M("Yayik", Hex("BCAAA4"));
            Prim(PrimitiveType.Cylinder, "Body", m, new Vector3(0, 0.6f, -0.25f), new Vector3(1.0f, 0.6f, 1.0f), wood);
            Prim(PrimitiveType.Cylinder, "Hoop1", m, new Vector3(0, 0.35f, -0.25f), new Vector3(1.04f, 0.04f, 1.04f), M("YayikCember", Hex("6D4C41")));
            Prim(PrimitiveType.Cylinder, "Hoop2", m, new Vector3(0, 0.95f, -0.25f), new Vector3(1.04f, 0.04f, 1.04f), M("YayikCember", Hex("6D4C41")));
            Prim(PrimitiveType.Cylinder, "Handle", m, new Vector3(0, 1.5f, -0.25f), new Vector3(0.08f, 0.35f, 0.08f), M("YayikSap", Hex("8D6E63")));
            Box("Grip", m, new Vector3(0, 1.85f, -0.25f), new Vector3(0.5f, 0.06f, 0.06f), M("YayikSap", Hex("8D6E63")));
        }
    }
}
