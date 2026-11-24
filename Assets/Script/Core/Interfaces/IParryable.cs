namespace HUMAN.Core
{
    /// <summary>
    /// Gelen saldırıları saptırma (parry) yeteneğine sahip varlıklar tarafından uygulanır.
    /// </summary>
    public interface IParryable
    {
        bool TryParry(DamageData incomingDamage);
    }
}
