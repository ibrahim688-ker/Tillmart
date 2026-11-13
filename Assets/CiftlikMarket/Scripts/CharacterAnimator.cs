using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Ayrı uzuvlu low-poly karakter için prosedürel yürüme animasyonu.
    /// Hız, karakter kökünün (bu objenin parent'ı) konum değişiminden ölçülür; bu sayede
    /// oyuncu (CharacterController), çalışan ve müşteri (NavMesh) için aynı bileşen çalışır.
    /// Bir şey taşırken kollar öne uzanır (yığını tutuyormuş gibi).
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        public Transform hips, torso, head, armL, armR, legL, legR;

        [Header("Yürüme")]
        public float stepsPerMeter = 1.35f;     // adım sıklığı (mesafeye göre)
        public float legSwing = 38f;
        public float armSwing = 32f;
        public float bobHeight = 0.07f;
        public float fullSpeed = 3.5f;          // bu hızda animasyon tam genlikte
        public float leanAngle = 6f;

        [Header("Taşıma")]
        public ItemContainer carry;             // boş değilse kollar öne
        public float carryArmAngle = -70f;

        Transform root;
        Vector3 lastPos;
        float phase, amount, carryBlend, idleT;
        Quaternion rArmL, rArmR, rLegL, rLegR, rTorso, rHead;
        Vector3 basePos;

        void Awake()
        {
            root = transform.parent != null ? transform.parent : transform;
            if (!hips) hips = transform.Find("Hips");
            if (!torso) torso = transform.Find("Torso");
            if (!head) head = transform.Find("Head");
            if (!armL) armL = transform.Find("ArmL");
            if (!armR) armR = transform.Find("ArmR");
            if (!legL) legL = transform.Find("LegL");
            if (!legR) legR = transform.Find("LegR");
            if (armL) rArmL = armL.localRotation;
            if (armR) rArmR = armR.localRotation;
            if (legL) rLegL = legL.localRotation;
            if (legR) rLegR = legR.localRotation;
            if (torso) rTorso = torso.localRotation;
            if (head) rHead = head.localRotation;
            basePos = transform.localPosition;
            if (carry == null && root != null)
            {
                var c = root.GetComponent<Carrier>();
                if (c != null) carry = c.stack;
                else carry = root.GetComponentInChildren<ItemContainer>();
            }
            idleT = Random.value * 10f;
        }

        void OnEnable()
        {
            if (root == null) root = transform.parent != null ? transform.parent : transform;
            lastPos = root.position;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 delta = root.position - lastPos; delta.y = 0f;
            lastPos = root.position;
            float dist = delta.magnitude;
            if (dist > 2f) dist = 0f;                     // ışınlanma / spawn
            float speed = dist / dt;

            float target = Mathf.Clamp01(speed / fullSpeed);
            if (speed < 0.15f) target = 0f;
            amount = Mathf.Lerp(amount, target, 1f - Mathf.Exp(-12f * dt));

            phase += dist * stepsPerMeter * Mathf.PI;     // her metre ~1.35 adım
            if (amount < 0.02f) phase = Mathf.Lerp(phase, Mathf.Round(phase / Mathf.PI) * Mathf.PI, 1f - Mathf.Exp(-8f * dt));
            idleT += dt;

            bool carrying = carry != null && carry.Count > 0;
            carryBlend = Mathf.Lerp(carryBlend, carrying ? 1f : 0f, 1f - Mathf.Exp(-10f * dt));

            float s = Mathf.Sin(phase);
            float legA = s * legSwing * amount;
            float armA = s * armSwing * amount;
            Vector3 ax = Vector3.right;                   // kök uzayında X ekseni (model düz duruyor)

            if (legL) legL.localRotation = Quaternion.AngleAxis(legA, ax) * rLegL;
            if (legR) legR.localRotation = Quaternion.AngleAxis(-legA, ax) * rLegR;

            float carryA = carryArmAngle;
            float aL = Mathf.Lerp(-armA, carryA + armA * 0.15f, carryBlend);
            float aR = Mathf.Lerp(armA, carryA - armA * 0.15f, carryBlend);
            if (armL) armL.localRotation = Quaternion.AngleAxis(aL, ax) * rArmL;
            if (armR) armR.localRotation = Quaternion.AngleAxis(aR, ax) * rArmR;

            // gövde: yürürken öne eğilme + hafif yalpalama, dururken nefes
            float breathe = Mathf.Sin(idleT * 2.2f) * 0.012f * (1f - amount);
            if (torso) torso.localRotation = Quaternion.Euler(leanAngle * amount, 0f, Mathf.Sin(phase) * 3f * amount) * rTorso;
            if (head) head.localRotation = Quaternion.Euler(-leanAngle * 0.5f * amount, 0f, -Mathf.Sin(phase) * 2f * amount) * rHead;

            float bob = Mathf.Abs(Mathf.Cos(phase)) * bobHeight * amount;
            transform.localPosition = basePos + new Vector3(0f, bob + breathe, 0f);
        }
    }
}
