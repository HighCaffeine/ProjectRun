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
        serverPos = new Vector3(rawPos.x, rawPos.y + heightOffset, rawPos.z);

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

        // [내 캐릭터 보정]
        // 거리가 크게 벌어졌을 때(밀치기, 텔레포트 등)만 '단 한 번' 강제로 스냅(Snap)합니다.
        if (distXZ > snapThreshold || distY > 2.0f)
        {
            if (Movement != null && Movement.Controller != null) Movement.Controller.enabled = false;
            transform.position = serverPos;
            if (Movement != null && Movement.Controller != null) Movement.Controller.enabled = true;
        }
        // 오차가 작을 때는 서버 위치로 억지로 당기지 않고 내버려 둡니다.
        // 내 조작감(Client Prediction)을 우선시해야 화면이 덜덜 떨리지 않습니다.
    }

    void ProcessRemoteMovement()
    {
        float dist = Vector3.Distance(transform.position, serverPos);
        if (dist < deadZone) return;

        // [상대방 캐릭터 보정]
        // 상대방은 컨트롤러 On/Off 없이 무조건 Lerp로 부드럽게 서버 위치를 따라가게 합니다.
        // (상대방 프리팹에는 CharacterController를 아예 빼두거나 비활성화해 두는 것이 좋습니다.)
        transform.position = Vector3.Lerp(transform.position, serverPos, Time.deltaTime * 15f);

        Vector3 dir = (serverPos - transform.position);
        dir.y = 0; // 회전 시에는 수평 방향만 고려

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 15f);
        }
    }
}