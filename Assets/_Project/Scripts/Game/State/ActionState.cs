using UnityEngine;

public class ActionState : IState
{
    private Actor actor;
    private eState actionType;
    private float timer;

    private const float CAST_TIME_PLAYER = 0.6f; 
    private const float CAST_TIME_MONSTER = 0.7f;
    private const float ATTACK_TIME = 0.05f;

    private const float PLAYER_EFFECT_TIME = 0.15f; 

    private float pushForce = 100f;
    private float pow = 2f;

    private Actor targetActor;
    private BaseGimmick targetGimmick;

    private bool attackStarted = false;
    private bool hitProcessed = false;
    private bool effectProcessed = false; 

    public ActionState(Actor actor, eState type, Actor targetActor = null, BaseGimmick targetGimmick = null)
    {
        this.actor = actor;
        this.actionType = type;
        this.targetActor = targetActor;
        this.targetGimmick = targetGimmick;

        pushForce = (actionType == eState.Push) ? (5f * pow) : (3f * pow);
    }

    // ──────────────────────────────────────────────
    // IState
    // ──────────────────────────────────────────────

    public void Enter()
    {
        ResetTimer();
        TrySendActionState();
        MarkSkillUseTime();

        if (actor is PlayerActor player)
        {
            if (actionType == eState.Push)
            {
                if (player.audioSource != null && player.pushSound != null)
                {
                    Debug.Log(player.audioSource);
                    Debug.Log(player.pushSound);
                    Debug.Log(player.audioSource.volume);
                    player.audioSource.PlayOneShot(player.pushSound);
                    Debug.Log($"Played push sound for {player.name}");
                }
            }
            else if (actionType == eState.Pull)
            {
                if (player.audioSource != null && player.pullSound != null)
                {
                    Debug.Log(player.audioSource);
                    Debug.Log(player.pullSound);
                    Debug.Log(player.audioSource.volume);
                    player.audioSource.PlayOneShot(player.pullSound);
                    Debug.Log($"Played pull sound for {player.name}");
                }
            }

            PlayActionAnimation();
        }
    }

    public void Execute()
    {
        UpdateTimer();

        if (!effectProcessed && timer >= PLAYER_EFFECT_TIME && actor is PlayerActor)
        {
            effectProcessed = true;
            ProcessTarget();
        }

        if (!attackStarted && timer >= ATTACK_TIME && actor is MonsterActor)
        {
            attackStarted = true;
            PlayActionAnimation();
        }

        TryReturnToIdle();
    }
    public void Exit()
    {
        StopActionEffects();
    }

    // ──────────────────────────────────────────────
    // Enter 헬퍼
    // ──────────────────────────────────────────────

    private void ResetTimer() => timer = 0f;

    private void PlayActionAnimation()
        => actor.animator.SetTrigger(GetActionTriggerName());

    private string GetActionTriggerName()
        => (actionType == eState.Push) ? "Push" : "Pull";

    private void TrySendActionState()
    {
        if (!actor.IsLocal) return;
        if (GameManager.Instance.currentMode != GameManager.PlayMode.Server_Online) return;

        actor.SendStateChange(actionType);
    }

    private void MarkSkillUseTime()
    {
        if (!actor.IsLocal) return;

        // 서버 시간 기준으로 마지막 스킬 사용 시간 기록
        actor.lastSkillUseTime = NetworkTimeManager.Instance.GetServerTime();
    }

   /* private void PlayActionEffects()
    {
        if (!actor.IsLocal) return;

        if (actor is PlayerActor pActor)
        {
            if (actionType == eState.Push)
            {
                pActor.PushParticle();
                pActor.PushIndicator.gameObject.SetActive(true);
            }
            else
            {
                pActor.PullIndicator.gameObject.SetActive(true);
            }
        }
    }*/

    // ──────────────────────────────────────────────
    // 타겟 처리
    // ──────────────────────────────────────────────

    private void ProcessTarget()
    {
        // 로컬 액터만 타겟 처리 (리모트는 서버 브로드캐스트로 처리됨)
        if (!actor.IsLocal) return;

        if (targetActor != null)
        {
            ProcessActorTarget();
            return;
        }

        if (targetGimmick != null)
        {
            ProcessGimmickTarget();
        }
    }

    // ── Actor 타겟 ──

    private void ProcessActorTarget()
    {
        if (actor is PlayerActor hitPlayer)
        {
            if (hitPlayer.audioSource != null && hitPlayer.hitSound != null)
            {
                Debug.Log(hitPlayer.audioSource);
                Debug.Log(hitPlayer.hitSound);
                Debug.Log(hitPlayer.audioSource.volume);
                hitPlayer.audioSource.PlayOneShot(hitPlayer.hitSound);
                Debug.Log($"Played hit sound for {hitPlayer.name}");
            }
        }
        Vector3 dirToTarget = targetActor.transform.position - actor.transform.position;
        dirToTarget.y = 0f;
        float minDistance = dirToTarget.magnitude;

        float finalDistance = (actionType == eState.Push)
            ? pushForce
            : Mathf.Max(0f, minDistance - 1.5f);

     Vector3 knockbackDir;

        if (actionType == eState.Push)
        {
            knockbackDir = actor.GetPushDir();
        }
        else
        {
            knockbackDir = -dirToTarget.normalized;
        }
        if (actionType == eState.Pull)
        {
            if (targetActor is MonsterActor monster)
            {
                if (monster.monsterState == MonsterState.Stunned)
                    monster.RequestMonsterDead();
                return;
            }

            knockbackDir = -knockbackDir;
        }

        finalDistance *= actor.PushMulti;
        if (actor is PlayerActor player)
        {
            if (actionType == eState.Push)
                player.pushCount++;
            else if (actionType == eState.Pull)
                player.pullCount++;
        }

        if (ShouldSendActorTargetPacket(out Player targetPlayer))
        {
            actor.SendStateChange(
                eState.Knockback,
                knockbackDir,
                finalDistance,
                targetPlayer.ID,
                actionType == eState.Pull,
                actor.transform.position);
            return;
        }

        targetActor.sm.ChangeState(new KnockbackState(
            targetActor, knockbackDir, finalDistance,
            actionType == eState.Pull, actor.transform.position));
    }

    private bool ShouldSendActorTargetPacket(out Player targetPlayer)
    {
        targetPlayer = targetActor.GetComponent<Player>();

        return actor is PlayerActor
            && targetPlayer != null
            && GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online;
    }

    // ── Gimmick 타겟 ──

    private void ProcessGimmickTarget()
    {
        Vector3 pushDir = actor.GetPushDir();

        Vector3 destPos;
        float moveDist;

        if (actionType == eState.Pull)
        {
            Vector3 pullDestPos = actor.transform.position + pushDir * 1.5f;
            pullDestPos.y = targetGimmick.transform.position.y;

            moveDist = Vector3.Distance(targetGimmick.transform.position, pullDestPos);

            destPos = (targetGimmick.gimmickType == eGimmickType.Movable)
                ? GetSafePushDestination(targetGimmick.transform.position, targetGimmick.TargetTransform.localScale, -pushDir, moveDist)
                : pullDestPos;
        }
        else // Push
        {
            moveDist = 3f;

            destPos = (targetGimmick.gimmickType == eGimmickType.Movable)
                ? GetSafePushDestination(targetGimmick.transform.position, targetGimmick.TargetTransform.localScale, pushDir, moveDist)
                : targetGimmick.transform.position + pushDir * moveDist;
        }

        destPos.y = targetGimmick.transform.position.y;

        if (targetGimmick.gimmickType == eGimmickType.Breakable)
        {
            if (!CanInteractWithBreakable()) return;
        }

        if (ShouldSendGimmickPacket())
        {
            SendGimmickInteractPacket(pushDir, moveDist, destPos);
            return;
        }

        ApplyGimmickAction(destPos);
    }

    private Vector3 GetPushDirection()
    {
        Vector3 dir;

        if (actor is PlayerActor pActor)
            dir = pActor.Is2p ? pActor.GetForward() : pActor.GetActionDir();
        else
            dir = actor.transform.forward;

        dir.y = 0f;
        dir.Normalize();
        return dir;
    }

    private bool CanInteractWithBreakable()
    {
        BreakableObj breakable = targetGimmick as BreakableObj;
        if (breakable == null) return true;

        return breakable.interactMode == BreakableObj.InteractMode.All
            || (breakable.interactMode == BreakableObj.InteractMode.Push && actionType == eState.Push)
            || (breakable.interactMode == BreakableObj.InteractMode.Pull && actionType == eState.Pull);
    }

    private bool ShouldSendGimmickPacket()
        => actor is PlayerActor
            && GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online;

    private void SendGimmickInteractPacket(Vector3 pushDir, float moveDist, Vector3 destPos)
    {
        byte stateToSend;
        P_PacketVector3 packetData;

        if (targetGimmick.gimmickType == eGimmickType.Movable)
        {
            stateToSend = 5;
            Vector3 sendDir = (actionType == eState.Pull) ? -pushDir : pushDir;
            float scaledForce = (moveDist / 3.0f) * pushForce;
            Vector3 forceVec = sendDir * scaledForce;
            packetData = new P_PacketVector3 { x = forceVec.x, y = forceVec.y, z = forceVec.z };
        }
        else
        {
            stateToSend = (byte)eGimmickState.Push;
            packetData = new P_PacketVector3 { x = destPos.x, y = destPos.y, z = destPos.z };
        }

        P_GimmickInteractReq req = new P_GimmickInteractReq
        {
            activeUUID = LocalPlayerInfo.ID,
            gimmickID = targetGimmick.gimmickUID,
            gimmickKey = (byte)targetGimmick.gimmickType,
            state = stateToSend,
            targetPos = packetData,
            param = pushForce,
            timestamp = NetworkTimeManager.Instance.GetServerTime()
        };

        Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
    }

    private void ApplyGimmickAction(Vector3 destPos)
    {
        if (targetGimmick.gimmickType == eGimmickType.Movable)
        {
            ((MovableGimmick)targetGimmick).StartMove(destPos);
            return;
        }

        if (targetGimmick.gimmickType == eGimmickType.Breakable)
        {
            BreakTargetGimmick();
        }
    }

    private void BreakTargetGimmick()
    {
        FractureObject fracture = targetGimmick.GetComponent<FractureObject>();
        if (fracture != null)
        {
            BreakFractureObject(fracture);
            return;
        }

        targetGimmick.stat?.TakeDamage(1, eDamageType.PushPull);
    }

    private void BreakFractureObject(FractureObject fracture)
    {
        Vector3 forward = actor.GetForward();
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = actor.transform.forward;
            forward.y = 0f;
        }

        Vector3 leftDir = Vector3.Cross(forward.normalized, Vector3.up).normalized;
        fracture.BreakToDirection(leftDir);
    }

    private Vector3 GetSafePushDestination(Vector3 startPos, Vector3 targetExtents, Vector3 pushDir, float pushDistance)
    {
        int wallLayer = LayerMask.GetMask("Wall");

        if (Physics.BoxCast(startPos, targetExtents, pushDir, out RaycastHit hit, Quaternion.identity, pushDistance, wallLayer))
        {
            float safeDistance = Mathf.Max(0f, hit.distance - 0.05f);
            return startPos + pushDir * safeDistance;
        }

        return startPos + pushDir * pushDistance;
    } 

    // ──────────────────────────────────────────────
    // Execute 헬퍼
    // ──────────────────────────────────────────────

    private void UpdateTimer() => timer += Time.deltaTime;

    private void TryReturnToIdle()
    {
        if (actor is MonsterActor)
        {
            // ★ 애니메이션 이벤트 대신 타이머로 직접 히트 처리
            if (!hitProcessed && timer >= CAST_TIME_MONSTER)
            {
                hitProcessed = true;
                ProcessTarget(); // 직접 호출
            }

            if (hitProcessed && timer >= CAST_TIME_MONSTER + 0.3f)
            {
                actor.sm.ChangeState(new IdleState(actor));
            }
            return;
        }

        float castTime = CAST_TIME_PLAYER;
        if (timer < castTime) return;
        actor.sm.ChangeState(new IdleState(actor));
    }

    // ──────────────────────────────────────────────
    // Exit 헬퍼
    // ──────────────────────────────────────────────

    private void StopActionEffects()
    {
        if (actor is PlayerActor pActor)
        {
            pActor.InvokeSSaGay();
        }
    }

    // ──────────────────────────────────────────────
    // 외부 콜백 (Monster 애니메이션 이벤트 등)
    // ──────────────────────────────────────────────

    public void OnAttackHit()
    {
        if (hitProcessed) return;
      
        hitProcessed = true;
        ProcessTarget();
    }
}