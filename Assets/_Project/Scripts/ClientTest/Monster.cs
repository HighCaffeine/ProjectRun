using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AdaptivePerformance.Provider;
using UnityEngine.Rendering;

public enum MonsterState
{
    Normal,
    Knockback,
    Stunned,
    Dead
}
public class Monster : Actor
{
    [SerializeField]
    private PlayerActor currentTarget;
    public override bool IsLocal => true;

    [SerializeField]
    private float recoveryTime = 5f;
    public MonsterState monsterState = MonsterState.Normal;

    private Coroutine stunCoroutine;

    [SerializeField] private SkinnedMeshRenderer meshRenderer;
    [SerializeField] private Material normalMat;
    [SerializeField] private Material stunnedMat;

    [SerializeField]private float heightOffset = 1f;


    //�ĵ�
    [SerializeField]
    private float attackCooldown = 1f;
    private float lastAttackTime;



    public override Vector3 GetMovementDirection()
    {
        if (monsterState == MonsterState.Stunned) return Vector3.zero;

        if (currentTarget == null) return Vector3.zero;

        Vector3 dir = currentTarget.transform.position - transform.position;
        dir.y = 0f;

        return dir.normalized;
    }

    public override bool HasMoveIntent()
    {
        if (monsterState != MonsterState.Normal) return false;
        if (currentTarget == null) return false;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        return distance > 2f;
    }

    public override bool CheckActionIntent()
    {
        if (currentTarget == null) return false;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        return distance <= 2f;
    }

    protected new void Start()
    {
        //usegravity = false;
        base.Start();

    }
    private void Update()
    {
        if (monsterState == MonsterState.Dead)
            return;

        UpdateTarget();

        const float HEIGHT_EPSILON = 0.05f;
        if (Mathf.Abs(transform.position.y - (currentTarget.transform.position.y + heightOffset)) > HEIGHT_EPSILON)
        {
            UpdateHeight();
        }
        else
        {
            sm.Update();

            ApplyMovement();

            if (!(sm.currentState is KnockbackState))
            {
                TryChangeToActionState();
            }
        }
        UpdateMaterialByState();

    }

    private void UpdateTarget()
    {
        if (currentTarget == null || currentTarget.isDead)
        {
            ChooseTarget();
        }
    }

    private void TryChangeToActionState()
    {
        if (!CanStartAction()) return;
        if (!CheckActionIntent()) return;

        lastAttackTime = Time.time;

        sm.ChangeState(new ActionState(this, eState.Push, currentTarget));
    }

    private bool CanStartAction()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return false;

        return sm.currentState is IdleState || sm.currentState is MoveState;
    }
    public PlayerActor GetRandomPlayerTarget()
    {
        if (Match.Instance == null || Match.Instance.Players == null)
        {
            return null;
        }

        List<PlayerActor> players = new List<PlayerActor>();
        foreach (Player player in Match.Instance.Players.Values)
        {
            if (player == null)
            {
                continue;
            }
            PlayerActor actor = player.GetComponent<PlayerActor>();
            if (actor == null)
            {
                continue;
            }
            if (actor.isDead)
            { 
                continue; 
            }
            if (!actor.gameObject.activeInHierarchy)
            {
                continue;
            }

            players.Add(actor);
        }

        if (players.Count == 0)
        {
            return null;
        }

        return players[Random.Range(0, players.Count)];
    }

    private void ChooseTarget()
    {
        if (currentTarget != null && !currentTarget.isDead)
        {
            return;
        }

        currentTarget = GetRandomPlayerTarget();
    }

    public void MonsterDead(Transform player)
    {
        monsterState = MonsterState.Dead;
        FractureObject fractureObject = GetComponentInChildren<FractureObject>();
        if (fractureObject != null)
        {
            Vector3 dir = (player.transform.position - transform.position).normalized;

            fractureObject.BreakToDirection(dir);
        }
        Destroy(gameObject, 2f);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {

        if (monsterState == MonsterState.Knockback)
        {
            if (hit.gameObject.CompareTag("Wall"))
            {
                SetStunned(recoveryTime);
                sm.ChangeState(new IdleState(this));
            }
        }
    }


    public void SetStunned(float duration)
    {
        monsterState = MonsterState.Stunned;
        animator.SetTrigger("Stunned");
        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        stunCoroutine = StartCoroutine(StateRecovery(duration));
    }

    private IEnumerator StateRecovery(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (monsterState == MonsterState.Stunned)
        {
            monsterState = MonsterState.Normal;
            animator.SetTrigger("Recovery");
        }

        stunCoroutine = null;
    }

    private void UpdateMaterialByState()
    {
        if(meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }
        Material[] mats = meshRenderer.materials;

        switch (monsterState)
        {
            case MonsterState.Stunned:
                mats[1] = stunnedMat;
                break;

            case MonsterState.Normal:
                mats[1] = normalMat;
                break;
        }

        meshRenderer.materials = mats;
    }

    void UpdateHeight()
    {
        if (currentTarget == null)
            return;
        float targetY = currentTarget.transform.position.y + heightOffset;

        float nextY = Mathf.MoveTowards(transform.position.y,targetY,2f * Time.deltaTime);

        float deltaY = nextY - transform.position.y;

        // transform.position = pos;
        controller.Move(Vector3.up* deltaY);
    }

    public void OnAttackHit()
    {
        Debug.Log(sm.currentState);
        if (sm.currentState is ActionState actionState)
        {
            actionState.OnAttackHit();
        }
    }
}
