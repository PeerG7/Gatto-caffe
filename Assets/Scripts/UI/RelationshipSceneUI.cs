using UnityEngine;
using System.Collections;

public class RelationshipUI : MonoBehaviour
{
    bool isEnding = false;

    public void FinishInteraction()
    {
        if (isEnding) return;
        isEnding = true;

        StartCoroutine(FinishRoutine());
    }

    IEnumerator FinishRoutine()
    {
        var npc = RelationshipManager.Instance.GetCurrentNPC();

        if (npc != null)
        {
            npc.GoExit();
        }

        RelationshipManager.Instance.ClearNPC();

        yield return StartCoroutine(FadeOut());

        SceneLoader.Instance.CloseRelationshipScene();
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(0.3f);
    }
}