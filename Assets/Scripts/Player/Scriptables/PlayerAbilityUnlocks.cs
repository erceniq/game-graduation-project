using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "AbilityData")]
public class PlayerAbilityUnlocks : ScriptableObject
{
    public string abilityName;
    public string description;
    public float energyCost ;
    public Sprite abilityIcon;

    [Header("Status")]
    public bool isUnlocked = false;
}
