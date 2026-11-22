using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string lastSavedAt;
    public string sceneName;

    public Vector3 checkpointPosition;
    public float energy;


    public List<AbilityId> unlockedAbilities = new List<AbilityId>();
    public AbilityId equippedLeg = AbilityId.None;
    public AbilityId equippedBack = AbilityId.None;
    public AbilityId equippedShoulder = AbilityId.None;
}
