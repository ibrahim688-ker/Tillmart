using UnityEditor;
using UnityEngine;

namespace CiftlikMarket.EditorTools
{
    public static partial class PrototypeBuilder
    {
        static GameObject CarPrefab()
        {
            var root = new GameObject("Car");
            var car = root.AddComponent<DriveThruCar>();
            var body = Box("Body", root.transform, new Vector3(0, 0.55f, 0), new Vector3(1.4f, 0.6f, 2.6f), M("Araba", Color.white));
            Box("Cabin", root.transform, new Vector3(0, 1.1f, -0.15f), new Vector3(1.2f, 0.55f, 1.3f), M("ArabaCam", Hex("B3E5FC")));
            var tire = M("Teker", Hex("212121"));
            foreach (var p in new[] { new Vector3(-0.72f, 0.28f, 0.85f), new Vector3(0.72f, 0.28f, 0.85f), new Vector3(-0.72f, 0.28f, -0.85f), new Vector3(0.72f, 0.28f, -0.85f) })
                Prim(PrimitiveType.Cylinder, "Wheel", root.transform, p, new Vector3(0.5f, 0.08f, 0.5f), tire, new Vector3(0, 0, 90));
            var lightMat = M("Far", Hex("FFF59D"));
            Box("LightL", root.transform, new Vector3(-0.45f, 0.62f, 1.31f), new Vector3(0.3f, 0.15f, 0.02f), lightMat);
            Box("LightR", root.transform, new Vector3(0.45f, 0.62f, 1.31f), new Vector3(0.3f, 0.15f, 0.02f), lightMat);

            var trunkGo = Empty("Trunk", root.transform, new Vector3(0, 1.38f, -0.15f));
            var tc = trunkGo.AddComponent<ItemContainer>();
            tc.capacity = 12; tc.layout = StackLayout.Grid; tc.gridColumns = 3; tc.gridRows = 2; tc.spacingX = 0.38f; tc.spacingZ = 0.45f; tc.layerHeight = 0.3f;

            var bar = Empty("PatienceBar", root.transform, new Vector3(0, 2.9f, 0));
            bar.AddComponent<Billboard>();
            Box("BarBG", bar.transform, Vector3.zero, new Vector3(2.1f, 0.3f, 0.04f), M("BarArka", Hex("263238")));
            var pivot = Empty("FillPivot", bar.transform, new Vector3(-1.0f, 0, -0.03f));
            var fill = Box("Fill", pivot.transform, new Vector3(1.0f, 0, 0), new Vector3(2.0f, 0.2f, 0.04f), M("BarDolum", Color.white));

            car.trunk = tc;
            car.body = body.GetComponent<Renderer>();
            car.barRoot = bar;
            car.barFill = pivot.transform;
            car.barRenderer = fill.GetComponent<Renderer>();
            car.label = Label(root.transform, new Vector3(0, 3.4f, 0), "", 0.045f, Hex("D32F2F"));

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/Car.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Marketin sol duvarındaki cam + dışındaki araba şeridi. Kök, camın iç tarafındaki oyuncu alanıdır.</summary>
        static DriveThru DriveThruB(Transform parent, MoneyPile money, GameObject carPrefab)
        {
            var root = Empty("DriveThru", parent, new Vector3(-7.2f, 0, 13.5f));
            var frame = M("CamCerceve", Hex("37474F"));
            Box("Window", root.transform, new Vector3(-0.95f, 0.8f, 0), new Vector3(0.36f, 0.8f, 1.5f), M("Cam", Hex("90CAF9")));
            Box("WindowTop", root.transform, new Vector3(-0.95f, 1.25f, 0), new Vector3(0.4f, 0.1f, 1.6f), frame);
            Box("Sill", root.transform, new Vector3(-1.3f, 0.85f, 0), new Vector3(0.4f, 0.06f, 1.5f), frame);
            Box("BoardPole", root.transform, new Vector3(-1.75f, 0.8f, 3.0f), new Vector3(0.1f, 1.6f, 0.1f), frame);
            Box("Board", root.transform, new Vector3(-1.75f, 1.75f, 3.0f), new Vector3(0.12f, 0.7f, 1.0f), M("Menu", Hex("E53935")));

            ZoneMark(root.transform, Vector3.zero, new Vector2(1.4f, 1.8f));

            var dt = root.AddComponent<DriveThru>();
            dt.zoneSize = new Vector2(1.4f, 1.8f);
            dt.carPrefab = carPrefab;
            dt.moneyPile = money;
            dt.payPoint = Empty("PayPoint", root.transform, new Vector3(-0.6f, 1.0f, 0)).transform;
            dt.spawnPoint = Empty("CarSpawn", root.transform, new Vector3(-3.6f, 0, 14f)).transform;   // dünya z ≈ 27.5
            dt.exitPoint = Empty("CarExit", root.transform, new Vector3(-3.6f, 0, -30f)).transform;    // dünya z ≈ -16.5
            dt.queuePoints = new[]
            {
                Empty("CarStop0", root.transform, new Vector3(-3.6f, 0, 0f)).transform,
                Empty("CarStop1", root.transform, new Vector3(-3.6f, 0, 3.4f)).transform,
                Empty("CarStop2", root.transform, new Vector3(-3.6f, 0, 6.8f)).transform,
            };
            dt.orderLabel = Label(root.transform, new Vector3(0.9f, 2.4f, 0), "DRIVE-THRU", 0.05f, Dark);
            return dt;
        }
    }
}
