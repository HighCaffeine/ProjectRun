using UnityEngine;
using TMPro;
public class NickNameUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI nickName;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        nickName = gameObject.GetComponentInChildren<TextMeshProUGUI>();    
        nickName.text = gameObject.GetComponentInParent<PlayerActor>().gameObject.name;
    }
    void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }
}
