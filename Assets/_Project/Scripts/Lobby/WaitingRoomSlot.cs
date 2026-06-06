using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaitingRoomSlot : MonoBehaviour
{
    public long userUUID = -1;
    public TextMeshProUGUI nameText;
    public GameObject readyImage;
    public GameObject hostImage;

    public Image portraitImage;
    public Sprite[] portraitSprites;

    public void InitEmpty()
    {
        userUUID = -1;
        nameText.text = "비어있음";
        readyImage.SetActive(false);
        hostImage.SetActive(false);
        if (portraitImage != null) portraitImage.gameObject.SetActive(false);
    }

    public void SetUser(long uuid, string userName, int characterID, bool isReady = false, bool isHost = false)
    {
        userUUID = uuid;
        nameText.text = userName;

        readyImage.SetActive(isReady);
        hostImage.SetActive(isHost);

        if (portraitImage != null && characterID >= 0 && characterID < portraitSprites.Length)
        {
            portraitImage.gameObject.SetActive(true);
            portraitImage.sprite = portraitSprites[characterID]; // 0번이면 여캐, 1번이면 남캐
        }
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