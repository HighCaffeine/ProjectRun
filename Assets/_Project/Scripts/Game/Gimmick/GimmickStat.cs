using UnityEngine;

public enum eDamageType { Normal, PushPull, Bomb, Wind }

public class GimmickStat : MonoBehaviour
{
    public int hp = 1;
    public int weight = 1;
    public bool isBreakableByBombOnly = false;

    // 데미지 처리
    public void TakeDamage(int damage, eDamageType type)
    {
        if (hp <= 0) return;

        if (isBreakableByBombOnly && type != eDamageType.Bomb) return;

        hp -= damage;

        if (hp <= 0)
        {
            BreakObject();
        }
    }

    private void BreakObject()
    {
        // 파티클 재생
        // 재화(동,은,금) 드랍

        Destroy(gameObject);
    }
}