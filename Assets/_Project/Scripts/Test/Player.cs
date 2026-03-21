using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private string name;
    [SerializeField] private long id;
    [SerializeField] private bool isLocal;

    [Header("Sync Data")]
    [SerializeField] private Vector3 serverPos;
    [SerializeField] private float currentSpeed = 5.0f;
    [SerializeField] private bool isMoving;
    [SerializeField] private uint lastProcessedSeq = 0;

    [Header("Calibration Settings")]
    [SerializeField] private float snapThreshold = 5.0f; // 이 이상 벌어지면 강제 텔레포트
    [SerializeField] private float lerpSpeed = 15.0f;    // 타 플레이어 부드러운 이동 속도

    public string Name => this.name;
    public long ID => this.id;
    public bool IsLocal => this.isLocal;

    public void Init(PlayerMovement movement, string name, long id, bool isLocal, Vector3 serverPos)
    {
        this.movement = movement;
        this.name = name;
        this.id = id;
        this.isLocal = isLocal;
        this.serverPos = serverPos;
    }

    public void SetSpeed(float speed) { this.currentSpeed = speed; }
    public void SetPos(Vector3 pos) { this.serverPos = pos; }


    public void OnSyncMovement(P_UpdatePlayerMovement pkt)
    {
        // if (isLocal)
        // {
        //     if (pkt.lastInputSeq < lastProcessedSeq) return;
        //     lastProcessedSeq = pkt.lastInputSeq;
        // }

        if (pkt.lastInputSeq < lastProcessedSeq) return;
        lastProcessedSeq = pkt.lastInputSeq;

        serverPos = pkt.currentPos.ToVector3();
        currentSpeed = pkt.currentSpeed;
        isMoving = pkt.isMoving;
    }

    void Update()
    {
        if (isLocal)
        {
            ProcessLocalCalibration();
        }
        else
        {
            ProcessRemoteMovement();
        }

        if (isLocal && Time.frameCount % 60 == 0) // 1초에 한 번씩 출력
        {
            Debug.Log($"[Sync Check] Client: {transform.position.x:F2}, {transform.position.z:F2} | Server: {serverPos.x:F2}, {serverPos.z:F2}");
        }
    }

    private void ProcessLocalCalibration()
    {
        Vector3 currentPos = transform.position;
        float distXZ = Vector2.Distance(new Vector2(currentPos.x, currentPos.z), new Vector2(serverPos.x, serverPos.z));

        // 오차가 클 경우 텔포시킴
        if (distXZ > snapThreshold)
        {
            if (movement?.Controller != null) movement.Controller.enabled = false;
            transform.position = serverPos;
            if (movement?.Controller != null) movement.Controller.enabled = true;
            return;
        }

        //이동중에 조금씩 당김
        if (distXZ > 0.01f)
        {
            Vector3 pullDir = (serverPos - currentPos);
            pullDir.y = 0;

            // 이동 중일 때는 더 약하게, 멈췄을 때는 조금 더 강하게 보정
            float strength = isMoving ? 1.5f : 3.0f;
            movement.Controller.Move(pullDir * Time.deltaTime * strength);
        }

        // Y축 높이 보정은 실시간 Lerp 유지
        float lerpY = Mathf.Lerp(currentPos.y, serverPos.y, Time.deltaTime * 30.0f);
        transform.position = new Vector3(transform.position.x, lerpY, transform.position.z);
    }

    private void ProcessRemoteMovement()
    {
        float dist = Vector3.Distance(transform.position, serverPos);

        if (dist > snapThreshold * 2.0f)
        {
            transform.position = serverPos;
            return;
        }

        //거리 따라 속도 조절
        float adaptiveSpeed = dist > 1.0f ? lerpSpeed * 1.5f : lerpSpeed;

        transform.position = Vector3.Lerp(transform.position, serverPos, Time.deltaTime * adaptiveSpeed);

        Vector3 dir = (serverPos - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lerpSpeed);
        }
    }
}