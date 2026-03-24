using UnityEngine;

[CreateAssetMenu(fileName = "QTEData", menuName = "Game/QTE Data")]
public class QTEData : ScriptableObject
{

    public enum QTEType
    {
        Petting,       // ลูบ
        OpenCan,       // เปิดกระป๋อง
        WaveToy        // แกว่งของเล่น
    }
    public QTEType type;

    [Header("Common")]
    public float duration = 3f;

    [Header("Slider / Fill QTE")]
    public float speed = 1f;

    [Header("Spam QTE")]
    public int requiredPress = 10;

    [Header("Timing QTE")]
    public float successWindow = 0.2f;
}