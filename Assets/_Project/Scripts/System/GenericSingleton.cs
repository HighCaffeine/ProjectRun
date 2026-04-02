using UnityEngine;

public class GenericSingleton<T> : MonoBehaviour where T : Component
{
    private static T instance;

    public static T Instance{ get { Init(); return instance; } }

    protected virtual void Awake()
    {
        Init();
    }

    private static void Init()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<T>();

            if (instance == null)
            {
                GameObject obj = new GameObject();

                #if UNITY_EDITOR_64
                #else
                instance = obj.AddComponent<T>();
                #endif 
                obj.name = typeof(T).Name;
                DontDestroyOnLoad(obj);
            }
        }
    }
}
