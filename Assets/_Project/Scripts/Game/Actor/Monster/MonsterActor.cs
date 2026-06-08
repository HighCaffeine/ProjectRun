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

    public override Vector3 GetMovementDirection()
    {
        if (monsterState == MonsterState.Stunned) return Vector3.zero;
        if (currentTarget == null) return Vector3.zero;

        Vector3 dir = currentTarget.transform.position - transform.position;
        dir.y = 0f;

        return dir.normalized;
    }

    public override bool HasMoveIntent()
    {
        if (monsterState != MonsterState.Normal) return false;
        if (currentTarget == null) return false;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        return distance > 2f;
    }

    public override bool CheckActionIntent()
    {
        if (currentTarget == null) return false;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
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
    }

private void Update()
{
    if (monsterState == MonsterState.Dead) return;

        if (IsLocal)
        {
            Debug.Log("몬스터는 이즈로컬임");
            UpdateTarget();

            const float HEIGHT_EPSILON = 0.05f;

            // 1. 높이 조절은 독립적으로 실행
            if (currentTarget != null && Mathf.Abs(transform.position.y - (currentTarget.transform.position.y + heightOffset)) > HEIGHT_EPSILON)
            {
                UpdateHeight();
            }

            // ★ 2. else를 지우고 무조건 상태머신과 이동을 실행하게 수정!
            sm.Update();
            ApplyMovement();

            if (!(sm.currentState is KnockbackState))
            {
                TryChangeToActionState();
            }

            HandleNetworkSync();
        }
        else
        {
            sm.Update(); // 게스트 애니메이션 업데이트
            ProcessRemoteMovement(); // 게스트 위치 동기화
        }

    UpdateMaterialByState();
}
    private void UpdateHeight()
    {
        if (currentTarget == null || controller == null) return;

        float targetY = currentTarget.transform.position.y + heightOffset;
        float nextY = Mathf.MoveTowards(transform.position.y, targetY, 2f * Time.deltaTime);
        float deltaY = nextY - transform.position.y;

        controller.Move(Vector3.up * deltaY);
    }

    private void UpdateTarget()
    {
        if (currentTarget == null || currentTarget.isDead)
        {
            Debug.Log("UpdateTarget들어옴");
            ChooseTarget();
        }
    }

    private void TryChangeToActionState()
    {
        if (!CanStartAction()) return;
        if (!CheckActionIntent()) return;

        sm.ChangeState(new ActionState(this, eState.Push, currentTarget));
    }

    private bool CanStartAction()
    {
        return sm.currentState is IdleState || sm.currentState is MoveState;
    }

    public PlayerActor GetClosestPlayerTarget()
    {
        if (Match.Instance == null || Match.Instance.Players == null) return null;
        Debug.Log("GetClosestPlayerTarget들어옴");
        PlayerActor closest = null;
        float minDistance = float.MaxValue;

        foreach (Player player in Match.Instance.Players.Values)
        {
            Debug.Log("플레이어 탐색중: " + player.Name);
            if (player == null) continue;

            PlayerActor actor = player.GetComponent<PlayerActor>();
            if (actor == null || actor.isDead || !actor.gameObject.activeInHierarchy) continue;

            float dist = Vector3.Distance(transform.position, actor.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = actor;
            }
            Debug.Log(closest.name);
            closest = ActorManager.Instance.p1; // ★ 임시로 p1을 타겟으로 고정 (테스트용)
        }

        return closest;
    }

    private void ChooseTarget()
    {
        if (currentTarget != null && !currentTarget.isDead) return;

        // currentTarget = GetRandomPlayerTarget(); <-- 기존 로직 삭제
        currentTarget = GetClosestPlayerTarget(); // ★ 가장 가까운 타겟으로 변경
        Debug.Log("호출했니");
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
            return;
        }

        transform.position = Vector3.Lerp(transform.position, serverPos, Time.deltaTime * lerpSpeed);
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
            currentRot = new P_PacketQuaternion { x = transform.rotation.x, y = transform.rotation.y, z = transform.rotation.z, w = transform.rotation.w }
        };

        Client.UDP.SendPacket2(E_PACKET.MONSTER_MOVEMENT, pkt);
        //Client.TCP.SendPacket2(E_PACKET.MONSTER_MOVEMENT, pkt);
    }

    public override void SendStateChange(eState stateCode, Vector3 dir = default, float param = 0f, long targetUUID = 0, bool isPull = false, Vector3 casterPos = default)
    {
        if (!IsLocal) return;

        P_MonsterStateNtf pkt = new P_MonsterStateNtf
        {
            monsterID = this.monsterID,
            newState = (byte)stateCode,
            targetDir = new P_PacketVector3 { x = dir.x, y = dir.y, z = dir.z },
            param = param,
            isPull = isPull ? (byte)1 : (byte)0,
            casterPos = new P_PacketVector3 { x = casterPos.x, y = casterPos.y, z = casterPos.z }
        };

        Client.TCP.SendPacket2(E_PACKET.MONSTER_STATE_NTF, pkt);
    }

    private void SetControllerActive(bool isActive)
    {
        if (controller != null) controller.enabled = isActive;
    }
}