using UnityEngine;

public class Bomb : BaseGimmick
{
    public float radius = 5.0f;

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        throw new System.NotImplementedException();
    }

    public void Explode()
    {
        
    }

    private void OGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, radius);   
    }
}
