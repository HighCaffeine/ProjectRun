using UnityEngine;

public class BreakableWall : BaseGimmick
{
    private FractureObject fractureObj;

    private void Start()
    {
        fractureObj = GetComponent<FractureObject>();
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
    }
}