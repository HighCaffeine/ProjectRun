using System.Collections;
using UnityEngine;

public class MovableGimmick : MonoBehaviour
{
    public int gimmickUID;
    private bool isMoving = false;

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