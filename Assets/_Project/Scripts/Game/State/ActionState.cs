using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ActionState : IState
{
    private Actor actor;
    private eState actionType;
    private float timer;
    private const float CAST_TIME = 0.7f;

    private float pushForce = 100f;
    private float pow = 2f;

    private Actor targetActor;
    private BaseGimmick targetGimmick;

    private const float ATTACK_TIME = 0.2f;
    private bool attacked = false;
    public ActionState(Actor actor, eState type, Actor targetActor = null, BaseGimmick targetGimmick = null)
    {
        this.actor = actor;
        this.actionType = type;
        this.targetActor = targetActor;
        this.targetGimmick = targetGimmick;

        pushForce = (actionType == eState.Push) ? (5f * pow) : (3f * pow);
    }

    public void Enter()
    {
        ResetTimer();
        PlayActionAnimation();
        TrySendActionState();
        MarkSkillUseTime();
        PlayActionEffects();
       
    }

    public void Execute()
    {
        UpdateTimer();

        if (!attacked && timer >= ATTACK_TIME)
        {
            attacked = true;
            ProcessTarget();
        }

        TryReturnToIdle();
    }
    public void Exit()
    {
        StopActionEffects();
    }

    private void ResetTimer()
    {
        timer = 0f;
    }

    private void PlayActionAnimation()
    {
        actor.animator.SetTrigger(GetActionTriggerName());
    }

    private string GetActionTriggerName()
    {
        return (actionType == eState.Push) ? "Push" : "Pull";
    }

    private void TrySendActionState()
    {
        if (!actor.IsLocal) return;
        if (GameManager.Instance.currentMode != GameManager.PlayMode.Server_Online) return;

        actor.SendStateChange(actionType);
    }

    private void MarkSkillUseTime()
    {
        if (!actor.IsLocal) return;

        actor.lastSkillUseTime = Time.time;
    }

    private void PlayActionEffects()
    {
        if (!actor.IsLocal) return;

        actor.OnActionStateEnter(actionType);
    }

    private void ProcessTarget()
    {
        if (!actor.IsLocal) return;

        if (targetActor != null)
        {
            ProcessActorTarget();
            return;
        }

        if (targetGimmick != null)
        {
            ProcessGimmickTarget();
        }
    }

    private void ProcessActorTarget()
    {
        Vector3 dirToTarget = targetActor.transform.position - actor.transform.position;
        dirToTarget.y = 0f;
        float minDistance = dirToTarget.magnitude;
        float finalDistance = (actionType == eState.Push) ? pushForce : Mathf.Max(0f, minDistance - 1.5f);
        Vector3 knockbackDir = dirToTarget.normalized;

        if (actionType == eState.Pull)
        {
            if (targetActor is Monster)
            {
                if (targetActor.GetComponent<Monster>().monsterState == MonsterState.Stunned)
                {
                    targetActor.GetComponent<Monster>().MonsterDead(actor.transform);
                }
                return;
            }

            knockbackDir = -knockbackDir;

        }

        if (ShouldSendActorTargetPacket(out Player targetPlayer))
        {
            actor.SendStateChange(eState.Knockback, knockbackDir, finalDistance, targetPlayer.ID, actionType == eState.Pull, actor.transform.position);
            return;
        }

        targetActor.sm.ChangeState(new KnockbackState(targetActor, knockbackDir, finalDistance, actionType == eState.Pull, actor.transform.position));
    }

    private bool ShouldSendActorTargetPacket(out Player targetPlayer)
    {
        targetPlayer = targetActor.GetComponent<Player>();

        return actor is PlayerActor
            && targetPlayer != null
            && GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online;
    }

    private void ProcessGimmickTarget()
    {
        Vector3 dirToTarget = targetGimmick.transform.position - actor.transform.position;
        Vector3 pushDir = dirToTarget.normalized;
        pushDir.y = 0;

        if (actionType == eState.Pull) pushDir = -pushDir;

        float moveDist = (actionType == eState.Push) ? 3f : (dirToTarget.magnitude - 1.5f);
        Vector3 destPos = targetGimmick.transform.position + (pushDir * moveDist);
        destPos.y = targetGimmick.transform.position.y;

        if (ShouldSendGimmickPacket())
        {
            SendGimmickPacket(destPos);
            return;
        }

        ApplyGimmickAction(destPos);
    }

    private bool ShouldSendGimmickPacket()
    {
        return actor is PlayerActor
            && GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online;
    }

    private void SendGimmickPacket(Vector3 destPos)
    {
        P_GimmickInteractReq req = new P_GimmickInteractReq
        {
            activeUUID = LocalPlayerInfo.ID,
            gimmickID = targetGimmick.gimmickUID,
            gimmickKey = (byte)targetGimmick.gimmickType,
            state = 3,
            targetPos = new P_PacketVector3 { x = destPos.x, y = destPos.y, z = destPos.z },
            param = pushForce * pow
        };

        Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
    }

    private void ApplyGimmickAction(Vector3 destPos)
    {
        if (targetGimmick.gimmickType == eGimmickType.Movable)
        {
            MoveTargetGimmick(destPos);
            return;
        }

        if (targetGimmick.gimmickType == eGimmickType.Breakable)
        {
            BreakTargetGimmick();
        }
    }

    private void MoveTargetGimmick(Vector3 destPos)
    {
        ((MovableGimmick)targetGimmick).StartMove(destPos);
    }

    private void BreakTargetGimmick()
    {
        FractureObject fracture = targetGimmick.GetComponent<FractureObject>();
        if (fracture != null)
        {
            BreakFractureObject(fracture);
            return;
        }

        targetGimmick.stat?.TakeDamage(1, eDamageType.PushPull);
    }

    private void BreakFractureObject(FractureObject fracture)
    {
        Vector3 forward = actor.GetForward();
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = actor.transform.forward;
            forward.y = 0f;
        }

        Vector3 leftDir = Vector3.Cross(forward.normalized, Vector3.up).normalized;

        fracture.BreakToDirection(leftDir);
    }

    private void UpdateTimer()
    {
        timer += Time.deltaTime;
    }

    private void TryReturnToIdle()
    {
        if (timer < CAST_TIME) return;

        actor.sm.ChangeState(new IdleState(actor));
    }

    private void StopActionEffects()
    {
        actor.OnActionStateExit(actionType);
    }
}
