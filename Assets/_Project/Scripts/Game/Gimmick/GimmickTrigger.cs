using UnityEngine;

public class GimmickTrigger : MonoBehaviour
{
    public int targetGimmickID;
    private bool isTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!isTriggered && other.CompareTag("Player"))
        {
            PlayerActor actor = other.GetComponent<PlayerActor>();

            // 내 캐릭터가 밟았을 때만 서버로
            if (actor != null && actor.IsLocal)
            {
                isTriggered = true;

                P_GimmickInteractReq req = new P_GimmickInteractReq
                {
                    gimmickID = targetGimmickID,
                    state = 1, // 작동
                    targetPos = new P_PacketVector3(),
                    param = 0f
                };
                Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
            }
        }
    }
}