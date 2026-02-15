#pragma once

#include "Packet.h"

#include <string>

class Actor
{
public:
	enum class DOMAIN_STATE 
	{
		NONE = 0,
		LOGIN = 1,
		ROOM = 2
	};

	enum class ModifierType { ADD, MULTIPLY, FLAT };

	struct SpeedModifier 
	{
		ModifierType type;
		float value;
		float duration; // 남은 시간 (초)
		int sourceID;   // 중복 적용 방지용 (예: 같은 스킬 슬로우 중첩 불가)
	};


	Actor() = default;
	~Actor() = default;

	void Init(const INT32 index)
	{
		mIndex = index;
		position.x = 5.0f;
		position.y = 0.0f;
		position.z = 5.0f;

		serverPos.x = 5.0f;
		serverPos.y = 0.0f;
		serverPos.z = 5.0f;
	}

	void Clear()
	{
		mRoomIndex = -1;
		mUserID = "";
		mIsConfirm = false;
		mCurDomainState = DOMAIN_STATE::NONE;
	}

	int SetLogin(const char* userID_)
	{
		mCurDomainState = DOMAIN_STATE::LOGIN;
		mUserID = userID_;

		return 0;
	}
		
	void EnterRoom(INT32 roomIndex_)
	{
		mRoomIndex = roomIndex_;
		mCurDomainState = DOMAIN_STATE::ROOM;
	}
		
	void SetDomainState(DOMAIN_STATE value_) { mCurDomainState = value_; }

	INT32 GetCurrentRoom() 
	{
		return mRoomIndex;
	}

	INT32 GetNetConnIdx() 
	{
		return mIndex;
	}

	std::string GetUserId() const
	{
		return  mUserID;
	}

	DOMAIN_STATE GetDomainState() 
	{
		return mCurDomainState;
	}

	const UINT32& GetLastInputSeq() const { return lastInputSeq; }
	const bool& GetIsMoving() const { return isMoving; }
	const Vector3& GetPosition() const { return serverPos;	}
	const Quaternion& GetRotation() const { return rotation; }

	//임시 테스트 함수
	Vector3 UpdateMovement(float dx, float dy, Quaternion& rotation_)
	{
		const float SPEED = 20.0f;

		dx *= (dx <= 1.0f);
		dy *= (dy <= 1.0f);

		// same as the client-sided calculation
		Vector3 right = Quaternion_Multiply(rotation_, Vector3_right());
		Vector3 forward = Quaternion_Multiply(rotation_, Vector3_forward());
		Vector3 mx = Vector3_Multiply(right, dx);
		Vector3 my = Vector3_Multiply(forward, dy);
		Vector3 motion = Vector3_Addition(mx, my);
		motion = Vector3_Multiply(motion, FIXED_DELTA_TIME * SPEED);

		
		position = Vector3_Addition(position, motion);
		rotation = rotation_;

		return motion;
	}

	
	//마우스 이동 설정
	void SetTarget(Vector3& clickedPos, UINT32 seq) 
	{
		targetPos = clickedPos;
		lastInputSeq = seq;
		isMoving = true;
	}

	// WASD 이동 설정
	void SetInput(float dx, float dy, UINT32 seq) 
	{
		inputX = dx;
		inputZ = dy;
		lastInputSeq = seq;
	}

	//속도 모디파이어
	std::vector<SpeedModifier> mSpeedModifiers;

	void AddSpeedModifier(ModifierType type, float value, float duration) 
	{
		mSpeedModifiers.push_back({ type, value, duration });
	}

	// 매 틱마다 최종 속도 계산
	float GetCurrentSpeed() 
	{
		float addValue = 0.0f;
		float multiplier = 1.0f;

		for (auto& mod : mSpeedModifiers) 
		{
			if (mod.type == ModifierType::ADD) addValue += mod.value;
			else if (mod.type == ModifierType::MULTIPLY) multiplier *= mod.value;
		}

		// (기본 속도 + 합연산) * 곱연산
		return (BASE_SPEED + addValue) * multiplier;
	}

	// 20ms 틱마다 호출
	void UpdateServerPhysics(float dt, bool isMoveMouse = false)
	{
		const float currentSpeed = GetCurrentSpeed();;

		for (auto it = mSpeedModifiers.begin(); it != mSpeedModifiers.end(); ) 
		{
			it->duration -= dt;
			if (it->duration <= 0.0f)
			{
				it = mSpeedModifiers.erase(it);
			}
			else
			{
				++it;
			}
		}

		if (isMoveMouse) 
		{
			if (!isMoving) return;
			Vector3 dir = { targetPos.x - serverPos.x, 0, targetPos.z - serverPos.z };
			float dist = sqrt(dir.x * dir.x + dir.z * dir.z);

			if (dist < 0.1f) 
			{
				serverPos = targetPos;
				isMoving = false;

				return;
			}
			isMoving = true;
			dir.x /= dist; dir.z /= dist;
			serverPos.x += dir.x * currentSpeed * dt;
			serverPos.z += dir.z * currentSpeed * dt;
		}
		else 
		{
			if (inputX == 0 && inputZ == 0)
			{
				isMoving = false;
				return;
			}
			isMoving = true;
			float mag = sqrt(inputX * inputX + inputZ * inputZ);

			if (mag > 0.01f) 
			{
				float dirX = inputX / mag;
				float dirZ = inputZ / mag;
				serverPos.x += dirX * currentSpeed * dt;
				serverPos.z += dirZ * currentSpeed * dt;
			}
		}
	}

private:
#pragma region Move
	bool isMoving = false;
	Vector3 serverPos;   // 서버 확정 현재 위치
	Vector3 targetPos;   // 마우스로 클릭된 최종 목적지

	float inputX = 0, inputZ = 0;

	INT32 lastInputSeq;	//보정용 번호

	//임시 함수용 변수
	// position of the player
	Vector3 position;

	// rotation of the player
	Quaternion rotation;
#pragma endregion

	INT32 mIndex = -1;
	INT32 mRoomIndex = -1;

	std::string mUserID;
	bool mIsConfirm = false;
	std::string mAuthToken;
	
	DOMAIN_STATE mCurDomainState = DOMAIN_STATE::NONE;		

};

