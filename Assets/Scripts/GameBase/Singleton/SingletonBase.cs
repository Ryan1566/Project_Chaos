using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//���̳�MonoBehaviour�ĵ�������
public class SingletonBase<T> where T : new()
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new T();
            }
            return instance;
        }
    }
}
