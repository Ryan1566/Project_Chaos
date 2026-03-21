using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 对话管理器 - 负责在游戏中播放对话
/// </summary>
public class DialogueManager : SingletonMono<DialogueManager>
{
    [Header("UI引用")]
    public GameObject dialoguePanel;//对话面板
    public TMPro.TextMeshProUGUI characterNameText;//角色名文本
    public TMPro.TextMeshProUGUI dialogueText;//对话文本
    public UnityEngine.UI.Image avatarImage;//头像图片
    public GameObject optionsPanel;//选项面板
    public GameObject optionButtonPrefab;//选项按钮预制体
    public GameObject continueIndicator;//继续指示器（如箭头）

    [Header("设置")]
    public float fastForwardSpeed = 0.01f;//快进时的打字速度
    public KeyCode skipKey = KeyCode.Space;//跳过/继续按键
    public KeyCode fastForwardKey = KeyCode.LeftControl;//快进按键

    //事件
    public UnityEvent OnDialogueStart;//对话开始事件
    public UnityEvent OnDialogueEnd;//对话结束事件
    public UnityEvent<int> OnDialogueEntryStart;//单条对话开始事件
    public UnityEvent<int> OnDialogueEntryEnd;//单条对话结束事件
    public UnityEvent<string> OnEventTriggered;//事件触发

    private DialogueData currentDialogue;//当前对话数据
    private int currentEntryIndex = -1;//当前对话条目索引
    private bool isPlaying = false;//是否正在播放
    private bool isTyping = false;//是否正在打字
    private bool isWaitingForInput = false;//是否等待输入
    private Coroutine typingCoroutine;//打字协程
    private Coroutine autoSkipCoroutine;//自动跳过协程

    private List<GameObject> optionButtons = new List<GameObject>();//选项按钮列表

    /// <summary>
    /// 是否正在播放对话
    /// </summary>
    public bool IsPlaying => isPlaying;

    /// <summary>
    /// 开始对话
    /// </summary>
    public void StartDialogue(DialogueData dialogueData)
    {
        if (dialogueData == null || dialogueData.dialogueEntries.Count == 0)
        {
            Debug.LogWarning("对话数据为空或没有对话条目");
            return;
        }

        currentDialogue = dialogueData;
        currentEntryIndex = 0;
        isPlaying = true;

        //暂停游戏
        if (currentDialogue.pauseGame)
        {
            Time.timeScale = 0f;
        }

        //显示面板
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        //触发开始事件
        OnDialogueStart?.Invoke();

        //播放第一条对话
        PlayEntry(currentEntryIndex);
    }

    /// <summary>
    /// 结束对话
    /// </summary>
    public void EndDialogue()
    {
        isPlaying = false;
        currentEntryIndex = -1;
        currentDialogue = null;

        //停止所有协程
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        if (autoSkipCoroutine != null)
            StopCoroutine(autoSkipCoroutine);

        //隐藏面板
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        //清除选项按钮
        ClearOptions();

        //恢复游戏
        Time.timeScale = 1f;

        //触发结束事件
        OnDialogueEnd?.Invoke();
    }

    /// <summary>
    /// 播放下一条对话
    /// </summary>
    public void PlayNextEntry()
    {
        if (!isPlaying || currentDialogue == null) return;

        //触发当前条目结束事件
        OnDialogueEntryEnd?.Invoke(currentEntryIndex);

        //检查是否有选项
        var currentEntry = currentDialogue.dialogueEntries[currentEntryIndex];
        if (currentEntry.options != null && currentEntry.options.Count > 0)
        {
            //有选项时显示选项，不自动继续
            ShowOptions(currentEntry.options);
            return;
        }

        //检查是否有下一条对话
        if (!string.IsNullOrEmpty(currentEntry.nextDialogueId))
        {
            //跳转到指定ID的对话
            int nextIndex = FindEntryIndexById(currentEntry.nextDialogueId);
            if (nextIndex >= 0)
            {
                currentEntryIndex = nextIndex;
                PlayEntry(currentEntryIndex);
                return;
            }
        }

        //默认播放下一条
        currentEntryIndex++;
        if (currentEntryIndex < currentDialogue.dialogueEntries.Count)
        {
            PlayEntry(currentEntryIndex);
        }
        else
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// 跳转到指定对话条目
    /// </summary>
    public void JumpToEntry(string entryId)
    {
        int index = FindEntryIndexById(entryId);
        if (index >= 0)
        {
            currentEntryIndex = index;
            PlayEntry(currentEntryIndex);
        }
    }

    /// <summary>
    /// 播放指定索引的对话条目
    /// </summary>
    private void PlayEntry(int index)
    {
        if (index < 0 || index >= currentDialogue.dialogueEntries.Count) return;

        var entry = currentDialogue.dialogueEntries[index];

        //触发条目开始事件
        OnDialogueEntryStart?.Invoke(index);

        //触发配置的事件
        if (entry.events != null)
        {
            foreach (var evt in entry.events)
            {
                TriggerEvent(evt);
            }
        }

        //设置人物信息
        var character = currentDialogue.characters.Find(c => c.characterId == entry.characterId);
        if (character != null)
        {
            if (characterNameText != null)
            {
                characterNameText.text = character.characterName;
                characterNameText.color = character.nameColor;
            }
            if (avatarImage != null)
                avatarImage.sprite = character.avatar;
        }

        //开始打字效果
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(entry));

        //播放语音
        if (entry.voiceClip != null)
        {
            //AudioManager.Instance.PlaySound(entry.voiceClip.name);//假设AudioManager有播放音效的方法
        }
        else if (character != null && character.voiceClip != null)
        {
            //AudioManager.Instance.PlaySound(character.voiceClip.name);
        }
    }

    /// <summary>
    /// 打字机效果协程
    /// </summary>
    private IEnumerator TypeText(DialogueEntry entry)
    {
        isTyping = true;
        isWaitingForInput = false;

        if (continueIndicator != null)
            continueIndicator.SetActive(false);

        string fullText = entry.dialogueText;
        dialogueText.text = "";

        float currentSpeed = entry.typingSpeed;

        for (int i = 0; i < fullText.Length; i++)
        {
            //检查快进
            if (Input.GetKey(fastForwardKey))
            {
                currentSpeed = fastForwardSpeed;
            }
            else
            {
                currentSpeed = entry.typingSpeed;
            }

            dialogueText.text += fullText[i];

            //等待（考虑游戏暂停）
            if (currentDialogue.pauseGame)
            {
                yield return new WaitForSecondsRealtime(currentSpeed);
            }
            else
            {
                yield return new WaitForSeconds(currentSpeed);
            }
        }

        isTyping = false;
        isWaitingForInput = true;

        if (continueIndicator != null)
            continueIndicator.SetActive(true);

        //自动跳过
        if (entry.autoSkipDelay > 0)
        {
            autoSkipCoroutine = StartCoroutine(AutoSkip(entry.autoSkipDelay));
        }
    }

    /// <summary>
    /// 自动跳过协程
    /// </summary>
    private IEnumerator AutoSkip(float delay)
    {
        if (currentDialogue.pauseGame)
        {
            yield return new WaitForSecondsRealtime(delay);
        }
        else
        {
            yield return new WaitForSeconds(delay);
        }

        if (isWaitingForInput)
        {
            PlayNextEntry();
        }
    }

    /// <summary>
    /// 显示选项
    /// </summary>
    private void ShowOptions(List<DialogueOption> options)
    {
        ClearOptions();

        if (optionsPanel != null)
            optionsPanel.SetActive(true);

        foreach (var option in options)
        {
            GameObject buttonObj = Instantiate(optionButtonPrefab, optionsPanel.transform);
            var button = buttonObj.GetComponent<UnityEngine.UI.Button>();
            var text = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            if (text != null)
                text.text = option.optionText;

            //复制选项数据避免闭包问题
            var opt = option;
            button.onClick.AddListener(() => OnOptionSelected(opt));

            optionButtons.Add(buttonObj);
        }
    }

    /// <summary>
    /// 清除选项按钮
    /// </summary>
    private void ClearOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        foreach (var button in optionButtons)
        {
            if (button != null)
                Destroy(button);
        }
        optionButtons.Clear();
    }

    /// <summary>
    /// 选项被选中
    /// </summary>
    private void OnOptionSelected(DialogueOption option)
    {
        //触发选项事件
        if (option.events != null)
        {
            foreach (var evt in option.events)
            {
                TriggerEvent(evt);
            }
        }

        ClearOptions();

        //跳转到指定对话
        if (!string.IsNullOrEmpty(option.nextDialogueId))
        {
            JumpToEntry(option.nextDialogueId);
        }
        else
        {
            PlayNextEntry();
        }
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    private void TriggerEvent(DialogueEvent evt)
    {
        OnEventTriggered?.Invoke(evt.eventName);
        Debug.Log($"触发对话事件: {evt.eventName}, 参数: {evt.eventParam}");
        //这里可以扩展更多事件处理逻辑
    }

    /// <summary>
    /// 根据ID查找对话条目索引
    /// </summary>
    private int FindEntryIndexById(string entryId)
    {
        //这里假设可以通过某种方式找到对应的索引
        //简单实现：可以遍历查找或使用字典
        for (int i = 0; i < currentDialogue.dialogueEntries.Count; i++)
        {
            //如果实现了entryId字段，可以在这里比较
            //目前使用索引作为ID
            if (i.ToString() == entryId)
                return i;
        }
        return -1;
    }

    private void Update()
    {
        if (!isPlaying) return;

        //跳过/继续
        if (Input.GetKeyDown(skipKey))
        {
            if (isTyping)
            {
                //如果正在打字，立即完成
                StopCoroutine(typingCoroutine);
                var entry = currentDialogue.dialogueEntries[currentEntryIndex];
                dialogueText.text = entry.dialogueText;
                isTyping = false;
                isWaitingForInput = true;

                if (continueIndicator != null)
                    continueIndicator.SetActive(true);
            }
            else if (isWaitingForInput)
            {
                //等待输入时继续
                if (autoSkipCoroutine != null)
                    StopCoroutine(autoSkipCoroutine);
                PlayNextEntry();
            }
        }
    }
}