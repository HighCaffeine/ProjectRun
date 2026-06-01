using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaitingRoomSlot : MonoBehaviour
{
    public long userUUID = -1;
    public TextMeshProUGUI nameText;
    public GameObject readyImage; // 레디 했을 때 켜질 V체크 이미지
    public GameObject hostImage;  // 방장일 때 켜질 왕관 이미지

    public void InitEmpty()
    {
        userUUID = -1;
        nameText.text = "비어있음";
        readyImage.SetActive(false);
        hostImage.SetActive(false);
    }

    public void SetUser(long uuid, string userName)
    {
        userUUID = uuid;
        nameText.text = userName;
        readyImage.SetActive(false); // 처음 들어오면 무조건 레디 해제 상태
    }

    public void SetReady(bool isReady)
    {
        readyImage.SetActive(isReady);
    }

    public void SetHost(bool isHost)
    {
        hostImage.SetActive(isHost);
    }
}