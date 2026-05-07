using UnityEngine;
using System.Collections;

//보정 클래스
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerActor actor;
    [SerializeField] private string name;
    [SerializeField] private long id;
    [SerializeField] private bool isLocal;

    [Header("Sync Data")]
    [SerializeField] private Vector3 serverPos;
    [SerializeField] private Quaternion serverRot;
    [SerializeField] private float currentSpeed = 5.0f;
    [SerializeField] private bool isMoving;
    [SerializeField] private uint lastProcessedSeq = 0;

    [Header("Calibration Settings")]
    [SerializeField] private float snapThreshold = 5.0f; // 이 이상 벌어지면 강제 텔레포트
    [SerializeField] private float lerpSpeed = 15.0f;    // 타 플레이어 부드러운 이동 속도

    public string Name => this.name;
    public long ID => this.id;
    public bool IsLocal => this.isLocal;

    public void Init(PlayerActor actor, string name, long id, bool isLocal, Vector3 serverPos)
    {
        this.actor = actor;
        this.name = name;
        this.id = id;
        this.isLocal = isLocal;
        this.serverPos = serverPos;
    }

    public void SetSpeed(float speed) { this.currentSpeed = speed; }
    public void SetPos(Vector3 pos) { this.serverPos = pos; }
    public void SetRot(Quaternion rot) { this.serverRot = rot; }
    private bool hasReceivedFirstSync = false;

    public void OnSyncMovement(P_UpdatePlayerMovement pkt)
    {
        if (isLocal)
        {
            if (pkt.lastInputSeq < lastProcessedSeq) return;
            lastProcessedSeq = pkt.lastInputSeq;
        }

        serverPos = pkt.currentPos.ToVector3();
        serverRot = pkt.currentRot.ToQuaternion();
        currentSpeed = pkt.currentSpeed;
        isMoving = pkt.isMoving;

        if (!hasReceivedFirstSync)
        {
            hasReceivedFirstSync = true;
            transform.position = serverPos;
            transform.rotation = serverRot;
        }
    }

    void Update()
    {
        if (float.IsNaN(serverPos.x) || float.IsNaN(serverRot.x)) return;

        if (isLocal)
        {
            ProcessLocalCalibration();
        }
        else
        {
            ProcessRemoteMovement();
        }

        // if (isLocal && Time.frameCount % 60 == 0) // 1초에 한 번씩 출력
        // {
        //     Debug.Log($"[Sync Check] Client: {transform.position.x:F2}, {transform.position.z:F2} | Server: {serverPos.x:F2}, {serverPos.z:F2}");
        // }
    }

    // private void ProcessLocalCalibration()
    // {
    //     Vector3 currentPos = transform.position;
    //     float distXZ = Vector2.Distance(new Vector2(currentPos.x, currentPos.z), new Vector2(serverPos.x, serverPos.z));

    //     // 오차가 클 경우 텔포시킴
    //     if (distXZ > snapThreshold)
    //     {
    //         actor.SetControllerActive(false);
    //         transform.position = serverPos;
    //         actor.SetControllerActive(true);
    //         return;
    //     }

    //     //이동중에 조금씩 당김
    //     if (distXZ > 0.01f)
    //     {
    //         Vector3 pullDir = (serverPos - currentPos);
    //         pullDir.y = 0;

    //         // 이동 중일 때는 더 약하게, 멈췄을 때는 조금 더 강하게 보정
    //         float strength = isMoving ? 1.5f : 3.0f;
    //         actor.Move(pullDir.normalized, strength);
    //     }

    //     // Y축 높이 보정은 실시간 Lerp 유지
    //     float lerpY = Mathf.Lerp(currentPos.y, serverPos.y, Time.deltaTime * 30.0f);
    //     transform.position = new Vector3(transform.position.x, lerpY, transform.position.z);
    // }

    // private void ProcessRemoteMovement()
    // {
    //     float dist = Vector3.Distance(transform.position, serverPos);

    //     if (dist > snapThreshold * 2.0f)
    //     {
    //         transform.position = serverPos;
    //         return;
    //     }

    //     //거리 따라 속도 조절
    //     float adaptiveSpeed = dist > 1.0f ? lerpSpeed * 1.5f : lerpSpeed;
    //     if (Client.IS_SERVER_PLAY)
    //     {
    //         transform.position = Vector3.Lerp(transform.position, serverPos, Time.deltaTime * adaptiveSpeed);
    //     }
    //     else
    //     {
    //         Vector3 targetPos = new Vector3(serverPos.x, transform.position.y, serverPos.z);
    //         transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * adaptiveSpeed);
    //     }

    //     Vector3 dir = (serverPos - transform.position);
    //     dir.y = 0;
    //     if (dir.sqrMagnitude > 0.001f)
    //     {
    //         Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
    //         transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lerpSpeed);
    //     }
    // }

    public void ApplyKnockback(Vector3 attackerPos, byte actionType)
    {
        if (actor == null) return;

        bool isPull = (actionType == 1);
        Vector3 pushDir = (transform.position - attackerPos).normalized;
        pushDir.y = 0;
        if (isPull) pushDir = -pushDir;

        if (actor.IsLocal)
        {
            // 로컬: 실제 물리 넉백 적용
            actor.sm.ChangeState(new KnockbackState(actor, pushDir, 15.0f, isPull, attackerPos));
        }
        else
        {
            // 리모트: 이펙트만 (위치는 서버에서 UPDATE_PLAYER_MOVEMENT로 옴)
            StartCoroutine(RemoteVisualRoutine(actionType));
        }
    }

    private void ProcessLocalCalibration()
    {
        if (actor.sm.currentState is KnockbackState) return;

        Vector3 currentPos = transform.position;
        // 거리 체크를 X, Z 평면에서만 수행 (높이 차이는 무시)
        float dx = serverPos.x - currentPos.x;
        float dz = serverPos.z - currentPos.z;
        float sqrDistXZ = (dx * dx) + (dz * dz);

        if (sqrDistXZ > (snapThreshold * snapThreshold)) // 제곱값끼리 비교
        {
            actor.SetControllerActive(false);

            // 텔포 시에도 현재 내 Y값은 유지하고 X, Z만 맞춤
            transform.position = new Vector3(serverPos.x, transform.position.y, serverPos.z);
            actor.SetControllerActive(true);
            return;
        }

        if (sqrDistXZ > 0.01f)
        {
            Vector3 pullDir = (serverPos - currentPos);
            pullDir.y = 0; // 수평으로만 당김

            float strength = isMoving ? 1.5f : 3.0f;
            // actor.Move(pullDir, strength);
        }

        // transform.position = new Vector3(transform.position.x, lerpY, transform.position.z);
    }
    private void ProcessRemoteMovement()
    {
        // 거리를 X, Z 기준으로만 계산
        Vector3 currentPos = transform.position;
        float dx = serverPos.x - currentPos.x;
        float dz = serverPos.z - currentPos.z;
        float sqrDistXZ = (dx * dx) + (dz * dz);
        float snapLimit = snapThreshold * 2.0f;

        if (sqrDistXZ > (snapLimit * snapLimit))
        {
            transform.position = new Vector3(serverPos.x, serverPos.y, serverPos.z);
            transform.rotation = serverRot;
            return;
        }

        float adaptiveSpeed = sqrDistXZ > 1.0f ? lerpSpeed * 1.5f : lerpSpeed;

        Vector3 targetPos = new Vector3(serverPos.x, serverPos.y, serverPos.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * adaptiveSpeed);

        transform.rotation = Quaternion.Slerp(transform.rotation, serverRot, Time.deltaTime * lerpSpeed);
    }

    private IEnumerator RemoteVisualRoutine(byte actionType)
    {
        PlayerActor pActor = GetComponent<PlayerActor>();
        if (pActor != null)
        {
            if (pActor.trailRenderer != null) pActor.trailRenderer.emitting = true;
            if (pActor.travelSparkParticle != null) pActor.PlayTravelSpark((eState)actionType);
        }

        yield return new WaitForSeconds(0.25f); // 넉백 지속시간

        if (pActor != null)
        {
            if (pActor.trailRenderer != null) pActor.trailRenderer.emitting = false;
            if (pActor.travelSparkParticle != null) pActor.StopTravelSpark();
        }
    }
}