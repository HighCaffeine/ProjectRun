using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct TargetGimmickInfo
{
    public int gimmickID;
    public GimmickKey gimmickKey;
}

public class GimmickTrigger : MonoBehaviour
{
    [Header("타겟 기믹 리스트")]
    public List<TargetGimmickInfo> targetGimmicks = new List<TargetGimmickInfo>();

    [Header("트리거 설정")]
    public bool isOneTimeUse = true;
    private bool isTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (isOneTimeUse && isTriggered) return;

        if (other.CompareTag("Player"))
        {
            PlayerActor actor = other.GetComponent<PlayerActor>();

            if (actor != null && actor.IsLocal)
            {
                isTriggered = true;

                if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
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
                            param = 0f
                        };
                        Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
                    }
                }
                else
                {
                    Debug.Log($"[Offline] {targetGimmicks.Count}개의 기믹 작동됨");
                }
            }
        }
    }
}