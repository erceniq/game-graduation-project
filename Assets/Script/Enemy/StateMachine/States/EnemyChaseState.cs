using UnityEngine;

namespace HUMAN.Enemy.StateMachine.States
{
    /// <summary>
    /// Düşmanın tespit ettiği oyuncuyu kovaladığı durum.
    /// Saldırı menziline girdiğinde Attack durumuna, hedef çok uzaklaşırsa Idle durumuna geçer.
    /// </summary>
    public class EnemyChaseState : EnemyBaseState
    {
        public EnemyChaseState(EnemyBrain brain, EnemyStateMachine stateMachine) : base(brain, stateMachine) { }

        public override void Tick(float deltaTime)
        {
            if (brain.Target == null)
            {
                stateMachine.ChangeState(brain.IdleState);
                return;
            }

            // 2.5D X/Y Düzleminde mesafe kontrolü
            float distanceToTarget = brain.GetDistanceToTarget();

            // Saldırı menzili kontrolü
            if (distanceToTarget <= brain.AttackRadius)
            {
                brain.StopMovement();
                stateMachine.ChangeState(brain.AttackState);
                return;
            }

            // Hedefi kaybetme kontrolü
            if (distanceToTarget > brain.LoseTargetRadius)
            {
                brain.StopMovement();
                stateMachine.ChangeState(brain.IdleState);
                return;
            }

            brain.MoveTowards(brain.Target.position, brain.ChaseSpeed);
        }
    }
}
