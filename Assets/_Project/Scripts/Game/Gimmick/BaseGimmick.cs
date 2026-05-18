using UnityEngine;

public enum eGimmickState : byte
{
    Off_Destroy = 0, // 꺼짐, 부서짐, 비활성화, 닫힘
    On_Activate = 1, // 켜짐, 작동, 활성화, 열림
    Sync = 2,        // 지속적인 물리/좌표 동기화 (시소, 이동 플랫폼 등)
    Restore = 3,     // 다시 초기 위치로 돌아가야 하는 상태
    TriggerMove = 4
}

public enum eGimmickType
{
    NONE = 0,
    Movable,    // 밀거나 당길 수 있는 기믹
    Breakable   // 부서지기만 하는 기믹 (항아리, 벽 등)
}

public abstract class BaseGimmick : MonoBehaviour
{
    [Header("Base Gimmick Info")]
    public int gimmickUID; // 모든 기믹이 공통으로 가지는 ID
    public P_PacketVector3 v;

    public eGimmickType gimmickType;

    public GimmickStat stat { get; protected set; }

    protected virtual void Awake()
    {
        stat = GetComponent<GimmickStat>();
        GimmickInfo info = GetComponent<GimmickInfo>();
        if (info != null)
        {
            gimmickUID = info.gimmick_id;
        }
    }

    public abstract void Execute(P_GimmickInteractNtf ntf);
}