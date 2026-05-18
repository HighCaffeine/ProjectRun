using UnityEngine;
using System.Collections.Generic;

public class BreakableObj : BaseGimmick
{
    [Header("스폰 설정")]
    public GameObject bombPrefab;   

    private GimmickInfo gimmickInfo;
    private int spawnGimmickKey = 0;
    private float bombRadius = 5f;
    private float bombForce = 10f;

    private FractureObject fractureObj;

    private void Start()
    {
        fractureObj = GetComponent<FractureObject>();
        gimmickInfo = GetComponent<GimmickInfo>();
        
        if (gimmickInfo != null)
        {
            foreach (var prop in gimmickInfo.properties)
            {
                if (prop.key.ToString() == "SpawnGimmickKey" || (int)prop.key == (int)eGimmickPropKey.SpawnGimmickKey)
                    spawnGimmickKey = (int)prop.value;
                
                if (prop.key.ToString() == "Radius") bombRadius = prop.value;
                if (prop.key.ToString() == "Force") bombForce = prop.value;
            }
        }
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (ntf.state == 99)
        {
            Break(ntf.activeUUID);
        }
    }

    private void Break(long attackerUUID)
    {
        Vector3 pushDir = Vector3.left; 
        if (Match.Instance.Players.TryGetValue(attackerUUID, out Player attacker))
        {
            pushDir = -attacker.transform.right; 
        }

        if (fractureObj != null)
        {
            fractureObj.Break(pushDir * 15f); 
        }

        if (spawnGimmickKey == (int)eGimmickKey.Bomb && bombPrefab != null)
        {
            GameObject bombObj = Instantiate(bombPrefab, transform.position, Quaternion.identity);
            Bomb bombScript = bombObj.GetComponent<Bomb>();
            if (bombScript != null) bombScript.InitBomb(bombRadius, bombForce);
        }
    }
}