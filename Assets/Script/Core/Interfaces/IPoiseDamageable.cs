namespace HUMAN.Core
{
    /// <summary>
    /// Poise (duruş / denge) kırılması yaşayan varlıklar tarafından uygulanır.
    /// Poise sıfırlandığında stagger (sersemleme) durumuna geçer.
    /// </summary>
    public interface IPoiseDamageable
    {
        void TakePoiseDamage(float poiseDamage);
    }
}
