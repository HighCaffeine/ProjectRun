using System;
using UnityEngine;
using System.Collections;

public class PlayerActor : Actor
{
    const float CAMERA_SHAKE = 1.0f;

    [Header("밀치기 힘 배율")][SerializeField] private float pushMulti = 1.0f;
    public float PushMulti => pushMulti;

    public float moveSpeed = 5.0f;
    private bool wasMoving = false;
    public LayerMask targetLayer;
    [SerializeField] private Transform playerPivot;
    private CharacterController controller;
    private Vector3 horizontalMove;

    [SerializeField] private CameraShakeEffect cameraShake;

    //동기화
    public uint inputSeq = 0;
    public bool IsLocal = false;
    public float sendTimer = 0f;
    public const float sendInterval = 0.02f;
    private P_PacketVector3 curPos;
    private P_PacketQuaternion curRot;

    [Header("Visual Effects")]
    public TrailRenderer trailRenderer;
    public ParticleSystem travelSparkParticle;
    public ParticleSystem[] brakeParticle;

    [Header("Gravity Settings")]
    public float gravity = -20f;       // 기본 중력 값
    private float verticalVelocity;     // 현재 수직 속도
    private float maxVerticalVelocity = -30f; // 최대 낙하 속도 제한

    // 상태머신 값
    public float h { private set; get; }
    public float v { private set; get; }


    public enum ActionType : byte { PUSH = 0, PULL = 1, }

    public Vector3 GetForward() { return playerPivot.forward; }
    public void SetController(CharacterController cc) => this.controller = cc;
    public void SetControllerActive(bool isActive) { if (this.controller != null) this.controller.enabled = isActive; }
    public void SetPlayerPivot(Transform pivot) => this.playerPivot = pivot;

    [SerializeField]
    public DashCameraEffect dashCameraEffect;

    protected override void Start()
    {
        controller = GetComponent<CharacterController>();
        if (IsLocal)
        {
            sm.ChangeState(new IdleState(this));
        }
    }

    void Update()
    {
        if (IsLocal)
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
        }

        ApplyGravity();
        horizontalMove = Vector3.zero;

        if (sm != null)
        {
            sm.Update();
        }

        if (controller != null && controller.enabled)
        {
            Vector3 finalMove = horizontalMove + (Vector3.up * verticalVelocity);
            controller.Move(finalMove * Time.deltaTime);
        }
    }

    public void Move(Vector3 dir, float speed)
    {
        horizontalMove += dir * speed;
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    //마우스 방향 보기
    public void LookAtMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, transform.position);

        if (ground.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            Vector3 dir = hit - transform.position;
            dir.y = 0; // 수평 회전만

            if (playerPivot != null)
            {
                playerPivot.rotation = Quaternion.LookRotation(dir);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }

    public void LookAtDirection(Vector3 dir)
    {
        if (playerPivot != null)
        {
            playerPivot.rotation = Quaternion.LookRotation(dir);
        }
        else
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
    public void PlayBrakeParticles()
    {
        if (brakeParticle == null) return;
        foreach (ParticleSystem p in brakeParticle)
        {
            if (p != null) p.Play();
        }
    }
    public void SetVerticalVelocity(float velocity) => verticalVelocity = velocity;

    public IEnumerator HitStopRoutine(float duration = 0.05f)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
    }

    public void PlayTravelSpark(ActionType actionType)
    {
        if (travelSparkParticle == null) return;

        Transform sparkTransform = travelSparkParticle.transform;

        if (actionType == ActionType.PULL)
        {
            sparkTransform.localRotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            sparkTransform.localRotation = Quaternion.identity;
        }

        travelSparkParticle.Play();
    }

    public void StopTravelSpark()
    {
        if (travelSparkParticle != null && travelSparkParticle.isPlaying)
        {
            travelSparkParticle.Stop();
        }
    }

    public void ShakeCamera() { CameraManager.Instance.PlayEffect(new CameraShakeEffect(CAMERA_SHAKE, CAMERA_SHAKE, 0.3f)); }//카메라 쉐이크 값/값/지속시간

    // 상태 머신이 호출, 확정 좌표 패킷 전송
    public void SendMovePacket(float axisH, float axisV)
    {
        curPos.Set(transform.position);
        curRot.Set(transform.rotation);

        P_PlayerMovement pkt = new P_PlayerMovement
        {
            userUUID = LocalPlayerInfo.ID,
            inputSeq = ++inputSeq,
            currentPos = curPos,
            currentRot = curRot,
            axisH = axisH,
            axisV = axisV
        };
        Client.UDP.SendPacket2(E_PACKET.PLAYER_MOVEMENT, pkt);
    }

    public void SendStateChange(eState stateCode, Vector3 dir = default, float param = 0f)
    {
        if (!Client.IS_SERVER_PLAY || !IsLocal) return;

        P_PlayerStatusNtf pkt = new P_PlayerStatusNtf
        {
            userUUID = LocalPlayerInfo.ID,
            newState = (byte)stateCode,
            targetDir = new P_PacketVector3 { x = dir.x, y = dir.y, z = dir.z },
            powerOrTime = param
        };

        Client.TCP.SendPacket2(E_PACKET.PLAYER_STATUS_NTF, pkt);
    }
}