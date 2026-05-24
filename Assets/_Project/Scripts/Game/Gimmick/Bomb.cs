using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bomb : BaseGimmick
{
    public float radius = 5.0f;
    public float force = 10f; // 기본 데미지
    public float explodeDelay = 2.0f;

    public GameObject explosionEffect;

    private FractureObject fractureObj;
    private bool hasExploded = false;

    private void Start()
    {
        fractureObj = GetComponent<FractureObject>();

        GimmickInfo info = GetComponent<GimmickInfo>();
        if (info != null)
        {
            foreach (var prop in info.properties)
            {
                if (prop.key.ToString() == "Damage" || (int)prop.key == 9)
                {
                    force = prop.value;
                }
            }
        }
    }

    // 상자에서 스폰될 때 호출되는 지연 폭발용
    public void InitBomb(float r, float f)
    {
        this.radius = r;
        this.force = f;
        StartCoroutine(ExplosionRoutine());
    }

    private IEnumerator ExplosionRoutine()
    {
        yield return new WaitForSeconds(explodeDelay);
        Explode();
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (ntf.state == 99 && !hasExploded)
        {
            if (fractureObj != null) fractureObj.Break();
            Explode();
        }
    }

    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, TargetTransform.position, Quaternion.identity);
        }

        if (GameManager.Instance.isHost)
        {
            Collider[] hits = Physics.OverlapSphere(TargetTransform.position, radius, LayerMask.GetMask("Actionable"));
            List<int> hitGimmickIDs = new List<int>();

            foreach (Collider hit in hits)
            {
                BaseGimmick targetGimmick = hit.GetComponentInParent<BaseGimmick>();

                if (targetGimmick != null && targetGimmick != this)
                {
                    bool canBeHit = (targetGimmick.gimmickType == eGimmickType.Breakable ||
                                     targetGimmick.gimmickType == eGimmickType.Bomb);

                    if (canBeHit && !hitGimmickIDs.Contains(targetGimmick.gimmickUID))
                    {
                        GimmickInfo info = targetGimmick.GetComponent<GimmickInfo>();

                        hitGimmickIDs.Add(targetGimmick.gimmickUID);

                        P_GimmickInteractReq req = new P_GimmickInteractReq
                        {
                            activeUUID = LocalPlayerInfo.ID,
                            gimmickID = targetGimmick.gimmickUID,
                            gimmickKey = (byte)targetGimmick.gimmickType,
                            state = (byte)eGimmickState.Push,
                            targetPos = new P_PacketVector3 { x = TargetTransform.position.x, y = TargetTransform.position.y, z = TargetTransform.position.z },
                            param = force
                        };

                        Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
                    }
                }
            }
        }

        if (TargetTransform.gameObject != this.gameObject)
        {
            Destroy(TargetTransform.gameObject);
        }
        else
        {
            MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
            Collider col = GetComponentInChildren<Collider>();
            if (mr != null) mr.enabled = false;
            if (col != null) col.enabled = false;
        }
    }
}