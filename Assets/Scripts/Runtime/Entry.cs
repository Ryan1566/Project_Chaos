using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entry : MonoBehaviour
{
    private void Awake()
    {
        UIManager.Instance.OnInit();
    }
}
