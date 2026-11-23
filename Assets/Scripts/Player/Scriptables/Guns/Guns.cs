using UnityEngine;
public enum GunType
{
    Automatic,
    Shotgun,
}
[CreateAssetMenu(fileName = "Guns", menuName = "Scriptable Objects/Guns")]

public class Guns : ScriptableObject
{
    public string gunName;
    public GunType gunType;
    public bool gunUnlocked;

    public int gunProjectileNumber = 1;
    public float gunRange;
    public float gunFireRate;
    public float gunEnergyDrain;
    public float gunDamage;
    public float gunSpread;


    public ParticleSystem gunParticle;
    public AudioClip gunSound;
   
}
