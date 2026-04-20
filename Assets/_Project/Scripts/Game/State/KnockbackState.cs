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

        actor.trailRenderer.enabled = true;

        // 연출
        if (actor.trailRenderer != null) actor.trailRenderer.emitting = true;

        eState actionType = isPull ? eState.Pull : eState.Push;

        actor.animator.SetTrigger("Knockback");
        actor.PlayTravelSpark(actionType);

        if (isPull)
        {
            actor.pullCount++;
        }
        else
        {
            actor.pushCount++;
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
            Vector3 myPos2D = new Vector3(actor.transform.position.x, 0f, actor.transform.position.z);
            Vector3 casterPos2D = new Vector3(casterPos.x, 0f, casterPos.z);
            
            float dist2D = Vector3.Distance(myPos2D, casterPos2D);

            Vector3 dirToCaster = (casterPos2D - myPos2D).normalized;
            float dotProduct = Vector3.Dot(knockbackDir, dirToCaster);

            if (dist2D <= 1.5f || dotProduct <= 0f)
            {
                actor.sm.ChangeState(new IdleState(actor)); 
                return;
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
            windFactor = 1f + (dot * 0.4f); // 최대 1.4배
        }
        else if (dot < 0f)
        {
            // 역풍 → 감속
            windFactor = 1f + (dot * 0.3f); // 최소 0.7배
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
                GimmickTrigger gInfo = wall.GetComponent<GimmickTrigger>();
                if (gInfo != null)
                {
                    foreach (var target in gInfo.targetGimmicks)
                    {
                        P_GimmickInteractReq req = new P_GimmickInteractReq
                        {
                            activeUUID = LocalPlayerInfo.ID,
                            gimmickID = target.gimmickID,
                            gimmickKey = (byte)target.gimmickKey,
                            state = (byte)eGimmickState.Off_Destroy,
                            targetPos = new P_PacketVector3(),
                            param = 0f
                        };
                        Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
                    }
                }
                actor.PlayBrakeParticles();
                actor.sm.ChangeState(new IdleState(actor));

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

        actor.trailRenderer.enabled = false;
        actor.StopTravelSpark();
        actor.SendMovePacket(0f, 0f);
    }
}