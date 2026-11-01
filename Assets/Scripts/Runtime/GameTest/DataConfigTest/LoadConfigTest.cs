using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChaosDebug;

public class LoadConfigTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        /* 同步读取配置信息
        JsonDataList<TestTable_TestConfig> testList = JsonDataManager.Instance.LoadData<TestTable_TestConfig>();
        ChaosLog.Info(LogChannel.Config, testList.datas[0].Occupation.ToString());*/

        //异步读取配置信息
        ConfigLoader.Instance.LoadDataAsync<TestTable_TestConfig>((json) =>
        {
            ChaosLog.Info(LogChannel.Config, json.datas[0].Occupation.ToString());
        });
    }
}
