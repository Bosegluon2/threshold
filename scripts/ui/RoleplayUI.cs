using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Threshold.Core.Agent;
using Threshold.Core;

namespace Threshold.UI
{
    /// <summary>
    /// 角色扮演UI控制脚本
    /// </summary>
    public partial class RoleplayUI : Control
    {
        private Agent currentAgent;
        private OptionButton agentOptionButton;
        // 移除createAgentButton，因为不再需要创建功能
        // private Button createAgentButton;
        private Label agentStatus;
        
        // 移除characterOptionButton，因为现在只需要选择Agent
        // private OptionButton characterOptionButton;
        
        private TextEdit characterInfo;
        private Button refreshButton;
        private RichTextLabel conversationHistory;
        private LineEdit messageInput;
        private Button sendButton;
        private Button clearButton;
        private Button exportButton;
        
        // 状态标签
        private Label functionStatus;
        private Label worldLoreStatus;
        private Label ragStatus;
        
        private Array<Agent> availableAgents;
        
        // 初始化重试计数
        private int initRetryCount = 0;
        private const int MAX_INIT_RETRIES = 10;
        

        
        public override void _Ready()
        {
            GD.Print("=== RoleplayUI _Ready 开始 ===");
            
            // 获取UI节点引用
            if (!GetUINodes())
            {
                GD.PrintErr("UI节点获取失败，无法继续初始化");
                return;
            }
            
            // 连接按钮信号
            if (!ConnectSignals())
            {
                GD.PrintErr("信号连接失败，无法继续初始化");
                return;
            }
            
            // 连接刷新按钮信号
            if (refreshButton != null)
            {
                refreshButton.Pressed += OnRefreshButtonPressed;
            }
            
            // 初始化系统
            InitializeSystem();
            

            
            GD.Print("=== RoleplayUI _Ready 完成 ===");
        }
        
        /// <summary>
        /// 获取UI节点引用
        /// </summary>
        private bool GetUINodes()
        {
            try
            {
                // 移除createAgentButton的获取
                // createAgentButton = GetNode<Button>("VBoxContainer/CharacterSelection/CreateAgentButton");
                
                agentOptionButton = GetNode<OptionButton>("VBoxContainer/CharacterSelection/AgentOptionButton");
                agentStatus = GetNode<Label>("VBoxContainer/CharacterSelection/StatusContainer/AgentStatus");
                
                // 其他UI节点保持不变
                characterInfo = GetNode<TextEdit>("VBoxContainer/CharacterInfoContainer/CharacterInfo");
                refreshButton = GetNode<Button>("VBoxContainer/CharacterInfoContainer/CharacterInfoHeader/RefreshButton");
                conversationHistory = GetNode<RichTextLabel>("VBoxContainer/ConversationArea/ConversationHistory");
                messageInput = GetNode<LineEdit>("VBoxContainer/InputArea/MessageInput");
                sendButton = GetNode<Button>("VBoxContainer/InputArea/SendButton");
                clearButton = GetNode<Button>("VBoxContainer/InputArea/ClearButton");
                exportButton = GetNode<Button>("VBoxContainer/InputArea/ExportButton");
                
                // 状态标签
                functionStatus = GetNode<Label>("VBoxContainer/CharacterSelection/StatusContainer/FunctionStatus");
                worldLoreStatus = GetNode<Label>("VBoxContainer/CharacterSelection/StatusContainer/WorldLoreStatus");
                ragStatus = GetNode<Label>("VBoxContainer/CharacterSelection/StatusContainer/RAGStatus");
                
                // 检查关键节点是否获取成功
                if (agentOptionButton == null || messageInput == null)
                {
                    GD.PrintErr("关键UI节点获取失败");
                    return false;
                }
                
                // 检查状态标签是否获取成功
                if (functionStatus == null || worldLoreStatus == null || ragStatus == null || agentStatus == null)
                {
                    GD.PrintErr("状态标签节点获取失败");
                    return false;
                }
                
                GD.Print("所有UI节点获取成功");
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"获取UI节点时发生异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 连接信号
        /// </summary>
        private bool ConnectSignals()
        {
            try
            {
                sendButton.Pressed += OnSendButtonPressed;
                clearButton.Pressed += OnClearButtonPressed;
                exportButton.Pressed += OnExportButtonPressed;
                // 移除createAgentButton的信号连接
                // createAgentButton.Pressed += OnCreateAgentButtonPressed;
                agentOptionButton.ItemSelected += OnAgentSelected;
                
                // 设置回车键发送消息
                messageInput.TextSubmitted += OnMessageSubmitted;
                
                GD.Print("所有信号连接成功");
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"连接信号时发生异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 初始化系统
        /// </summary>
        private void InitializeSystem()
        {
            // 等待一帧确保AgentManager已初始化
            CallDeferred(nameof(DeferredInitialize));
        }
        

        
        /// <summary>
        /// 延迟初始化，确保GameManager和CharacterManager已就绪
        /// </summary>
        private void DeferredInitialize()
        {
            if (GameManager.Instance == null || GameManager.Instance.CharacterManager == null)
            {
                initRetryCount++;
                if (initRetryCount >= MAX_INIT_RETRIES)
                {
                    GD.PrintErr($"GameManager或CharacterManager初始化失败，已重试{MAX_INIT_RETRIES}次，停止重试");
                    return;
                }
                
                GD.PrintErr($"GameManager或CharacterManager未初始化，等待下一帧 (重试 {initRetryCount}/{MAX_INIT_RETRIES})");
                CallDeferred(nameof(DeferredInitialize));
                return;
            }
            
            try
            {
                // 初始化Agent列表
                InitializeAgents();
                
                // 更新各种状态
                UpdateFunctionStatus();
                UpdateWorldLoreStatus();
                UpdateRAGStatus();
                UpdateAgentStatus();
                
                GD.Print("RoleplayUI系统初始化完成");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"RoleplayUI初始化失败: {ex.Message}");
                GD.PrintErr($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 初始化角色列表（已废弃，使用InitializeAgents替代）
        /// </summary>
        private void InitializeCharacters()
        {
            GD.Print("InitializeCharacters方法已废弃，请使用InitializeAgents");
            InitializeAgents();
        }
        
        /// <summary>
        /// 初始化Agent列表
        /// </summary>
        private void InitializeAgents()
        {
            try
            {
                if (GameManager.Instance?.CharacterManager == null)
                {
                    GD.PrintErr("GameManager或CharacterManager未初始化");
                    return;
                }
                
                // 获取所有可用的Agent（每个CharacterCard对应一个Agent）
                var availableAgents = GameManager.Instance.CharacterManager.AllCharacters;
                
                // 清空选项
                agentOptionButton.Clear();
                agentOptionButton.AddItem("请选择Agent...");
                
                // 使用HashSet去重，避免显示重复的角色名称
                var uniqueNames = new HashSet<string>();
                var addedAgents = new List<Threshold.Core.Agent.Agent>();
                
                // 添加所有可用的Agent（去重）
                foreach (var agent in availableAgents)
                {
                    if (agent.Character != null)
                    {
                        string displayText = $"{agent.Character.Name} ({agent.Character.Profession})";
                        
                        // 检查是否已添加过相同名称的角色
                        if (!uniqueNames.Contains(agent.Character.Name))
                        {
                            uniqueNames.Add(agent.Character.Name);
                            addedAgents.Add(agent);
                            agentOptionButton.AddItem(displayText);
                        }
                        else
                        {
                            GD.Print($"跳过重复角色: {agent.Character.Name}");
                        }
                    }
                }
                
                GD.Print($"已自动载入 {addedAgents.Count} 个唯一Agent（总共 {availableAgents.Count} 个）");
            }
            catch (Exception e)
            {
                GD.PrintErr($"初始化Agent列表时发生错误: {e}");
            }
        }
        
                    /// <summary>
        /// Agent选择改变事件
        /// </summary>
        private void OnAgentSelected(long index)
        {
            try
            {
                // 先断开当前Agent的信号连接
                if (currentAgent != null)
                {
                    currentAgent.MessageAdded -= OnAgentMessageAdded;
                    currentAgent.AIResponseReceived -= OnAgentAIResponseReceived;
                    currentAgent.ErrorOccurred -= OnAgentErrorOccurred;
                }
                
                if (index <= 0) // 0是"请选择Agent..."
                {
                    currentAgent = null;
                    UpdateAgentStatus();
                    UpdateConversationDisplay();
                    return;
                }
                
                var availableAgents = GameManager.Instance.CharacterManager.AllCharacters;
                if ((int)index - 1 < availableAgents.Count)
                {
                    currentAgent = availableAgents[(int)index - 1];
                    GD.Print($"已选择Agent: {currentAgent.Character?.Name}");
                    
                    // 连接新Agent的信号
                    currentAgent.MessageAdded += OnAgentMessageAdded;
                    currentAgent.AIResponseReceived += OnAgentAIResponseReceived;
                    currentAgent.ErrorOccurred += OnAgentErrorOccurred;
                    
                    UpdateAgentStatus();
                    UpdateConversationDisplay();
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"选择Agent时发生错误: {e}");
            }
        }
        
        // 移除OnCreateAgentButtonPressed方法，因为不再需要创建功能
        // private void OnCreateAgentButtonPressed()
        // {
        //     try
        //     {
        //         // 创建新角色（会自动创建对应的Agent）
        //         var newCharacter = AgentManager.Instance.CreateCharacter(
        //             "新角色",
        //             "待设定",
        //             "待设定",
        //             25,
        //             "待设定",
        //             "待设定",
        //             "待设定背景故事"
        //         );
                
        //         GD.Print($"已创建新角色: {newCharacter.Name}");
                
        //         // 刷新Agent列表
        //         InitializeAgents();
        //     }
        //     catch (Exception e)
        //     {
        //         GD.PrintErr($"创建新角色时发生错误: {e}");
        //     }
        // }
        
        /// <summary>
        /// 角色改变事件（已废弃，现在直接使用Agent）
        /// </summary>
        private void OnCharacterChanged(CharacterCard character)
        {
            GD.Print($"角色改变事件已废弃，现在直接使用Agent");
        }
        
        /// <summary>
        /// 消息添加事件
        /// </summary>
        private void OnMessageAdded(ConversationMessage message)
        {
            UpdateConversationDisplay();
        }
        
        /// <summary>
        /// AI响应接收事件
        /// </summary>
        private void OnAIResponseReceived(string response)
        {
            GD.Print($"AI回复: {response}");
        }
        
        /// <summary>
        /// 错误发生事件
        /// </summary>
        private void OnErrorOccurred(string error)
        {
            GD.PrintErr($"错误: {error}");
            // 可以在这里显示错误提示
        }
        
        /// <summary>
        /// 发送按钮点击事件
        /// </summary>
        private void OnSendButtonPressed()
        {
            SendMessage();
        }
        
        /// <summary>
        /// 消息提交事件（回车键）
        /// </summary>
        private void OnMessageSubmitted(string text)
        {
            SendMessage();
        }
        
        /// <summary>
        /// 发送消息
        /// </summary>
        private void SendMessage()
        {
            if (messageInput == null)
            {
                GD.PrintErr("消息输入框未初始化");
                return;
            }
            
            var message = messageInput.Text.Trim();
            if (string.IsNullOrEmpty(message))
            {
                GD.Print("消息为空，无法发送");
                return;
            }
            
            if (currentAgent == null)
            {
                GD.PrintErr("未选择Agent，无法发送消息");
                return;
            }
            var targetAgent = GameManager.Instance.CharacterManager.GetPlayerAgent();
            currentAgent.SendMessageToTarget(message, targetAgent);
            messageInput.Text = "";
            messageInput.GrabFocus();
            
            GD.Print($"发送消息: {message}");
            
            // 立即更新对话显示，显示用户消息
            UpdateConversationDisplay();
        }
        
        /// <summary>
        /// 清空历史按钮点击事件
        /// </summary>
        private void OnClearButtonPressed()
        {
            if (currentAgent != null)
            {
                currentAgent.ClearHistory();
                UpdateConversationDisplay();
            }
        }
        
        /// <summary>
        /// 导出按钮点击事件
        /// </summary>
        private void OnExportButtonPressed()
        {
            if (currentAgent != null)
            {
                var filePath = $"user://conversation_{currentAgent.AgentName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var historyText = currentAgent.GetHistoryAsText();
                
                try
                {
                    var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
                    if (file != null)
                    {
                        file.StoreString(historyText);
                        file.Close();
                        GD.Print($"对话历史已导出到: {filePath}");
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"导出失败: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 更新函数调用状态显示
        /// </summary>
        private void UpdateFunctionStatus()
        {
            if (functionStatus == null)
            {
                GD.PrintErr("函数状态标签未初始化");
                return;
            }
            
            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                functionStatus.Text = "函数调用：未初始化";
                functionStatus.Modulate = new Color(1, 0, 0, 1);
                return;
            }
            
            try
            {
                var characterManager = gameManager.CharacterManager;
                if (characterManager != null)
                {
                    var functionManager = characterManager.GetFunctionManager();
                    if (functionManager != null)
                    {
                        var hasFunctions = functionManager.GetAvailableFunctions().Count > 0;
                        functionStatus.Text = $"函数调用：{(hasFunctions ? "已启用" : "未启用")}";
                        functionStatus.Modulate = hasFunctions ? new Color(0, 1, 0, 1) : new Color(1, 0, 0, 1);
                    }
                    else
                    {
                        functionStatus.Text = "函数调用：管理器未就绪";
                        functionStatus.Modulate = new Color(1, 0, 0, 1);
                    }
                }
                else
                {
                    functionStatus.Text = "函数调用：CharacterManager未就绪";
                    functionStatus.Modulate = new Color(1, 0, 0, 1);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"更新函数状态失败: {ex.Message}");
                functionStatus.Text = "函数调用：错误";
                functionStatus.Modulate = new Color(1, 0, 0, 1);
            }
        }
        
        /// <summary>
        /// 更新世界观状态显示
        /// </summary>
        private void UpdateWorldLoreStatus()
        {
            if (worldLoreStatus == null)
            {
                GD.PrintErr("世界观状态标签未初始化");
                return;
            }
            
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                var characterManager = gameManager.CharacterManager;
                if (characterManager != null)
                {
                    var worldLoreManager = characterManager.GetWorldLoreManager();
                    if (worldLoreManager != null)
                    {
                        var totalEntries = worldLoreManager.GetTotalEntries();
                        worldLoreStatus.Text = $"世界观：{totalEntries} 个条目";
                        worldLoreStatus.Modulate = totalEntries > 0 ? new Color(0, 1, 0, 1) : new Color(1, 0, 0, 1);
                    }
                    else
                    {
                        worldLoreStatus.Text = "世界观：管理器未就绪";
                        worldLoreStatus.Modulate = new Color(1, 0, 0, 1);
                    }
                }
                else
                {
                    worldLoreStatus.Text = "世界观：CharacterManager未就绪";
                    worldLoreStatus.Modulate = new Color(1, 0, 0, 1);
                }
            }
            else
            {
                worldLoreStatus.Text = "世界观：未初始化";
                worldLoreStatus.Modulate = new Color(1, 0, 0, 1);
            }
        }
        
        /// <summary>
        /// 更新RAG系统状态显示
        /// </summary>
        private void UpdateRAGStatus()
        {
            if (ragStatus == null)
            {
                GD.PrintErr("RAG状态标签未初始化");
                return;
            }
            
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                var characterManager = gameManager.CharacterManager;
                if (characterManager != null)
                {
                    var functionManager = characterManager.GetFunctionManager();
                    if (functionManager != null)
                    {
                        var functions = functionManager.GetAvailableFunctions();
                        var hasRAGFunctions = false;
                        
                        foreach (var function in functions)
                        {
                            if (function.Name.Contains("lore"))
                            {
                                hasRAGFunctions = true;
                                break;
                            }
                        }
                        
                        ragStatus.Text = $"RAG系统：{(hasRAGFunctions ? "已启用" : "未启用")}";
                        ragStatus.Modulate = hasRAGFunctions ? new Color(0, 1, 0, 1) : new Color(1, 0, 0, 1);
                    }
                    else
                    {
                        ragStatus.Text = "RAG系统：函数管理器未就绪";
                        ragStatus.Modulate = new Color(1, 0, 0, 1);
                    }
                }
                else
                {
                    ragStatus.Text = "RAG系统：CharacterManager未就绪";
                    ragStatus.Modulate = new Color(1, 0, 0, 1);
                }
            }
            else
            {
                ragStatus.Text = "RAG系统：未初始化";
                ragStatus.Modulate = new Color(1, 0, 0, 1);
            }
        }
        
        /// <summary>
        /// 更新Agent状态显示
        /// </summary>
        private void UpdateAgentStatus()
        {
            try
            {
                if (agentStatus != null)
                {
                    if (currentAgent != null && currentAgent.Character != null)
                    {
                        var character = currentAgent.Character;
                        agentStatus.Text = $"当前Agent: {character.Name}\n职业: {character.Profession}\n派别: {character.Faction}\n状态: 活跃";
                    }
                    else
                    {
                        agentStatus.Text = "当前Agent: 未选择";
                    }
                }
                
                // 同时更新角色信息框
                UpdateCharacterInfo();
            }
            catch (Exception e)
            {
                GD.PrintErr($"更新Agent状态时发生错误: {e}");
            }
        }
        
        /// <summary>
        /// 更新角色信息显示（包含关系与记忆）
        /// </summary>
        private void UpdateCharacterInfo()
        {
            try
            {
                if (characterInfo == null) return;
                
                if (currentAgent != null && currentAgent.Character != null)
                {
                    var character = currentAgent.Character;
                    var info = new System.Text.StringBuilder();
                    
                    // 基本信息
                    info.AppendLine($"=== {character.Name} 的完整信息 ===");
                    info.AppendLine();
                    
                    info.AppendLine("【基本信息】");
                    info.AppendLine($"ID: {character.Id}");
                    info.AppendLine($"名称: {character.Name}");
                    info.AppendLine($"职业: {character.Profession}");
                    info.AppendLine($"派别: {character.Faction}");
                    info.AppendLine($"年龄: {character.Age}");
                    info.AppendLine($"性别: {character.Gender}");
                    info.AppendLine($"知识等级: {character.KnowledgeLevel}");
                    info.AppendLine();
                    
                    // 外观和性格信息
                    if (character.Appearance != null)
                        info.AppendLine($"外貌: {character.Appearance.Content} (秘密程度: {character.Appearance.SecrecyLevel})");
                    if (character.SpeechStyle != null)
                        info.AppendLine($"说话风格: {character.SpeechStyle.Content} (秘密程度: {character.SpeechStyle.SecrecyLevel})");
                    if (character.Goals != null)
                        info.AppendLine($"目标: {character.Goals.Content} (秘密程度: {character.Goals.SecrecyLevel})");
                    if (character.Fears != null)
                        info.AppendLine($"恐惧: {character.Fears.Content} (秘密程度: {character.Fears.SecrecyLevel})");
                    if (character.Secrets != null)
                        info.AppendLine($"秘密: {character.Secrets.Content} (秘密程度: {character.Secrets.SecrecyLevel})");
                    info.AppendLine();
                    
                    // 状态信息
                    info.AppendLine("【状态信息】");
                    info.AppendLine($"生命值: {currentAgent.CurrentHealth}/{currentAgent.Character.MaxHealth}");
                    info.AppendLine($"精力值: {currentAgent.CurrentEnergy}/{currentAgent.Character.MaxEnergy}");
                    info.AppendLine($"状态: {string.Join(", ", currentAgent.CurrentStatus)}");
                    info.AppendLine();
                    
                    // 战斗属性
                    info.AppendLine("【战斗属性】");
                    info.AppendLine($"战斗技能 (W): {currentAgent.CurrentWarpedInfo.Warfare}/10");
                    info.AppendLine($"适应能力 (A): {currentAgent.CurrentWarpedInfo.Adaptability}/10");
                    info.AppendLine($"推理能力 (R): {currentAgent.CurrentWarpedInfo.Reasoning}/10");
                    info.AppendLine($"感知能力 (P): {currentAgent.CurrentWarpedInfo.Perception}/10");
                    info.AppendLine($"耐力 (E): {currentAgent.CurrentWarpedInfo.Endurance}/10");
                    info.AppendLine($"敏捷性 (D): {currentAgent.CurrentWarpedInfo.Dexterity}/10");
                    info.AppendLine();
                    
                    // 技能
                    info.AppendLine("【技能列表】");
                    if (character.Skills != null && character.Skills.Count > 0)
                    {
                        foreach (var skill in character.Skills)
                        {
                            info.AppendLine($"- {skill}");
                        }
                    }
                    else
                    {
                        info.AppendLine("暂无技能记录");
                    }
                    info.AppendLine();
                    
                    // 关系网络
                    info.AppendLine("【关系网络】");
                    if (currentAgent.Relationships != null && currentAgent.Relationships.Count > 0)
                    {
                        foreach (var relation in currentAgent.Relationships)
                        {
                            var rel = relation.Value;
                            info.AppendLine($"与 {rel.TargetCharacterName} (ID: {rel.TargetCharacterId}):");
                            info.AppendLine($"  信任度: {rel.TrustLevel}/100");
                            info.AppendLine($"  关系度: {rel.RelationshipLevel}/100");
                            info.AppendLine($"  印象: {rel.Impression}");
                            info.AppendLine($"  关系描述: {rel.RelationshipDescription}");
                            info.AppendLine($"  关系类型: {rel.RelationshipType}");
                            info.AppendLine($"  互动次数: {rel.InteractionCount}");
                            info.AppendLine($"  最后互动内容: {rel.LastInteraction}");
                            info.AppendLine();
                        }
                    }
                    else
                    {
                        info.AppendLine("暂无关系记录");
                        info.AppendLine();
                    }
                    
                    // 记忆系统
                    info.AppendLine("【记忆系统】");
                    if (currentAgent.Memories != null && currentAgent.Memories.Count > 0)
                    {
                        info.AppendLine("一般记忆:");
                        foreach (var memory in currentAgent.Memories)
                        {
                            info.AppendLine($"- {memory}");
                        }
                        info.AppendLine();
                    }
                    else
                    {
                        info.AppendLine("一般记忆: 暂无记录");
                        info.AppendLine();
                    }
                    
                    if (currentAgent.ImportantEvents != null && currentAgent.ImportantEvents.Count > 0)
                    {
                        info.AppendLine("重要事件:");
                        foreach (var eventItem in currentAgent.ImportantEvents)
                        {
                            info.AppendLine($"- {eventItem}");
                        }
                        info.AppendLine();
                    }
                    else
                    {
                        info.AppendLine("重要事件: 暂无记录");
                        info.AppendLine();
                    }
                    
                    if (currentAgent.PersonalSecrets != null && currentAgent.PersonalSecrets.Count > 0)
                    {
                        info.AppendLine("个人秘密:");
                        foreach (var secret in currentAgent.PersonalSecrets)
                        {
                            info.AppendLine($"- {secret}");
                        }
                        info.AppendLine();
                    }
                    else
                    {
                        info.AppendLine("个人秘密: 暂无记录");
                        info.AppendLine();
                    }
                    
                    // 房间信息
                    info.AppendLine("【系统信息】");
                    if (currentAgent.CurrentPlace != null)
                    {
                        info.AppendLine($"房间ID: {currentAgent.CurrentPlace.Id}");
                    }
                    else
                    {
                        info.AppendLine("房间ID: 未分配");
                    }
                    info.AppendLine($"绑定Agent ID: {currentAgent.AgentId}");
                    info.AppendLine();
                    
                    info.AppendLine("=== 信息结束 ===");
                    
                    characterInfo.Text = info.ToString();
                }
                else
                {
                    characterInfo.Text = "请选择一个Agent以查看角色信息";
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"更新角色信息时发生错误: {e}");
                if (characterInfo != null)
                {
                    characterInfo.Text = $"更新角色信息时发生错误: {e.Message}";
                }
            }
        }
        
        /// <summary>
        /// 更新对话显示
        /// </summary>
        private void UpdateConversationDisplay()
        {
            if (currentAgent != null)
            {
                conversationHistory.Text = currentAgent.GetHistoryAsText();
                
                // 滚动到底部
                var scrollContainer = conversationHistory.GetVScrollBar();
                if (scrollContainer != null)
                {
                    scrollContainer.Value = scrollContainer.MaxValue;
                }
                

            }
            else
            {
                conversationHistory.Text = "请先选择一个Agent";
            }
        }
        
        /// <summary>
        /// Agent消息添加事件处理
        /// </summary>
        private void OnAgentMessageAdded(ConversationMessage message)
        {
            GD.Print($"Agent消息添加: {message.Content}");
            UpdateConversationDisplay();
        }
        
        /// <summary>
        /// Agent AI响应接收事件处理
        /// </summary>
        private void OnAgentAIResponseReceived(string response)
        {
            GD.Print($"Agent AI回复: {response}");
            UpdateConversationDisplay();
        }
        
        /// <summary>
        /// Agent错误发生事件处理
        /// </summary>
        private void OnAgentErrorOccurred(string error)
        {
            GD.PrintErr($"Agent错误: {error}");
            // 可以在这里显示错误提示
        }
        
        /// <summary>
        /// 刷新按钮点击事件处理
        /// </summary>
        private void OnRefreshButtonPressed()
        {
            GD.Print("手动刷新角色信息");
            UpdateCharacterInfo();
        }
        

    }
}
