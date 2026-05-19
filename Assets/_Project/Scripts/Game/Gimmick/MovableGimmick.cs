using System.Collections;
using UnityEngine;

public class MovableGimmick : BaseGimmick
{
    private bool isMoving = false;

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        Debug.Log($"[MovableGimmick Execute] state={ntf.state}, targetPos={ntf.targetPos}");
        
        // 목표 좌표(ntf.targetPos)로 이동!
        if (!isMoving) StartCoroutine(MoveRoutine(ntf.targetPos.ToVector3()));
    }


    public void StartMove(Vector3 destPos)
    {
        if (!isMoving) StartCoroutine(MoveRoutine(destPos));
    }

    private IEnumerator MoveRoutine(Vector3 destPos)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        float timer = 0f;
        float duration = 0.25f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float smooth = 1f - (1f - t) * (1f - t);
            transform.position = Vector3.Lerp(startPos, destPos, smooth);
            yield return null;
        }

        transform.position = destPos;
        isMoving = false;
    }
}