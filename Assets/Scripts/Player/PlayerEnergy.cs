using TMPro;
using UnityEngine;
public enum EnergyLevel
{
    Normal,
    Low,
    Zero,
}
public class PlayerEnergy : MonoBehaviour
{
    [Header("Energy")]
    public float maxEnergy;
    public float firstThresholdEnergy;
    public float SecondTresholdEnergy;
    public float currentEnergy;
    public float autoEnergyGain;

    public EnergyLevel currentEnergyLevel = EnergyLevel.Normal;



    public PlayerAbilityUnlocks sprint;

    [Header("UI Reference")]
    public TextMeshProUGUI energyText;




    [Header("PlayerMovement Reference")]
    public PlayerMovement playerMovement;






    private void Update()
    {
        EnergyManagement();
    }



    private void EnergyManagement()
    {
        EnergyChange();
        EnergyLevelTransition();
        switch (currentEnergyLevel)
        {
            case EnergyLevel.Normal:
                break;
            case EnergyLevel.Low:
                break;
            case EnergyLevel.Zero:
                currentEnergy = 0;
                autoEnergyGain = 0;
                break;
        }
    }
    private void EnergyLevelTransition()
    {
        if (currentEnergy <= SecondTresholdEnergy)
        {
            if (currentEnergyLevel != EnergyLevel.Zero)
            {
                currentEnergyLevel = EnergyLevel.Zero;
            }
        }
        else if (currentEnergy <= firstThresholdEnergy)
        {
            if (currentEnergyLevel != EnergyLevel.Low)
            {
                currentEnergyLevel = EnergyLevel.Low;
            }
        }
        else
        {
            if (currentEnergyLevel != EnergyLevel.Normal)
            {
                currentEnergyLevel = EnergyLevel.Normal;
            }
        }
    }

    private void EnergyChange()
    {
        currentEnergy= currentEnergy+autoEnergyGain*Time.deltaTime;
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;


        if (playerMovement.isSprinting)
        {
            currentEnergy = currentEnergy-sprint.energyCost*Time.deltaTime;
        }

        UpdateEnergyUI();
    }

    public void UpdateEnergyUI()
    {
        if (energyText != null)
        {
            energyText.text = $"Energy: {currentEnergy:F1} / {maxEnergy}";
        }
    }
}
