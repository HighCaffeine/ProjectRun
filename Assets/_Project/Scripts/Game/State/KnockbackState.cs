using UnityEngine;
using Unity.Cinemachine;

public class KnockbackState : IState
{
    private PlayerActor actor;
    private Vector3 knockbackDir;

    private float initialPower; // 처음 힘
    private float currentPower; // 선형 감쇠 적용된 현재 힘

    private float timer;
    private const float DURATION = 0.25f; // 날아가는 시간

    private bool isPull;
    private Vector3 casterPos;
    private const float STOP_DISTANCE = 0.5f;

    private const float K = 10f;

    public KnockbackState(PlayerActor actor, Vector3 dir, float power, bool isPull, Vector3 casterPos)
    {
        this.actor = actor;
        this.knockbackDir = dir.normalized;
        this.initialPower = power;
        this.currentPower = power;
        this.isPull = isPull;
        this.casterPos = casterPos;
    }

    public void Enter()
    {
        timer = 0f;

        actor.StartCoroutine(actor.HitStopRoutine());

        // actor.SetAni(AniState.Hit); // 피격 애니메이션 재생
        var impulseSource = actor.GetComponent<CinemachineImpulseSource>();
        if (impulseSource != null) impulseSource.GenerateImpulse(knockbackDir * 2.0f);

        // 연출
        if (actor.trailRenderer != null) actor.trailRenderer.emitting = true;
        if (actor.travelSparkParticle != null) actor.travelSparkParticle.Play();
    }

    public void Execute()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (actor.brakeParticle != null)
            {
                foreach (var p in actor.brakeParticle)
                {
                    p?.Play();
                }
            }
            actor.sm.ChangeState(new IdleState(actor));
            return;
        }
        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / DURATION);

        if (isPull)
        {
            float kValue = K * 0.5f;

            float logValue = Mathf.Log(1 + kValue * t) / Mathf.Log(1 + kValue);

            float reversed = 1f - logValue;
            float smooth = reversed * reversed;

            currentPower = initialPower * smooth;

            Vector3 dir = casterPos - actor.transform.position;
            dir.y = 0f;
            knockbackDir = dir.normalized;
        }
        else
        {
            float decayFactor = 1f - (Mathf.Log(1 + K * t) / Mathf.Log(1 + K));
            currentPower = initialPower * decayFactor;
        }

        if (isPull)
        {
            Vector3 myPos = new Vector3(actor.transform.position.x, 0, actor.transform.position.z);
            Vector3 targetPos = new Vector3(this.casterPos.x, 0, this.casterPos.z);
            float dist = Vector3.Distance(myPos, targetPos);

            if (dist <= STOP_DISTANCE)
            {
                currentPower = 0f;
                timer = DURATION;
            }
        }

        // 이동
        actor.Move(knockbackDir, currentPower);

        if (timer >= DURATION)
        {
            actor.sm.ChangeState(new IdleState(actor));
        }
    }


    public void Exit()
    {
        if (actor.trailRenderer != null) actor.trailRenderer.emitting = false;
        if (actor.travelSparkParticle != null) actor.travelSparkParticle.Stop();

        actor.SendMovePacket(0f, 0f);
    }
}