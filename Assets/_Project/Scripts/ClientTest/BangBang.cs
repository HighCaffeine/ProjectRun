using UnityEngine; 
public class BangBang : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
[SerializeField]
    private float speed = 90f;

    private void Update()
    {
        transform.Rotate(Vector3.up, speed * Time.deltaTime);
    }
}
