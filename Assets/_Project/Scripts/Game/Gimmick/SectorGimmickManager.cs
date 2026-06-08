using System.Collections.Generic;
using UnityEngine;

public class SectorGimmickManager : MonoBehaviour
{
    [Header("섹터 정보")]
    public int sectorIndex;

    [Header("관리 중인 기믹 목록")]
    public List<BaseGimmick> myGimmicks = new List<BaseGimmick>();

    private void Awake()
    {
        myGimmicks = new List<BaseGimmick>(GetComponentsInChildren<BaseGimmick>(true));
    }

    public void RequestSectorReset()
    {
        if (!GameManager.Instance.isHost) return;

        P_GimmickBulkResetReq req = new P_GimmickBulkResetReq();
        req.count = Mathf.Min(myGimmicks.Count, 20);
        req.gimmickIDs = new int[20];

        for (int i = 0; i < req.count; i++)
        {
            req.gimmickIDs[i] = myGimmicks[i].gimmickUID;
        }

        Client.TCP.SendPacket2(E_PACKET.GIMMICK_BULK_RESET_REQ, req);
      //  Debug.Log($"[SectorManager] 섹터 {sectorIndex}의 기믹 {req.count}개 초기화 패킷 서버로 전송");
    }
}