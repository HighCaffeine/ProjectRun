using UnityEngine;

public class ActionState : IState
{
    private PlayerActor actor;
    private eState actionType;
    private float timer;
    private const float CAST_TIME = 0.3f;

    private float maxDistance;
    private float maxAngle = 30f;
    private float pushForce = 100f;
    private float pow = 1.5f;

    private float sphereRadius = 1f;

    public ActionState(PlayerActor actor, eState type)
    {
        this.actor = actor;
        this.actionType = type;

        if (actionType == eState.Push)
        {
            maxDistance = 3f;
            pushForce = 100;
        }
        else if (actionType == eState.Pull)
        {
            maxDistance = 10f;
            pushForce = 120;
        }
    }

    public void Enter()
    {
        timer = 0f;

        if (actor.IsLocal)
        {
            if (Time.time - actor.lastSkillUseTime < PlayerActor.SKILL_COOLDOWN)
            {
                actor.sm.ChangeState(new IdleState(actor));
                return;
            }
            actor.lastSkillUseTime = Time.time; // 스킬 사용 시간 갱신
        }

        actor.animator.SetTrigger((actionType == eState.Push) ? "Push" : "Pull");

        if (!actor.IsLocal) return;

        actor.ShakeCamera();

        Vector3 searchForward = actor.GetMouseDir();
        if (actor.is2p)
        {
            searchForward = actor.GetForward();
        }

        actor.LookAtDirection(searchForward);

        if (actionType == eState.Pull)
        {
            Vector3 origin = actor.transform.position;
            origin.y += 1.0f;

            Vector3 dir = searchForward.normalized;

            RaycastHit hit;

            // int mask = LayerMask.GetMask("Player", "Gimmick");
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

            Debug.DrawRay(origin + right * sphereRadius, dir * maxDistance, Color.green, 2f);
            Debug.DrawRay(origin - right * sphereRadius, dir * maxDistance, Color.green, 2f);
            Debug.DrawRay(origin, dir * maxDistance, Color.red, 2f);


            if (Physics.SphereCast(origin, sphereRadius, dir, out hit, maxDistance))
            {

                PlayerActor targetActor = hit.collider.GetComponentInParent<PlayerActor>();
                MovableGimmick targetGimmick = hit.collider.GetComponentInParent<MovableGimmick>();

                if (targetActor != null && targetActor != actor)
                {
                    Vector3 pulldir = (targetActor.transform.position - actor.transform.position);

                    if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
                    {
                        Player targetPlayer = targetActor.GetComponent<Player>();
                        pulldir *= actionType == eState.Push ?  1.0f : -1.0f;
                        actor.SendStateChange(eState.Knockback, pulldir, pushForce * pow, targetPlayer.ID);
                    }
                    else
                    {
                        var a = ActorManager.Instance.GetActor(targetActor.gameObject.name);
                        a.sm.ChangeState(new KnockbackState(
                            (PlayerActor)a, pulldir, pushForce * pow, true, actor.transform.position));
                    }
                }
                else if (targetGimmick != null)
                {
                    Vector3 pullDir = (actor.transform.position - targetGimmick.transform.position).normalized;

                    float dist = Vector3.Distance(actor.transform.position, targetGimmick.transform.position);
                    float moveDist = Mathf.Min(3f, dist);

                    Vector3 destPos = targetGimmick.transform.position + pullDir * moveDist;

                    if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
                    {
                        P_GimmickInteractReq req = new P_GimmickInteractReq
                        {
                            activeUUID = LocalPlayerInfo.ID,
                            gimmickID = targetGimmick.gimmickUID,
                            gimmickKey = (byte)eGimmickKey.MovableObject,
                            state = 3,
                            targetPos = new P_PacketVector3 { x = destPos.x, y = destPos.y, z = destPos.z },
                            param = pushForce * pow
                        };
                        Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
                    }
                    else
                    {
                        targetGimmick.StartMove(destPos);
                    }
                }
            }

            return;
        }
        Collider[] colliders = Physics.OverlapSphere(actor.transform.position, maxDistance);

        PlayerActor closestTarget = null;
        MovableGimmick closestGimmick = null;

        float minDistance = float.MaxValue;
        Vector3 dirToTarget = Vector3.zero;

        foreach (Collider hit in colliders)
        {
            if (hit.transform == actor.transform) continue;

            PlayerActor targetActor = hit.GetComponentInParent<PlayerActor>();
            if (targetActor != null)
            {
                dirToTarget = targetActor.transform.position - actor.transform.position;
                float distance = dirToTarget.magnitude;
                float angleToTarget = Vector3.Angle(searchForward, dirToTarget);

                if (angleToTarget <= maxAngle && distance < minDistance)
                {
                    closestTarget = targetActor;
                    minDistance = distance;
                }
            }

            MovableGimmick targetGimmick = hit.GetComponentInParent<MovableGimmick>();
            if (targetGimmick != null)
            {
                Vector3 dir = targetGimmick.transform.position - actor.transform.position;
                float distance = dir.magnitude;
                float angle = Vector3.Angle(searchForward, dir);

                if (angle <= maxAngle && distance < minDistance)
                {
                    closestGimmick = targetGimmick;
                    closestTarget = null;
                    minDistance = distance;
                    dirToTarget = dir;
                }
            }
        }

        if (closestGimmick == null)
        {
            if (closestTarget == null) return;

            if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
            {
                Player targetPlayer = closestTarget.GetComponent<Player>();
                if (targetPlayer != null)
                {
                    dirToTarget *= (actionType == eState.Push ? 1.0f : -1.0f);
                    actor.SendStateChange(eState.Knockback, dirToTarget.normalized, pushForce * pow, targetPlayer.ID);
                }
            }
            else
            {
                PlayerActor targetPlayer = closestTarget.GetComponent<PlayerActor>();
                var a = ActorManager.Instance.GetActor(targetPlayer.gameObject.name);
                a.sm.ChangeState(new KnockbackState((PlayerActor)a, dirToTarget, pushForce * pow, false, actor.transform.position));
            }
        }
        else
        {
            Vector3 pushDir = dirToTarget.normalized;
            pushDir.y = 0;

            float moveDist = 3f;
            Vector3 destPos = closestGimmick.transform.position + (pushDir * moveDist);

            if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
            {
                P_GimmickInteractReq req = new P_GimmickInteractReq
                {
                    activeUUID = LocalPlayerInfo.ID,
                    gimmickID = closestGimmick.gimmickUID,
                    gimmickKey = (byte)eGimmickKey.MovableObject,
                    state = 3,
                    targetPos = new P_PacketVector3 { x = destPos.x, y = destPos.y, z = destPos.z },
                    param = pushForce * pow
                };
                Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
            }
            else
            {
                closestGimmick.StartMove(destPos);
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

    public void Exit() { }
}