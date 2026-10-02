using UnityEngine;

[CreateAssetMenu(fileName = "NewFurnitureItemData", menuName = "Furniture System/Furniture Item Data")]
public class FurnitureItemData : ScriptableObject
{
    public string id;
    public string furnitureName;
    public Sprite icon;
    public int price;
    public GameObject prefab;
}