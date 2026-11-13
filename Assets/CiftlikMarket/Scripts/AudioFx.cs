using System.Collections.Generic;
using UnityEngine;

namespace CiftlikMarket
{
    public enum Sfx { Pickup, Drop, Coin, Cash, Unlock, Horn, Tap, LevelUp, Popup, Moo, Cluck, Rush }

    /// <summary>
    /// Oyun sesleri ve müzik. Efektler Resources/Audio'daki dosyalardan yüklenir (yoksa kısa sentez sesine düşer).
    /// Arka plan müziği döngüde çalar, yoğun saatte biraz hızlanır. Hayvanlar ara sıra ses çıkarır.
    /// Ses ve müzik ayrı ayrı açılıp kapatılabilir (kalıcı). Kendini otomatik kurar: AudioFx.Play(Sfx.Coin) yeterli.
    /// </summary>
    public class AudioFx : MonoBehaviour
    {
        public static bool Muted;   // geriye dönük uyumluluk (tüm efektleri susturur)

        public static bool SfxOn
        {
            get => PlayerPrefs.GetInt("cmg_sfx", 1) == 1;
            set { PlayerPrefs.SetInt("cmg_sfx", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt("cmg_music", 1) == 1;
            set { PlayerPrefs.SetInt("cmg_music", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>İnek, tavuk gibi ortam sesleri.</summary>
        public static bool AmbientOn
        {
            get => PlayerPrefs.GetInt("cmg_ambient", 1) == 1;
            set { PlayerPrefs.SetInt("cmg_ambient", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Müzik ses seviyesi (0..1).</summary>
        public static float MusicLevel
        {
            get => PlayerPrefs.GetFloat("cmg_musicvol", 1f);
            set => PlayerPrefs.SetFloat("cmg_musicvol", Mathf.Clamp01(value));
        }

        static AudioFx I;

        const int Rate = 44100;
        const float MusicVolume = 0.32f;
        static readonly string[] Files = { "pickup", "drop", "coin", "cash", "unlock", "horn", "tap", "levelup", "popup", "moo", "cluck", "rush" };

        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        float[] last;
        AudioSource[] pool;
        AudioSource music;
        int next;
        float ambientTimer = 6f;
        static Transform playerT;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("AudioFx");
            DontDestroyOnLoad(go);
            I = go.AddComponent<AudioFx>();
        }

        void Awake()
        {
            int count = System.Enum.GetValues(typeof(Sfx)).Length;
            last = new float[count];
            pool = new AudioSource[10];
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = gameObject.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
                pool[i].spatialBlend = 0f;
            }

            for (int i = 0; i < count; i++)
            {
                var clip = Resources.Load<AudioClip>("Audio/" + Files[i]);
                clips[(Sfx)i] = clip != null ? clip : Fallback((Sfx)i);
            }

            music = gameObject.AddComponent<AudioSource>();
            music.clip = Resources.Load<AudioClip>("Audio/music_farm");
            music.loop = true;
            music.playOnAwake = false;
            music.volume = 0f;
            music.priority = 0;
            if (music.clip) music.Play();
        }

        void Update()
        {
            // müzik: aç/kapa yumuşak geçişli, yoğun saatte hızlanır
            if (music && music.clip)
            {
                float target = MusicOn ? MusicVolume * MusicLevel : 0f;
                if (Mathf.Abs(music.volume - target) < 0.05f && MusicOn) music.volume = target;   // kaydırıcı anında etkisin
                music.volume = Mathf.MoveTowards(music.volume, target, Time.unscaledDeltaTime * 0.5f);
                music.pitch = Mathf.MoveTowards(music.pitch, RushHour.Active ? 1.08f : 1f, Time.unscaledDeltaTime * 0.1f);
            }

            // hayvanların ara sıra sesi (ekranda görünen, oyuncuya yakın olanlar)
            ambientTimer -= Time.deltaTime;
            if (ambientTimer <= 0f)
            {
                ambientTimer = Random.Range(7f, 15f);
                var animals = FindObjectsByType<AnimalAnimator>(FindObjectsSortMode.None);
                if (animals.Length > 0 && AmbientOn)
                {
                    var a = animals[Random.Range(0, animals.Length)];
                    PlayAt(a.kind == AnimalAnimator.Kind.Cow ? Sfx.Moo : Sfx.Cluck, a.transform.position, 0.55f);
                }
            }
        }

        public static void Play(Sfx s, float volume = 1f)
        {
            if (I == null || Muted) return;
            bool ambient = s == Sfx.Moo || s == Sfx.Cluck;
            if (ambient ? !AmbientOn : !SfxOn) return;
            int i = (int)s;
            float t = Time.unscaledTime;
            if (t - I.last[i] < 0.045f) return; // aynı sesi üst üste bindirme
            I.last[i] = t;
            var src = I.pool[I.next];
            I.next = (I.next + 1) % I.pool.Length;
            bool fixedPitch = s == Sfx.Horn || s == Sfx.LevelUp || s == Sfx.Unlock || s == Sfx.Rush;
            src.pitch = fixedPitch ? 1f : Random.Range(0.94f, 1.06f);
            if (s == Sfx.Moo) src.pitch = Random.Range(0.9f, 1.08f);
            src.PlayOneShot(I.clips[s], volume);
        }

        /// <summary>Dünyadaki bir noktadan gelen ses: oyuncuya uzaklaştıkça kısılır, uzaktaysa çalmaz.</summary>
        public static void PlayAt(Sfx s, Vector3 worldPos, float volume = 1f)
        {
            if (!playerT) { var pc = FindAnyObjectByType<PlayerController>(); if (pc) playerT = pc.transform; }
            Vector3 listener = playerT ? playerT.position : worldPos;
            float d = Vector3.Distance(new Vector3(listener.x, 0, listener.z), new Vector3(worldPos.x, 0, worldPos.z));
            float k = Mathf.Clamp01(1f - (d - 4f) / 14f);
            if (k <= 0.05f) return;
            Play(s, volume * k);
        }

        // ---------------- Yedek sentez (dosya bulunamazsa) ----------------

        static AudioClip Fallback(Sfx s)
        {
            switch (s)
            {
                case Sfx.Pickup: return Sweep("pickup", 0.06f, 700f, 1150f, 0.35f);
                case Sfx.Drop: return Sweep("drop", 0.07f, 620f, 380f, 0.35f);
                case Sfx.Coin: return Notes("coin", new[] { 1318.5f, 1760f }, new[] { 0.06f, 0.16f }, 0.45f);
                case Sfx.Cash: return Notes("cash", new[] { 1046.5f, 1568f }, new[] { 0.05f, 0.25f }, 0.4f);
                case Sfx.Horn: return Sweep("horn", 0.3f, 350f, 340f, 0.3f);
                case Sfx.Tap: return Sweep("tap", 0.03f, 1200f, 1300f, 0.25f);
                default: return Notes(s.ToString(), new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, new[] { 0.08f, 0.08f, 0.08f, 0.3f }, 0.45f);
            }
        }

        static AudioClip Sweep(string name, float len, float f0, float f1, float amp)
        {
            int n = (int)(Rate * len);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n;
                phase += 2f * Mathf.PI * Mathf.Lerp(f0, f1, k) / Rate;
                data[i] = Mathf.Sin(phase) * Mathf.Min(1f, i / (Rate * 0.004f)) * (1f - k) * (1f - k) * amp;
            }
            return Make(name, data);
        }

        static AudioClip Notes(string name, float[] freqs, float[] lens, float amp)
        {
            int total = 0;
            foreach (var l in lens) total += (int)(Rate * l);
            var data = new float[total];
            int o = 0;
            for (int j = 0; j < freqs.Length; j++)
            {
                int n = (int)(Rate * lens[j]);
                for (int i = 0; i < n; i++)
                {
                    float tt = (float)i / Rate;
                    float env = Mathf.Min(1f, i / (Rate * 0.003f)) * Mathf.Exp(-tt * (j == freqs.Length - 1 ? 9f : 20f));
                    data[o + i] = (Mathf.Sin(2f * Mathf.PI * freqs[j] * tt) + 0.3f * Mathf.Sin(4f * Mathf.PI * freqs[j] * tt)) * env * amp * 0.77f;
                }
                o += n;
            }
            return Make(name, data);
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
