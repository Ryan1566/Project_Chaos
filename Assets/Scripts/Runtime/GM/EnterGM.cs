using ChaosDebug;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnterGM : MonoBehaviour
{
    private Button _gmBtn;
    public GameObject _gmPanel;

    private void Awake()
    {
        _gmBtn = GetComponent<Button>();
        _gmBtn.onClick.AddListener(ShowGM);
    }

    private void ShowGM()
    {
        if (_gmPanel.gameObject.activeSelf)
            _gmPanel.gameObject.SetActive(false);
        else
            _gmPanel.gameObject.SetActive(true);
    }


}
