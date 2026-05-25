using UnityEngine;

public class BreakableObj : BaseGimmick
{
    public enum InteractMode { Push, Pull, All }

    [Header("상호작용 설정")]
    public InteractMode interactMode = InteractMode.All;

    [Header("스폰 설정")]
    public BaseGimmick spawnPrefab;

    private FractureObject fractureObj;

    private void Start()
    {
        fractureObj = GetComponent<FractureObject>();
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (ntf.state == 99)
        {
            BreakObject(ntf.activeUUID);
        }
    }

    private void BreakObject(long attackerUUID)
    {
        Vector3 pushDir = Vector3.left;
        if (Match.Instance.Players.TryGetValue(attackerUUID, out Player attacker))
        {
            pushDir = -attacker.transform.right;
        }

        // 본체 파쇄 연출
        if (fractureObj != null)
        {
            fractureObj.BreakToDirection(pushDir);
        }

        // 프리팹 스폰
        if (spawnPrefab != null)
        {
            GameObject spawned = Instantiate(spawnPrefab.gameObject, TargetTransform.position, Quaternion.identity);
            Bomb bomb = spawned.GetComponent<Bomb>();
            if (bomb != null)
            {
                bomb.InitBomb(bomb.radius, bomb.force);
            }
        }
    }
}