using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjPoolTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //GameObject cubePrefab = Resources.Load("Test/Cube") as GameObject;
            //GameObject cubePrefab = ResManager.Instance.Load<GameObject>("Test/Cube");
            PoolManager.Instance.GetObj("Test/","Cube",(obj) => {

            });
        }

        //if (Input.GetMouseButton(1))
        //{
        //    PoolManager.Instance.PushObj();
        //}
    }

    //private void DoSth(GameObject obj)
    //{
    //    //PoolManager.Instance.GetObj(obj);
    //    //obj.transform.localScale = new Vector3(2,2,2);
    //}
}
