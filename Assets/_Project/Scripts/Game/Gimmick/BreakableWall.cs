using UnityEngine;

public class BreakableWall : BaseGimmick
{
    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (ntf.state == 99)
        {
            Debug.Log($"[BreakableWall] break");

            // TODO : 연출


            gameObject.SetActive(false);
        }
    }
}