using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Panels")]
    public GameObject relationshipPanel;

    void Awake()
    {
        Instance = this;
        relationshipPanel.SetActive(false);
    }

    public void OpenRelationshipPanel()
    {
        relationshipPanel.SetActive(true);

        // pause game
        Time.timeScale = 0f;

        CatSystemManager.Instance.StartInteraction();
    }

    public void CloseRelationshipPanel()
    {
        relationshipPanel.SetActive(false);

        // resume game
        Time.timeScale = 1f;
    }
    public bool IsOpen()
    {
        return relationshipPanel.activeSelf;
    }
}