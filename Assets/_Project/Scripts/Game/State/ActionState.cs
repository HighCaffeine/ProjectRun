using UnityEngine;
using System;
using System.Collections.Generic;

public class ActionState : IState
{
    private Actor actor;
    private eState actionType;
    private float timer;
    private const float CAST_TIME = 0.2f;

    private float pushForce = 100f;
    private float pow = 2f;

    private Actor targetActor;
    private BaseGimmick targetGimmick;

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
        timer = 0f;
        actor.animator.SetTrigger((actionType == eState.Push) ? "Push" : "Pull");

        if (!actor.IsLocal) return;

        if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
        {
            actor.SendStateChange(actionType);
        }

        actor.lastSkillUseTime = Time.time;

        if (actor is PlayerActor pActor)
        {
            if (actionType == eState.Push)
            {
                pActor.PushParticle();
                pActor.PushIndicator.gameObject.SetActive(true);
            }
            else
            {
                pActor.PullIndicator.gameObject.SetActive(true);
            }
        }

        if (targetActor != null)
        {
            Vector3 dirToTarget = targetActor.transform.position - actor.transform.position;
            float minDistance = dirToTarget.magnitude;

            float finalDistance = (actionType == eState.Push) ? pushForce : Mathf.Max(0f, minDistance - 1.5f);
            Vector3 knockbackDir = dirToTarget.normalized;

            if (actionType == eState.Pull) knockbackDir = -knockbackDir;

            if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
            {
                Player p = targetActor.GetComponent<Player>();
                long targetID = (p != null) ? p.ID : 0;
                actor.SendStateChange(eState.Knockback, knockbackDir, finalDistance, targetID, actionType == eState.Pull, actor.transform.position);
            }
            else
            {
                targetActor.sm.ChangeState(new KnockbackState(targetActor, knockbackDir, finalDistance, actionType == eState.Pull, actor.transform.position));
            }
        }
        else if (targetGimmick != null)
        {
            ProcessGimmickTarget();
        }
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

        // ★ BreakableObj Push/Pull 모드 필터링 ★
        if (targetGimmick.gimmickType == eGimmickType.Breakable)
        {
            BreakableObj breakable = targetGimmick as BreakableObj;
            if (breakable != null)
            {
                bool canInteract = false;
                if (breakable.interactMode == BreakableObj.InteractMode.All) canInteract = true;
                else if (breakable.interactMode == BreakableObj.InteractMode.Push && actionType == eState.Push) canInteract = true;
                else if (breakable.interactMode == BreakableObj.InteractMode.Pull && actionType == eState.Pull) canInteract = true;

                if (!canInteract)
                {
                    Debug.Log($"<color=yellow>[ActionState]</color> 설정된 방향과 달라 BreakableObj 상호작용 무시됨");
                    return; // 설정과 다르면 서버로 패킷 안 보냄! (데미지 안 들어감)
                }
            }
        }

        if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
        {
            SendGimmickInteractPacket(destPos);
        }
        else
        {
            ProcessGimmickLocal(destPos);
        }
    }

    private void SendGimmickInteractPacket(Vector3 destPos)
    {
        byte stateToSend = (byte)eGimmickState.Push;

        P_GimmickInteractReq req = new P_GimmickInteractReq
        {
            activeUUID = LocalPlayerInfo.ID,
            gimmickID = targetGimmick.gimmickUID,
            gimmickKey = (byte)targetGimmick.gimmickType,
            state = stateToSend,
            targetPos = new P_PacketVector3 { x = destPos.x, y = destPos.y, z = destPos.z },
            param = pushForce // 일반 공격은 액션 파워 전달
        };

        Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
    }

    private void ProcessGimmickLocal(Vector3 destPos)
    {
        if (targetGimmick.gimmickType == eGimmickType.Movable)
        {
            ((MovableGimmick)targetGimmick).StartMove(destPos);
        }
        else if (targetGimmick.gimmickType == eGimmickType.Breakable)
        {
            FractureObject fracture = targetGimmick.GetComponent<FractureObject>();
            if (fracture != null)
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
        if (actor is PlayerActor pActor)
        {
            pActor.InvokeSSaGay();
        }
    }
}