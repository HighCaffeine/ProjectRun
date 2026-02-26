#pragma once

#include "..\Detour\Include\DetourNavMesh.h"
#include "..\Detour\Include\DetourNavMeshQuery.h"
#include "unity.h"

#include <string>

using namespace std;

class NavMeshManager
{
private:
	NavMeshManager() = default;
	~NavMeshManager();

	dtNavMesh* m_NavMesh = nullptr;
	dtNavMeshQuery* m_NavQuery = nullptr;
	dtQueryFilter m_Filter;

	//탐색 범위
	const float m_Extents[3] = { 2.0f, 4.0f, 2.0f };
public:
	static NavMeshManager* GetInstance()
	{
		static NavMeshManager instance;
		return &instance;
	}

	bool Init(const string& binFilePath);	//초기화
	//벽 충돌 및 슬라이딩 처리 realPos에 최종 좌표 
	bool GetValidMovePosition(const Vector3& startPos, const Vector3& targetPos, Vector3& realPos);
	bool IsInBush(const Vector3& pos);	//부쉬 판정


};