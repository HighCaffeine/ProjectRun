using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController Controller;
    private uint inputSeq = 0;

    private bool IsLocal = false;

    void FixedUpdate()
    {
        if (!IsLocal) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h != 0 || v != 0)
        {
            inputSeq++;

            P_PlayerMovement pkt = new P_PlayerMovement
            {
                userUUID = LocalPlayerInfo.ID,
                inputSeq = inputSeq,
                dx = h,
                dz = v
            };

            Client.UDP.SendPacket2(E_PACKET.PLAYER_MOVEMENT, pkt);
        }
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
