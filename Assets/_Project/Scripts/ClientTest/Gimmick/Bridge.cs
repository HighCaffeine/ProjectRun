using System.Collections;
using UnityEngine;

public class Bridge : BaseGimmick
{
    [Header("ȸ�� ��� (Pivot)")]
    [SerializeField] private Transform pivot;

    [Header("���� ����")]
    [SerializeField] private float closeAngle = 0f;

    [Header("�ӵ�")]
    [SerializeField] private float speed = 2f;

    private Coroutine currentCoroutine;

    // �ܺο��� ȣ�� (�з��� ��)

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (ntf.state == (byte)eGimmickState.On_Activate) // Active 명령이면 다리를 닫음
        {
            StartRotate(closeAngle);
        }
    }

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