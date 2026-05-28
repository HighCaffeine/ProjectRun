using UnityEngine;

public class AimState : IState
{
    private const float PLAYER_ON_MOVABLE_PRIORITY_SCORE_TOLERANCE = 5f;

    private Actor actor;
    private eState actionType;

    private float maxDistance;
    private float maxAngle;
    private Vector3 aimDir;

    private BaseGimmick currentTargetGimmick;
    private Actor currentTargetPlayer;

    private static Collider[] hitBuffer = new Collider[20];
    private static int targetLayerMask = -1;

    public AimState(Actor actor, eState type)
    {
        this.actor = actor;
        this.actionType = type;

        SetAimRange();
    }

    public void Enter()
    {
        InitializeTargetLayerMask();
    }

    public void Execute()
    {
        UpdateAimDirection();
        RotateActorToAimDirection();
        UpdateBestTarget();
        DrawAimLineToCurrentTarget();
        TryChangeToActionState();
    }

    public void Exit()
    {
        HideAimLine();
    }

    private void SetAimRange()
    {
        if (actionType == eState.Push)
        {
            maxDistance = 3f;
            maxAngle = 60f;
            return;
        }

        maxDistance = 13f;
        maxAngle = 30f;
    }

    private void InitializeTargetLayerMask()
    {
        if (targetLayerMask == -1) targetLayerMask = LayerMask.GetMask("Actionable", "Player");
    }

    private void UpdateAimDirection()
    {
        aimDir = actor.Is2p ? actor.GetForward() : actor.GetActionDir();
    }

    private void RotateActorToAimDirection()
    {
        actor.LookAtDirection(aimDir, true);
    }

    private void UpdateBestTarget()
    {
        FindBestTarget(aimDir);
    }

    private void DrawAimLineToCurrentTarget()
    {
        if (currentTargetGimmick != null)
        {
            actor.DrawAimLine(currentTargetGimmick.transform.position);
            return;
        }

        if (currentTargetPlayer != null)
        {
            actor.DrawAimLine(currentTargetPlayer.transform.position);
            return;
        }

        actor.DrawAimLine(actor.transform.position + aimDir * maxDistance);
    }

    private void TryChangeToActionState()
    {
        if (!IsActionReleased()) return;

        actor.sm.ChangeState(new ActionState(actor, actionType, currentTargetPlayer, currentTargetGimmick));
    }

    private bool IsActionReleased()
    {
        return (actionType == eState.Push && Input.GetMouseButtonUp(0))
            || (actionType == eState.Pull && Input.GetMouseButtonUp(1));
    }

    private void HideAimLine()
    {
        ((PlayerActor)actor).HideAimLine();
    }

    private void FindBestTarget(Vector3 searchForward)
    {
        ClearCurrentTarget();
        SearchBestTarget(searchForward);
    }

    private void ClearCurrentTarget()
    {
        currentTargetPlayer = null;
        currentTargetGimmick = null;
    }

    private void SearchBestTarget(Vector3 searchForward)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(actor.transform.position, maxDistance, hitBuffer, targetLayerMask);
        float bestScore = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            TrySelectBestTarget(hitBuffer[i], searchForward, ref bestScore);
        }
    }

    private void TrySelectBestTarget(Collider hit, Vector3 searchForward, ref float bestScore)
    {
        BaseGimmick gimmick = hit.GetComponentInParent<BaseGimmick>();
        Actor targetActor = hit.GetComponentInParent<Actor>();

        if (targetActor != null && targetActor == actor) return;

        if (actionType == eState.Pull && targetActor is Monster monster)
        {
            if (monster.monsterState != MonsterState.Stunned)
                return;
        }

        Transform targetTransform = GetTargetTransform(gimmick, targetActor);
        if (targetTransform == null) return;

        float distance = GetDistanceToHit(hit);
        float angle = GetAngleToTarget(searchForward, targetTransform);

        if (!IsTargetInRange(distance, angle)) return;

        float score = distance + (angle * 0.5f);
        if (!ShouldSelectTarget(gimmick, targetActor, score, bestScore)) return;

        bestScore = score;
        SetCurrentTarget(gimmick, targetActor);
    }

    private bool ShouldSelectTarget(BaseGimmick gimmick, Actor targetActor, float score, float bestScore)
    {
        if (currentTargetGimmick == null && currentTargetPlayer == null) return true;

        bool isCloseScore = Mathf.Abs(score - bestScore) <= PLAYER_ON_MOVABLE_PRIORITY_SCORE_TOLERANCE;
        if (isCloseScore)
        {
            if (IsPlayerStandingOnMovable(targetActor, currentTargetGimmick)) return true;
            if (IsPlayerStandingOnMovable(currentTargetPlayer, gimmick)) return false;
        }

        return score < bestScore;
    }

    private static bool IsPlayerStandingOnMovable(Actor player, BaseGimmick gimmick)
    {
        return player != null
            && gimmick != null
            && ((PlayerActor)player).CurrentMovableGround == gimmick;
    }

    private Transform GetTargetTransform(BaseGimmick gimmick, Actor targetActor)
    {
        return gimmick != null ? gimmick.transform : (targetActor != null ? targetActor.transform : null);
    }

    private float GetDistanceToHit(Collider hit)
    {
        Vector3 closestPoint = hit.ClosestPoint(actor.transform.position);

        return Vector3.Distance(actor.transform.position, closestPoint);
    }

    private float GetAngleToTarget(Vector3 searchForward, Transform targetTransform)
    {
        Vector3 dirToCenter = targetTransform.position - actor.transform.position;

        return Vector3.Angle(searchForward, dirToCenter);
    }

    private bool IsTargetInRange(float distance, float angle)
    {
        return angle <= maxAngle && distance <= maxDistance;
    }

    private void SetCurrentTarget(BaseGimmick gimmick, Actor targetActor)
    {
        if (gimmick != null)
        {
            currentTargetGimmick = gimmick;
            currentTargetPlayer = null;
            return;
        }

        if (targetActor != null)
        {
            currentTargetPlayer = targetActor;
            currentTargetGimmick = null;
        }
    }
}
