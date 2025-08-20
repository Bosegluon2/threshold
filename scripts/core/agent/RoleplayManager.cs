using Godot;
using System;
using Godot.Collections;

namespace Threshold.Core.Agent
{
    /// <summary>
    /// 角色扮演管理器
    /// </summary>
    public partial class RoleplayManager : Node
    {
        private AICommunication aiCommunication;
        private FunctionManager functionManager;
        private WorldLoreManager worldLoreManager;
        private CharacterCard currentCharacter;
        private Array<ConversationMessage> conversationHistory;
        private int maxHistoryLength = 50;
        
        [Signal]
        public delegate void CharacterChangedEventHandler(CharacterCard character);
        
        [Signal]
        public delegate void MessageAddedEventHandler(ConversationMessage message);
        
        [Signal]
        public delegate void AIResponseReceivedEventHandler(string response);
        
        [Signal]
        public delegate void ErrorOccurredEventHandler(string error);
        
        public CharacterCard CurrentCharacter => currentCharacter;
        public Array<ConversationMessage> ConversationHistory => conversationHistory;
        
        /// <summary>
        /// 检查是否支持函数调用
        /// </summary>
        public bool HasFunctions()
        {
            return functionManager != null && functionManager.HasFunctions();
        }
        
        /// <summary>
        /// 获取世界观统计信息
        /// </summary>
        public string GetWorldLoreStats()
        {
            if (worldLoreManager != null)
                return $"世界观条目总数: {worldLoreManager.GetTotalEntries()}\n可用分类: {string.Join(", ", worldLoreManager.GetAllCategories())}";
            return "世界观管理器未初始化";
        }
        
        /// <summary>
        /// 获取RAG系统统计信息
        /// </summary>
        public string GetRAGStats()
        {
            if (worldLoreManager != null)
                return $"RAG系统 (Function-Call): 已启用\n世界观条目总数: {worldLoreManager.GetTotalEntries()}\n可用分类: {string.Join(", ", worldLoreManager.GetAllCategories())}";
            return "RAG系统未初始化";
        }
        
        public override void _Ready()
        {
            conversationHistory = new Array<ConversationMessage>();
            
            // 初始化世界观管理器
            InitializeWorldLoreManager();
            
            // 初始化函数管理器
            InitializeFunctionManager();
            
            aiCommunication = new AICommunication();
            AddChild(aiCommunication);
            
            // 设置函数管理器
            aiCommunication.SetFunctionManager(functionManager);
            
            aiCommunication.ResponseReceived += OnAIResponseReceived;
            aiCommunication.ErrorOccurred += OnAIErrorOccurred;
        }
        
        public void SetCharacter(CharacterCard character)
        {
            currentCharacter = character;
            conversationHistory.Clear();
            EmitSignal(nameof(CharacterChanged), character);
        }
        
        public void SendMessage(string message)
        {
            GD.Print("=== 角色扮演管理器发送消息 ===");
            GD.Print($"消息内容: {message}");
            
            if (currentCharacter == null)
            {
                GD.PrintErr("当前没有选择角色");
                EmitSignal(nameof(ErrorOccurred), "请先选择一个角色");
                return;
            }
            
            GD.Print($"当前角色: {currentCharacter.Name}");
            GD.Print($"对话历史长度: {conversationHistory.Count}");
            
            var userMessage = new ConversationMessage(message, "user");
            AddMessageToHistory(userMessage);
            
            GD.Print("正在通过AI通信模块发送消息...");
            //aiCommunication.SendMessage(message, agent, conversationHistory);
        }
        
        private void AddMessageToHistory(ConversationMessage message)
        {
            conversationHistory.Add(message);
            EmitSignal(nameof(MessageAdded), message);
            
            if (conversationHistory.Count > maxHistoryLength)
            {
                conversationHistory.RemoveAt(0);
            }
        }
        
        public void ClearHistory()
        {
            conversationHistory.Clear();
        }
        
        public string GetHistoryAsText()
        {
            if (conversationHistory.Count == 0)
                return "暂无对话记录";
            
            var historyText = "";
            foreach (var message in conversationHistory)
            {
                var role = message.Role;
                var time = message.Timestamp.ToString("HH:mm:ss");
                historyText += $"[{time}] {role}: {message.Content}\n";
            }
            
            return historyText;
        }
        
        /// <summary>
        /// 初始化世界观管理器
        /// </summary>
        private void InitializeWorldLoreManager()
        {
            GD.Print("=== 初始化世界观管理器 ===");
            worldLoreManager = new WorldLoreManager();
            worldLoreManager.InitializeDefaultLore();
            GD.Print("世界观管理器初始化完成");
        }
        

        
        /// <summary>
        /// 初始化函数管理器
        /// </summary>
        private void InitializeFunctionManager()
        {
            GD.Print("=== 初始化函数管理器 ===");
            functionManager = new FunctionManager();
            
            // 注册游戏相关函数
            GD.Print("正在注册游戏函数...");
            functionManager.RegisterFunction(new Functions.GetWorldInfoFunction());
            functionManager.RegisterFunction(new Functions.GetInventoryFunction());
            functionManager.RegisterFunction(new Functions.GetQuestsFunction());
            functionManager.RegisterFunction(new Functions.GetRelationshipsFunction());
            
            // 注册RAG相关函数
            GD.Print("正在注册RAG函数...");
            var worldLoreRetrieval = new Functions.WorldLoreRetrievalFunction(worldLoreManager);
            var characterLore = new Functions.CharacterLoreFunction(worldLoreManager);
            functionManager.RegisterFunction(worldLoreRetrieval);
            functionManager.RegisterFunction(characterLore);
            
            var functionCount = functionManager.GetAvailableFunctions().Count;
            GD.Print($"函数管理器初始化完成，已注册 {functionCount} 个函数");
            
            // 列出所有注册的函数
            var functions = functionManager.GetAvailableFunctions();
            foreach (var function in functions)
            {
                GD.Print($"  - {function.Name}: {function.Description}");
            }
        }
        
        private void OnAIResponseReceived(string response)
        {
            var aiMessage = new ConversationMessage(response, "assistant");
            AddMessageToHistory(aiMessage);
            EmitSignal(nameof(AIResponseReceived), response);
        }
        
        private void OnAIErrorOccurred(string error)
        {
            EmitSignal(nameof(ErrorOccurred), error);
        }
    }
}
