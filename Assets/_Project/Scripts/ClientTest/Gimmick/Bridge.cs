using System.Collections;
using UnityEngine;

public class Bridge : MonoBehaviour
{
    [Header("회전 대상 (Pivot)")]
    [SerializeField] private Transform pivot;

    [Header("각도 설정")]
    [SerializeField] private float closeAngle = 0f;

    [Header("속도")]
    [SerializeField] private float speed = 2f;

    private Coroutine currentCoroutine;

    // 외부에서 호출 (압력판 등)

    public void CloseBridge()
    {
        StartRotate(closeAngle);
    }

    private void StartRotate(float targetAngle)
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(RotateBridge(targetAngle));
    }

    private IEnumerator RotateBridge(float targetAngle)
    {
        Quaternion startRot = pivot.rotation;
        Quaternion targetRot = Quaternion.Euler(targetAngle, pivot.eulerAngles.y, pivot.eulerAngles.z);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * speed;

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            pivot.rotation = Quaternion.Lerp(startRot, targetRot, smoothT);

            yield return null;
        }

        pivot.rotation = targetRot;
    }
}