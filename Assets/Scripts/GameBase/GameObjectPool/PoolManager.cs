using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//池子对象
public class PoolData
{
    public GameObject fatherObj;
    public List<GameObject> poolList;

    public PoolData(GameObject obj, GameObject poolObj)
    {
        string poolName = obj.name + "Pool";
        fatherObj = new GameObject(poolName);
        fatherObj.transform.parent = poolObj.transform;

        poolList = new List<GameObject>() {};
        PushObj(obj);
    }

    public GameObject GetPool()
    {
        GameObject obj = null;
        obj = poolList[0];
        poolList.RemoveAt(0);
        obj.SetActive(true);
        obj.transform.parent = null;

        return obj;
    }

    public void PushObj(GameObject obj)
    {
        poolList.Add(obj);
        obj.transform.parent = fatherObj.transform;
        obj.gameObject.SetActive(false);
    }
}

// 对象池
public class PoolManager : SingletonBase<PoolManager>
{
    /// <summary>
    /// 缓存池
    /// </summary>
    public Dictionary<string,PoolData> poolDic = new Dictionary<string,PoolData>();
    private GameObject poolObj;

    /// <summary>
    /// 从池子中取出
    /// </summary>
    /// <param name="name">取出物品的名字</param>
    /// <returns></returns>
    public GameObject GetObj(GameObject prefab)
    {
        GameObject obj = null;
        if(poolDic.ContainsKey(prefab.name) && poolDic[prefab.name].poolList.Count > 0)
        {
            obj = poolDic[prefab.name].GetPool();
        }
        else
        {
            obj = GameObject.Instantiate(prefab);
            string tempName = prefab.name.Replace("Clone",string.Empty);
            obj.name = tempName;
        }

        return obj;
    }

    /// <summary>
    /// 存入池子
    /// </summary>
    /// <param name="name"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    public GameObject PushObj(string name,GameObject obj)
    {
        if (poolObj == null)
        {
            poolObj = new GameObject("Pool");
        }

        if(poolDic.ContainsKey(name))
        {
            poolDic[name].PushObj(obj);
        }
        else
        {
            poolDic.Add(name, new PoolData(obj,poolObj));
        }

        return obj;
    }

    /// <summary>
    /// 清空缓存池
    /// </summary>
    public void Clear()
    {
        poolDic.Clear();
        poolObj = null;
    }
}
