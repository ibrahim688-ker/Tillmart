using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine.AI;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CiftlikMarket.EditorTools
{
    public static partial class PrototypeBuilder
    {
        /// <summary>
        /// Açık sahneye çarpışma ekler: duvarlar, raflar, tezgah, çöp kutusu, hayvanlar, kümes.
        /// Oyuncuya CharacterController ekler. Sahneyi baştan kurmaz; tekrar çalıştırmak güvenlidir.
        /// </summary>
        [MenuItem("CiftlikMarket/Add Colliders (sahneye ekle)")]
        public static void AddCollidersMenu()
        {
            int n = AddCollidersToScene();
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CiftlikMarket] Çarpışma eklendi: " + n + " nesne.");
        }

        static int AddCollidersToScene()
        {
            var world = FindInScene("World");
            if (world == null) return 0;
            int n = 0;
            string[] paths =
            {
                "Market/WallBack", "Market/WallLeftFront", "Market/WallLeftBack", "Market/WallRight_Gecici",
                "Market/Genisleme_1/WallBack", "Market/Genisleme_1/WallRight",
                "Market/Kasa/Counter", "Market/Cop/Bin",
                "Market/Raf_Sut/Base", "Market/Raf_Sut/Back",
                "Market/Raf_Yumurta/Base", "Market/Raf_Yumurta/Back",
                "Market/Raf_Domates/Base", "Market/Raf_Domates/Back",
                "Farm/Inek_1/Model/Body", "Farm/Inek_2/Model/Body", "Farm/Kumes_1/Model/House",
            };
            foreach (var p in paths)
            {
                var t = world.Find(p);
                if (t == null) continue;
                if (t.GetComponent<Collider>() == null) { t.gameObject.AddComponent<BoxCollider>(); n++; }
                // Müşteri/çalışan yol bulması için engel (aktif olunca NavMesh'i oyar, kapanınca açar)
                var ob = t.GetComponent<NavMeshObstacle>();
                if (ob == null) { ob = t.gameObject.AddComponent<NavMeshObstacle>(); n++; }
                ob.shape = NavMeshObstacleShape.Box;
                ob.center = Vector3.zero;
                ob.size = Vector3.one;
                ob.carving = true;
                ob.carveOnlyStationary = true;
            }

            // Zemin: NavMesh oyun başında sadece zeminden hesaplanır
            var grass = world.Find("Grass");
            if (grass != null)
            {
                if (grass.GetComponent<Collider>() == null) { grass.gameObject.AddComponent<BoxCollider>(); n++; }
                var surf = grass.GetComponent<NavMeshSurface>();
                if (surf == null) { surf = grass.gameObject.AddComponent<NavMeshSurface>(); n++; }
                surf.collectObjects = CollectObjects.Children;
                surf.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                if (grass.GetComponent<NavBaker>() == null) { grass.gameObject.AddComponent<NavBaker>(); n++; }
            }

            var player = FindInScene("Player");
            if (player != null && player.GetComponent<CharacterController>() == null)
            {
                var cc = player.gameObject.AddComponent<CharacterController>();
                cc.center = new Vector3(0, 0.85f, 0);
                cc.height = 1.6f;
                cc.radius = 0.35f;
                cc.skinWidth = 0.04f;
                cc.stepOffset = 0.05f;
                cc.minMoveDistance = 0f;
                n++;
            }
            return n;
        }
    }
}
