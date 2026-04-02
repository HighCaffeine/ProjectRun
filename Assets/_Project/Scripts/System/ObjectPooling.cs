using System.Collections.Generic;
using UnityEngine;

public interface OnReturnPool<PoolType>
{
    void Init(OnReturnPoolEvent<PoolType> onReturnPoolEvent);
}


public delegate void OnReturnPoolEvent<PoolType>(PoolType type);

public class ObjectPooling<ManagerType, PoolType> : GenericSingleton<ManagerType>
    where PoolType : MonoBehaviour, OnReturnPool<PoolType>
    where ManagerType : MonoBehaviour
{
    [SerializeField] protected Transform storageParent;
    [SerializeField] PoolType prefab;
    [SerializeField] private int poolCount;

    [SerializeField] private Stack<PoolType> pool;

    protected new void Awake()
    {
        base.Awake();

        pool = new Stack<PoolType>();

        GenerateStorageParent();

        Pooling();

    }

    protected void Start()
    {
        storageParent.localPosition = Vector3.zero;
    }

    protected virtual void GenerateStorageParent()
    {
        if (storageParent == null)
        {
            storageParent = new GameObject("StorageParent").transform;
            storageParent.SetParent(transform);
        }
        //storageParent.gameObject.SetActive(false);
    }

    protected void PooledObjectSetParent(Transform transform)
    {
        transform.SetParent(storageParent);
    }

    private void Pooling()
    {
        for (int i = 0; i < poolCount; i++)
        {
            CreateObject();
        }
    }

    private PoolType CreateObject()
    {
        PoolType newObj = Instantiate(prefab, storageParent);

        newObj.Init(OnReturnUp);

        pool.Push(newObj);

        newObj.gameObject.SetActive(false);

        return newObj;
    }

    public PoolType GetPool()
    {
        PoolType obj;

        if (pool.Count <= 0)
        {
            obj = CreateObject();
            obj.gameObject.SetActive(true);

            return obj;
        }

        obj = pool.Pop();
        obj.gameObject.SetActive(true);

        return obj;
    }

    private void OnReturnUp(PoolType poolObject)
    {
        poolObject.transform.SetParent(storageParent);

        pool.Push(poolObject);

        poolObject.gameObject.SetActive(false);
    }
}