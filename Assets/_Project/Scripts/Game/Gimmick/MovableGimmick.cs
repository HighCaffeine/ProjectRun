using System.Collections;
using UnityEngine;

public class MovableGimmick : BaseGimmick
{
    private bool isMoving = false;
    private Vector3 initialPos;


    protected override void Awake()
    {
        base.Awake();
        initialPos = TargetTransform.position;
    }
    public override void Execute(P_GimmickInteractNtf ntf)
    {
     //   //Debug.Log($"[MovableGimmick Execute] state={ntf.state}, targetPos={ntf.targetPos}");

        if (!isMoving) StartCoroutine(MoveRoutine(ntf.targetPos.ToVector3()));
    }


    public void StartMove(Vector3 destPos)
    {
        if (!isMoving) StartCoroutine(MoveRoutine(destPos));
    }

    public override void ResetGimmick()
    {
        StopAllCoroutines();
        isMoving = false;
        TargetTransform.position = initialPos;
    }

    private IEnumerator MoveRoutine(Vector3 destPos)
    {
        isMoving = true;
        Vector3 startPos = TargetTransform.position;
        float timer = 0f;
        float duration = 0.25f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float smooth = 1f - (1f - t) * (1f - t);
            TargetTransform.position = Vector3.Lerp(startPos, destPos, smooth);
            yield return null;
        }

        TargetTransform.position = destPos;
        isMoving = false;
    }
}