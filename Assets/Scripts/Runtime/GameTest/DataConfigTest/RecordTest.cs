using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChaosDebug;

/// <summary>
/// 该脚本用于存档测试
/// </summary>
public class RecordTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //Read
        TestTableConfig model = Recorder.Instance.ReadData<TestTableConfig>(0);
        ChaosLog.Info("读取" + model.Index);
        model.Index = 2;

        //Update
        Recorder.Instance.UpdateData<TestTableConfig>(0, model, true);
        model = Recorder.Instance.ReadData<TestTableConfig>(0);

        ChaosLog.Info("读取" + model.Index);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
