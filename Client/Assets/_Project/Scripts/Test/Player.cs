using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerMovement Movement;
    public string Name;
    public long ID;
    public bool IsLocal;

    public Vector3 serverPos; // 보정된 서버 좌표
    public float currentSpeed = 5.0f;
    public bool isMoving;
    public uint lastProcessedSeq = 0;

    private float snapThreshold = 1.5f;
    private float deadZone = 0.05f;

    private float heightOffset = 0.5f;

    public void OnSyncMovement(P_UpdatePlayerMovement pkt)
    {
        if (IsLocal)
        {
            if (pkt.lastInputSeq < lastProcessedSeq) return;
            lastProcessedSeq = pkt.lastInputSeq;
        }

        Vector3 rawPos = pkt.currentPos.ToVector3();
        serverPos = new Vector3(-rawPos.x, rawPos.y + heightOffset, rawPos.z);

        currentSpeed = pkt.currentSpeed;
        isMoving = pkt.isMoving;
    }

    void Update()
    {
        if (IsLocal)
            ProcessLocalCalibration();
        else
            ProcessRemoteMovement();
    }

    void ProcessLocalCalibration()
    {
        Vector3 currentPos = transform.position;
        float distXZ = Vector2.Distance(new Vector2(currentPos.x, currentPos.z), new Vector2(serverPos.x, serverPos.z));
        float distY = Mathf.Abs(currentPos.y - serverPos.y);

        // 거리가 크게 벌어졌을 때 (밀치기 등) 바로 보정
        if (distXZ > snapThreshold || distY > 2.0f)
        {
            if (Movement != null && Movement.Controller != null) Movement.Controller.enabled = false;
            transform.position = serverPos;
            if (Movement != null && Movement.Controller != null) Movement.Controller.enabled = true;
        }
        // 경사로 보정
        else if (distXZ > 0.05f || distY > 0.01f)
        {
            if (Movement != null && Movement.Controller != null) Movement.Controller.enabled = false;

            // 높이는 경사로를 타는 중이므로 수평 이동보다 더 빠르게 따라가도록 설정
            float newY = Mathf.Lerp(currentPos.y, serverPos.y, Time.deltaTime * 15f);
            float newX = Mathf.Lerp(currentPos.x, serverPos.x, Time.deltaTime * 10f);
            float newZ = Mathf.Lerp(currentPos.z, serverPos.z, Time.deltaTime * 10f);

            transform.position = new Vector3(newX, newY, newZ);

            if (Movement != null && Movement.Controller != null) Movement.Controller.enabled = true;
        }
    }

    void ProcessRemoteMovement()
    {
        float dist = Vector3.Distance(transform.position, serverPos);
        if (dist < deadZone) return;

        // 상대방과 큐브는 무조건 서버 위치 추적
        transform.position = Vector3.Lerp(transform.position, serverPos, Time.deltaTime * 20f);

        Vector3 dir = (serverPos - transform.position);
        dir.y = 0; // 회전 시에는 수평 방향만 고려
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 15f);
        }
    }
}