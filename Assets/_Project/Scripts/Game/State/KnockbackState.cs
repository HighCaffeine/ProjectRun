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
        actor.SendStateChange(eState.Knockback, knockbackDir, initialPower);
        actor.StartCoroutine(actor.HitStopRoutine());

        actor.SetVerticalVelocity(3.0f);

        // actor.SetAni(AniState.Hit); // 피격 애니메이션 재생
        // var impulseSource = actor.GetComponent<CinemachineImpulseSource>();
        // if (impulseSource != null) impulseSource.GenerateImpulse(knockbackDir * 1.0f);

        // 연출
        if (actor.trailRenderer != null) actor.trailRenderer.emitting = true;

        eState actionType = isPull ? eState.Pull : eState.Push;

        actor.animator.SetTrigger("Knockback");
        actor.PlayTravelSpark(actionType);

        if(isPull)
        {
            actor.pullCount++;
            Debug.Log(actor.gameObject.name + "pull :" + actor.pullCount);      
        }
        else
        {
            actor.pushCount++;
            Debug.Log(actor.gameObject.name + "push :" + actor.pushCount);
        }
    }

    public void Execute()
    {
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
        Vector3 windDir = actor.windDir;
        float windPower = actor.windPower;

        // 방향 관계
        float dot = Vector3.Dot(knockbackDir, windDir);

        // 가속/감속 계수
        float windFactor = 1f;

        if (dot > 0f)
        {
            // 순풍 → 가속
            windFactor = 1f + (dot * 0.7f); // 최대 1.7배
        }
        else if (dot < 0f)
        {
            // 역풍 → 감속
            windFactor = 1f + (dot * 0.7f); // 최소 0.3배
        }

        // 최종 힘
        float finalPower = currentPower * windFactor;

        // 옆바람도 살짝 반영하고 싶으면 추가
        Vector3 sideWind = windDir * windPower * 0.2f;

        // 최종 이동
        Vector3 finalForce = (knockbackDir * finalPower) + sideWind;

        actor.Move(finalForce.normalized, finalForce.magnitude);

        // actor.Move(knockbackDir, currentPower);

        actor.sendTimer += Time.deltaTime;
        if (actor.sendTimer >= PlayerActor.sendInterval)
        {
            actor.SendMovePacket(0f, 0f);
            actor.sendTimer = 0f;
        }

        //충돌감지
        Collider[] hitWalls = Physics.OverlapSphere(actor.transform.position, 0.6f);
        foreach (var wall in hitWalls)
        {
            if (wall.CompareTag("Breakable"))
            {
                wall.gameObject.SetActive(false);
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