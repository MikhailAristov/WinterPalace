using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SpeechBubble : MonoBehaviour
{
    public static SpeechBubble Main;
    public TextMeshProUGUI Text;
    public Canvas RootCanvas;
    private RectTransform Rect;

    // Use this for initialization
    void Awake()
    {
        Main = this;
        Debug.Assert(Text != null);
        Debug.Assert(RootCanvas != null);
        Rect = GetComponent<RectTransform>();
    }

    public void Display(string Payload, Vector3 WorldPosition)
    {
        Text.text = Payload;
        Rect.anchoredPosition = CameraController.Cam.WorldToScreenPoint(WorldPosition) / RootCanvas.scaleFactor;
        RootCanvas.enabled = true;
    }

    public void Hide()
    {
        RootCanvas.enabled = false;
    }
}
