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
    private Quaternion lastSentRot;
    private Vector3 estimatedPos;

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

    // ★ 구버전(new)에 있던 공격 쿨다운 변수 복구
    [SerializeField]
    private float attackCooldown = 1f;
    private float lastAttackTime;

    // ────────────────────────────────────────────────
    // 이동 방향 및 회전
    // ────────────────────────────────────────────────
    public override Vector3 GetMovementDirection()
    {
        if (monsterState == MonsterState.Stunned) return Vector3.zero;
        if (currentTarget == null) return Vector3.zero;

        Vector3 dir = currentTarget.transform.position - transform.position;
        dir.y = 0f;

        // 이동 중 타겟 방향으로 부드럽게 회전
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

        // 몬스터는 중력 없이 horizontalMove만 적용 (높이는 UpdateHeight에서 조절)
        Vector3 moveDelta = horizontalMove * Time.deltaTime;
        controller.Move(moveDelta);
        horizontalMove = Vector3.zero;
    }

    public override bool HasMoveIntent()
    {
        if (monsterState != MonsterState.Normal) return false;
        if (currentTarget == null) return false;

        Vector3 myPos = transform.position;
        Vector3 targetPos = currentTarget.transform.position;

        myPos.y = 0;
        targetPos.y = 0;

        return Vector3.Distance(myPos, targetPos) > 2f;
    }

    public override bool CheckActionIntent()
    {
        if (currentTarget == null) return false;

        Vector3 myPos = transform.position;
        Vector3 targetPos = currentTarget.transform.position;

        myPos.y = 0;
        targetPos.y = 0;

        return Vector3.Distance(myPos, targetPos) <= 2f;
    }

    // ────────────────────────────────────────────────
    // 초기화
    // ────────────────────────────────────────────────
    protected new void Start()
    {
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
            meshRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); // 모델 회전값 보정
        }
    }

    // ────────────────────────────────────────────────
    // 메인 업데이트
    // ────────────────────────────────────────────────
    private void Update()
    {
        if (monsterState == MonsterState.Dead) return;

        if (IsLocal)
        {
            UpdateTarget();

            if (currentTarget != null)
            {
                UpdateHeight(); 
            }

            sm.Update();       
            ApplyMovement();   

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
        float nextY = Mathf.MoveTowards(transform.position.y, targetY, 2f * Time.deltaTime);
        float deltaY = nextY - transform.position.y;
        
        controller.Move(Vector3.up * deltaY);
    }

    private void UpdateTarget()
    {
        if (currentTarget == null || currentTarget.isDead)
        {
            ChooseTarget();
        }
    }

    // ────────────────────────────────────────────────
    // 공격 / 타겟팅 로직 (쿨다운 복구 완료)
    // ────────────────────────────────────────────────
    private void TryChangeToActionState()
    {
        if (!CanStartAction()) return;
        if (!CheckActionIntent()) return;

        // ★ 쿨다운 갱신
        lastAttackTime = Time.time;
        sm.ChangeState(new ActionState(this, eState.Push, currentTarget));
    }

    private bool CanStartAction()
    {
        // ★ 쿨다운 체크 복구 + 액션 중복 실행 방지
        if (Time.time - lastAttackTime < attackCooldown) return false;
        return sm.currentState is IdleState || sm.currentState is MoveState;
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

            PlayerActor actor = player.GetComponent<PlayerActor>();
            if (actor == null || actor.isDead || !actor.gameObject.activeInHierarchy) continue;

            float dist = Vector3.Distance(transform.position, actor.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = actor;
            }
        }

        return closest;
    }

    private void ChooseTarget()
    {
        if (currentTarget != null && !currentTarget.isDead) return;
        currentTarget = GetClosestPlayerTarget();
    }

    // ────────────────────────────────────────────────
    // 사망 및 스턴 (애니메이션 트리거 복구 완료)
    // ────────────────────────────────────────────────
    public void RequestMonsterDead()
    {
        if (!IsLocal) return;

        P_MonsterDeadReq pkt = new P_MonsterDeadReq { monsterID = this.monsterID };
        Client.TCP.SendPacket2(E_PACKET.MONSTER_DEAD_REQ, pkt);
    }

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
        
        // ★ 스턴 애니메이션 트리거 복구
        if (animator != null) animator.SetTrigger("Stunned");

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
            // ★ 회복 애니메이션 트리거 복구
            if (animator != null) animator.SetTrigger("Recovery"); 
        }

        stunCoroutine = null;
    }

    // ★ 유니티 애니메이션 이벤트 콜백 복구 (공격 모션 시 타격 판정 실행)
    public void OnAttackHit()
    {
        if (sm.currentState is ActionState actionState)
        {
            actionState.OnAttackHit();
        }
    }

    private void UpdateMaterialByState()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        
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

    // ────────────────────────────────────────────────
    // 네트워크 동기화 (추측 항법 및 타임스탬프 적용 완료)
    // ────────────────────────────────────────────────
    public void OnSyncMovement(Vector3 targetPos, Quaternion targetRot, float latency)
    {
        Vector3 moveDelta = targetPos - serverPos;

        serverPos = targetPos;
        serverRot = targetRot;

        // 추측 항법(Dead Reckoning) 적용
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