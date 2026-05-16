using UnityEngine;

public class BreakableObj : BaseGimmick
{
    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (ntf.state == 99)
        {
            Debug.Log($"[BreakableObj] break");

            //todo : 연출
            //todo : 아이템 드랍

            gameObject.SetActive(false);
        }
    }
}