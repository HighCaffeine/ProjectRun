using UnityEngine;

public class KnockbackState : IState
{
    private Actor actor;
    private Vector3 knockbackDir;

    private float initialPower;
    private float timer;
    private const float DURATION = 0.25f;

    private bool isPull;
    private Vector3 casterPos;
    private const float STOP_DISTANCE = 0.5f * 0.5f;

    private const float K = 10f;
    private float logDivisor;

    private Vector3 startPos;
    private Vector3 targetPos;

    private static Collider[] wallHitBuffer = new Collider[5];

    public KnockbackState(Actor actor, Vector3 dir, float power, bool isPull, Vector3 casterPos)
    {
        this.actor = actor;
        this.knockbackDir = dir.normalized;
        this.initialPower = power;
        this.isPull = isPull;
        this.casterPos = casterPos;
    }

    public void Enter()
    {
        timer = 0f;
        startPos = actor.transform.position;

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

        logDivisor = Mathf.Log(1 + K);

        actor.SendStateChange(eState.Knockback, knockbackDir, initialPower, targetUUID: 0, isPull: isPull, casterPos: casterPos);
        actor.animator.SetTrigger("Knockback");

        // 플레이어 및 몬스터 분기 처리
        if (actor is PlayerActor pActor)
        {
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

    public void Execute()
    {
        timer += Time.deltaTime;
        float t = timer / DURATION;
        float logValue = Mathf.Log(1f + K * t) / logDivisor;

        float smooth = isPull ? 1f - ((1f - logValue) * (1f - logValue)) : logValue;

        if (actor.IsLocal)
        {
            Vector3 nextPos = Vector3.Lerp(startPos, targetPos, smooth);
            Vector3 moveDelta = nextPos - actor.transform.position;
            actor.Move(moveDelta.normalized, moveDelta.magnitude / Time.deltaTime);

            if (isPull)
            {
                float dx = casterPos.x - actor.transform.position.x;
                float dz = casterPos.z - actor.transform.position.z;
                float sqrDist = (dx * dx) + (dz * dz);

                if (sqrDist <= STOP_DISTANCE)
                {
                    timer = DURATION;
                }

                Vector3 dir = casterPos - actor.transform.position;
                dir.y = 0f;
                knockbackDir = dir.normalized;
            }

            actor.sendTimer += Time.deltaTime;
            if (actor.sendTimer >= Actor.sendInterval)
            {
                actor.SendMovePacket(0f, 0f);
                actor.sendTimer = 0f;
            }
        }

        // 벽 충돌 체크 (BreakableWall 연동)
        CheckWallCollision();

        if (timer >= DURATION)
        {
            if (actor is PlayerActor pActor) pActor.PlayBrakeParticles();
            actor.sm.ChangeState(new IdleState(actor));
        }
    }

    private void CheckWallCollision()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(actor.transform.position, 0.6f, wallHitBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            if (wallHitBuffer[i] == null) continue;

            // Breakable 태그 확인
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
                param = 1f
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

    public void Exit()
    {
        if (actor is PlayerActor pActor)
        {
            if (pActor.trailRenderer != null) pActor.trailRenderer.emitting = false;
            pActor.StopTravelSpark();
        }
        actor.SendMovePacket(0f, 0f);
    }
}