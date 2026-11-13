using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Açılır pencere kartı ekrana (çarpı düğmesi ve başlık şeridiyle birlikte) sığmıyorsa orantılı küçültür.
    /// Uzun/dar telefonlarda tuval genişliği 1080'in altına düşer (ör. 20:9'da ~966); kart taşmasın.
    /// </summary>
    public class PopupFit : MonoBehaviour
    {
        const float SideRoom = 70f;     // sağ üstte taşan çarpı düğmesi + kenar payı (her iki yana)
        const float TopRoom = 140f;     // üstte taşan başlık şeridi + alt pay

        RectTransform rt, area;
        Vector2 lastArea = new Vector2(-1, -1);

        void OnEnable() { lastArea = new Vector2(-1, -1); Fit(); }
        void LateUpdate() { Fit(); }

        void Fit()
        {
            if (rt == null) rt = (RectTransform)transform;
            if (area == null) area = rt.parent as RectTransform;
            if (area == null) return;
            var a = area.rect.size;
            if (a == lastArea) return;
            lastArea = a;
            var s = rt.sizeDelta;
            float k = Mathf.Min(1f, a.x / (s.x + SideRoom * 2f), a.y / (s.y + TopRoom * 2f));
            rt.localScale = new Vector3(k, k, 1f);
        }
    }
}
