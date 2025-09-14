using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;

//�ṩһ��MonoController����Ľӿ�
public class MonoManager : SingletonBase<MonoManager>
{
    private MonoController controller;

    public MonoManager()
    {
        //��֤MonoController�����Ψһ��
        GameObject obj = new GameObject("MonoController");
        obj.AddComponent<MonoController>();
    }

    /// <summary>
    /// �ṩ���ⲿ �������֡�����¼�����
    /// </summary>
    /// <param name="func"></param>
    public void AddUpdateListener(UnityAction func)
    {
        controller.AddUpdateListener(func);
    }

    /// <summary>
    /// �ṩ���ⲿ �����Ƴ�֡�����¼�����
    /// </summary>
    /// <param name="func"></param>
    public void RemoveUpdateListener(UnityAction func)
    {
        controller.RemoveUpdateListener(func);
    }

    public Coroutine StartCoroutine(IEnumerator routine)
    {
        return controller.StartCoroutine(routine);
    }

    public Coroutine StartCoroutine(string methodName, [DefaultValue("null")] object value)
    {
        return controller.StartCoroutine(methodName, value);
    }

    public Coroutine StartCoroutine(string methodName)
    {
        return controller.StartCoroutine(methodName);
    }
}
