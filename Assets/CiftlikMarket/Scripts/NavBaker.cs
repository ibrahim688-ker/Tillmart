using Unity.AI.Navigation;
using UnityEngine;

namespace CiftlikMarket
{
    /// <summary>
    /// Oyun başlarken zemin için NavMesh'i hesaplar (sadece zemin; engeller NavMeshObstacle ile
    /// dinamik olarak oyulur, böylece açılan/kalkan duvarlar ve raflar anında yola yansır).
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [RequireComponent(typeof(NavMeshSurface))]
    public class NavBaker : MonoBehaviour
    {
        void Awake()
        {
            GetComponent<NavMeshSurface>().BuildNavMesh();
        }
    }
}
