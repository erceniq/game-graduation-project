namespace HUMAN.Enemy.StateMachine.States
{
    /// <summary>
    /// Tüm somut düşman durumlarının (Idle, Chase, Attack, vb.) türediği taban sınıf.
    /// </summary>
    public abstract class EnemyBaseState : IEnemyState
    {
        protected readonly EnemyBrain brain;
        protected readonly EnemyStateMachine stateMachine;

        public EnemyBaseState(EnemyBrain brain, EnemyStateMachine stateMachine)
        {
            this.brain = brain;
            this.stateMachine = stateMachine;
        }

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void FixedTick(float fixedDeltaTime) { }
        public virtual void Exit() { }
    }
}
