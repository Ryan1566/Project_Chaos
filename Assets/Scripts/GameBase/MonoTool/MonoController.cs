using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//Mono������ �������Mono���������ڡ��¼��Լ�Э��
public class MonoController : MonoBehaviour
{
    public event UnityAction updateEvent;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if(updateEvent != null)
            updateEvent.Invoke();
    }

    /// <summary>
    /// �ṩ���ⲿ �������֡�����¼�����
    /// </summary>
    /// <param name="func"></param>
    public void AddUpdateListener(UnityAction func)
    {
        updateEvent += func;
    }

    /// <summary>
    /// �ṩ���ⲿ �����Ƴ�֡�����¼�����
    /// </summary>
    /// <param name="func"></param>
    public void RemoveUpdateListener(UnityAction func)
    {
        updateEvent -= func;
    }

    public void Clear()
    {
        updateEvent = null;
    }
}
