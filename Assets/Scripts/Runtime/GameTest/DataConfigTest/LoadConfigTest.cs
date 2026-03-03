using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadConfigTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        /* 同步读取配置信息
        JsonDataList<TestTableConfig> testList = JsonDataManager.Instance.LoadData<TestTableConfig>();
        Debug.Log(testList.datas[0].Occupation);*/

        //异步读取配置信息
        JsonDataManager.Instance.LoadDataAsync<TestTableConfig>((json) =>
        {
            Debug.Log(json.datas[0].Occupation);
        });
    }
}
