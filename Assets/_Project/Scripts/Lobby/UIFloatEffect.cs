using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIFloatEffect : MonoBehaviour
{
    [Header("Floating Settings")]
    [Tooltip("움직이는 속도")]
    public float floatSpeed = 3f;
    public float floatAmount = 15f;

    private RectTransform rectTransform;
    private Vector2 startPos;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;
    }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;

        rectTransform.anchoredPosition = new Vector2(startPos.x, newY);
    }
}