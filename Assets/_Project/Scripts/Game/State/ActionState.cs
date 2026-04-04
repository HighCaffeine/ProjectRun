using UnityEngine;

public class ActionState : IState
{
    private PlayerActor actor;
    private byte actionType;
    private float timer;
    private const float CAST_TIME = 0.3f; // 후딜레이

    private float maxDistance;
    private float maxAngle = 30f;
    private float pushForce = 100f;      // 최대 밀쳐내는 힘
    private float pow = 3f;  // 계수

    public ActionState(PlayerActor actor, byte type)
    {
        this.actor = actor;
        this.actionType = type;

        if (actionType == 0) // 밀기
        {
            maxDistance = 3f;
            pushForce = 100f;
        }
        else if (actionType == 1) // 당기기
        {
            maxDistance = 10f;
            pushForce = 120f;
        }
    }

    public void Enter()
    {
        timer = 0f;
        
        //카메라 연출
        actor.ShakeCamera();

        Vector3 searchForward = actor.GetForward();
        // 타겟 탐색
        Collider[] colliders = Physics.OverlapSphere(actor.transform.position, maxDistance);
        PlayerActor closestTarget = null;
        float minDistance = float.MaxValue;

        foreach (Collider hit in colliders)
        {
            if (hit.transform.root == actor.transform.root) continue;

            PlayerActor targetActor = hit.GetComponentInParent<PlayerActor>();

            if (targetActor != null)
            {
                Vector3 dirToTarget = targetActor.transform.position - actor.transform.position;
                float distance = dirToTarget.magnitude;
                float angleToTarget = Vector3.Angle(searchForward, dirToTarget);

                // 정면 각도 내에 있고, 제일 가까운 놈 찾기
                if (angleToTarget <= maxAngle && distance < minDistance)
                {
                    closestTarget = targetActor;
                    minDistance = distance;
                }
            }
        }

        // 타겟을 찾았다면 타격 공식 및 넉백 상태 부여
        if (closestTarget != null)
        {
            Vector3 pushDir = closestTarget.transform.position - actor.transform.position;
            if (minDistance <= 0.1f) pushDir = searchForward;

            pushDir.y = 0;
            pushDir.Normalize();

            //거리 비례 감쇄
            float x = Mathf.Clamp01(minDistance / maxDistance);
            float falloffMultiplier = 1f - Mathf.Pow(x, pow);

            // 최종 넉백 파워 계산
            float finalPower = pushForce * falloffMultiplier * actor.PushMulti;

            // 당기기 방향을 반대로
            bool isPull = (actionType == 1);

            if (isPull)
            {
                pushDir = -pushDir;
            }

            //테스트 (나중에 서버로 보냄)
            closestTarget.sm.ChangeState(new KnockbackState(closestTarget, pushDir, finalPower, isPull, actor.transform.position));

            Debug.Log($"[{closestTarget.gameObject.name}] 적중 거리:{minDistance:F1}m 파워:{finalPower:F1}");
        }
    }

    public void Execute()
    {
        timer += Time.deltaTime;

        if (timer >= CAST_TIME)
        {
            actor.sm.ChangeState(new IdleState(actor));
        }
    }

    public void Exit()
    {
    }
}