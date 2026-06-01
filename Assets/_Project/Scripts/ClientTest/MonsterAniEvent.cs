using UnityEngine;

public class MonsterAniEvent : MonoBehaviour
{
    private Monster monster;

    private void Start()
    {
        monster = GetComponentInParent<Monster>();
    }

    public void OnAttackHit()
    {
        monster.OnAttackHit();
    }
}
