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

        //actor.lastSkillUseTime = Time.time;
        actor.lastSkillUseTime = NetworkTimeManager.Instance.GetServerTime();

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
            dirToTarget.y = 0f;
            float minDistance = dirToTarget.magnitude;

            float finalDistance = (actionType == eState.Push) ? pushForce : Mathf.Max(0f, minDistance - 1.5f);
            Vector3 knockbackDir = dirToTarget.normalized;

            if (actionType == eState.Pull) knockbackDir = -knockbackDir;
            finalDistance *= actor.PushMulti;

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

        // Pull일 경우 방향 반전
        if (actionType == eState.Pull) pushDir = -pushDir;

        float moveDist = (actionType == eState.Push) ? 3f : Mathf.Max(0f, dirToTarget.magnitude - 1.5f);
        Vector3 destPos;

        if (targetGimmick.gimmickType == eGimmickType.Movable)
        {
            destPos = GetSafePushDestination(targetGimmick.transform.position, targetGimmick.TargetTransform.localScale, pushDir, moveDist);
        }
        else
        {
            destPos = targetGimmick.transform.position + (pushDir * moveDist);
        }

        destPos.y = targetGimmick.transform.position.y;

        if (targetGimmick.gimmickType == eGimmickType.Breakable)
        {
            BreakableObj breakable = targetGimmick as BreakableObj;
            if (breakable != null)
            {
                bool canInteract = false;
                if (breakable.interactMode == BreakableObj.InteractMode.All) canInteract = true;
                else if (breakable.interactMode == BreakableObj.InteractMode.Push && actionType == eState.Push) canInteract = true;
                else if (breakable.interactMode == BreakableObj.InteractMode.Pull && actionType == eState.Pull) canInteract = true;

                if (!canInteract) return;
            }
        }

        if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
        {
            if (targetGimmick.gimmickType == eGimmickType.Movable)
            {
                float scaledForce = (moveDist / 3.0f) * pushForce;
                SendGimmickInteractPacket(pushDir * scaledForce);
            }
            else
            {
                SendGimmickInteractPacket(destPos);
            }
        }
        else
        {
            ProcessGimmickLocal(destPos);
        }
    }

    private void SendGimmickInteractPacket(Vector3 targetData)
    {
        byte stateToSend = (byte)eGimmickState.Push;

        if (targetGimmick.gimmickType == eGimmickType.Movable)
        {
            stateToSend = 5;
        }

        P_GimmickInteractReq req = new P_GimmickInteractReq
        {
            activeUUID = LocalPlayerInfo.ID,
            gimmickID = targetGimmick.gimmickUID,
            gimmickKey = (byte)targetGimmick.gimmickType,
            state = stateToSend,
            targetPos = new P_PacketVector3 { x = targetData.x, y = targetData.y, z = targetData.z },
            param = pushForce,
            timestamp = NetworkTimeManager.Instance.GetServerTime()
        };

        Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
    }

    // private void ProcessGimmickTarget()
    // {
    //     Vector3 dirToTarget = targetGimmick.transform.position - actor.transform.position;
    //     Vector3 pushDir = dirToTarget.normalized;
    //     pushDir.y = 0;

    //     if (actionType == eState.Pull) pushDir = -pushDir;

    //     float moveDist = (actionType == eState.Push) ? 3f : (dirToTarget.magnitude - 1.5f);
    //     Vector3 destPos;

    //     if (targetGimmick.gimmickType == eGimmickType.Movable)
    //     {
    //         destPos = GetSafePushDestination(targetGimmick.transform.position, targetGimmick.TargetTransform.localScale, pushDir, moveDist);
    //     }
    //     else
    //     {
    //         destPos = targetGimmick.transform.position + (pushDir * moveDist);
    //     }

    //     destPos.y = targetGimmick.transform.position.y;

    //     if (targetGimmick.gimmickType == eGimmickType.Breakable)
    //     {
    //         BreakableObj breakable = targetGimmick as BreakableObj;
    //         if (breakable != null)
    //         {
    //             bool canInteract = false;
    //             if (breakable.interactMode == BreakableObj.InteractMode.All) canInteract = true;
    //             else if (breakable.interactMode == BreakableObj.InteractMode.Push && actionType == eState.Push) canInteract = true;
    //             else if (breakable.interactMode == BreakableObj.InteractMode.Pull && actionType == eState.Pull) canInteract = true;

    //             if (!canInteract)
    //             {
    //                 return;
    //             }
    //         }
    //     }

    //     if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
    //     {
    //         SendGimmickInteractPacket(destPos);
    //     }
    //     else
    //     {
    //         ProcessGimmickLocal(destPos);
    //     }
    // }

    // private void SendGimmickInteractPacket(Vector3 destPos)
    // {
    //     Vector3 sendPos = destPos;

    //     // Movable일 때는 destPos 대신 힘벡터 전송
    //     if (targetGimmick.gimmickType == eGimmickType.Movable)
    //     {
    //         Vector3 dirToTarget = targetGimmick.transform.position - actor.transform.position;
    //         Vector3 pushDir = dirToTarget.normalized;
    //         pushDir.y = 0;
    //         if (actionType == eState.Pull) pushDir = -pushDir;
    //         sendPos = pushDir * pushForce; // 힘이 반영된 방향벡터
    //     }

    //     P_GimmickInteractReq req = new P_GimmickInteractReq
    //     {
    //         activeUUID = LocalPlayerInfo.ID,
    //         gimmickID = targetGimmick.gimmickUID,
    //         gimmickKey = (byte)targetGimmick.gimmickType,
    //         state = (byte)eGimmickState.Push,
    //         targetPos = new P_PacketVector3 { x = sendPos.x, y = sendPos.y, z = sendPos.z },
    //         param = pushForce,
    //         timestamp = NetworkTimeManager.Instance.GetServerTime()
    //     };

    //     Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
    // }

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

    //movable 오브젝트가 이동하는 곳이 갈 수 있는 곳인지 확인
    private Vector3 GetSafePushDestination(Vector3 startPos, Vector3 targetExtents, Vector3 pushDir, float pushDistance)
    {
        Vector3 extents = targetExtents;

        // wall로 우선 지정, 3스테이지의 경우 Wall레이어의 빈 오브젝트 추가 필요함
        int wallLayer = LayerMask.GetMask("Wall");

        if (Physics.BoxCast(startPos, extents, pushDir, out RaycastHit hit, Quaternion.identity, pushDistance, wallLayer))
        {
            float safeDistance = Mathf.Max(0f, hit.distance - 0.05f);
            return startPos + pushDir * safeDistance;
        }

        return startPos + pushDir * pushDistance;
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