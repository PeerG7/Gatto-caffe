using UnityEngine;

[System.Serializable]
public class FurnitureInstanceData
{
    public string instanceID;
    public FurnitureItemData itemData;
    public int currentLevel = 0;
    public bool isUnlocked = true;
    public bool isUpgraded = false;

    public FurnitureInstanceData(FurnitureItemData data, int level = 0, bool upgraded = false)
    {
        instanceID = System.Guid.NewGuid().ToString();
        itemData = data;
        currentLevel = level;
        isUpgraded = upgraded;
        isUnlocked = true;
    }
}