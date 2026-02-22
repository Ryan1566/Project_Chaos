using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TangLaoShi
{
    [Obsolete("该基类被废置",false)]
    public class BasePanel : MonoBehaviour
    {
        Dictionary<string,List<UIBehaviour>> controlDic = new Dictionary<string, List<UIBehaviour>>();

        private void Awake()
        {
            FindChildrenControl<Button>();
        }

        public virtual void OnEnter()
        {

        }

        public virtual void OnExit()
        {

        }

        protected T GetControl<T>(string controlName) where T : UIBehaviour
        {
            if(controlDic.ContainsKey(controlName))
            {
                for(int i = 0; i < controlDic[controlName].Count; i++)
                {
                    if(controlDic[controlName][i] is T)
                    {
                        return controlDic[controlName][i] as T;
                    }
                }
            }

            return null;
        }

        private void FindChildrenControl<T>() where T : UIBehaviour
        {
            T[] controls = GetComponentsInChildren<T>();
            string objName;
            for (int i = 0;i < controls.Length; ++i)
            {
                objName = controls[i].gameObject.name;
                if (controlDic.ContainsKey(objName))
                    controlDic[objName].Add(controls[i]);
                else
                    controlDic.Add(objName, new List<UIBehaviour>() { controls[i] });
            }
        }

    }
}