using UnityEngine;

[CreateAssetMenu(fileName = "Guns", menuName = "Scriptable Objects/Guns")]
public class Guns : ScriptableObject
{
    public string gunName;
    public bool gunUnlocked;


    public float gunRange;
    public float gunFireRate;
    public float gunEnergyDrain;
    public float gunDamage;
    public float gunSpread;
    
}
