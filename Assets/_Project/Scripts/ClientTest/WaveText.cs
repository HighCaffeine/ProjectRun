using TMPro;
using UnityEngine;

public class WaveText : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    [SerializeField] private float waveHeight = 25f;
    [SerializeField] private float waveDuration = 0.2f;
    [SerializeField] private float charDelay = 0.05f;

    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] originalVertices;

    private float waveStartTime;
    private bool isPlaying;

    private void Start()
    {
        text.ForceMeshUpdate();

        mesh = text.mesh;

        vertices = mesh.vertices;
        originalVertices = mesh.vertices.Clone() as Vector3[];
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayWave();
        }

        if (!isPlaying)
            return;

        AnimateWave();
    }

    public void PlayWave()
    {
        waveStartTime = Time.time;
        isPlaying = true;
    }

    private void AnimateWave()
    {
        text.ForceMeshUpdate();

        vertices = mesh.vertices;

        System.Array.Copy(originalVertices, vertices, originalVertices.Length);

        TMP_TextInfo textInfo = text.textInfo;

        bool finished = true;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible)
                continue;

            float charTime =
                Time.time - waveStartTime - (i * charDelay);

            if (charTime < 0)
                continue;

            if (charTime < waveDuration)
            {
                finished = false;

                float t = charTime / waveDuration;

                float offsetY =
                    Mathf.Sin(t * Mathf.PI) * waveHeight;

                int index = charInfo.vertexIndex;

                Vector3 offset = new Vector3(0, offsetY, 0);

                vertices[index + 0] += offset;
                vertices[index + 1] += offset;
                vertices[index + 2] += offset;
                vertices[index + 3] += offset;
            }
        }

        mesh.vertices = vertices;
        text.canvasRenderer.SetMesh(mesh);

        if (finished)
            isPlaying = false;
    }
}