using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public static int ActiveSlot { get; set; } = 0;

    private static string GetSlotPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");
    }

    public static void Save(SaveData data)
    {
        Save(data, ActiveSlot);
        Debug.Log("Saved to: " + GetSlotPath(ActiveSlot));
    }

    public static void Save(SaveData data, int slot)
    {
        if (data == null) return;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSlotPath(slot), json);
    }


}
