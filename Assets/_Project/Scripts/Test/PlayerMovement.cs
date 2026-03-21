using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController Controller;
    private uint inputSeq = 0;

    public bool IsLocal = false;
    public float moveSpeed = 5.0f;
    private bool wasMoving = false;

    public Transform playerPivot;

    private float sendTimer = 0f;
    private float sendInterval = 0.02f; // 초당 20회 전송 (서버 50Hz 처리에 최적화)


    [Header("Action State")]
    public bool isActionCasting = false;
    private float castTimer = 0.0f;
    private const float CAST_DURATION = 0.5f;

    private enum ActionType : byte { PUSH = 0, PULL = 1, }

    void Start()
    {
        if (IsLocal)
        {
            SendMovePacket(0, 0);
        }
    }

    void Update()
    {
        if (!IsLocal) return;

        PhysicsAction();
        Move();
    }

    private void PhysicsAction()
    {
        if (isActionCasting)
        {
            castTimer -= Time.deltaTime;
            if (castTimer <= 0.0f)
            {
                isActionCasting = false;
            }

            return;
        }

        if (Input.GetMouseButtonDown(0)) TryAction(ActionType.PUSH);
        else if (Input.GetMouseButtonDown(1)) TryAction(ActionType.PULL);
    }

    private void TryAction(ActionType type)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Player target = hit.collider.GetComponent<Player>();

            if (target != null && target.ID != LocalPlayerInfo.ID)
            {
                p_PlayerActionRequest pkt = new p_PlayerActionRequest
                {
                    actionType = (byte)type,
                    targetUUID = (int)target.ID
                };

                Client.TCP.SendPacket2(E_PACKET.PLAYER_ACTION_REQUEST, pkt);
                isActionCasting = true;
                castTimer = CAST_DURATION;

                SendMovePacket(0, 0);//밀 때 안미끄러지게 정지 패킷 전송
                Debug.Log($"[Physics] Type : {type}, Target : {target.Name}({target.ID})");
            }
        }
    }

    private void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 isometricForward = new Vector3(1f, 0f, 1f).normalized;
        Vector3 isometricRight = new Vector3(1f, 0f, -1f).normalized;
        Vector3 dir = (isometricForward * v + isometricRight * h).normalized;

        if (Controller != null)
        {
            Controller.Move(dir * moveSpeed * Time.deltaTime);
        }

        bool isMoving = (h != 0 || v != 0);

        sendTimer += Time.deltaTime;

        if (isMoving)
        {
            if (sendTimer >= sendInterval)
            {
                inputSeq++;
                SendMovePacket(dir.x, dir.z);
                sendTimer = 0f;
            }
        }
        else if (wasMoving)
        {
            inputSeq++;
            SendMovePacket(0, 0);
            sendTimer = 0f;
        }

        wasMoving = isMoving;
    }

    void SendMovePacket(float h, float v)
    {
        P_PlayerMovement pkt = new P_PlayerMovement
        {
            userUUID = LocalPlayerInfo.ID,
            inputSeq = inputSeq,
            dx = h,
            dz = v
        };
        Client.UDP.SendPacket2(E_PACKET.PLAYER_MOVEMENT, pkt);
    }

    // //기존 테스트 이동함수
    // public void Move_Test()
    // {
    //     if (Controller != null)
    //     {
    //         P_PlayerMovement playerMovement = default;
    //         playerMovement.player_id = LocalPlayerInfo.ID;
    //         playerMovement.rotation = Controller.transform.rotation;
    //         playerMovement.dx = Input.GetAxis("Horizontal");
    //         playerMovement.dy = Input.GetAxis("Vertical");
    //         if (playerMovement.dx != 0 || playerMovement.dy != 0)
    //         {
    //             //Client.UDP.SendPacket(E_PACKET.PLAYER_MOVEMENT, playerMovement);
    //             Client.TCP.SendPacket2(E_PACKET.PLAYER_MOVEMENT, playerMovement);
    //         }

    //         // client-sided
    //         //Vector3 motion = (transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical")) * 5f;
    //         //Move(motion);
    //     }
    // }


    // //키입력 이동
    // public void Move_WASD()
    // {
    //     //인풋값
    //     float h = Input.GetAxis("Horizontal");
    //     float v = Input.GetAxis("Vertical");

    //     //인풋이 있으면
    //     if (h != 0 || v != 0)
    //     {
    //         //시퀀스 값 추가
    //         currentInputSeq++;

    //         P_PlayerMovement packet = default;
    //         packet.player_id = LocalPlayerInfo.ID;
    //         packet.inputSeq = currentInputSeq;
    //         packet.dx = h;
    //         packet.dy = v;
    //         packet.rotation = transform.rotation;

    //         Client.UDP.SendPacket2(E_PACKET.PLAYER_MOVEMENT, packet);
    //     }
    // }

    // //마우스 입력 이동
    // public void Move()
    // {
    //     if (Input.GetMouseButtonDown(1))
    //     {
    //         Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //         if (Physics.Raycast(ray, out RaycastHit hit))
    //         {
    //             currentInputSeq++;

    //             P_PlayerMovement pkt = new P_PlayerMovement
    //             {
    //                 userUUID = LocalPlayerInfo.ID,
    //                 inputSeq = inputSeq,
    //                 dx = h,
    //                 dz = v
    //             };
    //             // UDP로 서버 전송
    //             Client.UDP.SendPacket2(E_PACKET.PLAYER_MOVEMENT, packet);
    //         }
    //     }
    // }

    public void Move(Vector3 motion)
    {
        Controller.Move(motion);
    }
}
