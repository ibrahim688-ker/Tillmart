using System.Collections.Generic;
using UnityEngine;

namespace CiftlikMarket
{
    public enum StackLayout { Column, Grid }

    /// <summary>Ürün tutan kap: oyuncunun sırtı, raf, istasyon girişi/çıkışı, para yığını.</summary>
    public class ItemContainer : MonoBehaviour
    {
        public int capacity = 4;
        public StackLayout layout = StackLayout.Column;
        [Header("Grid")]
        public int gridColumns = 2;
        public int gridRows = 2;
        public float spacingX = 0.4f;
        public float spacingZ = 0.4f;
        public float layerHeight = 0.3f;
        [Tooltip("Doluysa: her sıranın (0 = en ön) kendi z konumu. Ör. basamaklı rafta ön/orta/arka basamak. Boşsa düzenli ızgara.")]
        public float[] rowZ;
        [Tooltip("rowZ ile birlikte: her sıranın yüksekliği (basamak / buzdolabı rafı)")]
        public float[] rowY;

        public readonly List<Item> items = new List<Item>();

        public int Count => items.Count;
        public bool IsFull => items.Count >= capacity;

        public int CountOf(ItemType t)
        {
            int c = 0;
            for (int i = 0; i < items.Count; i++) if (items[i].type == t) c++;
            return c;
        }

        public void Add(Item it, bool animate = true)
        {
            items.Add(it);
            var lp = SlotPos(items.Count - 1);
            if (animate) it.FlyTo(transform, lp);
            else
            {
                it.transform.SetParent(transform, false);
                it.transform.localPosition = lp;
                it.transform.localRotation = Quaternion.identity;
            }
        }

        public Item TakeLast(ItemType t)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i].type != t) continue;
                var it = items[i];
                items.RemoveAt(i);
                Relayout();
                return it;
            }
            return null;
        }

        public Item TakeLastAny()
        {
            if (items.Count == 0) return null;
            var it = items[items.Count - 1];
            items.RemoveAt(items.Count - 1);
            return it;
        }

        void Relayout()
        {
            for (int i = 0; i < items.Count; i++) items[i].SetTarget(SlotPos(i));
        }

        /// <summary>Yığındaki i. ürünün salınımsız yerel konumu.</summary>
        public Vector3 BasePos(int i) => SlotPos(i);

        Vector3 SlotPos(int i)
        {
            if (layout == StackLayout.Column)
            {
                float y = 0f;
                for (int k = 0; k < i; k++) y += ItemHeight(items[k]);
                return new Vector3(0f, y, 0f);
            }
            int per = Mathf.Max(1, gridColumns * gridRows);
            int layer = i / per;
            int idx = i % per;
            int cx = idx % gridColumns;
            int cz = idx / gridColumns;
            float ox = (gridColumns - 1) * spacingX * 0.5f;
            if (rowZ != null && rowZ.Length >= gridRows)
            {
                // sıra sıra doldur: önce ön sıra, sonra arkadakiler; hepsi dolunca ön sıradan itibaren üstüne kat
                float ry = rowY != null && rowY.Length > cz ? rowY[cz] : 0f;
                return new Vector3(cx * spacingX - ox, ry + layer * layerHeight, rowZ[cz]);
            }
            float oz = (gridRows - 1) * spacingZ * 0.5f;
            return new Vector3(cx * spacingX - ox, layer * layerHeight, cz * spacingZ - oz);
        }

        static float ItemHeight(Item it)
        {
            var d = GameConfig.I != null ? GameConfig.I.Def(it.type) : null;
            return d != null ? d.height : 0.3f;
        }
    }
}
