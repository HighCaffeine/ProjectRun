using UnityEngine;

public class GrassController : MonoBehaviour
{
    public float radius = 2f;
    public float minMoveSpeed = 0.1f;

    Vector3 lastPos;
    Vector3 currentDir;

    void Start()
    {
        lastPos = transform.position;
    }

    void Update()
    {
        Vector3 velocity =
            (transform.position - lastPos) / Time.deltaTime;

        float speed = velocity.magnitude;

        if (speed > minMoveSpeed)
        {
            currentDir = Vector3.Lerp(
                currentDir,
                velocity.normalized,
                Time.deltaTime * 10f);
        }

        Shader.SetGlobalVector("_PlayerPos", transform.position);
        Shader.SetGlobalVector("_MoveDir", currentDir);
        Shader.SetGlobalFloat("_PushRadius", radius);

        lastPos = transform.position;
    }
}