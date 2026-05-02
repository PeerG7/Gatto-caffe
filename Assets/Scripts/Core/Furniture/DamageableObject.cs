using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DamageableObject : MonoBehaviour
{
    [Header("State GameObjects")]
    public GameObject normalObject;    // ✅ GameObject ปกติ
    public GameObject damagedObject;   // ✅ GameObject กำลังโดนทำลาย
    public GameObject brokenObject;    // ✅ GameObject พังแล้ว
    public GameObject repairingObject; // ✅ GameObject กำลังซ่อม (optional)

    [Header("Settings")]
    public float damageTime = 10f;
    public float repairTime = 5f;

    private bool isBeingDamaged = false;
    private bool isBroken = false;
    private bool isRepairing = false;

    public static List<DamageableObject> allObjects = new List<DamageableObject>();

    void OnEnable() => allObjects.Add(this);
    void OnDisable() => allObjects.Remove(this);

    void Start()
    {
        SetState(normal: true, damaged: false, broken: false, repairing: false);
    }

    // ✅ Helper ตั้งค่า State ทีเดียว
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

        // State: กำลังโดนทำลาย
        SetState(normal: false, damaged: true, broken: false, repairing: false);

        yield return new WaitForSeconds(damageTime);

        // State: พังแล้ว
        SetState(normal: false, damaged: false, broken: true, repairing: false);
        isBroken = true;
        isBeingDamaged = false;
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

        // State: กำลังซ่อม
        SetState(normal: false, damaged: false, broken: false, repairing: true);

        if (UINotificationManager.Instance != null)
            UINotificationManager.Instance.ShowNotification("Repairing... 5s");

        yield return new WaitForSeconds(repairTime);

        // State: ซ่อมเสร็จ กลับปกติ
        isBroken = false;
        isRepairing = false;
        SetState(normal: true, damaged: false, broken: false, repairing: false);

        PlayerController2D.IsLocked = false;

        if (UINotificationManager.Instance != null)
            UINotificationManager.Instance.ShowNotification("Repair Complete!");
    }

    // ✅ Reset กลับปกติ (เรียกตอน Next Day)
    public void ResetToNormal()
    {
        StopAllCoroutines();
        isBroken = false;
        isBeingDamaged = false;
        isRepairing = false;
        SetState(normal: true, damaged: false, broken: false, repairing: false);
        PlayerController2D.IsLocked = false;
    }

    public bool IsBroken() => isBroken;
    public bool IsRepairing() => isRepairing;
}