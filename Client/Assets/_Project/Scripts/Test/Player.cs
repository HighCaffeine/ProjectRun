using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string Name;
    public long ID;
    public bool IsLocal;
    // public PlayerMovement Movement;

    // // Update is called once per frame
    // void FixedUpdate()
    // {
    //     if (Movement != null && IsLocal)
    //     {
    //         Movement.Move();
    //     }
    // }

    public uint lastProcessedSeq = 0; // 서버가 처리 완료한 마지막 번호
    public Vector3 serverPos;
    public float lerpSpeed = 15f;

    void FixedUpdate()
    {
        // 거리 측정
        float distance = Vector3.Distance(transform.position, serverPos);

        if (IsLocal)
        {
            if (distance > 1.5f)
            {
                // 오차가 너무 크면 서버 위치로 강제 이동
                transform.position = serverPos;
            }
            else
            {
                //서버 위치로 lerp
                transform.position = Vector3.Lerp(transform.position, serverPos, Time.fixedDeltaTime * lerpSpeed);
            }
        }
        else
        {
            // 타 유저는 서버 좌표로 lerp
            transform.position = Vector3.Lerp(transform.position, serverPos, Time.fixedDeltaTime * lerpSpeed);
        }
    }
}
