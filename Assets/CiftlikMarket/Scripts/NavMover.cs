using UnityEngine;
using UnityEngine.AI;

namespace CiftlikMarket
{
    /// <summary>
    /// Müşteri ve çalışanlar için NavMesh ile yürüme yardımcısı. Engellerin (tezgah, raf, duvar)
    /// etrafından dolaşır, diğer karakterlerden kaçınır. NavMesh yoksa düz çizgide yürür.
    /// </summary>
    public class NavMover
    {
        readonly NavMeshAgent agent;
        readonly Transform tr;
        Vector3 lastTarget = new Vector3(99999f, 0f, 99999f);
        float retryTimer;
        readonly int basePriority;
        float stuckCheck, ghostTimer;
        Vector3 stuckPos;
        const int RestPriority = 99;   // yerinde bekleyen karakter: en önemsiz, yürüyenler onu engel saymaz

        public NavMover(GameObject go, float speed, float radius, int priority)
        {
            tr = go.transform;
            agent = go.GetComponent<NavMeshAgent>();
            if (agent == null) agent = go.AddComponent<NavMeshAgent>();
            agent.speed = speed;
            agent.angularSpeed = 720f;
            agent.acceleration = 30f;
            agent.radius = radius;
            agent.height = 1.6f;
            agent.baseOffset = 0f;
            agent.stoppingDistance = 0.05f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = priority;
            basePriority = priority;
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(tr.position, out var hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        public bool IsMoving => agent != null && agent.isOnNavMesh && agent.velocity.sqrMagnitude > 0.04f;

        /// <summary>Hedefe yürür; vardıysa true döner.</summary>
        public bool MoveTo(Vector3 target, float speed, float arriveDistance = 0.12f)
        {
            bool arrived = MoveToInner(target, speed, arriveDistance);
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return arrived;
            float dt = Time.deltaTime;

            // Yerinde bekleyen (kasiyer, sıradaki müşteri, raftaki çalışan) yol versin:
            // en düşük öncelikle yürüyenler onu engel saymaz, gerekirse kendisi kenara çekilip geri döner.
            int pr = arrived ? RestPriority : basePriority;
            if (agent.avoidancePriority != pr) agent.avoidancePriority = pr;

            // Takılma sigortası: 1 sn boyunca neredeyse hiç ilerleyemediyse kısa süre kaçınmayı kapatıp aradan geç
            if (ghostTimer > 0f)
            {
                ghostTimer -= dt;
                if (ghostTimer <= 0f) agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            }
            if (arrived) { stuckCheck = 0f; stuckPos = tr.position; return true; }
            stuckCheck += dt;
            if (stuckCheck >= 1f)
            {
                var moved = tr.position - stuckPos; moved.y = 0f;
                if (moved.sqrMagnitude < 0.04f && ghostTimer <= 0f && agent.hasPath && agent.remainingDistance > arriveDistance + 0.2f)
                {
                    ghostTimer = 1.5f;
                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                }
                stuckCheck = 0f;
                stuckPos = tr.position;
            }
            return arrived;
        }

        bool MoveToInner(Vector3 target, float speed, float arriveDistance)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return MoveDirect(target, speed);

            agent.speed = speed;
            if ((lastTarget - target).sqrMagnitude > 0.0025f)
            {
                lastTarget = target;
                agent.SetDestination(target);
                return false;
            }
            if (agent.pathPending) return false;
            // Yeni eklenen ajanda ilk SetDestination bazen tutmuyor (çalışan yerinde kalıyordu): hedef sapmışsa yeniden iste
            if (!agent.hasPath)
            {
                var dd = agent.destination - target; dd.y = 0f;
                var dp = tr.position - target; dp.y = 0f;
                if (dd.sqrMagnitude > 0.36f && dp.sqrMagnitude > arriveDistance * arriveDistance + 0.04f)
                {
                    retryTimer -= Time.deltaTime;
                    if (retryTimer <= 0f) { retryTimer = 0.4f; agent.SetDestination(target); }
                    return false;
                }
                // Vardıktan sonra başka biri itip yerinden ettiyse (hedef hâlâ aynı ama ajan ondan uzakta) geri yürü
                var dz = agent.destination - tr.position; dz.y = 0f;
                if (dz.sqrMagnitude > 0.09f && dp.sqrMagnitude > arriveDistance * arriveDistance + 0.04f)
                {
                    retryTimer -= Time.deltaTime;
                    if (retryTimer <= 0f) { retryTimer = 0.3f; agent.SetDestination(target); }
                    return false;
                }
            }
            float rd = agent.remainingDistance;
            if (float.IsInfinity(rd)) return false;
            if (rd <= arriveDistance) return true;
            // Hedef tam erişilemiyorsa (engelin içine düşüyorsa) en yakın noktaya varınca yeterli say
            if (agent.pathStatus != NavMeshPathStatus.PathComplete && rd < 0.8f) return true;
            return false;
        }

        bool MoveDirect(Vector3 target, float speed)
        {
            var p = tr.position;
            var d = target - p;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist < 0.05f) return true;
            float step = speed * Time.deltaTime;
            if (step >= dist) { tr.position = new Vector3(target.x, p.y, target.z); return true; }
            tr.position = p + d / dist * step;
            tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-10f * Time.deltaTime));
            return false;
        }
    }
}
