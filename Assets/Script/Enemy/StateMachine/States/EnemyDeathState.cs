namespace HUMAN.Enemy.StateMachine.States
{
    /// <summary>
    /// Düşmanın canı sıfıra indiğinde girdiği kalıcı ölüm durumu.
    /// Tüm saldırı ve hareket girdilerini keser.
    /// </summary>
    public class EnemyDeathState : EnemyBaseState
    {
        public EnemyDeathState(EnemyBrain brain, EnemyStateMachine stateMachine) : base(brain, stateMachine) { }

        public override void Enter()
        {
            brain.StopMovement();
            if (brain.WeaponHitbox != null)
            {
                brain.WeaponHitbox.DeactivateHitbox();
            }

            if (brain.RagdollController != null)
            {
                brain.RagdollController.ActivateRagdoll(brain.LastHitDirection);
            }
        }

        public override void Tick(float deltaTime)
        {
            // Ölüm durumunda ek bir mantık çalışmaz
        }
    }
}
