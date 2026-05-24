using UnityEngine;

public class AimState : IState
{
    private Actor actor;
    private eState actionType;

    private float maxDistance;
    private float maxAngle;

    private BaseGimmick currentTargetGimmick;
    private Actor currentTargetActor;

    private static Collider[] hitBuffer = new Collider[20];
    private static int targetLayerMask = -1;

    public AimState(Actor actor, eState type)
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
        if (targetLayerMask == -1) targetLayerMask = LayerMask.GetMask("Actionable", "Player", "Monster");
    }

    public void Execute()
    {
        Vector3 aimDir = Vector3.forward;

        if (actor is PlayerActor pActor)
        {
            aimDir = pActor.Is2p ? pActor.GetForward() : pActor.GetActionDir();
        }
        else
        {
            aimDir = actor.GetMovementDirection(); // 몬스터의 경우 타겟 방향
        }

        actor.LookAtDirection(aimDir, true);
        FindBestTarget(aimDir);

        // 플레이어일 경우에만 조준선 시각화
        if (actor is PlayerActor playerVisual)
        {
            if (currentTargetGimmick != null)
            {
                playerVisual.DrawAimLine(currentTargetGimmick.transform.position);
            }
            else if (currentTargetActor != null)
            {
                playerVisual.DrawAimLine(currentTargetActor.transform.position);
            }
            else
            {
                playerVisual.DrawAimLine(playerVisual.transform.position + aimDir * maxDistance);
            }
        }

        bool isReleased = false;

        // 몬스터는 즉시 액션으로 전환, 플레이어는 마우스 버튼 뗄 때 전환
        if (actor is PlayerActor)
        {
            isReleased = (actionType == eState.Push && Input.GetMouseButtonUp(0)) || (actionType == eState.Pull && Input.GetMouseButtonUp(1));
        }
        else
        {
            isReleased = true;
        }

        if (isReleased)
        {
            actor.sm.ChangeState(new ActionState(actor, actionType, currentTargetActor, currentTargetGimmick));
        }
    }

    public void Exit()
    {
        if (actor is PlayerActor pActor)
        {
            pActor.HideAimLine();
        }
    }

    private void FindBestTarget(Vector3 searchForward)
    {
        currentTargetActor = null;
        currentTargetGimmick = null;

        int hitCount = Physics.OverlapSphereNonAlloc(actor.transform.position, maxDistance, hitBuffer, targetLayerMask);
        float bestScore = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitBuffer[i];

            BaseGimmick gimmick = hit.GetComponentInParent<BaseGimmick>();
            Actor targetEntity = hit.GetComponentInParent<Actor>();

            if (targetEntity != null && targetEntity == actor) continue;

            Transform targetTransform = gimmick != null ? gimmick.transform : (targetEntity != null ? targetEntity.transform : null);
            if (targetTransform == null) continue;

            Vector3 closestPoint = hit.ClosestPoint(actor.transform.position);
            float distance = Vector3.Distance(actor.transform.position, closestPoint);

            Vector3 dirToCenter = targetTransform.position - actor.transform.position;
            float angle = Vector3.Angle(searchForward, dirToCenter);

            if (angle <= maxAngle && distance <= maxDistance)
            {
                float score = distance + (angle * 0.5f);

                if (score < bestScore)
                {
                    bestScore = score;

                    if (gimmick != null)
                    {
                        currentTargetGimmick = gimmick;
                        currentTargetActor = null;
                    }
                    else if (targetEntity != null)
                    {
                        currentTargetActor = targetEntity;
                        currentTargetGimmick = null;
                    }
                }
            }
        }
    }
}