using UnityEngine;
using System.Collections.Generic;
using System;

public enum GimmickKey : byte
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
    Count = 11
}


[Serializable]
public class GimmickProperty
{
    public GimmickKey key;
    public float value;
}

public class GimmickInfo : MonoBehaviour
{
    [Header("기믹 데이터")]
    public int gimmick_id;
    public string gimmick_type = "MagneticPlatform";

    [Header("기믹 세부 수치")]
    public List<GimmickProperty> properties = new List<GimmickProperty>();
}