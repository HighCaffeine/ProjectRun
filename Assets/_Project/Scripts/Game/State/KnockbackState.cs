using UnityEngine;
using Unity.Cinemachine;

public class KnockbackState : IState
{
    private PlayerActor actor;
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

    public KnockbackState(PlayerActor actor, Vector3 dir, float power, bool isPull, Vector3 casterPos)
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

        actor.SendStateChange(eState.Knockback, knockbackDir, initialPower);
        actor.StartCoroutine(actor.HitStopRoutine());
        actor.SetVerticalVelocity(3.0f);

        eState actionType = isPull ? eState.Pull : eState.Push;
        actor.animator.SetTrigger("Knockback");
        actor.PlayTravelSpark(actionType);
    }

    public void Execute()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / DURATION);

        float logValue = Mathf.Log(1 + K * t) / logDivisor;
        float smooth = isPull ? (1f - logValue) * (1f - logValue) : logValue;

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

        int hitCount = Physics.OverlapSphereNonAlloc(actor.transform.position, 0.6f, wallHitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            if (wallHitBuffer[i].CompareTag("Breakable"))
            {
                wallHitBuffer[i].gameObject.SetActive(false);
                return;
            }
        }

        if (timer >= DURATION)
        {
            actor.PlayBrakeParticles();
            actor.sm.ChangeState(new IdleState(actor));
        }
    }

    public void Exit()
    {
        if (actor.trailRenderer != null) actor.trailRenderer.emitting = false;
        actor.StopTravelSpark();
        actor.SendMovePacket(0f, 0f);
    }
}