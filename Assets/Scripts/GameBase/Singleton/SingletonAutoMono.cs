using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//�̳���MonoBehaviour�ĵ������࣬��֤��λ����
//�̳������Զ������ĵ���ģʽ���� ����Ҫ�ֶ�ȥ�϶� ����apiȥ����
public class SingletonAutoMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T GetInstance()
    {
        if (instance == null)
        {
            GameObject obj = new GameObject();
            obj.name = typeof(T).ToString();

            GameObject.DontDestroyOnLoad(obj);//���������Ƴ�

            instance = obj.AddComponent<T>();
        }
        return instance;
    }
}
