using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Hafif oyun efektleri: ürün alıp bırakırken parıltı, para toplarken altın parıltı,
    /// alan açılınca / yükseltme alınınca konfeti, oyuncu yürürken toz.
    /// Her efekt türü için tek bir ParticleSystem vardır ve sadece Emit ile tetiklenir (her olayda yeni nesne yok).
    /// </summary>
    public class Fx : MonoBehaviour
    {
        static Fx I;

        ParticleSystem sparkle, dust, confetti, coin;
        Carrier player;
        Vector3 lastDust;
        bool dustSide;
        float swayCheck;

        static readonly Color[] ConfettiColors =
        {
            new Color(1f, 0.32f, 0.38f), new Color(1f, 0.78f, 0.2f), new Color(0.3f, 0.78f, 1f),
            new Color(0.45f, 0.9f, 0.4f), new Color(0.75f, 0.5f, 1f), new Color(1f, 0.55f, 0.2f)
        };

        void Awake()
        {
            I = this;
            var round = Resources.Load<Material>("Fx/FxDaire");
            var square = Resources.Load<Material>("Fx/FxKare");
            sparkle = Make("Parilti", round, 300, 0f, true, false);
            coin = Make("AltinParilti", round, 200, 0f, true, false);
            dust = Make("Toz", round, 120, -0.05f, false, true);
            confetti = Make("Konfeti", square ? square : round, 400, 1.1f, true, false);

            var rot = confetti.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            var lim = confetti.limitVelocityOverLifetime;   // havada süzülsün
            lim.enabled = true;
            lim.limit = 9f;
            lim.dampen = 0.08f;
        }

        ParticleSystem Make(string n, Material mat, int max, float gravity, bool shrink, bool grow)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = 0.2f;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var size = ps.sizeOverLifetime;
            size.enabled = shrink || grow;
            if (shrink) size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));
            if (grow) size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.55f, 1f, 1.5f));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingFudge = -2f;   // yazı ve balonların arkasında kalsın
            ps.Play();
            return ps;
        }

        // ---------------- tetikleyiciler ----------------

        /// <summary>Ürün alma/bırakma: birkaç küçük parlak nokta yukarı saçılır.</summary>
        public static void Sparkle(Vector3 pos) { Sparkle(pos, new Color(1f, 0.96f, 0.7f)); }

        public static void Sparkle(Vector3 pos, Color c)
        {
            if (I == null || I.sparkle == null) return;
            var p = new ParticleSystem.EmitParams();
            for (int i = 0; i < 5; i++)
            {
                p.position = pos + Random.insideUnitSphere * 0.15f;
                var v = Random.insideUnitSphere * 1.2f; v.y = Mathf.Abs(v.y) + 1.4f;
                p.velocity = v;
                p.startLifetime = Random.Range(0.3f, 0.45f);
                p.startSize = Random.Range(0.16f, 0.26f);
                p.startColor = i == 0 ? Color.white : c;
                I.sparkle.Emit(p, 1);
            }
        }

        /// <summary>Para toplarken altın parıltı.</summary>
        public static void Coin(Vector3 pos)
        {
            if (I == null || I.coin == null) return;
            var p = new ParticleSystem.EmitParams();
            for (int i = 0; i < 10; i++)
            {
                p.position = pos + Random.insideUnitSphere * 0.3f;
                var v = Random.insideUnitSphere * 2f; v.y = Mathf.Abs(v.y) + 2f;
                p.velocity = v;
                p.startLifetime = Random.Range(0.35f, 0.6f);
                p.startSize = Random.Range(0.2f, 0.32f);
                p.startColor = Random.value < 0.3f ? Color.white : new Color(1f, 0.82f, 0.25f);
                I.coin.Emit(p, 1);
            }
        }

        /// <summary>Konfeti: alan açılınca (büyük) ya da yükseltme alınınca (küçük).</summary>
        public static void Confetti(Vector3 pos, bool big = true)
        {
            if (I == null || I.confetti == null) return;
            int n = big ? 70 : 34;
            var p = new ParticleSystem.EmitParams();
            for (int i = 0; i < n; i++)
            {
                p.position = pos + new Vector3(Random.Range(-0.6f, 0.6f), 0.3f, Random.Range(-0.6f, 0.6f));
                var v = Random.insideUnitSphere * (big ? 3.2f : 2.4f);
                v.y = Random.Range(big ? 5.5f : 4f, big ? 8.5f : 6f);
                p.velocity = v;
                p.startLifetime = Random.Range(1.3f, 2f);
                p.startSize = Random.Range(0.14f, 0.24f);
                p.rotation = Random.Range(0f, 360f);
                p.startColor = ConfettiColors[Random.Range(0, ConfettiColors.Length)];
                I.confetti.Emit(p, 1);
            }
        }

        /// <summary>Ayak tozu.</summary>
        public static void Dust(Vector3 pos)
        {
            if (I == null || I.dust == null) return;
            var p = new ParticleSystem.EmitParams();
            for (int i = 0; i < 2; i++)
            {
                p.position = pos + new Vector3(Random.Range(-0.12f, 0.12f), 0.08f, Random.Range(-0.12f, 0.12f));
                p.velocity = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0.25f, 0.5f), Random.Range(-0.35f, 0.35f));
                p.startLifetime = Random.Range(0.4f, 0.6f);
                p.startSize = Random.Range(0.32f, 0.48f);
                p.startColor = new Color(0.95f, 0.9f, 0.78f, 0.72f);
                I.dust.Emit(p, 1);
            }
        }

        // oyuncu yürürken her ~0,6 m'de bir sırayla sol / sağ ayaktan toz
        void Update()
        {
            if (player == null)
            {
                foreach (var c in Carrier.All) if (c.isPlayer) { player = c; lastDust = c.transform.position; break; }
                if (player == null) return;
                // oyuncunun modeline kalkış/duruş esnemesi
                var pc = player.GetComponent<PlayerController>();
                if (pc != null && pc.model != null && pc.model.GetComponent<SquashStretch>() == null)
                    pc.model.gameObject.AddComponent<SquashStretch>().root = player.transform;
            }
            // taşıyanların yığınına salınım (yeni gelen çalışanlar için ara ara bakılır)
            swayCheck -= Time.deltaTime;
            if (swayCheck <= 0f)
            {
                swayCheck = 1f;
                foreach (var c in Carrier.All)
                    if (c.stack != null && c.stack.layout == StackLayout.Column && c.stack.GetComponent<StackSway>() == null)
                        c.stack.gameObject.AddComponent<StackSway>();
            }
            var pos = player.transform.position;
            var d = pos - lastDust; d.y = 0f;
            if (d.sqrMagnitude > 4f) { lastDust = pos; return; }   // ışınlanma
            if (d.sqrMagnitude < 0.36f) return;
            var dir = d.normalized;
            var side = Vector3.Cross(Vector3.up, dir) * (dustSide ? 0.14f : -0.14f);
            dustSide = !dustSide;
            Dust(pos - dir * 0.25f + side);
            lastDust = pos;
        }
    }
}
