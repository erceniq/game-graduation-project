using UnityEngine;

namespace HUMAN.Enemy.StateMachine.States
{
    /// <summary>
    /// Düşmanın hareketsiz beklediği veya devriye aralarında mola verdiği durum.
    /// Hedef algılama yarıçapına girdiğinde Chase durumuna geçer.
    /// </summary>
    public class EnemyIdleState : EnemyBaseState
    {
        private float idleTimer;
        private readonly float idleDuration = 2.0f;

        public EnemyIdleState(EnemyBrain brain, EnemyStateMachine stateMachine) : base(brain, stateMachine) { }

        public override void Enter()
        {
            brain.StopMovement();
            idleTimer = idleDuration;
        }

        public override void Tick(float deltaTime)
        {
            // 1. Hedef kontrolü (2.5D X/Y Düzleminde)
            if (brain.Target != null)
            {
                float distanceToTarget = brain.GetDistanceToTarget();
                if (distanceToTarget <= brain.DetectionRadius)
                {
                    stateMachine.ChangeState(brain.ChaseState);
                    return;
                }
            }

            // 2. Devriye geçişi
            if (brain.PatrolWaypoints != null && brain.PatrolWaypoints.Length > 0)
            {
                idleTimer -= deltaTime;
                if (idleTimer <= 0f)
                {
                    stateMachine.ChangeState(brain.PatrolState);
                }
            }
        }
    }
}
