using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum MonsterState
{
    Normal,
    Knockback,
    Stunned,
    Dead
}
public class MonsterActor : Actor
{
    [Header("Monster Network Sync")]
    public int monsterID; // 서버에서 관리할 몬스터 ID
    private Vector3 serverPos;
    private Quaternion serverRot;
    private float snapThreshold = 5.0f;
    private float lerpSpeed = 10.0f;
    private Vector3 lastSentPos;

    [SerializeField]
    private PlayerActor currentTarget;

    //방장에게 권한 부여
    public override bool IsLocal => GameManager.Instance != null && GameManager.Instance.isHost;

    [SerializeField]
    private float recoveryTime = 5f;
    [SerializeField] private float heightOffset = 1.5f;
    public MonsterState monsterState = MonsterState.Normal;

    private Coroutine stunCoroutine;

    [SerializeField] private SkinnedMeshRenderer meshRenderer;
    [SerializeField] private Material normalMat;
    [SerializeField] private Material stunnedMat;

    public long lastAttackerID;
    private Vector3 estimatedPos;

    public override Vector3 GetMovementDirection()
    {
        if (monsterState == MonsterState.Stunned) return Vector3.zero;
        if (currentTarget == null) return Vector3.zero;

        Vector3 dir = currentTarget.transform.position - transform.position;
        dir.y = 0f;

        // ★ 이동 중 타겟 방향으로 회전
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        return dir.normalized;
    }
    public override void ApplyMovement()
    {
        if (controller == null || !controller.enabled) return;
        if (monsterState == MonsterState.Stunned || monsterState == MonsterState.Dead) return;

        // 넉백은 베이스 로직 사용 (verticalVelocity, horizontalMove 그대로)
        if (sm.currentState is KnockbackState)
        {
            base.ApplyMovement();
            return;
        }

        // 몬스터는 중력 없이 horizontalMove만 적용
        Vector3 moveDelta = horizontalMove * Time.deltaTime;
        controller.Move(moveDelta);
        horizontalMove = Vector3.zero;
        // ★ verticalVelocity, gravity 일절 건드리지 않음
    }
    public override bool HasMoveIntent()
    {
        if (monsterState != MonsterState.Normal) return false;
        if (currentTarget == null) return false;

        Vector3 myPos = transform.position;
        Vector3 targetPos = currentTarget.transform.position;

        myPos.y = 0;
        targetPos.y = 0;

        float distance = Vector3.Distance(myPos, targetPos);

        return distance > 2f;
    }

    public override bool CheckActionIntent()
    {
        if (currentTarget == null)
            return false;

        Vector3 myPos = transform.position;
        Vector3 targetPos = currentTarget.transform.position;

        myPos.y = 0;
        targetPos.y = 0;

        float distance = Vector3.Distance(myPos, targetPos);


        return distance <= 2f;
    }

    protected new void Start()
    {
        //serverPos = transform.position;
        //serverRot = transform.rotation;
        base.Start();
        
    }

    public void InitMonster(int id, Vector3 pos, Quaternion rot)
    {
        serverPos = pos; transform.position = pos;
        serverRot = rot; transform.rotation = rot;
        monsterID = id;

        if (Match.Instance != null)
        {
            Match.Instance._monsterCache[this.monsterID] = this;
        }

        if (!IsLocal)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }
        if (meshRenderer != null)
        {
            meshRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); // 값 조정
        }
    }

    private void Update()
    {
        Debug.Log($"[MonsterUpdate] {name} IsLocal={IsLocal}, state={monsterState}"); // ★ 추가

        if (monsterState == MonsterState.Dead) return;

        if (IsLocal)
        {
            UpdateTarget();

            if (currentTarget != null)
                UpdateHeight(); // ★ sm.Update() 전에 호출해서 horizontalMove에 Y 누적

            sm.Update();       // ← sm 내부에서 horizontalMove에 수평 방향 누적
            ApplyMovement();   // ← 한번에 수평+수직 이동 (중력 없음)

            if (!(sm.currentState is KnockbackState))
                TryChangeToActionState();

            HandleNetworkSync();
        }
        else
        {
            sm.Update();
            ProcessRemoteMovement();
        }

        UpdateMaterialByState();
    }
    private void UpdateHeight()
    {
        if (currentTarget == null || controller == null || !controller.enabled) return;

        float targetY = currentTarget.transform.position.y + heightOffset;
        const float HEIGHT_EPSILON = 0.05f;
        float diff = targetY - transform.position.y;

        if (Mathf.Abs(diff) <= HEIGHT_EPSILON) return;

        // ★ horizontalMove의 Y에 얹어서 ApplyMovement에서 한번에 처리
        float yStep = Mathf.MoveTowards(0f, diff, 3f * Time.deltaTime);
        horizontalMove += Vector3.up * yStep / Time.deltaTime;
        // ApplyMovement에서 * Time.deltaTime 하므로 나눠서 넣음
    }

    private void UpdateTarget()
    {
        if (currentTarget == null || currentTarget.isDead)
        {
            Debug.Log("UpdateTarget들어옴");
            ChooseTarget();
            Debug.Log("최종 currentTarget = " +(currentTarget == null ? "NULL" : currentTarget.name));
        }
    }

    private void TryChangeToActionState()
    {
        Debug.Log($"[TryAction] currentState={sm.currentState?.GetType().Name}, CanStart={CanStartAction()}, CheckIntent={CheckActionIntent()}");
        if (!CanStartAction()) return;

        float distance = currentTarget == null
            ? -1f
            : Vector3.Distance(transform.position, currentTarget.transform.position);

   

        if (!CheckActionIntent()) return;

        Debug.Log($"{name} ActionState 진입!");
        
        sm.ChangeState(new ActionState(this, eState.Push, currentTarget));
    }

    private bool CanStartAction()
    {
        return sm.currentState is IdleState || sm.currentState is MoveState || sm.currentState is ActionState; ;
    }

    public PlayerActor GetClosestPlayerTarget()
    {
        if (Match.Instance == null || Match.Instance.Players == null) return null;

        PlayerActor closest = null;
        float minDistance = float.MaxValue;

        foreach (var kvp in Match.Instance.Players)
        {
            Player player = kvp.Value;
            if (player == null || player.gameObject == null) continue;

            // ★ Player 컴포넌트에서 직접 GetComponent (매 프레임 호출 최적화 위해 Player가 캐싱하면 더 좋음)
            PlayerActor actor = player.GetComponent<PlayerActor>();
            if (actor == null) continue;
            if (actor.isDead) continue;
            if (!actor.gameObject.activeInHierarchy) continue;

            float dist = Vector3.Distance(transform.position, actor.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = actor;
            }
        }

        Debug.Log($"[ChooseTarget] 결과: {(closest == null ? "NULL" : closest.name)}, Players 수: {Match.Instance.Players.Count}");
        return closest;
    }

    private void ChooseTarget()
    {
        if (currentTarget != null && !currentTarget.isDead) return;

        currentTarget = GetClosestPlayerTarget();

        if (currentTarget == null)
            Debug.LogWarning($"[{name}] 타겟을 찾지 못함. Players.Count = {Match.Instance?.Players?.Count}");
    }

    // 사망 처리 동기화
    public void RequestMonsterDead()
    {
        if (!IsLocal) return;

        P_MonsterDeadReq pkt = new P_MonsterDeadReq
        {
            monsterID = this.monsterID
        };

        Client.TCP.SendPacket2(E_PACKET.MONSTER_DEAD_REQ, pkt);
    }

    // 서버 브로드캐스트수신 시 모든 클라이언트가 공통으로 실행할 파괴 로직
    public void ExecuteMonsterDead(Vector3 hitDirection)
    {
        if (monsterState == MonsterState.Dead) return;

        monsterState = MonsterState.Dead;

        if (Match.Instance != null && Match.Instance._monsterCache.ContainsKey(monsterID))
        {
            Match.Instance._monsterCache.Remove(monsterID);
        }

        FractureObject fractureObject = GetComponent<FractureObject>();
        if (fractureObject != null)
        {
            fractureObject.BreakToDirection(hitDirection);
        }

        Destroy(gameObject, 2f);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!IsLocal) return;

        if (monsterState == MonsterState.Knockback)
        {
            if (hit.gameObject.CompareTag("Wall"))
            {
                SetStunned(recoveryTime);
                sm.ChangeState(new IdleState(this));
            }
        }
    }

    public void SetStunned(float duration)
    {
        monsterState = MonsterState.Stunned;

        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        stunCoroutine = StartCoroutine(StateRecovery(duration));
    }

    private IEnumerator StateRecovery(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (monsterState == MonsterState.Stunned)
        {
            monsterState = MonsterState.Normal;
        }

        stunCoroutine = null;
    }

    private void UpdateMaterialByState()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }
        Material[] mats = meshRenderer.materials;

        switch (monsterState)
        {
            case MonsterState.Stunned:
                mats[1] = stunnedMat;
                break;
            case MonsterState.Normal:
                mats[1] = normalMat;
                break;
        }

        meshRenderer.materials = mats;
    }

    // 이동 위치 수신 및 보간 처리
    public void OnSyncMovement(Vector3 targetPos, Quaternion targetRot)
    {
        serverPos = targetPos;
        serverRot = targetRot;
    }

    private void ProcessRemoteMovement()
    {
        float dist = Vector3.Distance(transform.position, serverPos);

        if (dist > snapThreshold)
        {
            SetControllerActive(false);
            transform.position = serverPos;
            SetControllerActive(true);
            transform.rotation = serverRot;
            estimatedPos = serverPos;
            return;
        }

        transform.position = Vector3.Lerp(transform.position, estimatedPos, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, serverRot, Time.deltaTime * lerpSpeed);
    }

    private Quaternion lastSentRot; 

private void HandleNetworkSync()
{
    sendTimer += Time.deltaTime;
    if (sendTimer >= Actor.sendInterval)
    {
        bool isPositionChanged = Vector3.Distance(transform.position, lastSentPos) > 0.05f;
        bool isRotationChanged = Quaternion.Angle(transform.rotation, lastSentRot) > 5.0f; 

        if (HasMoveIntent() || sm.currentState is KnockbackState || isPositionChanged || isRotationChanged)
        {
            SendMovePacket(h, v);
            lastSentPos = transform.position;
            lastSentRot = transform.rotation;
            sendTimer = 0f;
        }
    }
}

    public override void SendMovePacket(float axisH, float axisV)
    {
        if (!IsLocal) return;

        P_MonsterMovement pkt = new P_MonsterMovement
        {
            userUUID = LocalPlayerInfo.ID,
            monsterID = this.monsterID,
            currentPos = new P_PacketVector3 { x = transform.position.x, y = transform.position.y, z = transform.position.z },
            currentRot = new P_PacketQuaternion { x = transform.rotation.x, y = transform.rotation.y, z = transform.rotation.z, w = transform.rotation.w },
            timestamp = NetworkTimeManager.Instance.GetServerTime()
        };

        Client.UDP.SendPacket2(E_PACKET.MONSTER_MOVEMENT, pkt);
        //Client.TCP.SendPacket2(E_PACKET.MONSTER_MOVEMENT, pkt);
    }

    public void OnSyncMovement(Vector3 targetPos, Quaternion targetRot, float latency)
    {
        Vector3 moveDelta = targetPos - serverPos;

        serverPos = targetPos;
        serverRot = targetRot;

        if (moveDelta.magnitude > 0.001f && moveDelta.magnitude < snapThreshold)
        {
            Vector3 inferredVelocity = moveDelta / Actor.sendInterval;
            estimatedPos = serverPos + (inferredVelocity * latency);
        }
        else
        {
            estimatedPos = serverPos;
        }
    }

    public override void SendStateChange(eState stateCode, Vector3 dir = default, float param = 0f, long targetUUID = 0, bool isPull = false, Vector3 casterPos = default, long casterUUID = 0)
    {
        if (!IsLocal) return;

        P_MonsterStateNtf pkt = new P_MonsterStateNtf
        {
            monsterID = this.monsterID,
            newState = (byte)stateCode,
            targetDir = new P_PacketVector3 { x = dir.x, y = dir.y, z = dir.z },
            param = param,
            isPull = isPull ? (byte)1 : (byte)0,
            casterPos = new P_PacketVector3 { x = casterPos.x, y = casterPos.y, z = casterPos.z },
            timestamp = NetworkTimeManager.Instance.GetServerTime()
        };

        Client.TCP.SendPacket2(E_PACKET.MONSTER_STATE_NTF, pkt);
    }

    private void SetControllerActive(bool isActive)
    {
        if (controller != null) controller.enabled = isActive;
    }


}