using UnityEngine;

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
        // actor.SetAni(AniState.Hit); // 피격 애니메이션 재생
    }

    public void Execute()
    {
        timer += Time.deltaTime;

        // 시간에 따른 선형 감쇠
        float t = timer / DURATION;
        currentPower = Mathf.Lerp(initialPower, 0f, t);

        // 당길 때 시전자 위치 넘어가지 않게 조절
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
        actor.SendMovePacket(0f, 0f);
    }
}