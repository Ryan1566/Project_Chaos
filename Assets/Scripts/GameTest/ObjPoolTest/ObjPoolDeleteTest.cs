using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjPoolDeleteTest : MonoBehaviour
{
    // Start is called before the first frame update
    void OnEnable()
    {
        Invoke("PushObj", 1);
    }

    public void PushObj()
    {
        PoolManager.Instance.PushObj(this.gameObject.name,this.gameObject);
    }
}
