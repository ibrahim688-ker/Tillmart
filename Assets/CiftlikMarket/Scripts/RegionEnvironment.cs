using System.Collections.Generic;
using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Şubenin bölgesine göre ortam: sis, ışık, yağmur ve bölgeye özel dekor.
    /// Karadeniz: serin ışık, hafif sis, ince yağmur, haritanın güneyinde teraslı çay bahçeleri.
    /// Sahneye kalıcı bir şey eklemez; her şey çalışma anında kurulur.
    /// </summary>
    public static class RegionEnvironment
    {
        public static void Apply(BranchManager.Theme t)
        {
            // sis
            if (t.fog.a > 0f)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = t.fog;
                RenderSettings.fogStartDistance = 30f;
                RenderSettings.fogEndDistance = 72f;
            }

            // ışık: bulutlu, serin
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                l.color = t.light;
                if (t.weather == BranchManager.Weather.Rain) l.intensity *= 0.82f;
            }

            if (t.weather == BranchManager.Weather.Rain) MakeRain();
            if (t.shortName == "Karadeniz") TeaTerraces();
        }

        // ---------------- yağmur ----------------

        static void MakeRain()
        {
            var go = new GameObject("Yagmur");
            go.AddComponent<FollowPlayer>().offset = new Vector3(0f, 13f, 3f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // kutu şeklin +Z yönü aşağı baksın

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 0.75f;
            main.startSpeed = 21f;
            main.startSize = 0.045f;
            main.startColor = new Color(0.86f, 0.92f, 1f, 0.42f);
            main.maxParticles = 1600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;

            var em = ps.emission;
            em.rateOverTime = 650f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 46f, 0.1f);

            var vel = ps.velocityOverLifetime;   // hafif rüzgâr
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(1.2f);
            vel.y = new ParticleSystem.MinMaxCurve(0f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.055f;
            r.lengthScale = 1.2f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var mat = Resources.Load<Material>("RainMat");
            if (mat == null)
            {
                var sh = Shader.Find("CiftlikMarket/Rain");
                if (sh != null) mat = new Material(sh);
            }
            if (mat != null) r.sharedMaterial = mat;
            ps.Play();
        }

        /// <summary>Yağmuru oyuncunun üstünde tutar (kamera neredeyse yağmur orada).</summary>
        public class FollowPlayer : MonoBehaviour
        {
            public Vector3 offset;
            Transform target;
            void LateUpdate()
            {
                if (!target) { var pc = FindAnyObjectByType<PlayerController>(); if (pc) target = pc.transform; else return; }
                transform.position = target.position + offset;
            }
        }

        // ---------------- Karadeniz çay bahçesi ----------------

        static readonly Color[] TeaColors = { new Color32(0x2F, 0x7A, 0x34, 255), new Color32(0x3B, 0x8E, 0x3E, 255), new Color32(0x26, 0x6B, 0x2D, 255) };

        /// <summary>Haritanın güneyindeki boş çimende, eğime yayılmış sıra sıra yuvarlak çay çalıları.</summary>
        static void TeaTerraces()
        {
            var root = new GameObject("CayBahcesi");
            var rnd = new System.Random(42);
            var verts = new List<Vector3>[3];
            var tris = new List<int>[3];
            var norms = new List<Vector3>[3];
            for (int i = 0; i < 3; i++) { verts[i] = new List<Vector3>(); tris[i] = new List<int>(); norms[i] = new List<Vector3>(); }

            // sıralar: z -16 .. -27, x -8 .. 38 (çiftlik parsellerinin güneyi)
            for (int row = 0; row < 7; row++)
            {
                float z = -16.2f - row * 1.7f;
                float y = row * 0.12f;                    // hafif yamaç
                for (float x = -8f; x < 38f; x += 1.05f)
                {
                    if (rnd.NextDouble() < 0.06) continue;            // ara sıra boşluk
                    int c = rnd.Next(3);
                    float jx = (float)(rnd.NextDouble() - 0.5) * 0.25f;
                    float jz = (float)(rnd.NextDouble() - 0.5) * 0.2f;
                    float s = 0.85f + (float)rnd.NextDouble() * 0.3f;
                    AddBlob(verts[c], tris[c], norms[c], new Vector3(x + jx, y + 0.28f * s, z + jz), new Vector3(0.62f, 0.4f, 0.5f) * s);
                }
                // sıra arası toprak şerit
            }

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            for (int i = 0; i < 3; i++)
            {
                var mesh = new Mesh { name = "CayCalisi_" + i, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(verts[i]);
                mesh.SetNormals(norms[i]);
                mesh.SetTriangles(tris[i], 0);
                mesh.RecalculateBounds();
                var go = new GameObject("Cay_" + i);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                var m = new Material(lit) { name = "Cay_" + i };
                m.SetColor("_BaseColor", TeaColors[i]);
                m.SetFloat("_Smoothness", 0.25f);
                mr.sharedMaterial = m;
            }
        }

        /// <summary>Düşük poligonlu yassı küre (çay çalısı).</summary>
        static void AddBlob(List<Vector3> v, List<int> t, List<Vector3> n, Vector3 c, Vector3 r)
        {
            const int U = 8, V = 5;
            int start = v.Count;
            for (int j = 0; j <= V; j++)
            {
                float phi = Mathf.PI * j / V;                       // 0 üst, pi alt
                for (int i = 0; i < U; i++)
                {
                    float th = 2f * Mathf.PI * i / U;
                    var d = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    if (d.y < -0.2f) d.y = -0.2f;                    // altı düz, yere otursun
                    v.Add(c + Vector3.Scale(d, r));
                    n.Add(new Vector3(d.x / r.x, d.y / r.y, d.z / r.z).normalized);
                }
            }
            for (int j = 0; j < V; j++)
                for (int i = 0; i < U; i++)
                {
                    int a = start + j * U + i, b = start + j * U + (i + 1) % U;
                    int cc = start + (j + 1) * U + i, d = start + (j + 1) * U + (i + 1) % U;
                    t.Add(a); t.Add(b); t.Add(cc);
                    t.Add(b); t.Add(d); t.Add(cc);
                }
        }
    }
}
