using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveStation : MonoBehaviour, IInteractable
{
    public PlayerEnergy playerEnergy;
   


    public void Interact()
    {
        EnergyFill();
        Save(); 

    }

    private void Save()
    {
        Vector3 checkPoint = transform.position;
        SaveData data = new SaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            checkpointPosition = checkPoint,
            energy = playerEnergy.maxEnergy,

            unlockedAbilities = new List<AbilityId>(),
            equippedLeg = AbilityId.None,
            equippedBack = AbilityId.None,
            equippedShoulder = AbilityId.None
        };

        SaveSystem.Save(data);
    }

    private void EnergyFill()
    {
        playerEnergy.currentEnergy = playerEnergy.maxEnergy;
    }
}
