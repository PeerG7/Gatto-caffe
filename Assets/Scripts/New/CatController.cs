using System.Xml.Linq;
using UnityEngine;

public class CatController : MonoBehaviour
{
    public float relationship = 0f;

    public void AddRelationship(float amount)
    {
        relationship += amount;
        Debug.Log(name + " relation: " + relationship);
    }
}