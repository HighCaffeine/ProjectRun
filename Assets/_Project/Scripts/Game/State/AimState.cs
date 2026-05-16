using UnityEngine;

public class AimState : IState
{
    private PlayerActor actor;
    private eState actionType;

    private float maxDistance;
    private float maxAngle;

    private BaseGimmick currentTargetGimmick;
    private PlayerActor currentTargetPlayer;

    private static Collider[] hitBuffer = new Collider[20];
    private static int targetLayerMask = -1;

    public AimState(PlayerActor actor, eState type)
    {
        this.actor = actor;
        this.actionType = type;

        if (actionType == eState.Push)
        {
            maxDistance = 3f;
            maxAngle = 60f;
        }
        else
        {
            maxDistance = 13f;
            maxAngle = 30f;
        }
    }

    public void Enter()
    {
        if (targetLayerMask == -1) targetLayerMask = LayerMask.GetMask("Actionable", "Player");
    }

    public void Execute()
    {
        Vector3 aimDir = actor.Is2p ? actor.GetForward() : actor.GetActionDir();

        // 조준 방향 실시간 바라보기
        actor.LookAtDirection(aimDir, true);

        // 타겟팅 점수 계산
        FindBestTarget(aimDir);

        // 대상에게 선 긋기 시각화
        if (currentTargetGimmick != null)
        {
            actor.DrawAimLine(currentTargetGimmick.transform.position);
        }
        else if (currentTargetPlayer != null)
        {
            actor.DrawAimLine(currentTargetPlayer.transform.position);
        }
        else
        {
            actor.DrawAimLine(actor.transform.position + aimDir * maxDistance); // 타겟이 없으면 허공에 사거리만큼 긋기
        }

        bool isReleased = false;

        isReleased = (actionType == eState.Push && Input.GetMouseButtonUp(0)) || (actionType == eState.Pull && Input.GetMouseButtonUp(1));

        if (isReleased)
        {
            // 확정된 타겟들을 들고 ActionState로 전환
            actor.sm.ChangeState(new ActionState(actor, actionType, currentTargetPlayer, currentTargetGimmick));
        }
    }

    public void Exit()
    {
        actor.HideAimLine(); // 상태를 벗어나면 선 지우기
    }

    private void FindBestTarget(Vector3 searchForward)
    {
        currentTargetPlayer = null;
        currentTargetGimmick = null;

        int hitCount = Physics.OverlapSphereNonAlloc(actor.transform.position, maxDistance, hitBuffer, targetLayerMask);
        float bestScore = float.MaxValue; // 점수가 낮을수록 1순위 타겟

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitBuffer[i];

            BaseGimmick gimmick = hit.GetComponentInParent<BaseGimmick>();
            PlayerActor targetActor = hit.GetComponentInParent<PlayerActor>();

            if (targetActor != null && targetActor == actor) continue;

            Transform targetTransform = gimmick != null ? gimmick.transform : (targetActor != null ? targetActor.transform : null);
            if (targetTransform == null) continue;

            Vector3 dirToTarget = targetTransform.position - actor.transform.position;
            float distance = dirToTarget.magnitude;
            float angle = Vector3.Angle(searchForward, dirToTarget);

            if (angle <= maxAngle && distance <= maxDistance)
            {
                float score = distance + (angle * 0.5f);

                if (score < bestScore)
                {
                    bestScore = score;

                    if (gimmick != null)
                    {
                        currentTargetGimmick = gimmick;
                        currentTargetPlayer = null;
                    }
                    else if (targetActor != null)
                    {
                        currentTargetPlayer = targetActor;
                        currentTargetGimmick = null;
                    }
                }
            }
        }
    }
}