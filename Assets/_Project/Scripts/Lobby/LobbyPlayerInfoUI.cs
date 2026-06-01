using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyPlayerInfoUI : MonoBehaviour
{
    [Header("UI Elements")]
    public RawImage characterRenderView;
    public TextMeshProUGUI nicknameText;
    public TMP_InputField nicknameChangeInput;

    [Header("3D Character Models (카메라 앞에 배치된 객체들)")]
    //0 여캐 1 남캐
    public GameObject[] characterModels;

    private int currentCharID = 0;

    void Start()
    {
        nicknameText.text = LocalPlayerInfo.Name;

        // 시작할 때 첫 번째 캐릭터만 켜고 나머지는 다 끔
        UpdateCharacterModel();
    }

    public void OnClickChangeCharacter()
    {
        if (characterModels.Length == 0) return;

        characterModels[currentCharID].SetActive(false);    //현재 캐 끄기
        currentCharID = (currentCharID + 1) % characterModels.Length;

        // 새 캐릭터 켜기
        UpdateCharacterModel();
    }

    private void UpdateCharacterModel()
    {
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