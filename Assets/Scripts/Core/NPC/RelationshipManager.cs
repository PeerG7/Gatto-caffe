using System.Collections.Generic;
using UnityEngine;

public class RelationshipManager : MonoBehaviour
{
    public static RelationshipManager Instance;

    [Header("All Cats in Game")]
    public List<CatRelationshipData> allCats;

    private Dictionary<string, float> relationshipValues = new Dictionary<string, float>();
    private const string REL_KEY = "rel_";

    // compat: NPCInteract และ RelationshipSceneUI ยังใช้ method เหล่านี้อยู่
    private NPCController currentNPC;
    public void SetCurrentNPC(NPCController npc) { currentNPC = npc; }
    public NPCController GetCurrentNPC() { return currentNPC; }
    public void ClearNPC() { currentNPC = null; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        LoadAll();
    }

    void LoadAll()
    {
        foreach (var cat in allCats)
        {
            if (string.IsNullOrEmpty(cat.catID)) continue;
            relationshipValues[cat.catID] = PlayerPrefs.GetFloat(REL_KEY + cat.catID, 0f);
        }
    }

    void SaveAll()
    {
        foreach (var cat in allCats)
        {
            if (string.IsNullOrEmpty(cat.catID)) continue;
            PlayerPrefs.SetFloat(REL_KEY + cat.catID, relationshipValues[cat.catID]);
        }
        PlayerPrefs.Save();
    }

    public void AddRelationship(string catID, float amount)
    {
        if (!relationshipValues.ContainsKey(catID))
        {
            Debug.LogWarning($"RelationshipManager: catID '{catID}' not found.");
            return;
        }

        CatRelationshipData data = allCats.Find(c => c.catID == catID);
        float max = data != null ? data.maxRelationship : 100f;

        relationshipValues[catID] = Mathf.Clamp(relationshipValues[catID] + amount, 0f, max);
        SaveAll();
    }

    public float GetRelationship(string catID)
    {
        return relationshipValues.TryGetValue(catID, out float v) ? v : 0f;
    }

    public List<CatRelationshipData> GetAllCats() => allCats;

    public string GetRelationshipLabel(string catID)
    {
        CatRelationshipData data = allCats.Find(c => c.catID == catID);
        float max = data != null ? data.maxRelationship : 100f;
        float ratio = GetRelationship(catID) / max;

        if (ratio < 0.2f) return "Stranger";
        if (ratio < 0.4f) return "Acquaintance";
        if (ratio < 0.6f) return "Friend";
        if (ratio < 0.8f) return "Good Friend";
        return "Best Friend";
    }

    public void ResetAllRelationships()
    {
        foreach (var cat in allCats)
        {
            if (string.IsNullOrEmpty(cat.catID)) continue;
            relationshipValues[cat.catID] = 0f;
            PlayerPrefs.DeleteKey(REL_KEY + cat.catID);
        }
        PlayerPrefs.Save();
    }
}