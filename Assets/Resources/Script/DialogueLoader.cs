using UnityEngine;

public static class DialogueLoader
{
    public static DialogueData Load(string fileName)
    {
        TextAsset json = Resources.Load<TextAsset>("Dialogue/" + fileName);

        if (json == null)
        {
      
            return null;
        }

        DialogueData data = JsonUtility.FromJson<DialogueData>(json.text);

        return data;
    }
}
