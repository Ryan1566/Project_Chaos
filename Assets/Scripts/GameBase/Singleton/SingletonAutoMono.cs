using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//继承了MonoBehaviour的单例基类，保证了位移性
//继承这种自动创建的单例模式基类 不需要手动去拖动 或者api去加了
public class SingletonAutoMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T GetInstance()
    {
        if (instance == null)
        {
            GameObject obj = new GameObject();
            obj.name = typeof(T).ToString();

            GameObject.DontDestroyOnLoad(obj);//过场景不移除

            instance = obj.AddComponent<T>();
        }
        return instance;
    }
}
