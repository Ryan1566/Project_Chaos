using System;
using System.Collections.Generic;

namespace DialogueSystem
{
    /// <summary>
    /// 对话运行时数据 - 用于游戏中实际使用
    /// </summary>
    [Serializable]
    public class DialogueRuntimeData
    {
        public string dialogueId;
        public string dialogueName;
        public string description;
        public List<DialogueCharacterRuntime> characters;
        public List<DialogueEntryRuntime> dialogueEntries;
        public bool canSkip;
        public bool autoPlay;
        public bool showHistory;
        public bool pauseGame;
    }

    [Serializable]
    public class DialogueCharacterRuntime
    {
        public string characterId;
        public string characterName;
        public string nameColorHex;
        public string avatarPath;
        public string voiceClipPath;
    }

    [Serializable]
    public class DialogueEntryRuntime
    {
        public string characterId;
        public string dialogueText;
        public float typingSpeed;
        public float autoSkipDelay;
        public string voiceClipPath;
        public List<DialogueOptionRuntime> options;
        public string nextDialogueId;
        public List<DialogueEventRuntime> events;
    }

    [Serializable]
    public class DialogueOptionRuntime
    {
        public string optionText;
        public string nextDialogueId;
        public List<DialogueEventRuntime> events;
    }

    [Serializable]
    public class DialogueEventRuntime
    {
        public string eventName;
        public string eventParam;
    }
}
