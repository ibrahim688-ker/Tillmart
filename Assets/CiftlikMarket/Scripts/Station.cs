using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Zeminde dikdörtgen bir etkileşim alanı olan istasyonların tabanı.</summary>
    public abstract class Station : MonoBehaviour
    {
        public Vector3 zoneOffset;
        public Vector2 zoneSize = new Vector2(2f, 2f);

        public bool InZone(Vector3 p)
        {
            var c = transform.position + zoneOffset;
            return Mathf.Abs(p.x - c.x) <= zoneSize.x * 0.5f && Mathf.Abs(p.z - c.z) <= zoneSize.y * 0.5f;
        }

        protected void ForEachCarrier(System.Action<Carrier> action)
        {
            for (int i = Carrier.All.Count - 1; i >= 0; i--)
            {
                var c = Carrier.All[i];
                if (InZone(c.transform.position)) action(c);
            }
        }

        protected Carrier PlayerInZone()
        {
            foreach (var c in Carrier.All)
                if (c.isPlayer && InZone(c.transform.position)) return c;
            return null;
        }

        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
            Gizmos.DrawWireCube(transform.position + zoneOffset, new Vector3(zoneSize.x, 0.05f, zoneSize.y));
        }
    }
}
