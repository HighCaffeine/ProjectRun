using UnityEngine;
using System.Collections.Generic;
using System;

public enum GimmickKey
{
    NONE,
    BreakableWall,
	Button,
	MovableObject,
	DrawBridge,
	SeeSaw,
	
	// 
	PresurePlate,
	DisappearPlate,
	MovePlate,
	
	//환경
	Wind,
    Count,
}


[Serializable]
public class GimmickProperty
{
    public GimmickKey key;  //magnetic_force 기믹 속성용
    public float value; //가중치
}

public class GimmickInfo : MonoBehaviour
{
    [Header("기믹 데이터")]
    public int gimmick_id;
    public string gimmick_type = "MagneticPlatform";

    [Header("기믹 세부 수치")]
    public List<GimmickProperty> properties = new List<GimmickProperty>();
}