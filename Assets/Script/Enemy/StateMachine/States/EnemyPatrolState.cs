using UnityEngine;

namespace HUMAN.Enemy.StateMachine.States
{
    /// <summary>
    /// Düşmanın devriye noktaları arasında hareket ettiği durum.
    /// Hedef algılandığında Chase durumuna geçer.
    /// </summary>
    public class EnemyPatrolState : EnemyBaseState
    {
        private int currentWaypointIndex = 0;
        private const float WaypointTolerance = 0.5f;

        public EnemyPatrolState(EnemyBrain brain, EnemyStateMachine stateMachine) : base(brain, stateMachine) { }

        public override void Tick(float deltaTime)
        {
            // Hedef kontrolü (2.5D)
            if (brain.Target != null)
            {
                float distanceToTarget = brain.GetDistanceToTarget();
                if (distanceToTarget <= brain.DetectionRadius)
                {
                    stateMachine.ChangeState(brain.ChaseState);
                    return;
                }
            }

            // Devriye mantığı (Yalnızca X ekseninde takip)
            if (brain.PatrolWaypoints == null || brain.PatrolWaypoints.Length == 0)
            {
                stateMachine.ChangeState(brain.IdleState);
                return;
            }

            Transform targetWaypoint = brain.PatrolWaypoints[currentWaypointIndex];
            if (targetWaypoint == null) return;

            brain.MoveTowards(targetWaypoint.position, brain.PatrolSpeed);

            float distanceToWaypointX = Mathf.Abs(brain.transform.position.x - targetWaypoint.position.x);
            if (distanceToWaypointX <= WaypointTolerance)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % brain.PatrolWaypoints.Length;
                stateMachine.ChangeState(brain.IdleState);
            }
        }
    }
}
