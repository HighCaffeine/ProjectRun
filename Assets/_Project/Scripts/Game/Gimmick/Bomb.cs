using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bomb : BaseGimmick
{
    public float radius = 5.0f;
    public float force = 10f;
    public float explodeDelay = 2.0f; // 2초 뒤 폭발
    
    public GameObject explosionEffect;

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

    public void Explode()
    {
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        if (GameManager.Instance.isHost)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius, LayerMask.GetMask("Actionable"));
            
            List<int> hitGimmickIDs = new List<int>();

            foreach (Collider hit in hits)
            {
                BaseGimmick targetGimmick = hit.GetComponentInParent<BaseGimmick>();
                if (targetGimmick != null && targetGimmick != this)
                {
                    if (targetGimmick.gimmickType == eGimmickType.Breakable)
                    {
                        if (!hitGimmickIDs.Contains(targetGimmick.gimmickUID))
                        {
                            hitGimmickIDs.Add(targetGimmick.gimmickUID);

                            P_GimmickInteractReq req = new P_GimmickInteractReq
                            {
                                activeUUID = LocalPlayerInfo.ID,
                                gimmickID = targetGimmick.gimmickUID,
                                gimmickKey = (byte)targetGimmick.gimmickType,
                                state = 3, // 밀기/타격 프로토콜
                                targetPos = new P_PacketVector3 { x = transform.position.x, y = transform.position.y, z = transform.position.z },
                                param = force
                            };

                            Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
                        }
                    }
                }
            }
            Debug.Log($"<color=red>[Bomb]</color> 호스트 폭발 판정 완료. {hitGimmickIDs.Count}개 타격");
        }

        Destroy(gameObject);
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        //유저가 폭탄을 공격해서 바로 터트리는 처리
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);   
    }
}