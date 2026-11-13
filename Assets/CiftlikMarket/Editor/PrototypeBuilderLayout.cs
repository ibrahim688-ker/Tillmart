using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CiftlikMarket.EditorTools
{
    public static partial class PrototypeBuilder
    {
        /// <summary>
        /// Market düzeni: süt ürünleri arka duvarda camlı soğuk dolaplarda, diğer ürünler ortadaki raf sırasında.
        /// İkinci orta sıra (z≈5.6) Bölge 3 ürünleri için boş. Rafları ve kilit alanlarını taşır; başka bir şeye dokunmaz.
        /// Tekrar çalıştırmak güvenlidir.
        /// </summary>
        [MenuItem("CiftlikMarket/Market Düzeni - Orta Raflar (uygula)")]
        public static void RearrangeShelvesMenu()
        {
            mats.Clear();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            int n = RearrangeShelves();
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CiftlikMarket] Market düzeni uygulandı: " + n + " raf taşındı.");
        }

        // Arka duvar dolapları (ürün tarafı aşağı bakar, müşteri z≈13.25'te durur)
        const float WallRowZ = 14.4f;
        // Orta sıra 1 (müşteri z≈8.85'te durur)
        const float MidRow1Z = 10.0f;

        static int RearrangeShelves()
        {
            var world = FindInScene("World");
            if (world == null) return 0;
            var market = world.Find("Market");
            var uz = world.Find("UnlockZones");
            int n = 0;

            n += MoveShelf(market, uz, "Raf_Sut", null, new Vector3(-1.5f, 0, WallRowZ), true);
            n += MoveShelf(market, uz, "Raf_Yogurt", "raf_yogurt", new Vector3(2.5f, 0, WallRowZ), true);
            n += MoveShelf(market, uz, "Raf_Ayran", "raf_ayran", new Vector3(6.5f, 0, WallRowZ), true);

            n += MoveShelf(market, uz, "Raf_Yumurta", "raf_yumurta", new Vector3(3.0f, 0, MidRow1Z), false);
            n += MoveShelf(market, uz, "Raf_Domates", "raf_domates", new Vector3(7.0f, 0, MidRow1Z), false);
            n += MoveShelf(market, uz, "Raf_Peynir", "raf_peynir", new Vector3(11.0f, 0, MidRow1Z), false);
            n += MoveShelf(market, uz, "Raf_Tereyag", "raf_tereyag", new Vector3(15.0f, 0, MidRow1Z), false);
            return n;
        }

        static int MoveShelf(Transform market, Transform uzRoot, string shelfName, string unlockId, Vector3 pos, bool fridge)
        {
            var shelf = market.Find(shelfName);
            if (shelf == null) return 0;
            shelf.localPosition = pos;
            shelf.localRotation = Quaternion.identity;
            if (fridge) MakeFridge(shelf);

            if (unlockId != null && uzRoot != null)
            {
                var z = uzRoot.Find("Unlock_" + unlockId);
                if (z != null) z.localPosition = new Vector3(pos.x, 0, pos.z - 0.4f);
            }
            EditorUtility.SetDirty(shelf.gameObject);
            return 1;
        }

        /// <summary>Rafı camlı soğuk dolap görünümüne çevirir (aynı nesneler, ek yan paneller ve ışık).</summary>
        static void MakeFridge(Transform shelf)
        {
            var white = M("Dolap", Hex("F5F7F8"));
            var baseT = shelf.Find("Base");
            if (baseT) baseT.GetComponent<Renderer>().sharedMaterial = white;

            var back = shelf.Find("Back");
            if (back)
            {
                back.localPosition = new Vector3(0, 1.3f, 0.36f);
                back.localScale = new Vector3(1.8f, 1.7f, 0.12f);
                back.GetComponent<Renderer>().sharedMaterial = white;
            }
            if (shelf.Find("SideL") == null)
            {
                Box("SideL", shelf, new Vector3(-0.92f, 1.3f, 0.02f), new Vector3(0.06f, 1.7f, 0.76f), white);
                Box("SideR", shelf, new Vector3(0.92f, 1.3f, 0.02f), new Vector3(0.06f, 1.7f, 0.76f), white);
                Box("Glass", shelf, new Vector3(0, 1.98f, -0.36f), new Vector3(1.78f, 0.22f, 0.02f), M("DolapCam", new Color(0.75f, 0.9f, 1f)));
                Box("Glow", shelf, new Vector3(0, 2.1f, 0.29f), new Vector3(1.7f, 0.08f, 0.02f), M("DolapIsik", Hex("80DEEA")));
            }
            var sign = shelf.Find("Sign");
            if (sign) sign.localPosition = new Vector3(0, 2.3f, 0.36f);
            var label = shelf.Find("Label");
            if (label) label.localPosition = new Vector3(0, 2.85f, 0.36f);
        }
    }
}
