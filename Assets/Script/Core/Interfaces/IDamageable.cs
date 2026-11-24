namespace HUMAN.Core
{
    /// <summary>
    /// Hasar alabilen tüm varlıkların (düşmanlar, kırılabilir nesneler, oyuncu hurtbox) uyguladığı ortak arayüz.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(DamageData damage);
        bool IsAlive { get; }
    }
}
