using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CiftlikMarket.EditorTools
{
    public static partial class PrototypeBuilder
    {
        /// <summary>
        /// Bölge 3 (Değirmen): Genişletme 3 (x 23..34), akarsu (doğu sınırı), çarkı suda dönen su değirmeni,
        /// buğday tarlaları, ikinci domates bahçesi, salça makinesi, market içinde fırın (un + tereyağı → poğaça),
        /// un/poğaça/salça rafları. Sahneyi baştan kurmaz; zaten eklenmişse bir şey yapmaz.
        /// </summary>
        [MenuItem("CiftlikMarket/Add Region 3 - Degirmen (sahneye ekle)")]
        public static void AddRegion3Menu()
        {
            mats.Clear();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bool added = AddRegion3ToScene();
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(added ? "[CiftlikMarket] Bölge 3 (Değirmen) eklendi." : "[CiftlikMarket] Bölge 3 zaten var, bir şey eklenmedi.");
        }

        static bool AddRegion3ToScene()
        {
            var world = FindInScene("World");
            if (world == null) { Debug.LogError("[CiftlikMarket] 'World' bulunamadı."); return false; }
            var market = world.Find("Market");
            var farm = world.Find("Farm");
            var uzRoot = world.Find("UnlockZones");
            if (market.Find("Genisleme_3") != null) return false;

            // ---------- Ürünler ----------
            var pBugday = ItemPrefab("Bugday", ItemType.Bugday, r =>
            {
                Prim(PrimitiveType.Cylinder, "Bundle", r, new Vector3(0, 0.1f, 0), new Vector3(0.22f, 0.1f, 0.22f), M("Bugday", Hex("E6B54A")));
                Prim(PrimitiveType.Cylinder, "Tie", r, new Vector3(0, 0.1f, 0), new Vector3(0.23f, 0.02f, 0.23f), M("BugdayBag", Hex("8D6E63")));
                Prim(PrimitiveType.Sphere, "Ears", r, new Vector3(0, 0.22f, 0), new Vector3(0.26f, 0.08f, 0.26f), M("BugdayBasak", Hex("F2CC6B")));
            });
            var pUn = ItemPrefab("Un", ItemType.Un, r =>
            {
                Box("Sack", r, new Vector3(0, 0.17f, 0), new Vector3(0.3f, 0.34f, 0.22f), M("UnCuval", Hex("F5F0E1")));
                Box("Label", r, new Vector3(0, 0.17f, 0.111f), new Vector3(0.18f, 0.12f, 0.005f), M("UnEtiket", Hex("8D6E63")));
                Box("Top", r, new Vector3(0, 0.35f, 0), new Vector3(0.2f, 0.04f, 0.14f), M("UnCuval", Hex("F5F0E1")));
            });
            var pPogaca = ItemPrefab("Pogaca", ItemType.Pogaca, r =>
            {
                Prim(PrimitiveType.Sphere, "Bun", r, new Vector3(0, 0.07f, 0), new Vector3(0.28f, 0.14f, 0.24f), M("Pogaca", Hex("E0A84E")));
                Prim(PrimitiveType.Sphere, "Seeds", r, new Vector3(0, 0.12f, 0), new Vector3(0.14f, 0.04f, 0.12f), M("PogacaSusam", Hex("FFF3D6")));
            });
            var pSalca = ItemPrefab("Salca", ItemType.Salca, r =>
            {
                Prim(PrimitiveType.Cylinder, "Jar", r, new Vector3(0, 0.12f, 0), new Vector3(0.2f, 0.12f, 0.2f), M("Salca", Hex("C62828")));
                Prim(PrimitiveType.Cylinder, "Lid", r, new Vector3(0, 0.25f, 0), new Vector3(0.21f, 0.015f, 0.21f), M("SalcaKapak", Hex("FDD835")));
            });
            var cfgGo = FindInScene("GameConfig");
            var cfg = cfgGo != null ? cfgGo.GetComponent<GameConfig>() : null;
            if (cfg != null)
            {
                AddDef(cfg, ItemType.Bugday, "Buğday", 0, 0.24f, pBugday);
                AddDef(cfg, ItemType.Un, "Un", 11, 0.37f, pUn);
                AddDef(cfg, ItemType.Pogaca, "Poğaça", 45, 0.14f, pPogaca);
                AddDef(cfg, ItemType.Salca, "Salça", 17, 0.27f, pSalca);
                EditorUtility.SetDirty(cfg);
            }

            // ---------- Akarsu (doğu sınırı) ----------
            Box("GrassEast", world, new Vector3(42f, -0.05f, 2f), new Vector3(16f, 0.1f, 64f), M("Cim", Hex("8BC34A")));
            var river = Empty("Akarsu", world, new Vector3(36.6f, 0, 4f)).transform;
            var waterMat = M("Su", Hex("4FC3F7"));
            waterMat.SetFloat("_Smoothness", 0.85f);
            var water = Box("Water", river, new Vector3(0, 0.02f, 0), new Vector3(3.2f, 0.06f, 62f), waterMat);
            Obstacle(water);
            var bank = M("Kiyi", Hex("8D6E63"));
            Box("BankW", river, new Vector3(-1.72f, 0.04f, 0), new Vector3(0.28f, 0.08f, 62f), bank);
            Box("BankE", river, new Vector3(1.72f, 0.04f, 0), new Vector3(0.28f, 0.08f, 62f), bank);
            var rock = M("Tas", Hex("9E9E9E"));
            foreach (var z in new[] { -18f, -9f, 3.5f, 14f, 22f })
                Prim(PrimitiveType.Sphere, "Rock", river, new Vector3(-1.8f, 0.12f, z - 4f), new Vector3(0.6f, 0.35f, 0.5f), rock);
            foreach (var z in new[] { -14f, 6f, 18f })
                Prim(PrimitiveType.Sphere, "Rock", river, new Vector3(1.85f, 0.12f, z - 4f), new Vector3(0.5f, 0.3f, 0.45f), rock);
            var reed = M("Saz", Hex("558B2F"));
            foreach (var z in new[] { -22f, -2f, 9f, 26f })
                for (int k = 0; k < 4; k++)
                    Box("Reed", river, new Vector3(-1.95f - 0.12f * (k % 2), 0.3f + 0.05f * k, z - 4f + 0.15f * k), new Vector3(0.05f, 0.6f + 0.1f * k, 0.05f), reed);
            var rippleMat = M("Dalga", Hex("E1F5FE"));
            var ripples = new List<Transform>();
            for (int i = 0; i < 16; i++)
                ripples.Add(Box("Ripple", river, new Vector3(0, 0.06f, -30f + i * 3.75f), new Vector3(1.1f, 0.012f, 0.12f), rippleMat).transform);
            var flow = river.gameObject.AddComponent<RiverFlow>();
            flow.ripples = ripples.ToArray();
            flow.zMin = -26f; flow.zMax = 34f;

            // ---------- Genişletme 3 (x 23..34) ----------
            var floorMat = M("Zemin", Hex("F5D7A1"));
            var wall = M("Duvar", Hex("ECEFF1"));
            var stripe = M("DuvarSerit", Hex("4A90E2"));
            var exp3 = new GameObject("Genisleme_3").transform;
            exp3.SetParent(market, false);
            Box("Floor", exp3, new Vector3(28.5f, 0.01f, 9), new Vector3(11, 0.02f, 14), floorMat);
            Obstacle(Box("WallBack", exp3, new Vector3(28.65f, 0.6f, 16.15f), new Vector3(11.3f, 1.2f, 0.3f), wall));
            Box("WallBackStripe", exp3, new Vector3(28.65f, 1.15f, 16.0f), new Vector3(11.3f, 0.12f, 0.02f), stripe);
            Obstacle(Box("WallRight", exp3, new Vector3(34.15f, 0.6f, 9f), new Vector3(0.3f, 1.2f, 14.3f), wall));
            exp3.gameObject.SetActive(false);

            // ---------- Raflar (orta sıra) ve fırın (arka duvarda) ----------
            var rUn = ShelfB("Raf_Un", market, new Vector3(25.5f, 0, 10f), ItemType.Un, "UN", Hex("D7CCC8"));
            var rPogaca = ShelfB("Raf_Pogaca", market, new Vector3(28.5f, 0, 10f), ItemType.Pogaca, "POĞAÇA", Hex("E0A84E"));
            var rSalca = ShelfB("Raf_Salca", market, new Vector3(31.5f, 0, 10f), ItemType.Salca, "SALÇA", Hex("E53935"));
            foreach (var s in new[] { rUn, rPogaca, rSalca })
            {
                Obstacle(s.transform.Find("Base"));
                Obstacle(s.transform.Find("Back"));
                s.gameObject.SetActive(false);
            }
            var firin = BakeryB(market, new Vector3(27.5f, 0, 14.6f));
            firin.gameObject.SetActive(false);

            // ---------- Çiftlik tarafı ----------
            var fBugday1 = FieldB("Tarla_Bugday_1", farm, new Vector3(23f, 0, -3f), ItemType.Bugday, "BUĞDAY");
            var fBugday2 = FieldB("Tarla_Bugday_2", farm, new Vector3(26.5f, 0, -3f), ItemType.Bugday, "BUĞDAY");
            var fDomates2 = FieldB("Tarla_Domates_2", farm, new Vector3(23f, 0, -8.5f), ItemType.Domates, "DOMATES BAHÇESİ 2");
            fBugday1.regrowTime = 6f; fBugday2.regrowTime = 6f; fDomates2.regrowTime = 6f;

            var mill = AnimalB("Degirmen", farm, new Vector3(31f, 0, -4.3f), ItemType.Bugday, ItemType.Un, MillModel);
            mill.unitTime = 1.5f; mill.inputPerOutput = 2; mill.inputCapacity = 6; mill.inputPile.capacity = 6; mill.inputPile.gridColumns = 3;
            var wheel = mill.transform.Find("Model/Wheel");
            if (wheel != null) wheel.GetComponent<WaterWheel>().mill = mill;

            var salca = AnimalB("Makine_Salca", farm, new Vector3(30.5f, 0, -10f), ItemType.Domates, ItemType.Salca, m => MachineModel(m, Hex("EF9A9A"), Hex("C62828"), "SALÇA"));
            salca.unitTime = 2f; salca.inputPerOutput = 3; salca.inputCapacity = 6; salca.inputPile.capacity = 6; salca.inputPile.gridColumns = 3;

            foreach (var c in new[] { mill, salca })
            {
                var body = c.transform.Find("Model/Body");
                if (body) Obstacle(body);
                c.gameObject.SetActive(false);
            }
            foreach (var f in new[] { fBugday1, fBugday2, fDomates2 }) f.gameObject.SetActive(false);

            // ---------- Kilit zinciri ----------
            // Değirmen bölgesi (bilgisayar, 6.000) → Buğday 1 → Su değirmeni → Un rafı → (Buğday 2, Fırın → Poğaça rafı)
            //                                       → Domates 2 → Salça makinesi → Salça rafı
            var zPogacaRaf = Unlock(uzRoot, "raf_pogaca", "POĞAÇA RAFI", 1500, new Vector3(28.5f, 0, 9.6f), new Vector2(2.2f, 1.8f), new[] { rPogaca.gameObject }, null);
            var zFirin = Unlock(uzRoot, "firin", "FIRIN", 3000, new Vector3(27.5f, 0, 13.2f), new Vector2(2.6f, 1.6f), new[] { firin.gameObject }, new[] { zPogacaRaf.gameObject });
            var zBugday2 = Unlock(uzRoot, "tarla_bugday_2", "BUĞDAY TARLASI 2", 1200, new Vector3(26.5f, 0, -3f), new Vector2(2.6f, 2.6f), new[] { fBugday2.gameObject }, null);
            var zUnRaf = Unlock(uzRoot, "raf_un", "UN RAFI", 900, new Vector3(25.5f, 0, 9.6f), new Vector2(2.2f, 1.8f), new[] { rUn.gameObject }, new[] { zBugday2.gameObject, zFirin.gameObject });
            var zMill = Unlock(uzRoot, "degirmen", "SU DEĞİRMENİ", 2000, new Vector3(31f, 0, -3.5f), new Vector2(2.6f, 2.4f), new[] { mill.gameObject }, new[] { zUnRaf.gameObject });
            var zBugday1 = Unlock(uzRoot, "tarla_bugday_1", "BUĞDAY TARLASI", 800, new Vector3(23f, 0, -3f), new Vector2(2.6f, 2.6f), new[] { fBugday1.gameObject }, new[] { zMill.gameObject });
            var zSalcaRaf = Unlock(uzRoot, "raf_salca", "SALÇA RAFI", 1200, new Vector3(31.5f, 0, 9.6f), new Vector2(2.2f, 1.8f), new[] { rSalca.gameObject }, null);
            var zSalca = Unlock(uzRoot, "mak_salca", "SALÇA MAKİNESİ", 2500, new Vector3(30.5f, 0, -9.2f), new Vector2(2.4f, 2.4f), new[] { salca.gameObject }, new[] { zSalcaRaf.gameObject });
            var zDomates2 = Unlock(uzRoot, "tarla_domates_2", "DOMATES BAHÇESİ 2", 1000, new Vector3(23f, 0, -8.5f), new Vector2(2.6f, 2.6f), new[] { fDomates2.gameObject }, new[] { zSalca.gameObject });

            var zExp3 = Unlock(uzRoot, "genisleme_3", "DEĞİRMEN BÖLGESİ", 6000, new Vector3(28f, 0, 6f), new Vector2(2.2f, 2.2f),
                new[] { exp3.gameObject }, new[] { zBugday1.gameObject, zDomates2.gameObject });
            zExp3.officeOnly = true;
            zExp3.description = "Market son kez büyür; buğday, su değirmeni, fırın (poğaça) ve salça açılır.";
            var exp2Wall = market.Find("Genisleme_2/WallRight");
            if (exp2Wall != null) zExp3.removeTargets = new[] { exp2Wall.gameObject };
            foreach (var r in zExp3.GetComponentsInChildren<Renderer>(true)) r.enabled = false;

            foreach (var z in new[] { zPogacaRaf, zFirin, zBugday2, zUnRaf, zMill, zBugday1, zSalcaRaf, zSalca, zDomates2, zExp3 })
                z.gameObject.SetActive(false);

            // Mandıra'nın son kilidi (tereyağı rafı) açılınca bilgisayarda belirir — fırın tereyağı ister
            var zTereyagRaf = uzRoot.Find("Unlock_raf_tereyag");
            if (zTereyagRaf != null)
            {
                var u = zTereyagRaf.GetComponent<UnlockZone>();
                var next = new List<GameObject>(u.nextZones ?? new GameObject[0]) { zExp3.gameObject };
                u.nextZones = next.ToArray();
                EditorUtility.SetDirty(u);
            }

            // ---------- Yükseltmeler ----------
            var upRoot = world.Find("Upgrades");
            if (upRoot != null)
            {
                Up(upRoot, "slot_degirmen", "DEĞİRMEN · SLOT", UpgradeKind.StationSlot, 800, 2.2f, 3, 38).station = mill;
                Up(upRoot, "slot_firin", "FIRIN · SLOT", UpgradeKind.StationSlot, 1200, 2.2f, 3, 39).station = firin;
                Up(upRoot, "slot_salca", "SALÇA MAK. · SLOT", UpgradeKind.StationSlot, 1000, 2.2f, 3, 39).station = salca;
            }

            var player = FindInScene("Player");
            if (player != null)
            {
                var pc = player.GetComponent<PlayerController>();
                if (pc != null) { pc.boundsMax = new Vector2(33.6f, pc.boundsMax.y); EditorUtility.SetDirty(pc); }
            }
            return true;
        }

        /// <summary>Market içindeki fırın: un + tereyağı → poğaça. Ürün tarafı (ped) aşağıya, koridora bakar.</summary>
        static Converter BakeryB(Transform parent, Vector3 pos)
        {
            var root = Empty("Firin", parent, pos);
            var model = Empty("Model", root.transform, Vector3.zero).transform;
            var brick = M("Tugla", Hex("BF6B4A"));
            var body = Box("Body", model, new Vector3(0, 0.7f, 0.3f), new Vector3(2.4f, 1.4f, 1.0f), brick);
            Box("Mouth", model, new Vector3(0, 0.7f, -0.21f), new Vector3(0.95f, 0.6f, 0.02f), M("FirinAgiz", Hex("3E2723")));
            Box("Fire", model, new Vector3(0, 0.52f, -0.225f), new Vector3(0.65f, 0.16f, 0.01f), M("Ates", Hex("FF7043")));
            Box("Top", model, new Vector3(0, 1.45f, 0.3f), new Vector3(2.5f, 0.1f, 1.1f), M("Tezgah", Hex("8D6E63")));
            Prim(PrimitiveType.Cylinder, "Chimney", model, new Vector3(0.8f, 1.95f, 0.45f), new Vector3(0.35f, 0.45f, 0.35f), brick);

            Box("Pad", root.transform, new Vector3(0, 0.025f, -1.4f), new Vector3(2.9f, 0.03f, 1.5f), M("Ped", Hex("D7CCC8")));
            Box("TrayA", root.transform, new Vector3(-1.0f, 0.05f, -1.3f), new Vector3(0.85f, 0.04f, 0.85f), M("Yalak", Hex("8D6E63")));
            Box("TrayB", root.transform, new Vector3(0f, 0.05f, -1.3f), new Vector3(0.85f, 0.04f, 0.85f), M("Yalak", Hex("8D6E63")));
            Box("TrayOut", root.transform, new Vector3(1.0f, 0.05f, -1.3f), new Vector3(0.85f, 0.04f, 0.85f), M("Tepsi", Hex("B0BEC5")));

            ItemContainer Pile(string n, float x, int cap, float layer)
            {
                var go = Empty(n, root.transform, new Vector3(x, 0.08f, -1.3f));
                var c = go.AddComponent<ItemContainer>();
                c.capacity = cap; c.layout = StackLayout.Grid; c.gridColumns = 2; c.gridRows = 2; c.spacingX = 0.38f; c.spacingZ = 0.36f; c.layerHeight = layer;
                return c;
            }

            var cv = root.AddComponent<Converter>();
            cv.input = ItemType.Un;
            cv.useInput2 = true;
            cv.input2 = ItemType.Tereyag;
            cv.output = ItemType.Pogaca;
            cv.inputCapacity = 4;
            cv.unitTime = 2f;
            cv.inputPile = Pile("Input", -1.0f, 4, 0.37f);
            cv.inputPile2 = Pile("Input2", 0f, 4, 0.16f);
            cv.outputTray = Pile("Output", 1.0f, 8, 0.15f);
            cv.spawnPoint = Empty("SpawnPoint", root.transform, new Vector3(0, 0.9f, -0.4f)).transform;
            cv.zoneOffset = new Vector3(0, 0, -1.4f);
            cv.zoneSize = new Vector2(2.9f, 1.5f);
            cv.label = Label(root.transform, new Vector3(0, 2.7f, 0.3f), "", 0.045f, Dark);
            Obstacle(body);
            return cv;
        }

        /// <summary>Taş değirmen evi + akarsuya uzanan aks + suyun içinde dönen çark.</summary>
        static void MillModel(Transform m)
        {
            var stone = M("DegirmenTas", Hex("D7CCC8"));
            var roof = M("DegirmenCati", Hex("8D3B2B"));
            var wood = M("DegirmenAhsap", Hex("6D4C41"));
            Box("Body", m, new Vector3(0, 0.85f, -0.3f), new Vector3(2.2f, 1.7f, 1.8f), stone);
            Box("Roof", m, new Vector3(0, 1.85f, -0.3f), new Vector3(2.5f, 0.3f, 2.1f), roof);
            Box("Roof2", m, new Vector3(0, 2.15f, -0.3f), new Vector3(1.7f, 0.3f, 1.5f), roof);
            Box("Door", m, new Vector3(0, 0.45f, 0.61f), new Vector3(0.55f, 0.9f, 0.02f), wood);
            Box("Window", m, new Vector3(1.11f, 1.1f, -0.3f), new Vector3(0.02f, 0.4f, 0.4f), M("DegirmenCam", Hex("90CAF9")));
            Box("SackPile", m, new Vector3(-0.8f, 0.2f, 0.75f), new Vector3(0.45f, 0.4f, 0.3f), M("UnCuval", Hex("F5F0E1")));

            // Aks: evden akarsuya (yerel x 1.1 → 4.4)
            Prim(PrimitiveType.Cylinder, "Axle", m, new Vector3(2.75f, 0.65f, -0.3f), new Vector3(0.16f, 1.65f, 0.16f), wood, new Vector3(0, 0, 90));

            // Çark: dünya x ≈ 35.6 (akarsu x 35..38.2), merkez y 0.65, yarıçap ~1.1 → alt kısmı suyun altında kalır
            var wheel = Empty("Wheel", m, new Vector3(4.6f, 0.75f, -0.3f));
            wheel.AddComponent<WaterWheel>();
            wheel.transform.localScale = Vector3.one * 1.4f;
            var w = wheel.transform;
            Prim(PrimitiveType.Cylinder, "Hub", w, Vector3.zero, new Vector3(0.38f, 0.25f, 0.38f), wood, new Vector3(0, 0, 90));
            for (int k = 0; k < 4; k++)
                Box("Spoke", w, Vector3.zero, new Vector3(0.1f, 2.1f, 0.1f), wood).transform.localEulerAngles = new Vector3(k * 45f, 0, 0);
            for (int k = 0; k < 8; k++)
            {
                float b = k * 45f + 22.5f;
                float rad = b * Mathf.Deg2Rad;
                var seg = Box("Rim", w, new Vector3(0, Mathf.Cos(rad) * 1.02f, Mathf.Sin(rad) * 1.02f), new Vector3(0.14f, 0.12f, 0.86f), wood);
                seg.transform.localEulerAngles = new Vector3(b, 0, 0);
                float a = k * 45f;
                float ra = a * Mathf.Deg2Rad;
                var paddle = Box("Paddle", w, new Vector3(0, Mathf.Cos(ra) * 0.95f, Mathf.Sin(ra) * 0.95f), new Vector3(0.5f, 0.45f, 0.08f), M("CarkKurek", Hex("8D6E63")));
                paddle.transform.localEulerAngles = new Vector3(a, 0, 0);
            }
        }
    }
}
