using UnityEngine;

namespace HUMAN.Enemy.StateMachine.States
{
    /// <summary>
    /// Düşmanın oyuncunun saldırılarını savuşturmak için savunma duruşuna geçtiği durum.
    /// - parryStanceDuration süresince aktif kalır.
    /// - Hareketi keser, oyuncuya döner ve savunma penceresini (IsParryStanceActive = true) açar.
    /// - Altın/turuncu savunma parlamasıyla oyuncuya görsel ipucu verir.
    /// - Süre dolana kadar saldırı gelmezse normal Chase/Attack döngüsüne geri döner.
    /// </summary>
    public class EnemyParryState : EnemyBaseState
    {
        private readonly float parryStanceDuration;
        private float timer;

        public EnemyParryState(EnemyBrain brain, EnemyStateMachine stateMachine, float duration = 0.65f) 
            : base(brain, stateMachine)
        {
            this.parryStanceDuration = duration;
        }

        public override void Enter()
        {
            brain.StopMovement();
            brain.LookAtTarget();
            brain.SetParryStanceActive(true);
            timer = parryStanceDuration;

            // Görsel Savunma İpucu (Altın/Turuncu parlama)
            brain.ShowParryStanceVisual(parryStanceDuration);
        }

        public override void Tick(float deltaTime)
        {
            brain.StopMovement();
            brain.LookAtTarget();

            timer -= deltaTime;
            if (timer <= 0f)
            {
                brain.SetParryStanceActive(false);

                // Savunma süresi bitti, menzile göre döngüye devam et
                if (brain.GetHorizontalDistanceToTarget() <= brain.AttackRadius)
                {
                    stateMachine.ChangeState(brain.AttackState);
                }
                else
                {
                    stateMachine.ChangeState(brain.ChaseState);
                }
            }
        }

        public override void Exit()
        {
            brain.SetParryStanceActive(false);
        }
    }
}
