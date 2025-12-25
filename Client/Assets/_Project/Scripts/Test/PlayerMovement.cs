using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController Controller;
    private uint currentInputSeq = 0;

    public void Move_Test()
    {
        if (Controller != null)
        {
            P_PlayerMovement playerMovement = default;
            playerMovement.player_id = LocalPlayerInfo.ID;
            playerMovement.rotation = Controller.transform.rotation;
            playerMovement.dx = Input.GetAxis("Horizontal");
            playerMovement.dy = Input.GetAxis("Vertical");
            if (playerMovement.dx != 0 || playerMovement.dy != 0)
            {
                //Client.UDP.SendPacket(E_PACKET.PLAYER_MOVEMENT, playerMovement);
                Client.TCP.SendPacket2(E_PACKET.PLAYER_MOVEMENT, playerMovement);
            }

            // client-sided
            //Vector3 motion = (transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical")) * 5f;
            //Move(motion);
        }
    }

    public void Move()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                currentInputSeq++;

                P_PlayerMovement packet = default;
                packet.player_id = LocalPlayerInfo.ID;
                packet.inputSeq = currentInputSeq; // 시퀀스 번호 할당
                packet.targetPos = hit.point;

                // UDP로 서버 전송
                Client.UDP.SendPacket2(E_PACKET.PLAYER_MOVEMENT, packet);
            }
        }
    }

    public void Move(Vector3 motion)
    {
        Controller.Move(motion);
    }
}
