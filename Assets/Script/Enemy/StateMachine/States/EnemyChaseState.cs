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

            // 2.5D X/Y Düzleminde mesafe ve yatay mesafe kontrolü
            float distanceToTarget = brain.GetDistanceToTarget();
            float horizontalDistance = brain.GetHorizontalDistanceToTarget();

            // Hedefi kaybetme kontrolü
            if (distanceToTarget > brain.LoseTargetRadius)
            {
                brain.StopMovement();
                stateMachine.ChangeState(brain.IdleState);
                return;
            }

            // Saldırı veya durma menzili (StoppingDistance) kontrolü:
            // Oyuncunun tam üstüne binmek yerine stoppingDistance mesafesinde durup saldırıya geçer
            if (horizontalDistance <= brain.StoppingDistance || distanceToTarget <= brain.AttackRadius)
            {
                brain.StopMovement();
                stateMachine.ChangeState(brain.AttackState);
                return;
            }

            // Oyuncunun stoppingDistance kadar açığına doğru koş (iç içe girmeyi baştan önler)
            float targetSide = brain.transform.position.x >= brain.Target.position.x ? 1f : -1f;
            Vector3 chaseDestination = new Vector3(
                brain.Target.position.x + targetSide * brain.StoppingDistance,
                brain.Target.position.y,
                brain.transform.position.z
            );

            brain.MoveTowards(chaseDestination, brain.ChaseSpeed);
        }
    }
}
