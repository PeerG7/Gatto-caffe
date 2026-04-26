using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DamageableObject : MonoBehaviour
{
    public Image image;

    public Sprite normalSprite;
    public Sprite damagedSprite;
    public Sprite brokenSprite;

    public float damageTime = 3f;

    private bool isBeingDamaged = false;
    private bool isBroken = false;

    public static List<DamageableObject> allObjects = new List<DamageableObject>();

    void OnEnable() => allObjects.Add(this);
    void OnDisable() => allObjects.Remove(this);

    void Start()
    {
        image.sprite = normalSprite;
    }

    public void StartDamage()
    {
        if (isBroken || isBeingDamaged) return;
        StartCoroutine(DamageProcess());
    }

    IEnumerator DamageProcess()
    {
        isBeingDamaged = true;

        image.sprite = damagedSprite;

        yield return new WaitForSeconds(damageTime);

        image.sprite = brokenSprite;
        isBroken = true;

        isBeingDamaged = false;
    }

    public void Repair()
    {
        if (!isBroken) return;

        isBroken = false;
        image.sprite = normalSprite;
    }

    public bool IsBroken()
    {
        return isBroken;
    }
}