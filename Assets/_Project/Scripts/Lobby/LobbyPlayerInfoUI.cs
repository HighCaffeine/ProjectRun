using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyPlayerInfoUI : MonoBehaviour
{
    [Header("UI Elements")]
    public RawImage characterRenderView;
    public TextMeshProUGUI nicknameText;
    public TMP_InputField nicknameChangeInput;

    public Button changeCharacterButton;

    [Header("3D Character Models (카메라 앞에 배치된 객체들)")]
    //0 여캐 1 남캐
    public GameObject[] characterModels;

    private int currentCharID = 0;

    void Start()
    {
        nicknameText.text = LocalPlayerInfo.Name;

        if (changeCharacterButton != null)
        {
            changeCharacterButton.onClick.AddListener(OnClickChangeCharacter);
        }

        currentCharID = LocalPlayerInfo.CharacterID;
        UpdateCharacterModel();
    }

    public void SetInteractable(bool isEnable)
    {
        if (changeCharacterButton != null) changeCharacterButton.interactable = isEnable;
    }

    public void ForceSetCharacter(int charID, bool isLocal = true)
{
    if (isLocal)
    {
        if (currentCharID >= 0 && currentCharID < characterModels.Length && characterModels[currentCharID] != null)
        {
            characterModels[currentCharID].SetActive(false);
        }
        
        currentCharID = charID;
        LocalPlayerInfo.CharacterID = currentCharID;
        UpdateCharacterModel();
    }
    else
    {
        for (int i = 0; i < characterModels.Length; i++)
        {
            if (characterModels[i] != null)
            {
                characterModels[i].SetActive(false);
            }
        }
        
        if (charID >= 0 && charID < characterModels.Length && characterModels[charID] != null)
        {
            characterModels[charID].SetActive(true);
        }
    }
}

    public void OnClickChangeCharacter()
    {
        characterModels[currentCharID].SetActive(false);
        currentCharID = (currentCharID + 1) % characterModels.Length;
        LocalPlayerInfo.CharacterID = currentCharID;
        UpdateCharacterModel();

        var lobbyMgr = FindObjectOfType<LobbyRoomManager>();
        if (lobbyMgr != null /*&& lobbyMgr.isInsideRoom*/)
        {
            P_RoomCharSelectReq req = new P_RoomCharSelectReq { charID = currentCharID };
            Client.TCP.SendPacket2(E_PACKET.ROOM_CHAR_SELECT_REQ, req);

            lobbyMgr.RequestRoomList();
        }
    }

    // public void OnClickChangeCharacter()
    // {
    //     if (characterModels.Length == 0) return;

    //     characterModels[currentCharID].SetActive(false);    //현재 캐 끄기
    //     currentCharID = (currentCharID + 1) % characterModels.Length;

    //     // 새 캐릭터 켜기
    //     UpdateCharacterModel();
    // }

    private void UpdateCharacterModel()
    {
        Debug.Log($"<color=red>[DEBUG] 모델 업데이트 호출됨! 호출위치 스택 확인: </color>\n{System.Environment.StackTrace}");
        for (int i = 0; i < characterModels.Length; i++)
        {
            if (characterModels[i] != null)
            {
                characterModels[i].SetActive(i == currentCharID);
            }
        }
    }

    //이거 이제 안씀
    public void OnClickChangeNickname()
    {
        if (nicknameChangeInput == null || string.IsNullOrEmpty(nicknameChangeInput.text)) return;

        string newName = nicknameChangeInput.text;
        LocalPlayerInfo.Name = newName;
        nicknameText.text = newName;

        Debug.Log($"[Lobby] 닉네임이 {newName}(으)로 변경됨!");
    }
}