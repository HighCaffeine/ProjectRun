using UnityEngine;

public class KnockbackState : IState
{
    private PlayerActor actor;
    private Vector3 knockbackDir;
    private float power;
    private float timer;
    private const float DURATION = 0.25f;

    public KnockbackState(PlayerActor actor, Vector3 dir, float power)
    {
        this.actor = actor;
        this.knockbackDir = dir;
        this.power = power;
    }

    public void Enter()
    {
        timer = 0f;
        // 피격 애니메이션 재생
    }

    public void Execute()
    {
    }

    public void Exit() { }
}