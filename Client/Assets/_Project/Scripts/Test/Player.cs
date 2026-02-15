using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerMovement Movement;
    public string Name;
    public long ID;
    public bool IsLocal;

    public Vector3 serverPos;
    public float currentSpeed = 5.0f;
    public bool isMoving;
    public uint lastProcessedSeq = 0;

    private float snapThreshold = 5.0f; // 2m는 너무 짧아서 핑 튀면 제자리로 당겨짐 -> 5m로 완화
    private float catchUpThreshold = 0.5f;
    private float deadZone = 0.05f; // 미세 떨림 방지용

    public void OnSyncMovement(P_UpdatePlayerMovement pkt)
    {
        if (pkt.lastInputSeq < lastProcessedSeq) return;
        lastProcessedSeq = pkt.lastInputSeq;

        serverPos = pkt.currentPos.ToVector3();
        currentSpeed = pkt.currentSpeed;
        isMoving = pkt.isMoving;
    }

    void Update()
    {
        if (IsLocal)
        {
            ProcessLocalCalibration();
        }
        else
        {
            ProcessRemoteMovement();
        }
    }

    // 내 캐릭터 보정 (텔레포트 방지)
    void ProcessLocalCalibration()
    {
        float dist = Vector3.Distance(transform.position, serverPos);

        // 오차가 5m 이내면 서버 위치 무시
        if (dist < snapThreshold)
        {
            return;
        }

        //  벽 뚫거나 했을 때만 강제 위치 보정
        transform.position = serverPos;
    }

    // 상대방 부드럽게 이동
    void ProcessRemoteMovement()
    {
        float dist = Vector3.Distance(transform.position, serverPos);

        if (dist < deadZone) return; // 떨림 방지

        if (dist > snapThreshold)
        {
            transform.position = serverPos;
            return;
        }

        float moveStep = currentSpeed * Time.deltaTime;
        if (dist > catchUpThreshold) moveStep *= 1.2f;

        transform.position = Vector3.MoveTowards(transform.position, serverPos, moveStep);

        Vector3 dir = (serverPos - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 15f);
        }
    }
}