using System.Collections.Generic;
using UnityEngine;

namespace CiftlikMarket
{
    public enum ItemType { Saman, Sut, Misir, Yumurta, Para, Domates, Yogurt, Ayran, Peynir, Tereyag, Bugday, Un, Pogaca, Salca,
        Cay, Seker, Makarna, Pirinc, Yag, Cikolata }   // son altısı toptancıdan gelir (Wholesale)

    [System.Serializable]
    public class ItemDef
    {
        public ItemType type;
        public string displayName;
        public int price;
        public float height = 0.3f;
        public GameObject prefab;
    }

    /// <summary>Oyundaki ürün tanımları ve ortak ayarlar. Sahnede tek olmalı.</summary>
    public class GameConfig : MonoBehaviour
    {
        public static GameConfig I;

        public List<ItemDef> items = new List<ItemDef>();
        [Tooltip("Bir ürünün taşıyıcı ile istasyon arasında aktarılma aralığı (sn)")]
        public float transferInterval = 0.12f;

        /// <summary>Ürün değeri yükseltme seviyeleri (her seviye fiyatı %10 artırır).</summary>
        public static readonly Dictionary<ItemType, int> ValueLevel = new Dictionary<ItemType, int>();

        void Awake() { I = this; ValueLevel.Clear(); }

        /// <summary>Ürünün şu anki satış fiyatı (ürün değeri yükseltmesi dahil).</summary>
        public int Price(ItemType t)
        {
            int lv;
            ValueLevel.TryGetValue(t, out lv);
            return PriceAt(t, lv);
        }

        public int PriceAt(ItemType t, int level)
        {
            var d = Def(t);
            if (d == null) return 0;
            // %10 / seviye; ucuz ürünlerde yuvarlama artışı yutmasın diye her seviye en az +1
            return level <= 0 ? d.price : d.price + Mathf.Max(level, Mathf.RoundToInt(d.price * 0.1f * level));
        }

        public ItemDef Def(ItemType t)
        {
            for (int i = 0; i < items.Count; i++)
                if (items[i].type == t) return items[i];
            return null;
        }

        public string Name(ItemType t)
        {
            var d = Def(t);
            return d != null ? d.displayName : t.ToString();
        }

        public Item Spawn(ItemType t, Vector3 pos)
        {
            var d = Def(t);
            var go = Instantiate(d.prefab, pos, Quaternion.identity);
            var it = go.GetComponent<Item>();
            it.type = t;
            return it;
        }
    }
}
