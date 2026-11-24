using UnityEngine;

namespace HUMAN.Enemy.StateMachine.States
{
    /// <summary>
    /// Düşmanın duruşu kırıldığında (Poise Broken) veya saldırısı Parry edildiğinde girdiği sersemleme durumu.
    /// Düşman hareket edemez, saldırı yapamaz ve oyuncunun kritik vuruşuna açık kalır.
    /// </summary>
    public class EnemyStaggerState : EnemyBaseState
    {
        private float staggerTimer;

        public EnemyStaggerState(EnemyBrain brain, EnemyStateMachine stateMachine) : base(brain, stateMachine) { }

        public override void Enter()
        {
            brain.StopMovement();
            if (brain.WeaponHitbox != null)
            {
                brain.WeaponHitbox.DeactivateHitbox();
            }

            staggerTimer = brain.StaggerDuration;

            // Sersemleme ânında materyalleri mavi slow-motion tonuna geçir
            if (HUMAN.Combat.CombatFeedbackController.HasInstance)
            {
                HUMAN.Combat.CombatFeedbackController.Instance.SetSlowMotionVisuals(brain.gameObject, true, new Color(0.25f, 0.65f, 1.0f, 1.0f));
            }
        }

        public override void Exit()
        {
            // Sersemleme bittiğinde materyal renklerini eksiksiz geri yükle
            if (HUMAN.Combat.CombatFeedbackController.HasInstance)
            {
                HUMAN.Combat.CombatFeedbackController.Instance.SetSlowMotionVisuals(brain.gameObject, false);
            }
        }

        public override void Tick(float deltaTime)
        {
            staggerTimer -= deltaTime;
            if (staggerTimer <= 0f)
            {
                // Sersemleme sona erdi, hedefe göre durum değiştir
                if (brain.Target != null)
                {
                    stateMachine.ChangeState(brain.ChaseState);
                }
                else
                {
                    stateMachine.ChangeState(brain.IdleState);
                }
            }
        }
    }
}
