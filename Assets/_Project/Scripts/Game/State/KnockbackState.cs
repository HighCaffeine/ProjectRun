using UnityEngine;


public class KnockbackState : IState
{
    private Actor actor;
    private Vector3 knockbackDir;
    private bool isPull;
    private Vector3 casterPos;
    private float latencyOffset = 0f;

    private float initialPower;
    private float timer;
    private const float DURATION = 0.25f;
    private const float STOP_DISTANCE = 0.5f * 0.5f;

    private const float K = 10f;
    private float logDivisor;
    private Vector3 startPos;
    private Vector3 targetPos;

    private long attackerID;
    private static Collider[] wallHitBuffer = new Collider[5];

    public KnockbackState(Actor actor, Vector3 dir, float power, bool isPull, Vector3 casterPos, float latency = 0f, long attackerID = -1)
    {
        this.actor = actor;
        this.knockbackDir = dir.normalized;
        this.initialPower = power;
        this.isPull = isPull;
        this.casterPos = casterPos;
        this.latencyOffset = latency;
        this.attackerID = attackerID;
    }

    public void Enter()
    {
        ResetTimer();
        CacheStartPosition();
        CalculateTargetPosition();
        CalculateLogDivisor();
        SendKnockbackState();
        PlayKnockbackAnimation();
        ApplyActorSpecificEnterLogic();
    }

    public void Execute()
    {
        UpdateTimer();
        UpdateKnockbackMovement();
        CheckWallCollision(); // 원본의 상세한 벽 충돌 처리 유지
        TryFinishKnockback();
    }

    public void Exit()
    {
        ApplyActorSpecificExitLogic();
        SendStopMovePacket();
    }

    private void ResetTimer()
    {
        // 원본의 레이턴시 오프셋 적용
        timer = latencyOffset;
    }

    private void CacheStartPosition()
    {
        startPos = actor.transform.position;
    }

    private void CalculateTargetPosition()
    {
        float distance = initialPower;
        if (isPull)
        {
            Vector3 dir = (casterPos - actor.transform.position).normalized;
            targetPos = actor.transform.position + dir * distance;
        }
        else
        {
            targetPos = actor.transform.position + knockbackDir * distance;
        }
    }

    private void CalculateLogDivisor()
    {
        logDivisor = Mathf.Log(1 + K);
    }

    private void SendKnockbackState()
    {
        actor.SendStateChange(eState.Knockback, knockbackDir, initialPower, targetUUID: attackerID, isPull: isPull, casterPos: casterPos);
        
    }

    private void PlayKnockbackAnimation()
    {
        actor.animator.SetTrigger("Knockback");
    }

    private void ApplyActorSpecificEnterLogic()
    {
        // 원본의 플레이어 및 몬스터 분기 처리 상세 유지
        if (actor is PlayerActor pActor)
        {
            pActor.lastAttackerID = attackerID;

            pActor.StartCoroutine(pActor.HitStopRoutine());
            pActor.SetVerticalVelocity(3.0f);

            eState actionType = isPull ? eState.Pull : eState.Push;
            pActor.PlayTravelSpark(actionType);
            pActor.ignoreServerPosTimer = DURATION + 0.5f;
        }
        else if (actor is MonsterActor monster)
        {
            monster.monsterState = MonsterState.Knockback;
        }
    }

    private void UpdateTimer()
    {
        timer += Time.deltaTime;
    }

    private void UpdateKnockbackMovement()
    {
        if (!actor.IsLocal) return;

        MoveActorByKnockback();
        TryStopPullNearCaster();
        UpdatePullDirection();
        TrySendMovePacket();
    }

    private void MoveActorByKnockback()
    {
        float smooth = GetSmoothProgress();
        Vector3 nextPos = Vector3.Lerp(startPos, targetPos, smooth);
        Vector3 moveDelta = nextPos - actor.transform.position;

        actor.Move(moveDelta.normalized, moveDelta.magnitude / Time.deltaTime);
    }

    private float GetSmoothProgress()
    {
        float t = timer / DURATION;
        float logValue = Mathf.Log(1f + K * t) / logDivisor;

        return isPull ? 1f - ((1f - logValue) * (1f - logValue)) : logValue;
    }

    private void TryStopPullNearCaster()
    {
        if (!isPull) return;

        float dx = casterPos.x - actor.transform.position.x;
        float dz = casterPos.z - actor.transform.position.z;
        float sqrDist = (dx * dx) + (dz * dz);

        if (sqrDist <= STOP_DISTANCE)
        {
            timer = DURATION;
        }
    }

    private void UpdatePullDirection()
    {
        if (!isPull) return;

        Vector3 dir = casterPos - actor.transform.position;
        dir.y = 0f;
        knockbackDir = dir.normalized;
    }

    private void TrySendMovePacket()
    {
        actor.sendTimer += Time.deltaTime;
        if (actor.sendTimer >= Actor.sendInterval)
        {
            actor.SendMovePacket(0f, 0f);
            actor.sendTimer = 0f;
        }
    }

    #region Wall Collision Logic (Original)
    private void CheckWallCollision()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(actor.transform.position, 0.6f, wallHitBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            if (wallHitBuffer[i] == null) continue;

            if (wallHitBuffer[i].CompareTag("Breakable"))
            {
                if (actor.IsLocal)
                {
                    ProcessBreakableWallHit(wallHitBuffer[i]);
                }
            }
        }
    }

    private void ProcessBreakableWallHit(Collider hitCollider)
    {
        BaseGimmick gimmick = hitCollider.GetComponentInParent<BaseGimmick>();

        if (gimmick == null)
        {
            // 레거시 GimmickTrigger 처리 지원
            GimmickTrigger gimmickTrigger = hitCollider.GetComponent<GimmickTrigger>();
            if (gimmickTrigger != null)
            {
                gimmickTrigger.ProcessInteract(actor.gameObject);
                hitCollider.gameObject.SetActive(false);
            }
            return;
        }

        // BreakableWall만 처리
        if (gimmick.gimmickType == eGimmickType.Breakable)
        {
            BreakableWall breakableWall = gimmick as BreakableWall;
            if (breakableWall != null)
            {
                SendBreakableWallHitPacket(breakableWall);
                hitCollider.gameObject.SetActive(false);
            }
        }
    }

    private void SendBreakableWallHitPacket(BreakableWall wall)
    {
        if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
        {
            P_GimmickInteractReq req = new P_GimmickInteractReq
            {
                activeUUID = LocalPlayerInfo.ID,
                gimmickID = wall.gimmickUID,
                gimmickKey = (byte)wall.gimmickType,
                state = (byte)eGimmickState.Push,
                targetPos = new P_PacketVector3
                {
                    x = actor.transform.position.x,
                    y = actor.transform.position.y,
                    z = actor.transform.position.z
                },
                param = 1f,
                timestamp = NetworkTimeManager.Instance.GetServerTime()
            };

            Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
            Debug.Log($"<color=yellow>[KnockbackState]</color> BreakableWall 충돌 패킷 전송 - GimmickID: {wall.gimmickUID}");
        }
        else
        {
            FractureObject fracture = wall.GetComponent<FractureObject>();
            if (fracture != null)
            {
                Vector3 hitDir = knockbackDir;
                hitDir.y = 0f;
                fracture.BreakToDirection(hitDir.normalized);
            }
        }
    }
    #endregion

    private void TryFinishKnockback()
    {
        if (timer >= DURATION)
        {
            if (actor is PlayerActor pActor) pActor.PlayBrakeParticles();
            actor.sm.ChangeState(new IdleState(actor));
        }
    }

    private void ApplyActorSpecificExitLogic()
    {
        if (actor is PlayerActor pActor)
        {
            if (pActor.trailRenderer != null) pActor.trailRenderer.emitting = false;
            pActor.StopTravelSpark();
        }
        else if (actor is MonsterActor monster)
        {
            // KnockbackState2에서 추가되었던 몬스터 상태 복구 요소 적용
            if (monster.monsterState == MonsterState.Knockback)
            {
                monster.monsterState = MonsterState.Normal;
            }
        }
    }

    private void SendStopMovePacket()
    {
        actor.SendMovePacket(0f, 0f);
    }
}