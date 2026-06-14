using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct TargetGimmickInfo
{
    public int gimmickID;
    public eGimmickKey gimmickKey;
}

public class GimmickTrigger : MonoBehaviour
{
    [Header("타겟 기믹 리스트")]
    public List<TargetGimmickInfo> targetGimmicks = new List<TargetGimmickInfo>();

    [Header("트리거 설정")]
    public bool isOneTimeUse = false;
    private bool isTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        //   //Debug.Log($"<color=yellow>[GimmickTrigger]</color> OnTriggerEnter 발동 밟은 객체: {other.name}");
        ProcessInteract(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        //   //Debug.Log($"<color=yellow>[GimmickTrigger]</color> OnCollisionEnter 발동 밟은 객체: {collision.gameObject.name}");
        ProcessInteract(collision.gameObject);
    }

    public void ProcessInteract(GameObject otherObj)
    {
        if (isOneTimeUse && isTriggered)
        {
            return;
        }

        if (otherObj.CompareTag("Player"))
        {
            PlayerActor actor = otherObj.GetComponent<PlayerActor>();

            if (actor != null)
            {
                if (actor.IsLocal)
                {
                    isTriggered = true;

                    if (Client.IS_SERVER_PLAY || GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
                    {
                        foreach (var target in targetGimmicks)
                        {
                            P_GimmickInteractReq req = new P_GimmickInteractReq
                            {
                                activeUUID = LocalPlayerInfo.ID,
                                gimmickID = target.gimmickID,
                                gimmickKey = (byte)target.gimmickKey,
                                state = (byte)eGimmickState.On_Activate,
                                targetPos = new P_PacketVector3(),
                                param = 0f,
                                timestamp = NetworkTimeManager.Instance.GetServerTime()
                            };
                            Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
                            //  //Debug.Log($"<color=cyan>[GimmickTrigger]</color> {target.gimmickID}번 기믹 REQ 패킷");
                        }
                    }
                }
            }
        }
    }
}