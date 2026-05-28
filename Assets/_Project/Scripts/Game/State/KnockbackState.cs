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
        ResetTimer();
        CacheStartPosition();
        CalculateTargetPosition();
        CalculateLogDivisor();
        HandleMonsterKnockbackEnter();
        SendKnockbackState();
        PlayKnockbackAnimation();
        PlayKnockbackEffects();
    }

    public void Execute()
    {
        UpdateTimer();
        UpdateKnockbackMovement();
        CheckBreakableWallCollision();
        TryFinishKnockback();
    }

    public void Exit()
    {
        HandleMonsterKnockbackExit();   

        StopKnockbackEffects();
        SendStopMovePacket();
    }

    private void ResetTimer()
    {
        timer = 0f;
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
            return;
        }

        targetPos = actor.transform.position + knockbackDir * distance;
    }

    private void CalculateLogDivisor()
    {
        logDivisor = Mathf.Log(1 + K);
    }

    private void SendKnockbackState()
    {
        actor.SendStateChange(eState.Knockback, knockbackDir, initialPower, isPull: isPull, casterPos: casterPos);
    }

    private void PlayKnockbackAnimation()
    {
        actor.animator.SetTrigger("Knockback");
    }

    private void PlayKnockbackEffects()
    {
        eState actionType = isPull ? eState.Pull : eState.Push;
        actor.OnKnockbackStateEnter(actionType, DURATION);
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
        if (actor.sendTimer < Actor.sendInterval) return;

        actor.SendMovePacket(0f, 0f);
        actor.sendTimer = 0f;
    }

    private void CheckBreakableWallCollision()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(actor.transform.position, 0.6f, wallHitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            TryBreakWall(wallHitBuffer[i]);
        }
    }

    private void TryBreakWall(Collider wallHit)
    {
        var baseGimmick = wallHit.GetComponent<BaseGimmick>();
        if (wallHit.CompareTag("Breakable") && baseGimmick is BreakableWall)
        {
            ProcessWallInteract(wallHit);
            wallHit.gameObject.SetActive(false);
        }
    }

    private void ProcessWallInteract(Collider wallHit)
    {
        if (!actor.IsLocal) return;

        GimmickTrigger gimmick = wallHit.GetComponent<GimmickTrigger>();
        if (gimmick != null)
        {
            gimmick.ProcessInteract(actor.gameObject);
        }
    }

    private void TryFinishKnockback()
    {
        if (timer < DURATION) return;

        actor.OnKnockbackStateComplete();
        actor.sm.ChangeState(new IdleState(actor));
    }

    private void StopKnockbackEffects()
    {
        actor.OnKnockbackStateExit();
    }

    private void SendStopMovePacket()
    {
        actor.SendMovePacket(0f, 0f);
    }

    private void HandleMonsterKnockbackEnter()
    {
        if (actor is Monster monster)
        {
            monster.monsterState = MonsterState.Knockback;
        }
    }

    private void HandleMonsterKnockbackExit()
    {
        if (actor is Monster monster)
        {
            // 벽에 안 박고 끝난 경우만 복귀
            if (monster.monsterState == MonsterState.Knockback)
            {
                monster.monsterState = MonsterState.Normal;
            }
        }
    }
}
