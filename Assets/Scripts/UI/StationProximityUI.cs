using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StationProximityUI : MonoBehaviour
{
    [Header("Proximity Icon")]
    [Tooltip("Drag the icon GameObject (child of this station) here.")]
    public GameObject proximityIcon;

    private void Start()
    {
        // Hide the icon when the game starts
        if (proximityIcon != null)
        {
            proximityIcon.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (proximityIcon != null)
            {
                proximityIcon.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (proximityIcon != null)
            {
                proximityIcon.SetActive(false);
            }
        }
    }
}