using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bridge : BaseGimmick
{
    [Header("ȸ�� ��� (Pivot)")]
    [SerializeField] private List<Transform> pivot;

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
        List<Quaternion> startRots = new List<Quaternion>();
        List<Quaternion> targetRots = new List<Quaternion>();

        // 초기값 저장
        foreach (var p in pivot)
        {
            startRots.Add(p.rotation);
            targetRots.Add(Quaternion.Euler(targetAngle, p.eulerAngles.y, p.eulerAngles.z));
        }

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < pivot.Count; i++)
            {
                pivot[i].rotation = Quaternion.Lerp(startRots[i], targetRots[i], smoothT);
            }

            yield return null;
        }

        // 마지막 보정
        for (int i = 0; i < pivot.Count; i++)
        {
            pivot[i].rotation = targetRots[i];
        }
    }
}