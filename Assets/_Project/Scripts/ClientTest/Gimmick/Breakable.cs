using UnityEngine;

public class Breakable : BaseGimmick
{
    public override void Execute(P_GimmickInteractNtf ntf)
    {
        Debug.Log($"[Breakable Gimmick]");
        Destroy(gameObject);
    }
}