using UnityEngine;
using UnityEngine.InputSystem;

namespace CiftlikMarket
{
    /// <summary>Ekranın herhangi bir yerine dokunup sürükleyince çıkan sanal joystick + editörde WASD.</summary>
    public class PlayerController : MonoBehaviour
    {
        public float speed = 4.5f;
        public Transform model;
        public RectTransform joyBase, joyKnob;
        public float joyRadius = 110f;
        public Vector2 boundsMin = new Vector2(-8f, -10f);
        public Vector2 boundsMax = new Vector2(7.5f, 12.5f);

        bool dragging, ignorePress;
        Vector2 start;
        Canvas canvas;
        float bob;

        CharacterController cc;

        void Start()
        {
            cc = GetComponent<CharacterController>();
            if (joyBase) canvas = joyBase.GetComponentInParent<Canvas>();
            SetJoy(false);
        }

        void Update()
        {
            Vector2 input = Vector2.zero;

            var p = Pointer.current;
            bool pressed = p != null && p.press.isPressed;
            // Yükseltme butonuna ya da açık ekrana yapılan dokunuş joystick'i çalıştırmasın
            if (!pressed) ignorePress = false;
            else if (!dragging && !ignorePress && (UIBlock.Active || OfficeUI.IsOverButton(p.position.ReadValue()))) ignorePress = true;
            if (pressed && !ignorePress && !UIBlock.Active)
            {
                Vector2 pos = p.position.ReadValue();
                if (!dragging) { dragging = true; start = pos; SetJoy(true); }
                float r = joyRadius * (canvas ? canvas.scaleFactor : 1f);
                Vector2 d = pos - start;
                if (d.magnitude > r) d = d.normalized * r;
                if (d.magnitude > r * 0.1f) input = d / r;
                if (joyBase) { joyBase.position = start; joyKnob.position = start + d; }
            }
            else if (dragging) { dragging = false; SetJoy(false); }

            var k = Keyboard.current;
            if (k != null)
            {
                float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
                float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
                if (x != 0f || y != 0f) input = new Vector2(x, y).normalized;
            }

            var mv = new Vector3(input.x, 0f, input.y);
            float mag = Mathf.Clamp01(mv.magnitude);
            if (mag > 0.01f)
            {
                var pos = transform.position;
                var np = pos + mv.normalized * speed * mag * Time.deltaTime;
                np.x = Mathf.Clamp(np.x, boundsMin.x, boundsMax.x);
                np.z = Mathf.Clamp(np.z, boundsMin.y, boundsMax.y);
                var delta = np - pos;
                delta.y = -pos.y; // hep zeminde kal
                if (cc != null && cc.enabled) cc.Move(delta); // duvar/raf/tezgah çarpışması
                else transform.position = np;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(mv), 1f - Mathf.Exp(-15f * Time.deltaTime));
                bob += Time.deltaTime * 14f;
            }
            else bob = 0f;

            PushOutOfWorkers();

            if (model) model.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(bob)) * 0.08f, 0f);
        }

        Worker[] workers;
        float workerScan;

        /// <summary>Oyuncu çalışanların içinden geçmesin: üst üste binerse dışarı it.</summary>
        void PushOutOfWorkers()
        {
            workerScan -= Time.deltaTime;
            if (workers == null || workerScan <= 0f) { workers = FindObjectsByType<Worker>(FindObjectsSortMode.None); workerScan = 1f; }
            const float minDist = 0.62f;
            var pos = transform.position;
            Vector3 push = Vector3.zero;
            foreach (var w in workers)
            {
                if (w == null || !w.isActiveAndEnabled) continue;
                var d = pos - w.transform.position; d.y = 0f;
                float m = d.magnitude;
                if (m >= minDist) continue;
                if (m < 0.001f) d = -transform.forward; else d /= m;
                push += d * (minDist - m);
            }
            if (push.sqrMagnitude < 1e-6f) return;
            if (cc != null && cc.enabled) cc.Move(push);
            else transform.position = pos + push;
        }

        void SetJoy(bool on)
        {
            if (joyBase) joyBase.gameObject.SetActive(on);
            if (joyKnob) joyKnob.gameObject.SetActive(on);
        }
    }
}
