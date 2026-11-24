using System;

namespace HUMAN.Enemy.StateMachine
{
    /// <summary>
    /// Hafif, sıfır GC ve doğrudan durum geçişlerine sahip düşman durum makinesi.
    /// </summary>
    public class EnemyStateMachine
    {
        public IEnemyState CurrentState { get; private set; }
        public IEnemyState PreviousState { get; private set; }

        public event Action<IEnemyState> OnStateChanged;

        public void ChangeState(IEnemyState newState)
        {
            if (newState == null || newState == CurrentState) return;

            CurrentState?.Exit();
            PreviousState = CurrentState;
            CurrentState = newState;
            CurrentState.Enter();

            OnStateChanged?.Invoke(CurrentState);
        }

        public void Tick(float deltaTime) => CurrentState?.Tick(deltaTime);
        public void FixedTick(float fixedDeltaTime) => CurrentState?.FixedTick(fixedDeltaTime);
    }
}
