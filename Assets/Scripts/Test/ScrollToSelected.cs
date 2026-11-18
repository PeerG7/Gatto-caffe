using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Put this on the same GameObject as the furniture dock's ScrollRect.
// When the gamepad/keyboard moves the EventSystem selection to an item that is
// scrolled out of view, the content slides so the item becomes visible.
// (Unity's ScrollRect does NOT do this by itself.)
[RequireComponent(typeof(ScrollRect))]
public class ScrollToSelected : MonoBehaviour
{
    public float padding = 12f;

    private ScrollRect scroll;
    private GameObject lastSelected;

    void Awake() => scroll = GetComponent<ScrollRect>();

    void Update()
    {
        EventSystem es = EventSystem.current;
        if (es == null || scroll == null || scroll.content == null) return;

        GameObject sel = es.currentSelectedGameObject;
        if (sel == null || sel == lastSelected) return;
        lastSelected = sel;

        if (!sel.transform.IsChildOf(scroll.content)) return;
        Snap((RectTransform)sel.transform);
    }

    void Snap(RectTransform item)
    {
        Canvas.ForceUpdateCanvases();

        RectTransform view = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(view, item);
        Rect v = view.rect;

        Vector2 delta = Vector2.zero;

        if (scroll.horizontal)
        {
            if (b.max.x > v.xMax - padding) delta.x = (v.xMax - padding) - b.max.x;
            else if (b.min.x < v.xMin + padding) delta.x = (v.xMin + padding) - b.min.x;
        }
        if (scroll.vertical)
        {
            if (b.max.y > v.yMax - padding) delta.y = (v.yMax - padding) - b.max.y;
            else if (b.min.y < v.yMin + padding) delta.y = (v.yMin + padding) - b.min.y;
        }

        if (delta != Vector2.zero)
            scroll.content.anchoredPosition += delta;
    }
}
