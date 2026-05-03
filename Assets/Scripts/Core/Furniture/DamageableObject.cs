using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DamageableObject : MonoBehaviour
{
    [Header("State GameObjects")]
    public GameObject normalObject;
    public GameObject damagedObject;
    public GameObject brokenObject;
    public GameObject repairingObject;

    [Header("Settings")]
    public float damageTime = 10f;
    public float repairTime = 5f;

    private bool isBeingDamaged = false;
    private bool isBroken = false;
    private bool isRepairing = false;

    // ✅ Flag ป้องกัน NPC หลายตัวเลือก Target เดียวกัน
    public bool isTargeted = false;

    public static List<DamageableObject> allObjects = new List<DamageableObject>();

    void OnEnable() => allObjects.Add(this);
    void OnDisable() => allObjects.Remove(this);

    void Start()
    {
        SetState(normal: true, damaged: false, broken: false, repairing: false);
    }

    void SetState(bool normal, bool damaged, bool broken, bool repairing)
    {
        if (normalObject != null) normalObject.SetActive(normal);
        if (damagedObject != null) damagedObject.SetActive(damaged);
        if (brokenObject != null) brokenObject.SetActive(broken);
        if (repairingObject != null) repairingObject.SetActive(repairing);
    }

    public void StartDamage()
    {
        if (isBroken || isBeingDamaged) return;
        StartCoroutine(DamageProcess());
    }

    IEnumerator DamageProcess()
    {
        isBeingDamaged = true;
        SetState(normal: false, damaged: true, broken: false, repairing: false);

        yield return new WaitForSeconds(damageTime);

        SetState(normal: false, damaged: false, broken: true, repairing: false);
        isBroken = true;
        isBeingDamaged = false;
        isTargeted = false; // ✅ Reset เมื่อพังแล้ว
    }

    public bool CanRepair()
    {
        return isBroken && !isRepairing;
    }

    public void StartRepair()
    {
        if (!CanRepair()) return;
        StartCoroutine(RepairProcess());
    }

    IEnumerator RepairProcess()
    {
        isRepairing = true;
        PlayerController2D.IsLocked = true;

        SetState(normal: false, damaged: false, broken: false, repairing: true);

        if (UINotificationManager.Instance != null)
            UINotificationManager.Instance.ShowNotification("Repairing... 5s");

        yield return new WaitForSeconds(repairTime);

        isBroken = false;
        isRepairing = false;
        isTargeted = false;
        SetState(normal: true, damaged: false, broken: false, repairing: false);

        PlayerController2D.IsLocked = false;

        if (UINotificationManager.Instance != null)
            UINotificationManager.Instance.ShowNotification("Repair Complete!");
    }

    public void ResetToNormal()
    {
        StopAllCoroutines();
        isBroken = false;
        isBeingDamaged = false;
        isRepairing = false;
        isTargeted = false;
        SetState(normal: true, damaged: false, broken: false, repairing: false);
        PlayerController2D.IsLocked = false;
    }

    public bool IsBroken() => isBroken;
    public bool IsRepairing() => isRepairing;

    // ✅ เช็คว่าสามารถถูก Target ได้ไหม
    public bool IsAvailable() => !isBroken && !isTargeted && !isBeingDamaged;
}