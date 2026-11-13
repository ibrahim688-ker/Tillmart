using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>Kasanın yanında biriken para. Oyuncu üstünden geçince toplanır.</summary>
    public class MoneyPile : Station
    {
        public ItemContainer visual;
        public int valuePerBill = 5;
        public int amount;

        public void Add(int value, Vector3 from, bool applyBonus = true)
        {
            if (applyBonus) value = Mathf.RoundToInt(value * BranchManager.IncomeMultiplier); // şube bonusu
            amount += value;
            if (OfflineEarnings.I != null) OfflineEarnings.I.ReportIncome(value);
            int target = Mathf.Min(Mathf.CeilToInt(amount / (float)valuePerBill), visual.capacity);
            while (visual.Count < target)
                visual.Add(GameConfig.I.Spawn(ItemType.Para, from + Vector3.up * 0.5f));
        }

        void Update()
        {
            if (amount <= 0) return;
            var player = PlayerInZone();
            if (player == null) return;

            GameManager.I.AddMoney(amount);
            AudioFx.Play(Sfx.Coin);
            Fx.Coin(transform.position + Vector3.up * 0.5f);
            Haptics.Medium();
            amount = 0;
            int i = 0;
            while (visual.Count > 0)
            {
                var b = visual.TakeLastAny();
                var go = b.gameObject;
                b.FlyTo(player.transform, new Vector3(0f, 1.2f, 0f), 0.2f + 0.015f * i++, () => Destroy(go));
            }
        }
    }
}
