using UnityEngine;
using System;
using System.Collections.Generic;

public class ActionState : IState
{
    private Actor actor;
    private eState actionType;
    private float timer;
    private const float CAST_TIME = 0.7f; // 후딜레이

    private float pushForce = 100f;      // 최대 밀쳐내는 힘
    private float pow = 2f;  // 계수

    // AimState에서 받아온 확정 타겟
    private PlayerActor targetPlayer;
    private BaseGimmick targetGimmick;

    public ActionState(Actor actor, eState type, PlayerActor targetPlayer = null, BaseGimmick targetGimmick = null)
    {
        this.actor = actor;
        this.actionType = type;
        this.targetPlayer = targetPlayer;
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

        if (actionType == eState.Push)
        {
            ((PlayerActor)actor).PushParticle();
            ((PlayerActor)actor).PushIndicator.gameObject.SetActive(true);
        }
        else
        {
            ((PlayerActor)actor).PullIndicator.gameObject.SetActive(true);
        }

        // [플레이어 타겟 처리]
        if (targetPlayer != null)
        {
            Vector3 dirToTarget = targetPlayer.transform.position - actor.transform.position;
            float minDistance = dirToTarget.magnitude;

            float finalDistance = (actionType == eState.Push) ? pushForce : Mathf.Max(0f, minDistance - 1.5f);
            Vector3 knockbackDir = dirToTarget.normalized;

            if (actionType == eState.Pull) knockbackDir = -knockbackDir;

            if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
            {
                Player p = targetPlayer.GetComponent<Player>();
                actor.SendStateChange(eState.Knockback, knockbackDir, finalDistance, p.ID, actionType == eState.Pull, actor.transform.position);
            }
            else
            {
                targetPlayer.sm.ChangeState(new KnockbackState(targetPlayer, knockbackDir, finalDistance, actionType == eState.Pull, actor.transform.position));
            }
        }
        // [기믹 오브젝트 타겟 처리]
        else if (targetGimmick != null)
        {
            Vector3 dirToTarget = targetGimmick.transform.position - actor.transform.position;
            Vector3 pushDir = dirToTarget.normalized;
            pushDir.y = 0;

            if (actionType == eState.Pull) pushDir = -pushDir;

            float moveDist = (actionType == eState.Push) ? 3f : (dirToTarget.magnitude - 1.5f);
            Vector3 destPos = targetGimmick.transform.position + (pushDir * moveDist);
            destPos.y = targetGimmick.transform.position.y;

            if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
            {
                P_GimmickInteractReq req = new P_GimmickInteractReq
                {
                    activeUUID = LocalPlayerInfo.ID,
                    gimmickID = targetGimmick.gimmickUID,
                    gimmickKey = (byte)targetGimmick.gimmickType,
                    state = 3, // 3 = 기믹 오브젝트 밀기/당기기 규약
                    targetPos = new P_PacketVector3 { x = destPos.x, y = destPos.y, z = destPos.z },
                    param = pushForce * pow
                };

                Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
            }
            else
            {
                if (targetGimmick.gimmickType == eGimmickType.Movable)
                {
                    // 필요 시 자식 클래스로 캐스팅해서 밀기 연출 진행
                    ((MovableGimmick)targetGimmick).StartMove(destPos);
                }
                else if (targetGimmick.gimmickType == eGimmickType.Breakable)
                {
                    targetGimmick.stat?.TakeDamage(1, eDamageType.PushPull);
                }
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
        ((PlayerActor)actor).InvokeSSaGay();
    }
}