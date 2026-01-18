using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerMovement Movement;
    public string Name;
    public long ID;
    public bool IsLocal;
    public uint lastProcessedSeq = 0; // 서버가 처리 완료한 마지막 번호
    public Vector3 serverPos;
    public float lerpSpeed = 15f;
    public float currentSpeed;       // 서버 확정 속도
    public bool isMoving;
    public void OnSyncMovement(P_UpdatePlayerMovement pkt)
    {
        serverPos = pkt.currentPos;
        currentSpeed = pkt.currentSpeed;
        isMoving = pkt.isMoving;
        lastProcessedSeq = pkt.lastInputSeq; // 내가 보낸 입력이 어디까지 반영됐는지 확인
    }

    void FixedUpdate()
    {
        float dist = Vector3.Distance(transform.position, serverPos);

        if (IsLocal)
        {
            // 오차 보정, 서버와 1m 이상 차이 나면 강제 순간이동, 아니면 부드럽게 Lerp
            if (dist > 1.0f) transform.position = serverPos;
            else transform.position = Vector3.Lerp(transform.position, serverPos, Time.deltaTime * lerpSpeed);
        }
        else
        {
            // 다른 플레이어 보간처리, AOI 범위 내 다른 유저는 서버 좌표로 부드럽게
            transform.position = Vector3.Lerp(transform.position, serverPos, Time.deltaTime * lerpSpeed);
        }

        // 속도 기반 애니메이션 제어 예정
        // animator.SetFloat("MoveSpeed", isMoving ? currentSpeed : 0);
    }
}
