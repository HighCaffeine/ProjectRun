using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System;

public enum Extension
{
    txt,
    xml,
    json,
    mp3,
    wave,
    mp4,
    avi,
    mov,
}

public class JsonManager : GenericSingleton<JsonManager>
{
    [SerializeField] private string path;
    public Extension extension = Extension.json;
    public string fileName;

    private new void Awake()
    {
        base.Awake();

#if UNITY_EDITOR || UNITY_EDITOR || UNITY_IOS
        path = Path.Combine(Application.streamingAssetsPath, fileName + "." + extension.ToString());
#else 
        path = Path.Combine(Application.persistentDataPath, fileName + "." + extension.ToString());
#endif

        GetData();
    }

    private void GetData()
    {
        string jsonString = string.Empty;
        
        path = Path.Combine(Application.persistentDataPath, fileName + "." + extension.ToString());
        string beforePath = Path.Combine(Application.streamingAssetsPath, fileName + "." + extension.ToString());

        if (!File.Exists(path))
        {
            System.IO.File.Copy(beforePath, path);
        }
        
#if UNITY_EDITOR || UNITY_IOS || UNITY_STANDALONE_WIN || UNITY_ANDROID

        jsonString = File.ReadAllText(path);

#else

        WWW www = new WWW(path);

        if (www.isDone)
        {
            Debug.Log("Downloaded");
        }

        yield return www;

        if (www.error != null)
        {
            throw new Exception("www downloaded : " + www.error);
        }

        jsonString = www.text;
#endif
        //data = JsonConvert.DeserializeObject<DataTable>(jsonString);
    }
}
