using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomListItem : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("UI 상단 닉네임, 방제")][SerializeField] private TextMeshProUGUI roomNameText;
    [Tooltip("인원수")][SerializeField] private TextMeshProUGUI playerCountText;
    [Tooltip("핑상태")][SerializeField] private TextMeshProUGUI pingText;
    [SerializeField] private Image[] pingImages;
    [Tooltip("게임상태")][SerializeField] private TextMeshProUGUI statusText;
    [Tooltip("참가버튼")][SerializeField] private Button joinButton;

    [SerializeField] private Image hostPlayerBlockImage;
    [SerializeField] private Image hostPlayerImage;
    [SerializeField] private Image guestPlayerBlockImage;
    [SerializeField] private Image guestPlayerImage;

    [Header("Character Portraits")]
    [SerializeField] private Sprite[] portraitSprites;

    private Image hostSlotBox;
    private Image guestSlotBox;

    private int myRoomNum;
    private int beforePingIndex = -1;

    public void Setup(P_RoomInfo info, LobbyRoomManager manager)
    {
        myRoomNum = info.roomNum;

        // UI 텍스트 갱신
        if (roomNameText != null) roomNameText.text = $"{info.title}";
        if (playerCountText != null) playerCountText.text = $"{info.curUser} / {info.maxUser}";

        bool isPlaying = info.isPlaying == 1;

        if (statusText != null)
        {
            statusText.text = isPlaying ? "<color=#A91929>게임 중</color>" : "<color=#332B28>대기 중</color>";
        }

        if (pingText != null)
        {
            int currentPing = info.hostPing; // 서버와 통신해서 계산된 실제 핑

            //string pingColor = string.Empty;
            int pingIndex = 0;
            if (currentPing <= 50)
            {
                //pingColor = "#00FF00";
                pingIndex = 0;
            }
            else if (currentPing <= 100)
            {
                //pingColor = "#FFFF00";
                pingIndex = 1;
            }
            else
            {
                //pingColor = "#FF0000";
                pingIndex = 2;
            }

            if (beforePingIndex != pingIndex)
            {
                //ColorUtility.TryParseHtmlString(pingColor, out Color imageColor);
                foreach (var obj in pingImages) obj.gameObject.SetActive(false);

                pingImages[pingIndex].gameObject.SetActive(true);
                //pingImages[pingIndex].color = imageColor;
                beforePingIndex = pingIndex;
            }

            //pingText.text = $"<color={pingColor}>{currentPing}ms</color>";
        }

        Debug.Log($"[RoomListItem] roomNum={info.roomNum}, hostCharID={info.hostCharID}, guestCharID={info.guestCharID}");
        if (portraitSprites != null && portraitSprites.Length > 1)
        {
            if (hostPlayerImage != null)
                hostPlayerImage.sprite = portraitSprites[info.hostCharID];

            if (guestPlayerImage != null)
                guestPlayerImage.sprite = portraitSprites[info.guestCharID];
        }

        if (hostPlayerBlockImage != null)
        {
            hostPlayerImage.gameObject.SetActive(true);
            hostPlayerBlockImage.gameObject.SetActive(false);
        }

        if (guestPlayerBlockImage != null)
        {
            if (info.curUser == 1)
            {
                guestPlayerBlockImage.gameObject.SetActive(true);
                guestPlayerImage.gameObject.SetActive(false);
            }
            else if (info.curUser == 2)
            {
                if (info.guestReadyState == 2)
                {
                    guestPlayerImage.gameObject.SetActive(true);
                    guestPlayerBlockImage.gameObject.SetActive(false);
                }
                else
                {
                    guestPlayerImage.gameObject.SetActive(true);
                    guestPlayerBlockImage.gameObject.SetActive(true);
                }
            }
        }

        // if (hostSlotBox != null)
        // {
        //     hostSlotBox.color = Color.green;
        // }

        // // 파티원 슬롯 처리
        // if (guestSlotBox != null)
        // {
        //     if (info.curUser == 1)
        //     {
        //         // 빈 슬롯 상태
        //         guestSlotBox.color = new Color(0.5f, 0.5f, 0.5f, 0.25f);
        //     }
        //     else if (info.curUser == 2)
        //     {
        //         if (info.guestReadyState == 2)
        //         {
        //             // 파티원이 준비를 완료한 상태 -> 초록박스
        //             ColorUtility.TryParseHtmlString("#332B28", out Color imageColor);
        //             guestSlotBox.color = imageColor;
        //         }
        //         else
        //         {
        //             // 파티원이 들어왔으나 준비 안 한 상태 -> 빨간박스 (기본값)
        //             ColorUtility.TryParseHtmlString("#A91929", out Color imageColor);
        //             guestSlotBox.color = imageColor;
        //         }
        //     }
        // }

        if (joinButton != null)
        {
            if (manager.isInsideRoom || isPlaying || info.curUser >= info.maxUser)
            {
                joinButton.interactable = false;
            }
            else
            {
                joinButton.interactable = true;
                joinButton.onClick.RemoveAllListeners();
                string roomTitleStr = info.title;
                joinButton.onClick.AddListener(() => manager.ShowJoinPopup(myRoomNum, roomTitleStr));
            }
        }
    }
}