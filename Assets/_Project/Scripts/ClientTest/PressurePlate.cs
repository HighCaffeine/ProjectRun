using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [SerializeField]
    private GameObject wall; // 벽 오브젝트를 연결할 변수
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("플레이어가 압력판에 들어왔습니다.");
            OpenWall(); 
        }
    }

    void OpenWall()
    {
        Debug.Log("벽이 열렸습니다.");
        wall.SetActive(false); // 벽 오브젝트를 비활성화하여 열리는 효과를 줍니다.
    }
}
