using UnityEngine;

public class MoveState : IState
{
    private Actor actor;
    public MoveState(Actor actor) { this.actor = actor; }

    public void Enter()
    {
        actor.animator.SetBool("Move", true);
        actor.SendStateChange(eState.Move);
    }

    public void Execute()
    {
        if (!actor.IsLocal) return;

        PlayerActor pActor = actor as PlayerActor;
        if (pActor != null && pActor.CheckActionInput()) return;

        if (actor.h == 0 && actor.v == 0)
        {
            actor.sm.ChangeState(new IdleState(actor));
            return;
        }

        Transform camTransform = Camera.main.transform;
        Vector3 forward = camTransform.forward;
        Vector3 right = camTransform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // 입력값에 따른 이동 방향 (카메라 기준)
        Vector3 moveDir = (forward * actor.v + right * actor.h).normalized;

        // 4. 캐릭터 회전 및 이동 명령
        actor.LookAtDirection(moveDir);
        actor.Move(moveDir, actor.MoveSpeed);

        // 5. 네트워크 패킷 전송
        HandleNetworkSync();
    }

    private void HandleNetworkSync()
    {
        actor.sendTimer += Time.deltaTime;
        if (actor.sendTimer >= PlayerActor.sendInterval)
        {
            actor.SendMovePacket(actor.h, actor.v);
            actor.sendTimer = 0f;
        }
    }
    public void Exit()
    {
        actor.animator.SetBool("Move", false);
        actor.SendMovePacket(0f, 0f);
    }
}