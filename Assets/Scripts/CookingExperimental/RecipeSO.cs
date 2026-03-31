// The Data
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Cooking/Recipe")]
public class RecipeSO : ScriptableObject
{
    public string recipeName;
    public List<string> requiredIngredients;
    public Sprite finalDishSprite;
}
