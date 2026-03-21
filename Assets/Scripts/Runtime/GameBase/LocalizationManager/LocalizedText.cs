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
        private bool isInitialized = false;

        private void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();
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

            isInitialized = true;
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
