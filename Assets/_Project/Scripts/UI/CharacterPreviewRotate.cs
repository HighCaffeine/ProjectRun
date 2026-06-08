using UnityEngine;

public class CharacterPreviewRotate : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform previewArea;

    [Header("Character")]
    [SerializeField] private Transform rotateTarget;

    [Header("Rotate")]
    [SerializeField] private float rotateSpeed = 250f;
    [SerializeField] private float maxAngle = 50f;

    [Header("Return")]
    [SerializeField] private float returnSpeed = 3f;

    [Header("Smooth")]
    [SerializeField] private float smoothTime = 0.1f;

    private bool isDragging;

    private float currentY;
    private float targetY;
    private float velocity;

    private void Update()
    {
        CheckDragStart();
        CheckDragEnd();

        if (isDragging)
        {
            RotateCharacter();
        }
        else
        {
            ReturnToFront();
        }

        currentY = Mathf.SmoothDampAngle(
            currentY,
            targetY,
            ref velocity,
            smoothTime);

        rotateTarget.localRotation =
            Quaternion.Euler(0f, currentY, 0f);
    }

    private void CheckDragStart()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (RectTransformUtility.RectangleContainsScreenPoint(
            previewArea,
            Input.mousePosition))
        {
            isDragging = true;
        }
    }

    private void CheckDragEnd()
    {
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    private void RotateCharacter()
    {
        float mouseX = Input.GetAxis("Mouse X");

        targetY -= mouseX * rotateSpeed * Time.deltaTime;

        targetY = Mathf.Clamp(
            targetY,
            -maxAngle,
            maxAngle);
    }

    private void ReturnToFront()
    {
        targetY = Mathf.Lerp(
            targetY,
            0f,
            Time.deltaTime * returnSpeed);
    }
}