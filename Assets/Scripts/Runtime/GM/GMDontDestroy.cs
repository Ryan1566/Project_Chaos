using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GMDontDestroy : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
}
