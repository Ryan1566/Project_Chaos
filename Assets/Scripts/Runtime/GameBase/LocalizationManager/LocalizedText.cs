using UnityEngine;
using TMPro;

namespace LocalizationSystem
{
    /// <summary>
    /// 本地化文本组件 - 自动根据Key显示对应语言的文本
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [Header("本地化设置")]
        [Tooltip("本地化Key")]
        public string localizationKey;//本地化Key

        [Tooltip("是否在启动时自动更新")]
        public bool autoUpdateOnStart = true;//是否在启动时自动更新

        [Tooltip("是否监听语言切换事件")]
        public bool listenToLanguageChange = true;//是否监听语言切换事件

        [Header("格式化参数（可选）")]
        [Tooltip("用于格式化文本的参数")]
        public string[] formatArgs;//格式化参数

        private TextMeshProUGUI textComponent;//文本组件
        //private bool isInitialized = false;

        private void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// 上一次真正写进界面的语言。
        ///
        /// ══════════════ 为什么需要它 ══════════════
        /// LocalizedText 过去只在 Start 里取一次词、并顺手订阅语言切换。
        /// 但【未激活的节点不会跑 Start】—— 面板里大量选择器档位默认是 SetActive(false) 的，
        /// 于是它们既不订阅、也没取过词。等到被点亮显示时：
        ///   · 若此刻语言已经切过，它显示的是 prefab 里烘焙的中文原文；
        ///   · 而且它始终没订阅，之后再切语言也不会跟。
        /// 现象是"同一行的档位，有的翻译了有的没翻译"，从界面完全看不出原因。
        ///
        /// 现在 OnEnable 也走一次取词，并用这个字段判断"语言没变就不重复写"，
        /// 于是先隐藏后显示的节点也能拿到正确语言，且不会每次显示都白刷一遍文本。
        /// </summary>
        private LanguageType renderedLanguage = (LanguageType)(-1);

        /// <summary>
        /// 每次被激活都确认一次文本，让"创建时未激活、之后才显示"的节点也能取到正确语言。
        /// 语言没变时直接返回，不会造成多余的文本重排。
        /// </summary>
        private void OnEnable()
        {
            if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
            if (string.IsNullOrEmpty(localizationKey)) return;

            LocalizationManager mgr = LocalizationManager.GetInstance();
            if (mgr == null) return;
            if (renderedLanguage == mgr.CurrentLanguage) return;
            UpdateText();
        }

        private void Start()
        {
            if (autoUpdateOnStart)
            {
                UpdateText();
            }

            //注册语言切换事件
            if (listenToLanguageChange && LocalizationManager.GetInstance() != null)
            {
                LocalizationManager.GetInstance().OnLanguageChanged.AddListener(OnLanguageChanged);
            }

            //isInitialized = true;
        }

        private void OnDestroy()
        {
            //注销语言切换事件
            if (listenToLanguageChange && LocalizationManager.GetInstance() != null)
            {
                LocalizationManager.GetInstance().OnLanguageChanged.RemoveListener(OnLanguageChanged);
            }
        }

        /// <summary>
        /// 语言切换回调
        /// </summary>
        private void OnLanguageChanged(LanguageType language)
        {
            UpdateText();
        }

        /// <summary>
        /// 更新文本
        /// </summary>
        public void UpdateText()
        {
            if (textComponent == null) return;
            if (string.IsNullOrEmpty(localizationKey)) return;
            if (LocalizationManager.GetInstance() == null) return;

            string text;
            if (formatArgs != null && formatArgs.Length > 0)
            {
                text = LocalizationManager.GetInstance().GetText(localizationKey, formatArgs);
            }
            else
            {
                text = LocalizationManager.GetInstance().GetText(localizationKey);
            }

            textComponent.text = text;
            renderedLanguage = LocalizationManager.GetInstance().CurrentLanguage;
        }

        /// <summary>
        /// 设置Key并更新文本
        /// </summary>
        public void SetKey(string key)
        {
            localizationKey = key;
            UpdateText();
        }

        /// <summary>
        /// 设置Key和格式化参数并更新文本
        /// </summary>
        public void SetKey(string key, params string[] args)
        {
            localizationKey = key;
            formatArgs = args;
            UpdateText();
        }

        /// <summary>
        /// 设置格式化参数并更新文本
        /// </summary>
        public void SetFormatArgs(params string[] args)
        {
            formatArgs = args;
            UpdateText();
        }

        /// <summary>
        /// 获取当前显示的文本
        /// </summary>
        public string GetCurrentText()
        {
            return textComponent != null ? textComponent.text : string.Empty;
        }

        /// <summary>
        /// 在编辑器中预览
        /// </summary>
#if UNITY_EDITOR
        [ContextMenu("Preview Localization")]
        private void PreviewInEditor()
        {
            if (Application.isPlaying) return;
            if (string.IsNullOrEmpty(localizationKey)) return;

            //在编辑器中尝试从LocalizationManager获取文本
            if (LocalizationManager.GetInstance() != null)
            {
                UpdateText();
            }
            else
            {
                //如果没有运行，显示Key作为预览
                textComponent = GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = $"[{localizationKey}]";
                }
            }
        }
#endif
    }
}
