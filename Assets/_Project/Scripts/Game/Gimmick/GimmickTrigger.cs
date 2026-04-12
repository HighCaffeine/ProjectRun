using UnityEngine;

public class GimmickTrigger : MonoBehaviour
{
    [Header("타겟 기믹 설정")]
    public int targetGimmickID;            // 작동시킬 대상의 UID
    public eGimmickKey targetGimmickKey;   // 작동시킬 대상의 종류

    [Header("트리거 설정")]
    public bool isOneTimeUse = true;       // 일회성 버튼인가? (false면 밟을 때마다 작동)
    private bool isTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (isOneTimeUse && isTriggered) return;

        if (other.CompareTag("Player"))
        {
            PlayerActor actor = other.GetComponent<PlayerActor>();

            // 내 캐릭터(로컬)가 밟았을 때만 서버로 발송
            if (actor != null && actor.IsLocal)
            {
                isTriggered = true;

                if (GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
                {
                    P_GimmickInteractReq req = new P_GimmickInteractReq
                    {
                        activeUUID = LocalPlayerInfo.ID,
                        gimmickID = targetGimmickID,
                        gimmickKey = (byte)targetGimmickKey,
                        state = (byte)eGimmickState.On_Activate,
                        targetPos = new P_PacketVector3(),
                        param = 0f
                    };
                    Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
                }
                else
                {
                    // 오프라인 테스트용 (옵션: 필요 시 추가)
                    Debug.Log($"[Offline] {targetGimmickKey}({targetGimmickID}) Triggered");
                }
            }
        }
    }
}