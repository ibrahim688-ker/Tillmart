using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CiftlikMarket.EditorTools
{
    /// <summary>
    /// Gri kutu prototip sahnesini koddan kurar. Menü: CiftlikMarket > Build Prototype Scene.
    /// Sahne her seferinde baştan oluşturulur (Assets/Scenes/Prototype.unity).
    /// </summary>
    public static partial class PrototypeBuilder
    {
        const string Root = "Assets/CiftlikMarket";
        const string ScenePath = "Assets/Scenes/Prototype.unity";
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static Font font;
        static readonly Color Dark = new Color(0.15f, 0.15f, 0.2f);

        [MenuItem("CiftlikMarket/Reset Save (PlayerPrefs)")]
        public static void ResetSave()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[CiftlikMarket] Kayıt sıfırlandı.");
        }

        [MenuItem("CiftlikMarket/Build Prototype Scene")]
        public static void Build()
        {
            EnsureFolder("Assets", "CiftlikMarket");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder("Assets", "Scenes");
            mats.Clear();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ---------- Prefablar ----------
            var pSaman = ItemPrefab("Saman", ItemType.Saman, r =>
            {
                Box("Bale", r, new Vector3(0, 0.15f, 0), new Vector3(0.5f, 0.3f, 0.36f), M("Saman", Hex("E8C547")));
                Box("Strap1", r, new Vector3(0, 0.15f, 0.09f), new Vector3(0.51f, 0.31f, 0.04f), M("SamanIp", Hex("B8932A")));
                Box("Strap2", r, new Vector3(0, 0.15f, -0.09f), new Vector3(0.51f, 0.31f, 0.04f), M("SamanIp", Hex("B8932A")));
            });
            var pSut = ItemPrefab("Sut", ItemType.Sut, r =>
            {
                Prim(PrimitiveType.Cylinder, "Bottle", r, new Vector3(0, 0.15f, 0), new Vector3(0.22f, 0.15f, 0.22f), M("SutBeyaz", Hex("FAFAFA")));
                Prim(PrimitiveType.Cylinder, "Neck", r, new Vector3(0, 0.33f, 0), new Vector3(0.12f, 0.03f, 0.12f), M("SutKapak", Hex("2F80ED")));
            });
            var pMisir = ItemPrefab("Misir", ItemType.Misir, r =>
            {
                Prim(PrimitiveType.Capsule, "Cob", r, new Vector3(0, 0.09f, 0), new Vector3(0.16f, 0.2f, 0.16f), M("Misir", Hex("F2B705")), new Vector3(0, 0, 90));
                Box("Leaf", r, new Vector3(-0.17f, 0.09f, 0), new Vector3(0.12f, 0.12f, 0.14f), M("Yaprak", Hex("6DBE45")));
            });
            var pYumurta = ItemPrefab("Yumurta", ItemType.Yumurta, r =>
            {
                Prim(PrimitiveType.Sphere, "Egg", r, new Vector3(0, 0.13f, 0), new Vector3(0.2f, 0.26f, 0.2f), M("Yumurta", Hex("FFF1D0")));
            });
            var pPara = ItemPrefab("Para", ItemType.Para, r =>
            {
                Box("Bill", r, new Vector3(0, 0.04f, 0), new Vector3(0.42f, 0.08f, 0.26f), M("Para", Hex("4CAF50")));
                Box("Band", r, new Vector3(0, 0.04f, 0), new Vector3(0.1f, 0.085f, 0.27f), M("ParaBant", Hex("2E7D32")));
            });
            var pDomates = ItemPrefab("Domates", ItemType.Domates, r =>
            {
                Prim(PrimitiveType.Sphere, "Tomato", r, new Vector3(0, 0.12f, 0), new Vector3(0.27f, 0.24f, 0.27f), M("Domates", Hex("E53935")));
                Box("Stem", r, new Vector3(0, 0.25f, 0), new Vector3(0.12f, 0.03f, 0.12f), M("Yaprak", Hex("6DBE45")));
            });
            var pCustomer = CustomerPrefab();

            // ---------- Sahne ----------
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("D6ECFF");
            RenderSettings.ambientEquatorColor = Hex("E6EED6");
            RenderSettings.ambientGroundColor = Hex("8C9A70");

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = Hex("FFF4E0");
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
            lightGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            var config = new GameObject("GameConfig").AddComponent<GameConfig>();
            config.items = new List<ItemDef>
            {
                new ItemDef { type = ItemType.Saman, displayName = "Saman", price = 0, height = 0.3f, prefab = pSaman },
                new ItemDef { type = ItemType.Sut, displayName = "Süt", price = 5, height = 0.36f, prefab = pSut },
                new ItemDef { type = ItemType.Misir, displayName = "Mısır", price = 0, height = 0.18f, prefab = pMisir },
                new ItemDef { type = ItemType.Yumurta, displayName = "Yumurta", price = 6, height = 0.26f, prefab = pYumurta },
                new ItemDef { type = ItemType.Para, displayName = "Para", price = 0, height = 0.08f, prefab = pPara },
                new ItemDef { type = ItemType.Domates, displayName = "Domates", price = 4, height = 0.26f, prefab = pDomates },
            };
            var gm = new GameObject("GameManager").AddComponent<GameManager>();

            var world = new GameObject("World").transform;

            // ---- Zemin ve sokak (solda: araba yolu + kaldırım) ----
            Box("Grass", world, new Vector3(3f, -0.05f, 2f), new Vector3(62, 0.1f, 64), M("Cim", Hex("8BC34A")));
            Box("Road", world, new Vector3(-10.8f, 0.005f, 4f), new Vector3(2.8f, 0.01f, 58), M("Yol", Hex("9E9E9E")));
            Box("RoadEdge", world, new Vector3(-12.2f, 0.012f, 4f), new Vector3(0.12f, 0.01f, 58), M("YolCizgi", Hex("FFFFFF")));
            Box("Sidewalk", world, new Vector3(-8.95f, 0.02f, 4f), new Vector3(0.9f, 0.04f, 58), M("Kaldirim", Hex("CFD8DC")));

            // ---- Market (başlangıç: x -8..8, z 2..16) ----
            var market = new GameObject("Market").transform;
            market.SetParent(world, false);
            var floorMat = M("Zemin", Hex("F5D7A1"));
            var wall = M("Duvar", Hex("ECEFF1"));
            var stripe = M("DuvarSerit", Hex("4A90E2"));
            Box("Floor", market, new Vector3(0, 0.01f, 9), new Vector3(16, 0.02f, 14), floorMat);
            Box("WallBack", market, new Vector3(0, 0.6f, 16.15f), new Vector3(16.6f, 1.2f, 0.3f), wall);
            Box("WallBackStripe", market, new Vector3(0, 1.15f, 16.0f), new Vector3(16.6f, 0.12f, 0.02f), stripe);
            Box("WallLeftFront", market, new Vector3(-8.15f, 0.6f, 2.675f), new Vector3(0.3f, 1.2f, 1.65f), wall);
            Box("WallLeftBack", market, new Vector3(-8.15f, 0.6f, 11.4f), new Vector3(0.3f, 1.2f, 9.8f), wall);
            var wallRight = Box("WallRight_Gecici", market, new Vector3(8.15f, 0.6f, 9f), new Vector3(0.3f, 1.2f, 14.3f), M("DuvarGecici", Hex("D5DCE0")));
            Box("DoorMat", market, new Vector3(-7.4f, 0.025f, 5f), new Vector3(1.2f, 0.01f, 3f), M("Paspas", Hex("E07A5F")));

            // ---- Genişleme 1: sağa doğru +6 birim ----
            var exp1 = new GameObject("Genisleme_1").transform;
            exp1.SetParent(market, false);
            Box("Floor", exp1, new Vector3(11f, 0.01f, 9), new Vector3(6, 0.02f, 14), floorMat);
            Box("WallBack", exp1, new Vector3(11.15f, 0.6f, 16.15f), new Vector3(6.3f, 1.2f, 0.3f), wall);
            Box("WallBackStripe", exp1, new Vector3(11.15f, 1.15f, 16.0f), new Vector3(6.3f, 0.12f, 0.02f), stripe);
            Box("WallRight", exp1, new Vector3(14.15f, 0.6f, 9f), new Vector3(0.3f, 1.2f, 14.3f), wall);
            exp1.gameObject.SetActive(false);

            // ---- Raflar (arka duvar boyunca) ----
            var shelfSut = ShelfB("Raf_Sut", market, new Vector3(-2.5f, 0, 14.4f), ItemType.Sut, "SÜT", Hex("2F80ED"));
            var shelfYumurta = ShelfB("Raf_Yumurta", market, new Vector3(1.5f, 0, 14.4f), ItemType.Yumurta, "YUMURTA", Hex("F2994A"));
            var shelfDomates = ShelfB("Raf_Domates", market, new Vector3(11f, 0, 14.4f), ItemType.Domates, "DOMATES", Hex("E53935"));
            shelfYumurta.gameObject.SetActive(false);
            shelfDomates.gameObject.SetActive(false);

            // ---- Kasa + para (kapı ile drive-thru camı arasında) ----
            var money = MoneyPileB(market, new Vector3(-6.6f, 0, 11f));
            var checkout = CheckoutB(market, new Vector3(-5f, 0, 8.5f), money);

            // ---- Çöp ----
            var trashGo = Empty("Cop", market, new Vector3(6.6f, 0, 4.6f));
            Box("Bin", trashGo.transform, new Vector3(0, 0.4f, 0), new Vector3(0.8f, 0.8f, 0.8f), M("Cop", Hex("E53935")));
            Box("Lid", trashGo.transform, new Vector3(0, 0.82f, 0), new Vector3(0.7f, 0.05f, 0.7f), M("CopIc", Hex("424242")));
            var trash = trashGo.AddComponent<TrashBin>();
            trash.mouth = Empty("Mouth", trashGo.transform, new Vector3(0, 0.8f, 0)).transform;
            trash.zoneOffset = new Vector3(0, 0, -1f);
            trash.zoneSize = new Vector2(1.2f, 1.0f);
            ZoneMark(trashGo.transform, new Vector3(0, 0, -1f), new Vector2(1.2f, 1.0f));
            Label(trashGo.transform, new Vector3(0, 1.4f, 0), "ÇÖP", 0.05f, Dark);

            // ---- Drive-thru (sol duvarda cam, arabalar soldaki yoldan) ----
            var pCar = CarPrefab();
            var drive = DriveThruB(market, money, pCar);
            drive.gameObject.SetActive(false);

            // ---- Çiftlik (marketin altında, sağa doğru büyür) ----
            var farm = new GameObject("Farm").transform;
            farm.SetParent(world, false);
            var samanField = FieldB("Tarla_Saman", farm, new Vector3(-4.5f, 0, -3f), ItemType.Saman, "SAMAN TARLASI");
            var cow1 = AnimalB("Inek_1", farm, new Vector3(0f, 0, -3.8f), ItemType.Saman, ItemType.Sut, CowModel);
            var cow2 = AnimalB("Inek_2", farm, new Vector3(4.5f, 0, -3.8f), ItemType.Saman, ItemType.Sut, CowModel);
            var misirField = FieldB("Tarla_Misir", farm, new Vector3(-4.5f, 0, -9.5f), ItemType.Misir, "MISIR TARLASI");
            var coop = AnimalB("Kumes_1", farm, new Vector3(0f, 0, -10.3f), ItemType.Misir, ItemType.Yumurta, CoopModel);
            var domatesField = FieldB("Tarla_Domates", farm, new Vector3(9.5f, 0, -3.4f), ItemType.Domates, "DOMATES BAHÇESİ");
            domatesField.regrowTime = 6f;
            cow2.gameObject.SetActive(false);
            misirField.gameObject.SetActive(false);
            coop.gameObject.SetActive(false);
            domatesField.gameObject.SetActive(false);

            // ---- Kilit alanları ----
            // Sıra: Mısır → Kümes → Yumurta rafı → (Kasiyer, İnek 2 → Bakıcı, Drive-thru → Genişleme → (Domates rafı, Domates bahçesi))
            var uzRoot = new GameObject("UnlockZones").transform;
            uzRoot.SetParent(world, false);
            var uzDomRaf = Unlock(uzRoot, "raf_domates", "DOMATES RAFI", 120, new Vector3(11f, 0, 14f), new Vector2(2.2f, 1.8f), new[] { shelfDomates.gameObject }, null);
            var uzDomField = Unlock(uzRoot, "tarla_domates", "DOMATES BAHÇESİ", 150, new Vector3(9.5f, 0, -3.4f), new Vector2(2.6f, 2.6f), new[] { domatesField.gameObject }, null);
            var uzExp = Unlock(uzRoot, "genisleme_1", "GENİŞLETME", 500, new Vector3(6.6f, 0, 9f), new Vector2(2.2f, 2.2f), new[] { exp1.gameObject }, new[] { uzDomRaf.gameObject, uzDomField.gameObject });
            uzExp.removeTargets = new[] { wallRight };
            var kasiyer = WorkerB("Calisan_Kasiyer", world, new Vector3(-4f, 0, 4.2f), Worker.Role.Cashier, checkout, Hex("43A047"), "KASİYER");
            var bakici = WorkerB("Calisan_Bakici", world, new Vector3(2.8f, 0, 0.4f), Worker.Role.Farmhand, checkout, Hex("8D6E63"), "BAKICI");
            var uzKasiyer = Unlock(uzRoot, "kasiyer", "KASİYER", 150, new Vector3(-4f, 0, 4.2f), new Vector2(1.8f, 1.8f), new[] { kasiyer }, null);
            var uzBakici = Unlock(uzRoot, "bakici", "BAKICI", 400, new Vector3(2.8f, 0, 0.4f), new Vector2(2f, 2f), new[] { bakici }, null);
            var uzCow2 = Unlock(uzRoot, "inek2", "İNEK 2", 200, new Vector3(4.5f, 0, -3f), new Vector2(2.4f, 2.4f), new[] { cow2.gameObject }, new[] { uzBakici.gameObject });
            var uzDrive = Unlock(uzRoot, "drive_thru", "DRIVE-THRU", 300, new Vector3(-7.2f, 0, 13.5f), new Vector2(1.4f, 1.8f), new[] { drive.gameObject }, new[] { uzExp.gameObject });
            var uzRaf = Unlock(uzRoot, "raf_yumurta", "YUMURTA RAFI", 80, new Vector3(1.5f, 0, 14f), new Vector2(2.2f, 1.8f), new[] { shelfYumurta.gameObject }, new[] { uzCow2.gameObject, uzDrive.gameObject, uzKasiyer.gameObject });
            var uzCoop = Unlock(uzRoot, "kumes", "TAVUK KÜMESİ", 100, new Vector3(0f, 0, -9.5f), new Vector2(2.4f, 2.4f), new[] { coop.gameObject }, new[] { uzRaf.gameObject });
            var uzMisir = Unlock(uzRoot, "tarla_misir", "MISIR TARLASI", 60, new Vector3(-4.5f, 0, -9.5f), new Vector2(2.6f, 2.6f), new[] { misirField.gameObject }, new[] { uzCoop.gameObject });
            foreach (var z in new[] { uzDomRaf, uzDomField, uzExp, uzKasiyer, uzBakici, uzCow2, uzDrive, uzRaf, uzCoop }) z.gameObject.SetActive(false);
            uzMisir.gameObject.SetActive(true);

            // ---- Yaya müşteriler: kaldırımdan gelir, sol duvardaki kapıdan girer ----
            var sp = new GameObject("CustomerSpawner").AddComponent<CustomerSpawner>();
            sp.transform.SetParent(world, false);
            sp.customerPrefab = pCustomer;
            sp.spawnPoint = Empty("Spawn", sp.transform, new Vector3(-9f, 0, -16f)).transform;
            sp.approachPoint = Empty("Approach", sp.transform, new Vector3(-9f, 0, 5.2f)).transform;
            sp.entryPoint = Empty("Entry", sp.transform, new Vector3(-6.8f, 0, 5.2f)).transform;
            sp.exitDoor = Empty("ExitDoor", sp.transform, new Vector3(-9f, 0, 4.6f)).transform;
            sp.exitPoint = Empty("ExitOut", sp.transform, new Vector3(-9f, 0, -18f)).transform;
            sp.checkout = checkout;

            // Oyuncu
            var player = PlayerB(new Vector3(0, 0, 0.8f));

            // Kamera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("8BC34A");
            cam.farClipPlane = 120f;
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow>();
            follow.target = player.transform;
            camGo.transform.rotation = Quaternion.LookRotation(-follow.offset);
            camGo.transform.position = player.transform.position + follow.offset;

            // HUD
            var hud = HudB(out var moneyText, out var joyBase, out var joyKnob);
            gm.moneyText = moneyText;
            var pc = player.GetComponent<PlayerController>();
            pc.joyBase = joyBase;
            pc.joyKnob = joyKnob;

            AddOfficeToScene();
            AddCollidersToScene();
            AddRegion2ToScene();
            AddRegion2PartBToScene();
            RearrangeShelves();
            AddRegion3ToScene();
            AddBranchToScene();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            AssetDatabase.SaveAssets();
            Debug.Log("[CiftlikMarket] Prototip sahnesi kuruldu: " + ScenePath);
        }

        // ================== Yapı parçaları ==================

        static GameObject PlayerB(Vector3 pos)
        {
            var player = new GameObject("Player");
            player.transform.position = pos;
            var model = Empty("Model", player.transform, Vector3.zero).transform;
            Prim(PrimitiveType.Capsule, "Body", model, new Vector3(0, 0.6f, 0), new Vector3(0.65f, 0.6f, 0.65f), M("Oyuncu", Hex("3D7BF7")));
            Prim(PrimitiveType.Sphere, "Head", model, new Vector3(0, 1.38f, 0), new Vector3(0.48f, 0.48f, 0.48f), M("Ten", Hex("F1C27D")));
            Prim(PrimitiveType.Cylinder, "HatBrim", model, new Vector3(0, 1.58f, 0), new Vector3(0.75f, 0.02f, 0.75f), M("Hasir", Hex("D9B26F")));
            Prim(PrimitiveType.Cylinder, "HatTop", model, new Vector3(0, 1.68f, 0), new Vector3(0.42f, 0.09f, 0.42f), M("Hasir", Hex("D9B26F")));
            Box("Nose", model, new Vector3(0, 1.36f, 0.24f), new Vector3(0.08f, 0.08f, 0.08f), M("Ten", Hex("F1C27D")));
            var stackGo = Empty("Stack", player.transform, new Vector3(0, 0.55f, -0.45f));
            var st = stackGo.AddComponent<ItemContainer>();
            st.capacity = 4;
            st.layout = StackLayout.Column;
            var carrier = player.AddComponent<Carrier>();
            carrier.stack = st;
            carrier.isPlayer = true;
            carrier.canCashier = true;
            var pc = player.AddComponent<PlayerController>();
            pc.model = model;
            pc.boundsMin = new Vector2(-7.8f, -14f);
            pc.boundsMax = new Vector2(13.6f, 15.6f);
            return player;
        }

        static GameObject CustomerPrefab()
        {
            var root = new GameObject("Customer");
            var cu = root.AddComponent<Customer>();
            var body = Prim(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0, 0.6f, 0), new Vector3(0.6f, 0.6f, 0.6f), M("Musteri", Color.white));
            Prim(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 1.33f, 0), new Vector3(0.44f, 0.44f, 0.44f), M("Ten", Hex("F1C27D")));
            var carryGo = Empty("Carry", root.transform, new Vector3(0, 0.7f, 0.42f));
            var cc = carryGo.AddComponent<ItemContainer>();
            cc.capacity = 10;
            cc.layout = StackLayout.Column;
            cu.carry = cc;
            cu.body = body.GetComponent<Renderer>();
            cu.bubble = Label(root.transform, new Vector3(0, 1.95f, 0), "", 0.045f, Dark);
            var p = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/Customer.prefab");
            Object.DestroyImmediate(root);
            return p;
        }

        static ResourceField FieldB(string name, Transform parent, Vector3 pos, ItemType type, string title)
        {
            var root = Empty(name, parent, pos);
            Box("Soil", root.transform, new Vector3(0, 0.02f, 0), new Vector3(2.6f, 0.04f, 2.6f), M("Toprak", Hex("8D5A3B")));
            Box("Rows", root.transform, new Vector3(0, 0.045f, 0), new Vector3(2.3f, 0.01f, 0.15f), M("ToprakKoyu", Hex("6D4128")));
            var f = root.AddComponent<ResourceField>();
            f.produces = type;
            f.zoneSize = new Vector2(2.6f, 2.6f);
            var slots = new List<Transform>();
            foreach (var o in new[] { new Vector3(-0.6f, 0.05f, 0.6f), new Vector3(0.6f, 0.05f, 0.6f), new Vector3(-0.6f, 0.05f, -0.6f), new Vector3(0.6f, 0.05f, -0.6f) })
                slots.Add(Empty("Slot", root.transform, o).transform);
            f.slots = slots.ToArray();
            Label(root.transform, new Vector3(0, 1.2f, 1.1f), title, 0.035f, Color.white);
            return f;
        }

        static Converter AnimalB(string name, Transform parent, Vector3 pos, ItemType input, ItemType output, System.Action<Transform> model)
        {
            var root = Empty(name, parent, pos);
            var modelRoot = Empty("Model", root.transform, Vector3.zero).transform;
            model(modelRoot);
            Box("Pad", root.transform, new Vector3(0, 0.015f, 1.4f), new Vector3(2.4f, 0.03f, 1.5f), M("Ped", Hex("D7CCC8")));
            Box("Trough", root.transform, new Vector3(-0.55f, 0.06f, 1.3f), new Vector3(1.0f, 0.08f, 0.9f), M("Yalak", Hex("8D6E63")));
            Box("Tray", root.transform, new Vector3(0.6f, 0.04f, 1.3f), new Vector3(1.0f, 0.04f, 0.9f), M("Tepsi", Hex("B0BEC5")));
            var inGo = Empty("Input", root.transform, new Vector3(-0.55f, 0.1f, 1.3f));
            var inC = inGo.AddComponent<ItemContainer>();
            inC.capacity = 4; inC.layout = StackLayout.Grid; inC.gridColumns = 2; inC.gridRows = 2; inC.spacingX = 0.42f; inC.spacingZ = 0.4f; inC.layerHeight = 0.3f;
            var outGo = Empty("Output", root.transform, new Vector3(0.6f, 0.06f, 1.3f));
            var outC = outGo.AddComponent<ItemContainer>();
            outC.capacity = 8; outC.layout = StackLayout.Grid; outC.gridColumns = 2; outC.gridRows = 2; outC.spacingX = 0.4f; outC.spacingZ = 0.38f; outC.layerHeight = 0.36f;
            var cv = root.AddComponent<Converter>();
            cv.input = input;
            cv.output = output;
            cv.inputCapacity = 4;
            cv.unitTime = 1f;
            cv.inputPile = inC;
            cv.outputTray = outC;
            cv.spawnPoint = Empty("SpawnPoint", root.transform, new Vector3(0, 1.0f, 0.3f)).transform;
            cv.zoneOffset = new Vector3(0, 0, 1.4f);
            cv.zoneSize = new Vector2(2.4f, 1.5f);
            cv.label = Label(root.transform, new Vector3(0, 2.4f, -0.8f), "", 0.05f, Dark);
            return cv;
        }

        static void CowModel(Transform m)
        {
            var white = M("InekBeyaz", Hex("FAFAFA"));
            var black = M("InekSiyah", Hex("263238"));
            var pink = M("InekPembe", Hex("F8BBD0"));
            Box("Body", m, new Vector3(0, 0.85f, -0.2f), new Vector3(0.9f, 0.7f, 1.4f), white);
            Box("Spot1", m, new Vector3(0.46f, 0.95f, -0.3f), new Vector3(0.02f, 0.35f, 0.45f), black);
            Box("Spot2", m, new Vector3(-0.46f, 0.8f, 0.0f), new Vector3(0.02f, 0.3f, 0.35f), black);
            Box("Spot3", m, new Vector3(0.1f, 1.21f, -0.5f), new Vector3(0.4f, 0.02f, 0.3f), black);
            Box("Head", m, new Vector3(0, 1.15f, 0.65f), new Vector3(0.55f, 0.5f, 0.5f), white);
            Box("Snout", m, new Vector3(0, 1.02f, 0.92f), new Vector3(0.45f, 0.25f, 0.08f), pink);
            Box("HornL", m, new Vector3(-0.22f, 1.45f, 0.65f), new Vector3(0.08f, 0.15f, 0.08f), M("Boynuz", Hex("FFE0B2")));
            Box("HornR", m, new Vector3(0.22f, 1.45f, 0.65f), new Vector3(0.08f, 0.15f, 0.08f), M("Boynuz", Hex("FFE0B2")));
            Box("Udder", m, new Vector3(0, 0.45f, -0.35f), new Vector3(0.35f, 0.15f, 0.3f), pink);
            foreach (var p in new[] { new Vector3(-0.3f, 0.25f, 0.3f), new Vector3(0.3f, 0.25f, 0.3f), new Vector3(-0.3f, 0.25f, -0.7f), new Vector3(0.3f, 0.25f, -0.7f) })
                Box("Leg", m, p, new Vector3(0.18f, 0.5f, 0.18f), white);
        }

        static void CoopModel(Transform m)
        {
            var wood = M("Kumes", Hex("C8875A"));
            Box("House", m, new Vector3(0, 0.5f, -0.35f), new Vector3(1.7f, 1.0f, 1.1f), wood);
            Box("Roof", m, new Vector3(0, 1.1f, -0.35f), new Vector3(1.9f, 0.2f, 1.3f), M("Cati", Hex("C0392B")));
            Box("Door", m, new Vector3(0, 0.3f, 0.21f), new Vector3(0.4f, 0.5f, 0.02f), M("KumesKapi", Hex("5D4037")));
            foreach (var x in new[] { -0.55f, 0.55f })
            {
                Prim(PrimitiveType.Sphere, "Chicken", m, new Vector3(x, 0.22f, 0.45f), new Vector3(0.36f, 0.34f, 0.4f), M("Tavuk", Hex("FFFFFF")));
                Box("Comb", m, new Vector3(x, 0.44f, 0.55f), new Vector3(0.06f, 0.1f, 0.12f), M("Ibik", Hex("E53935")));
                Box("Beak", m, new Vector3(x, 0.26f, 0.66f), new Vector3(0.06f, 0.05f, 0.08f), M("Gaga", Hex("FFA000")));
            }
        }

        static Shelf ShelfB(string name, Transform parent, Vector3 pos, ItemType type, string title, Color signColor)
        {
            var root = Empty(name, parent, pos);
            var wood = M("Ahsap", Hex("B08968"));
            Box("Base", root.transform, new Vector3(0, 0.45f, 0), new Vector3(1.8f, 0.9f, 0.8f), wood);
            Box("Back", root.transform, new Vector3(0, 1.2f, 0.36f), new Vector3(1.8f, 0.6f, 0.08f), wood);
            Box("Sign", root.transform, new Vector3(0, 1.65f, 0.36f), new Vector3(0.9f, 0.3f, 0.06f), M("Tabela_" + type, signColor));
            var cGo = Empty("Items", root.transform, new Vector3(0, 0.91f, -0.05f));
            var c = cGo.AddComponent<ItemContainer>();
            c.capacity = 6; c.layout = StackLayout.Grid; c.gridColumns = 3; c.gridRows = 2; c.spacingX = 0.5f; c.spacingZ = 0.32f; c.layerHeight = 0.36f;
            var s = root.AddComponent<Shelf>();
            s.type = type;
            s.container = c;
            s.customerPoint = Empty("CustomerPoint", root.transform, new Vector3(0, 0, -1.15f)).transform;
            s.zoneOffset = new Vector3(0, 0, -1.15f);
            s.zoneSize = new Vector2(2.0f, 1.3f);
            ZoneMark(root.transform, new Vector3(0, 0, -1.15f), new Vector2(2.0f, 1.3f));
            s.label = Label(root.transform, new Vector3(0, 2.7f, 0.36f), title, 0.05f, Dark);
            return s;
        }

        static MoneyPile MoneyPileB(Transform parent, Vector3 pos)
        {
            var root = Empty("ParaYigini", parent, pos);
            ZoneMark(root.transform, Vector3.zero, new Vector2(1.4f, 1.4f));
            var cGo = Empty("Bills", root.transform, new Vector3(0, 0.03f, 0));
            var c = cGo.AddComponent<ItemContainer>();
            c.capacity = 60; c.layout = StackLayout.Grid; c.gridColumns = 2; c.gridRows = 3; c.spacingX = 0.46f; c.spacingZ = 0.3f; c.layerHeight = 0.085f;
            var mp = root.AddComponent<MoneyPile>();
            mp.visual = c;
            mp.zoneSize = new Vector2(1.4f, 1.4f);
            return mp;
        }

        static Checkout CheckoutB(Transform parent, Vector3 pos, MoneyPile money)
        {
            var root = Empty("Kasa", parent, pos);
            Box("Counter", root.transform, new Vector3(0, 0.5f, 0), new Vector3(1.0f, 1.0f, 2.2f), M("Tezgah", Hex("4DB6AC")));
            Box("CounterTop", root.transform, new Vector3(0, 1.02f, 0), new Vector3(1.1f, 0.04f, 2.3f), M("TezgahUst", Hex("E0F2F1")));
            Box("Register", root.transform, new Vector3(-0.2f, 1.2f, 0.4f), new Vector3(0.4f, 0.3f, 0.45f), M("Yazarkasa", Hex("37474F")));
            var co = root.AddComponent<Checkout>();
            co.zoneOffset = new Vector3(-1.15f, 0, 0);
            co.zoneSize = new Vector2(1.3f, 1.8f);
            ZoneMark(root.transform, new Vector3(-1.15f, 0, 0), new Vector2(1.3f, 1.8f));
            var ind = Box("MannedIndicator", root.transform, new Vector3(-1.15f, 0.03f, 0), new Vector3(1.1f, 0.01f, 1.6f), M("KasaAktif", Hex("81C784")));
            co.mannedIndicator = ind;
            co.counterTop = Empty("CounterTopPoint", root.transform, new Vector3(0.1f, 1.1f, 0)).transform;
            co.moneyPile = money;
            var q = new List<Transform>();
            for (int i = 0; i < 5; i++) q.Add(Empty("Queue" + i, root.transform, new Vector3(1.1f + i * 1.0f, 0, 0)).transform);
            co.queuePoints = q.ToArray();
            Label(root.transform, new Vector3(0, 1.9f, -1.4f), "KASA", 0.05f, Dark);
            return co;
        }

        static UnlockZone Unlock(Transform parent, string id, string title, int cost, Vector3 pos, Vector2 size, GameObject[] targets, GameObject[] next)
        {
            var root = Empty("Unlock_" + id, parent, pos);
            Box("Frame", root.transform, new Vector3(0, 0.02f, 0), new Vector3(size.x, 0.02f, size.y), M("KilitCerceve", Hex("FFFFFF")));
            Box("Inner", root.transform, new Vector3(0, 0.025f, 0), new Vector3(size.x - 0.16f, 0.02f, size.y - 0.16f), M("KilitIc", Hex("A5C88A")));
            var pivot = Empty("FillPivot", root.transform, new Vector3(0, 0.03f, 0));
            Box("Fill", pivot.transform, Vector3.zero, new Vector3(size.x - 0.16f, 0.02f, size.y - 0.16f), M("KilitDolum", Hex("FFD54F")));
            Box("MoneyIcon", root.transform, new Vector3(0, 0.06f, -size.y * 0.18f), new Vector3(0.5f, 0.06f, 0.3f), M("Para", Hex("4CAF50")));
            var uz = root.AddComponent<UnlockZone>();
            uz.id = id;
            uz.title = title;
            uz.cost = cost;
            uz.unlockTargets = targets;
            uz.nextZones = next ?? new GameObject[0];
            uz.zoneSize = size;
            uz.fill = pivot.transform;
            pivot.transform.localScale = new Vector3(0.001f, 1f, 0.001f);
            uz.label = Label(root.transform, new Vector3(0, 0.9f, 0), title + "\n" + cost, 0.05f, Dark);
            return uz;
        }

        static Canvas HudB(out Text moneyText, out RectTransform joyBase, out RectTransform joyKnob)
        {
            var cgo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = cgo.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var panel = UIImage("MoneyPanel", cgo.transform, uiSprite, new Color(0, 0, 0, 0.45f));
            panel.type = Image.Type.Sliced;
            var prt = panel.rectTransform;
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(1, 1);
            prt.anchoredPosition = new Vector2(-30, -60);
            prt.sizeDelta = new Vector2(330, 100);

            var icon = UIImage("Icon", panel.transform, null, Hex("4CAF50"));
            var irt = icon.rectTransform;
            irt.anchorMin = irt.anchorMax = irt.pivot = new Vector2(0, 0.5f);
            irt.anchoredPosition = new Vector2(22, 0);
            irt.sizeDelta = new Vector2(70, 44);

            var tgo = new GameObject("MoneyText", typeof(RectTransform), typeof(Text));
            tgo.transform.SetParent(panel.transform, false);
            moneyText = tgo.GetComponent<Text>();
            moneyText.font = font;
            moneyText.fontSize = 58;
            moneyText.fontStyle = FontStyle.Bold;
            moneyText.alignment = TextAnchor.MiddleRight;
            moneyText.color = Color.white;
            moneyText.text = "0";
            var trt = moneyText.rectTransform;
            trt.anchorMin = new Vector2(0, 0); trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = new Vector2(100, 0); trt.offsetMax = new Vector2(-24, 0);

            var jb = UIImage("JoystickBase", cgo.transform, knob, new Color(1, 1, 1, 0.35f));
            jb.rectTransform.sizeDelta = new Vector2(260, 260);
            jb.raycastTarget = false;
            var jk = UIImage("JoystickKnob", cgo.transform, knob, new Color(1, 1, 1, 0.85f));
            jk.rectTransform.sizeDelta = new Vector2(120, 120);
            jk.raycastTarget = false;
            joyBase = jb.rectTransform;
            joyKnob = jk.rectTransform;
            jb.gameObject.SetActive(false);
            jk.gameObject.SetActive(false);
            return canvas;
        }

        // ================== Yardımcılar ==================

        static GameObject ItemPrefab(string name, ItemType type, System.Action<Transform> build)
        {
            var root = new GameObject(name);
            var it = root.AddComponent<Item>();
            it.type = type;
            build(root.transform);
            var p = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(root);
            return p;
        }

        static void ZoneMark(Transform parent, Vector3 lpos, Vector2 size)
        {
            var mat = M("AlanIsareti", Hex("FFFFFF"));
            float t = 0.08f, y = 0.03f;
            Box("ZoneEdge", parent, lpos + new Vector3(0, y, size.y * 0.5f), new Vector3(size.x, 0.01f, t), mat);
            Box("ZoneEdge", parent, lpos + new Vector3(0, y, -size.y * 0.5f), new Vector3(size.x, 0.01f, t), mat);
            Box("ZoneEdge", parent, lpos + new Vector3(size.x * 0.5f, y, 0), new Vector3(t, 0.01f, size.y), mat);
            Box("ZoneEdge", parent, lpos + new Vector3(-size.x * 0.5f, y, 0), new Vector3(t, 0.01f, size.y), mat);
        }

        static Image UIImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        static WorldLabel Label(Transform parent, Vector3 lpos, string text, float size, Color col)
        {
            var g = Empty("Label", parent, lpos);
            var tm = g.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = font;
            tm.fontSize = 64;
            tm.characterSize = size * 1.5f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = col;
            g.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            return g.AddComponent<WorldLabel>();
        }

        static GameObject Empty(string name, Transform parent, Vector3 lpos)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = lpos;
            return g;
        }

        static GameObject Box(string name, Transform parent, Vector3 lpos, Vector3 lscale, Material mat)
            => Prim(PrimitiveType.Cube, name, parent, lpos, lscale, mat);

        static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 lpos, Vector3 lscale, Material mat, Vector3? euler = null)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = name;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = lpos;
            g.transform.localScale = lscale;
            if (euler.HasValue) g.transform.localEulerAngles = euler.Value;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            return g;
        }

        static Material M(string name, Color c)
        {
            if (mats.TryGetValue(name, out var m)) return m;
            string path = Root + "/Materials/" + name + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(m);
            mats[name] = m;
            return m;
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
