using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SLPanel : BasePanel
{
    private Transform RecordsList;

    private void Awake()
    {
        RecordsList = transform.Find("RecordsList");
    }

    public override void OnEnter()
    {
        
    }

    public override void OnExit()
    {
        base.OnExit();
    }
}
