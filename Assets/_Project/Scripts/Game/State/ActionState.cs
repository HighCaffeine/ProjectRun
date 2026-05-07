using UnityEngine;

public class ActionState : IState
{
    private Actor actor;
    private eState actionType;
    private float timer;
    private const float CAST_TIME = 0.3f; // 후딜레이

    private float maxDistance;
    private float maxAngle = 30f;
    private float pushForce = 100f;      // 최대 밀쳐내는 힘
    private float pow = 1.5f;  // 계수

    private static Collider[] hitBuffer = new Collider[20];

    public ActionState(Actor actor, eState type)
    {
        this.actor = actor;
        this.actionType = type;

        if (actionType == eState.Push) // 밀기
        {
            maxDistance = 3f;
            pushForce = 100;
            maxAngle = 60;
        }
        else if (actionType == eState.Pull) // 당기기
        {
            maxDistance = 10f;
            pushForce = 120;
            maxAngle = 30f;
        }
    }

    public void Enter()
    {
        timer = 0f;

        //공통 연출 (애니메이션)
        actor.animator.SetTrigger((actionType == eState.Push) ? "Push" : "Pull");

        if (!actor.IsLocal) return;
       
        if (Time.time - actor.lastSkillUseTime < Actor.SKILL_COOLDOWN)
        {
            actor.sm.ChangeState(new IdleState(actor));
            return;
        }
      
        actor.lastSkillUseTime = Time.time; // 스킬 사용 시간 갱신

        //Vector3 searchForward = actor.GetForward();
        //Vector3 searchForward = actor.GetMouseDir();
        Vector3 searchForward = actor.GetActionDir();
        if (actor.Is2p)
        {
            searchForward = actor.GetForward();
        }

        actor.LookAtDirection(searchForward);
        // 타겟 탐색
        int hitCount = Physics.OverlapSphereNonAlloc(actor.transform.position, maxDistance, hitBuffer, mask);

        PlayerActor closestTarget = null;
        MovableGimmick closestGimmick = null;
        float minDistance = float.MaxValue;
        Vector3 dirToTarget = Vector3.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit.transform == actor.transform) continue;

            PlayerActor targetActor = hit.GetComponentInParent<PlayerActor>();
            if (targetActor != null)
            {
                dirToTarget = targetActor.transform.position - actor.transform.position;
                float distance = dirToTarget.magnitude;
                float angleToTarget = Vector3.Angle(searchForward, dirToTarget);

                // 정면 각도 내에 있고, 제일 가까운 놈 찾기
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
                    closestTarget = null; //플레이어 x 
                    minDistance = distance;
                    dirToTarget = dir;
                }
            }
        }

        // 타겟을 찾았다면 타격 공식 및 넉백 상태 부여
        if (closestGimmick == null)
        {
            if (closestTarget == null) return;
            if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
            {
                Player targetPlayer = closestTarget.GetComponent<Player>();
                if (targetPlayer != null)
                {
                    actor.SendStateChange(eState.Knockback, dirToTarget, pushForce * pow, targetPlayer.ID);
                }
            }
            else
            {
                PlayerActor targetPlayer = closestTarget.GetComponent<PlayerActor>();
                var a = ActorManager.Instance.GetActor(targetPlayer.gameObject.name);
                a.sm.ChangeState(new KnockbackState((PlayerActor)a, dirToTarget, pushForce * pow, actionType == eState.Pull, actor.transform.position));
            }
        }
        else if (closestGimmick != null)
        {
            Vector3 pushDir = dirToTarget.normalized;
            pushDir.y = 0;
            if (actionType == eState.Pull) pushDir = -pushDir;

            float moveDist = (actionType == 0) ? 3f : (minDistance - 1.5f); // 당길 땐 내 앞 1.5m까지만
            Vector3 destPos = closestGimmick.transform.position + (pushDir * moveDist);

            if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
            {
                // 서버 연동: 기믹 패킷 전송 (상호작용)
                P_GimmickInteractReq req = new P_GimmickInteractReq
                {
                    activeUUID = LocalPlayerInfo.ID,
                    gimmickID = closestGimmick.gimmickUID,
                    gimmickKey = (byte)eGimmickKey.MovableObject,
                    state = 3, // 3 = 기믹 오브젝트 밀기/당기기 규약
                    targetPos = new P_PacketVector3 { x = destPos.x, y = destPos.y, z = destPos.z },
                    param = pushForce * pow
                };
                Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
            }
            else
            {
                // 오프라인 테스트: 패킷 없이 상자를 즉시 부드럽게 이동시킴
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

    public void Exit()
    {
    }

   



}