using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadConfigTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        JsonDataList<TestTableConfig> testList = JsonDataManager.Instance.LoadData<TestTableConfig>();
        Debug.Log(testList.datas[0].Occupation);
    }
}
