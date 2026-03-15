using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 该脚本用于存档测试
/// </summary>
public class RecordTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //Read
        TestTableModel model = Recorder.Instance.ReadData<TestTableModel>(0);
        Debug.Log("读取" + model.Index);
        model.Index = 2;

        //Update
        Recorder.Instance.UpdateData<TestTableModel>(0, model, true);
        model = Recorder.Instance.ReadData<TestTableModel>(0);

        Debug.Log("读取" + model.Index);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
