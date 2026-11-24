namespace HUMAN.Enemy.StateMachine
{
    /// <summary>
    /// Düşman durum makinesindeki (FSM) her bir durumun sözleşmesi.
    /// </summary>
    public interface IEnemyState
    {
        void Enter();
        void Tick(float deltaTime);
        void FixedTick(float fixedDeltaTime);
        void Exit();
    }
}
