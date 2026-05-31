using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FPSDisplayUI : MonoBehaviour
{
    public enum AnchorPosition
    {
        TopLeft,
        TopRight
    }

    public AnchorPosition anchorPosition = AnchorPosition.TopRight;

    private TextMeshProUGUI fpsText;
    private float deltaTime;
    private float frameTime;
    private float frameCheckTime = 1f;

    [Header("Frame Text Color")][SerializeField] private Color textColor = Color.white;
    [Space(10f)]
    [Header("Frame Rate")][SerializeField] private int targetFrameRate = 60;
    [Header("Vsync")][SerializeField] private bool vSync = false;

    private void Awake()
    {
        QualitySettings.vSyncCount = vSync ? 1 : 0;
        Application.targetFrameRate = targetFrameRate;
    }

    private void Start()
    {
        CreateFPSUI();
    }

    private void Update()
    {
        if (fpsText == null)
        {
            return;
        }

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        float fps = 1.0f / deltaTime;
        float ms = deltaTime * 1000.0f;
        frameTime += Time.deltaTime;

        if (frameCheckTime <= frameTime)
        {
            frameTime = 0.0f;
            deltaTime = 0.0f;

            fpsText.text = string.Format("FPS : {0:N0} ({1:N1}ms)", fps, ms);
        }
    }

    private void CreateFPSUI()
    {
        // Canvas 생성
        GameObject canvasGO = new GameObject("FPS_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        // Text (TMPro) 생성
        GameObject textGO = new GameObject("FPS_Text", typeof(TextMeshProUGUI));
        textGO.transform.SetParent(canvasGO.transform);

        fpsText = textGO.GetComponent<TextMeshProUGUI>();
        fpsText.fontSize = 24;
        fpsText.fontStyle = FontStyles.Bold;
        fpsText.color = textColor;
        fpsText.alignment = TextAlignmentOptions.TopLeft;
        fpsText.text = "FPS : 0 (0ms)";

        // RectTransform 설정
        RectTransform rectTransform = fpsText.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 50);

        switch (anchorPosition)
        {
            case AnchorPosition.TopLeft:
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.anchoredPosition = new Vector2(10, -10);
                fpsText.alignment = TextAlignmentOptions.TopLeft;
                break;

            case AnchorPosition.TopRight:
                rectTransform.anchorMin = new Vector2(1, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.pivot = new Vector2(1, 1);
                rectTransform.anchoredPosition = new Vector2(-10, -10);
                fpsText.alignment = TextAlignmentOptions.TopRight;
                break;
        }
    }
}
