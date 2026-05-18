using UnityEngine;
using System.Collections.Generic;
using System;

public enum eGimmickKey : byte
{
    NONE = 0,
    BreakableWall = 1,
    Button = 2,
    MovableObject = 3,
    Bridge = 4,
    SeeSaw = 5,
    PresurePlate = 6,
    FallingPlatform = 7,
    MovePlatform = 8,
    Wind = 9,
    NextZone = 10,
    Checkpoint = 11,      
    BreakableObj = 12,   
    Bomb = 13,
    Count = 14            
}

public enum eGimmickPropKey
{
    HP = 0,             // 기믹 체력 (0 이 되면 파괴)
    Weight = 1,         // 기믹 무게 (0:고정, 1:일반, 2:무거움)
    IsBombOnly = 2,     // 1.0f 면 폭탄으로만 파괴 가능
    MoveSpeed = 3,      // 이동 플랫폼 등의 속도 수치
    WaitTime = 4,       // 목표지점 도달 후 대기 시간 수치
    ActivationType = 5, // 0 상시 작동, 1 밟은 때 작동
    SpawnGimmickKey = 6  // eGimmickKey에 해당하는 기믹이 스폰됨 
}

[Serializable]
public class GimmickProperty
{
    public eGimmickPropKey key;
    public float value;
}

public class GimmickInfo : MonoBehaviour
{
    [Header("기믹 데이터")]
    public int gimmick_id;
    public eGimmickKey gimmick_type;

    [Header("기믹 세부 수치")]
    public List<GimmickProperty> properties = new List<GimmickProperty>();
}