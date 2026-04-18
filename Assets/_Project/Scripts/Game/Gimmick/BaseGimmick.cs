using UnityEngine;

public enum eGimmickState : byte
{
    Off_Destroy = 0, // 꺼짐, 부서짐, 비활성화, 닫힘
    On_Activate = 1, // 켜짐, 작동, 활성화, 열림
    Sync = 2         // 지속적인 물리/좌표 동기화 (시소, 이동 플랫폼 등)
}

public abstract class BaseGimmick : MonoBehaviour
{
    [Header("Base Gimmick Info")]
    public int gimmickUID; // 모든 기믹이 공통으로 가지는 ID
    public P_PacketVector3 v;

    public abstract void Execute(P_GimmickInteractNtf ntf);
}