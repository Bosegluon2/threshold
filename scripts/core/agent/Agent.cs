using Godot;
using Godot.Collections;
using System;
using Threshold.Core.Enums;
using Threshold.Core.Data;
using System.Linq;
using System.Runtime.CompilerServices;
namespace Threshold.Core.Agent
{
    /// <summary>
    /// 独立的Agent类 - 包含所有相关数据和对话
    /// </summary>
    public partial class Agent : Node
    {
        [Signal]
        public delegate void MessageAddedEventHandler(ConversationMessage message);

        [Signal]
        public delegate void AIResponseReceivedEventHandler(string response);

        [Signal]
        public delegate void ErrorOccurredEventHandler(string error);

        [Signal]
        public delegate void CharacterChangedEventHandler(CharacterCard character);

        // 核心数据
        private CharacterCard character;
        private AICommunication aiCommunication;
        private Array<ConversationMessage> conversationHistory;
        private int maxHistoryLength = 50;

        // 动态属性（从CharacterCard分离）
        private int currentHealth = 0;
        private int currentEnergy = 0;
        private int currentThirst = 0; // 保水度
        private int currentSatiety = 0; // 饱食度

        private int currentHealthModifier = 0;
        private int currentEnergyModifier = 0;
        private int currentThirstModifier = 0;
        private int currentSatietyModifier = 0;
        // thirst_consumption_rate:
        // satiety_consumption_rate:
        private int thirstConsumptionRate = 0;
        private int satietyConsumptionRate = 0;
        // 负重
        public int CurrentLoadWeight { get; private set; } = 0;

        // 投票权重
        private float currentVoteWeight = 1.0f;

        private Array<Status> currentStatus = new Array<Status>();
        // 情感
        private Mood currentMood; // 当isDevastated为true时，将不考虑currentMood影响
        private bool isDevastated = false; // 是否崩溃

        private bool isAvailable = true; // 是否空闲
        private bool isInShelter = true; // 在避难所
        private Dictionary<string, Skill> currentSkills = new Dictionary<string, Skill>();
        private WarpedInfo currentWarpedInfo = new WarpedInfo();
        private Array<Item> inventory = new Array<Item>();

        // 关系系统（动态数据）
        private Dictionary<string, Relation> relationships = new Dictionary<string, Relation>();

        // 统一记忆系统（动态数据）
        private Array<string> memories = new Array<string>(); // 通用记忆
        private Array<string> importantEvents = new Array<string>(); // 重要事件
        private Array<string> personalSecrets = new Array<string>(); // 个人秘密
        private Array<string> relationshipMemories = new Array<string>(); // 关系相关记忆


        // 状态
        private bool isActive = false;

        // 对话轮数计数（用于提醒AI更新关系）
        private int conversationRoundCount = 0;
        private const int RELATIONSHIP_UPDATE_INTERVAL = 5; // 每5轮提醒一次

        // 房间支持
        private Place currentPlace;

        // 配置
        public string AgentId { get; private set; }
        public string AgentName { get; private set; }

        public CharacterCard Character => character;
        public Array<ConversationMessage> ConversationHistory => conversationHistory;
        public bool IsActive => isActive;
        public int ConversationRoundCount => conversationRoundCount;
        public bool IsAvailable => isAvailable;
        public bool IsInShelter => isInShelter;
        // 动态属性访问器
        public int CurrentHealth => currentHealth;
        public int CurrentEnergy => currentEnergy;
        public int CurrentThirst => currentThirst;
        public int CurrentSatiety => currentSatiety;
        public int CurrentThirstConsumptionRate => thirstConsumptionRate;
        public int CurrentSatietyConsumptionRate => satietyConsumptionRate;
        public Array<Status> CurrentStatus => currentStatus;
        public Dictionary<string, Skill> CurrentSkills => currentSkills;
        public WarpedInfo CurrentWarpedInfo => currentWarpedInfo;
        public Array<Item> Inventory => inventory;
        public float CurrentVoteWeight => currentVoteWeight;
        public void SetVoteWeight(float weight)
        {
            currentVoteWeight = weight;
        }
        public void SetHealth(int health)
        {
            currentHealth = health;
        }
        public void SetEnergy(int energy)
        {
            currentEnergy = energy;
        }
        public void SetThirst(int thirst)
        {
            currentThirst = thirst;
        }
        public void SetSatiety(int satiety)
        {
            currentSatiety = satiety;
        }
        public void SetThirstConsumptionRate(int rate)
        {
            thirstConsumptionRate = rate;
        }
        public void SetSatietyConsumptionRate(int rate)
        {
            satietyConsumptionRate = rate;
        }
        // 关系系统访问器
        public Dictionary<string, Relation> Relationships => relationships;

        // 记忆系统访问器
        public Array<string> Memories => memories;
        public Array<string> ImportantEvents => importantEvents;
        public Array<string> PersonalSecrets => personalSecrets;
        public Array<string> RelationshipMemories => relationshipMemories;
        public Place CurrentPlace { get; set; }

        public Agent() { }

        public Agent(string agentId, string agentName)
        {
            AgentId = agentId;
            AgentName = agentName;
        }

        public override void _Ready()
        {
            conversationHistory = new Array<ConversationMessage>();

            GD.Print($"=== Agent初始化: {AgentName} (ID: {AgentId}) ===");
        }
        public bool IsDead()
        {
            return CurrentHealth <= 0;
        }

        public bool CanAttendCommittee()
        {
            if (IsDead())
            {
                GD.Print($"Agent {AgentName} 死亡，无法参加议会");
                return false;
            }
            if (IsActive) // 忙于某项任务
            {
                GD.Print($"Agent {AgentName} 忙于某项任务，无法参加议会");
                return false;
            }
            if (CurrentEnergy <= 0) // 能量不足
            {
                GD.Print($"Agent {AgentName} 能量不足，无法参加议会");
                return false;
            }
            if (CurrentStatus.Any(status => status.Is("unconscious"))) // 昏迷
            {
                GD.Print($"Agent {AgentName} 昏迷，无法参加议会");
                return false;
            }
            if (isDevastated) // 崩溃
            {
                GD.Print($"Agent {AgentName} 崩溃，无法参加议会");
                return false;
            }
            if (Character.WakeUpTime > 12 || Character.SleepTime < 12) // 在议会时间睡觉，睡觉时间在12点之前
            {
                GD.Print($"Agent {AgentName} 在议会时间睡觉，无法参加议会");
                return false;
            }
            if (Character.Traits.Any(trait => trait == "不愿交流")) // 不愿交流
            {
                GD.Print($"Agent {AgentName} 不愿交流，无法参加议会");
                return false;
            }
            GD.Print($"Agent {AgentName} 可以参加议会");
            return true;
        }
        /// <summary>
        /// 从CharacterCard初始化动态属性
        /// </summary>
        public void InitializeFromCharacterCard(CharacterCard card)
        {
            if (card == null) return;

            // 初始化基础状态
            currentHealth = card.MaxHealth;
            currentEnergy = card.MaxEnergy;

            // 初始化WARPED属性
            currentWarpedInfo = new WarpedInfo(card.BaseWarpedInfo);

            // 初始化技能
            if (card.Skills != null)
            {
                foreach (var skill in card.Skills)
                {
                    currentSkills[skill] = new Skill(skill, skill, "基础技能", 1, 0); // 基础等级
                }
            }

            // 初始化状态
            currentStatus.Add(new Status("health", "健康", "健康", 100));
            currentStatus.Add(new Status("normal", "正常", "正常", 100));

            GD.Print($"Agent {AgentName} 动态属性初始化完成");
        }

        /// <summary>
        /// 设置角色
        /// </summary>
        public void SetCharacter(CharacterCard newCharacter)
        {
            if (character != newCharacter)
            {
                // 如果当前有角色，先解绑
                if (character != null)
                {
                    character.UnbindFromAgent();
                }

                // 设置新角色并绑定
                if (newCharacter != null)
                {
                    // 检查角色是否可以绑定
                    if (!newCharacter.CanBindToAgent(AgentId))
                    {
                        GD.PrintErr($"角色 {newCharacter.Name} 无法绑定到Agent {AgentName}");
                        return;
                    }

                    // 绑定角色
                    if (!newCharacter.BindToAgent(AgentId))
                    {
                        GD.PrintErr($"角色 {newCharacter.Name} 绑定失败");
                        return;
                    }

                    // 房间ID现在由Agent管理，不再从CharacterCard读取
                }

                character = newCharacter;

                // 初始化动态属性
                InitializeFromCharacterCard(character);

                // 确保conversationHistory已初始化
                if (conversationHistory == null)
                {
                    conversationHistory = new Array<ConversationMessage>();
                }
                conversationHistory.Clear();

                GD.Print($"Agent {AgentName} 设置角色: {character?.Name ?? "无"}");
                EmitSignal(nameof(CharacterChanged), character);
            }
        }

        /// <summary>
        /// 获取当前角色
        /// </summary>
        public CharacterCard GetCharacter()
        {
            return character;
        }

        /// <summary>
        /// 设置AI通信模块
        /// </summary>
        public void SetAICommunication(AICommunication aiComm)
        {
            aiCommunication = aiComm;
            
            if (aiCommunication != null)
            {
                GD.Print($"Agent {AgentName} 设置AI通信模块");
            }
        }

        /// <summary>
        /// 发送消息到AI
        /// </summary>
        public async void SendMessage(string message)
        {
            if (aiCommunication == null)
            {
                GD.PrintErr($"Agent {AgentName} 的AI通信模块未设置");
                return;
            }

            // 添加用户消息到历史
            var userMessage = new ConversationMessage(message, "user", AgentId);
            AddMessageToHistory(userMessage);

            // 发送到AI
            aiCommunication.SendMessage(message, this, null, conversationHistory, false);
        }

        /// <summary>
        /// 发送消息到特定目标
        /// </summary>
        public async void SendMessageToTarget(string message, Agent targetAgent)
        {
            if (aiCommunication == null)
            {
                GD.PrintErr($"Agent {AgentName} 的AI通信模块未设置");
                return;
            }

            // 添加用户消息到历史
            var userMessage = new ConversationMessage(message, "user", AgentId);
            AddMessageToHistory(userMessage);

            // 发送到AI（私聊模式）
            aiCommunication.SendMessage(message, this, targetAgent, conversationHistory, false);
        }

        /// <summary>
        /// 在房间中说话
        /// </summary>
        public async void SpeakInRoom(string message)
        {
            if (aiCommunication == null)
            {
                GD.PrintErr($"Agent {AgentName} 的AI通信模块未设置");
                return;
            }

            // 添加用户消息到历史
            var userMessage = new ConversationMessage(message, "user", AgentId);
            AddMessageToHistory(userMessage);

            // 发送到AI（房间模式）
            aiCommunication.SendMessage(message, this, null, conversationHistory, true);
        }

        /// <summary>
        /// 添加消息到历史
        /// </summary>
        public void AddMessageToHistory(ConversationMessage message)
        {
            conversationHistory.Add(message);
            EmitSignal(nameof(MessageAdded), message);

            // 限制历史长度
            if (conversationHistory.Count > maxHistoryLength)
            {
                conversationHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// 添加用户消息到历史（自动设置AgentId）
        /// </summary>
        public void AddUserMessageToHistory(string content)
        {
            var userMessage = new ConversationMessage(content, "user", AgentId);
            AddMessageToHistory(userMessage);
            
            // 增加对话轮数计数
            conversationRoundCount++;
            
            // 检查是否需要提醒AI更新关系
            if (conversationRoundCount % RELATIONSHIP_UPDATE_INTERVAL == 0)
            {
                var reminderMessage = new ConversationMessage(
                    $"[系统提醒] 对话已进行 {conversationRoundCount} 轮，请使用 update_relationship 更新与玩家的关系数据。",
                    "system",
                    AgentId
                );
                AddMessageToHistory(reminderMessage);
            }
        }

        /// <summary>
        /// 清空对话历史
        /// </summary>
        public void ClearHistory()
        {
            conversationHistory.Clear();
            // 重置对话轮数计数
            conversationRoundCount = 0;
        }

        /// <summary>
        /// 获取对话历史文本
        /// </summary>
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
        /// 获取对话历史统计
        /// </summary>
        public string GetHistoryStats()
        {
            var userCount = 0;
            var aiCount = 0;

            foreach (var message in conversationHistory)
            {
                if (message.Role == "user")
                    userCount++;
                else
                    aiCount++;
            }

            return $"总消息数: {conversationHistory.Count}\n用户消息: {userCount}\nAI回复: {aiCount}\n对话轮数: {conversationRoundCount}";
        }

        /// <summary>
        /// 设置最大历史长度
        /// </summary>
        public void SetMaxHistoryLength(int maxLength)
        {
            maxHistoryLength = Math.Max(10, maxLength);
        }

        /// <summary>
        /// 激活Agent
        /// </summary>
        public void Activate()
        {
            isActive = true;
        }

        /// <summary>
        /// 停用Agent
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
        }

        /// <summary>
        /// 检查是否超时
        /// </summary>
        public bool IsTimeout(TimeSpan timeout)
        {
            return false;
        }

        /// <summary>
        /// 获取Agent状态信息
        /// </summary>
        public string GetStatusInfo()
        {
            var status = $"Agent状态信息:\n";
            status += $"ID: {AgentId}\n";
            status += $"名称: {AgentName}\n";
            status += $"状态: {(isActive ? "活跃" : "停用")}\n";
            status += $"角色: {character?.Name ?? "无"}\n";
            status += $"对话历史: {conversationHistory.Count} 条\n";
            status += $"对话轮数: {conversationRoundCount}\n";
            status += $"AI通信: {(aiCommunication != null ? "已连接" : "未连接")}";
            status += $"==身体状态==\n";
            status += $"健康: {currentHealth}\n";
            status += $"能量: {currentEnergy}\n";
            status += $"保水度: {currentThirst}\n";
            status += $"饱食度: {currentSatiety}\n";
            status += $"==技能==\n";
            status += $"技能: {currentSkills.Count} 个\n";
            return status;
        }

        public override void _ExitTree()
        {
            // 解绑角色
            if (character != null)
            {
                character.UnbindFromAgent();
            }

            // 离开房间
            LeavePlace();
        }


        /// <summary>
        /// 加入房间
        /// </summary>
        public bool JoinPlace(Place place)
        {
            if (place == null)
            {
                GD.PrintErr("房间对象为空");
                return false;
            }

            if (currentPlace == place)
            {
                GD.PrintErr("Agent已经在房间中");
                return true;
            }

            // 离开当前房间
            LeavePlace();

            // 加入新房间
            if (place.AddAgent(this) == true)
            {
                currentPlace = place;
                return true;
            }


            return false;
        }

        /// <summary>
        /// 离开房间
        /// </summary>
        public void LeavePlace()
        {
            if (currentPlace != null)
            {
                currentPlace.RemoveAgent(this);
                currentPlace = null;
            }
        }

        /// <summary>
        /// 获取当前房间
        /// </summary>
        public Place GetCurrentPlace()
        {
            return currentPlace;
        }

        /// <summary>
        /// 获取当前房间ID
        /// </summary>
        public string GetCurrentPlaceId()
        {
            return currentPlace?.Id ?? "";
        }

        // ========== 动态属性更新方法 ==========

        /// <summary>
        /// 更新健康值
        /// </summary>
        public void UpdateHealth(int newHealth)
        {
            currentHealth = Math.Max(0, Math.Min(newHealth, character?.MaxHealth ?? 100));
            GD.Print($"Agent {AgentName} 健康值更新: {currentHealth}");
        }

        /// <summary>
        /// 更新能量值
        /// </summary>
        public void UpdateEnergy(int newEnergy)
        {
            currentEnergy = Math.Max(0, Math.Min(newEnergy, character?.MaxEnergy ?? 100));
            GD.Print($"Agent {AgentName} 能量值更新: {currentEnergy}");
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        public void AddStatus(Status newStatus)
        {
            // Contains的本质是调用Status的Equals方法，这里已经实现了
            if (!currentStatus.Contains(newStatus))
            {
                currentStatus.Add(newStatus);
            }
            GD.Print($"Agent {AgentName} 状态更新: {newStatus}");
        }
        public void RemoveStatus(Status newStatus)
        {
            if (currentStatus.Contains(newStatus))
            {
                currentStatus.Remove(newStatus);
            }
        }   
        /// <summary>
        /// 更新技能等级
        /// </summary>
        public void UpdateSkillLevel(string skillId, int newLevel)
        {
            if (currentSkills.ContainsKey(skillId))
            {
                currentSkills[skillId].Level = Math.Max(1, newLevel);
                GD.Print($"Agent {AgentName} 技能 {skillId} 等级更新: {newLevel}");
            }
        }

        /// <summary>
        /// 更新WARPED属性
        /// </summary>
        public void UpdateWarpedAttribute(string attributeName, int newValue)
        {
            if (attributeName == "Warfare")
            {
                currentWarpedInfo.Warfare = Math.Max(0, newValue);
            }
            else if (attributeName == "Adaptability")
            {
                currentWarpedInfo.Adaptability = Math.Max(0, newValue);
            }
            else if (attributeName == "Reasoning")
            {
                currentWarpedInfo.Reasoning = Math.Max(0, newValue);
            }
            else if (attributeName == "Perception")
            {
                currentWarpedInfo.Perception = Math.Max(0, newValue);
            }
            else if (attributeName == "Endurance")
            {
                currentWarpedInfo.Endurance = Math.Max(0, newValue);
            }
            else if (attributeName == "Dexterity")
            {
                currentWarpedInfo.Dexterity = Math.Max(0, newValue);
            }
        }

        /// <summary>
        /// 添加物品到背包
        /// </summary>
        public void AddToInventory(Item item)
        {
            if (!inventory.Contains(item))
            {
                inventory.Add(item);
                GD.Print($"Agent {AgentName} 获得物品: {item}");
            }
            else
            {
                var existingItem = inventory.FirstOrDefault(i => i.Id == item.Id);
                if (existingItem != null)
                {
                    existingItem.Quantity += item.Quantity;
                }
            }
            CurrentLoadWeight = CalculateCurrentLoadWeight();
        }
        public int CalculateCurrentLoadWeight()
        {
            return inventory.Sum(i => i.CalculateTotalWeight());
        }

        /// <summary>
        /// 从背包移除物品
        /// </summary>
        public void RemoveFromInventory(Item item)
        {
            if (inventory.Contains(item))
            {
                inventory.Remove(item);
                GD.Print($"Agent {AgentName} 失去物品: {item}");
            }
            CurrentLoadWeight = CalculateCurrentLoadWeight();
        }

        // ========== 关系管理方法 ==========



        /// <summary>
        /// 获取或创建与目标角色的关系
        /// </summary>
        public Relation GetOrCreateRelation(string targetCharacterId, string targetCharacterName, int initialTrust = 50)
        {
            if (relationships.ContainsKey(targetCharacterId))
            {
                return relationships[targetCharacterId];
            }

            var newRelation = new Relation(targetCharacterId, targetCharacterName, initialTrust);
            relationships[targetCharacterId] = newRelation;

            // 记录初次见面
            var firstMeeting = $"在 {DateTime.Now:yyyy-MM-dd HH:mm} 初次见到了 {targetCharacterName}";
            newRelation.FirstMeeting = firstMeeting;
            newRelation.Impression = $"初次见面，印象尚浅";

            GD.Print($"Agent {AgentName} 与 {targetCharacterName} 建立了新关系");
            return newRelation;
        }

        /// <summary>
        /// 获取与目标角色的关系
        /// </summary>
        public Relation GetRelation(string targetCharacterId)
        {
            return relationships.ContainsKey(targetCharacterId) ? relationships[targetCharacterId] : null;
        }

        /// <summary>
        /// 更新与目标角色的关系
        /// </summary>
        public void UpdateRelation(string targetCharacterId, string interaction, int trustChange = 0, int relationshipChange = 0)
        {
            if (relationships.ContainsKey(targetCharacterId))
            {
                var relation = relationships[targetCharacterId];
                relation.RecordInteraction(interaction, trustChange, relationshipChange);

                // 记录到关系记忆中
                var memory = $"{DateTime.Now:yyyy-MM-dd HH:mm} - 与 {relation.TargetCharacterName}: {interaction}";
                relationshipMemories.Add(memory);

                if (Math.Abs(trustChange) >= 10 || Math.Abs(relationshipChange) >= 10)
                {
                    importantEvents.Add(memory);
                }
            }
        }

        /// <summary>
        /// 记录分享秘密
        /// </summary>
        public void RecordSharedSecret(string targetCharacterId, string secretType, string secretContent)
        {
            if (relationships.ContainsKey(targetCharacterId))
            {
                var relation = relationships[targetCharacterId];
                relation.RecordSharedSecret(secretType, secretContent);

                // 记录到关系记忆和个人秘密记忆
                var secretMemory = $"{DateTime.Now:yyyy-MM-dd HH:mm} - 向 {relation.TargetCharacterName} 分享了{secretType}: {secretContent}";
                relationshipMemories.Add(secretMemory);
                personalSecrets.Add(secretMemory);
            }
        }

        /// <summary>
        /// 记录帮助行为
        /// </summary>
        public void RecordFavor(string targetCharacterId, string favorDescription, bool isGiving = true)
        {
            if (relationships.ContainsKey(targetCharacterId))
            {
                var relation = relationships[targetCharacterId];
                relation.RecordFavor(favorDescription, isGiving);

                // 记录到关系记忆中
                var memory = $"{DateTime.Now:yyyy-MM-dd HH:mm} - {(isGiving ? "帮助了" : "接受了")} {relation.TargetCharacterName}: {favorDescription}";
                relationshipMemories.Add(memory);
            }
        }

        /// <summary>
        /// 记录冲突
        /// </summary>
        public void RecordConflict(string targetCharacterId, string conflictDescription, int severity = 1)
        {
            if (relationships.ContainsKey(targetCharacterId))
            {
                var relation = relationships[targetCharacterId];
                relation.RecordConflict(conflictDescription, severity);

                // 记录到关系记忆中
                var memory = $"{DateTime.Now:yyyy-MM-dd HH:mm} - 与 {relation.TargetCharacterName} 发生冲突: {conflictDescription}";
                relationshipMemories.Add(memory);

                if (severity >= 2)
                {
                    importantEvents.Add(memory);
                }
            }
        }

        /// <summary>
        /// 检查是否可以分享信息给目标角色
        /// </summary>
        public bool CanShareInformation(string targetCharacterId, int informationSecrecyLevel)
        {
            if (relationships.ContainsKey(targetCharacterId))
            {
                var relation = relationships[targetCharacterId];
                return relation.CanShareInformation(informationSecrecyLevel);
            }

            // 如果没有关系记录，使用默认的陌生人信任度
            var defaultRelation = new Relation(targetCharacterId, "未知角色", 20);
            return defaultRelation.CanShareInformation(informationSecrecyLevel);
        }

        /// <summary>
        /// 检查是否为密切接触者
        /// </summary>
        public bool IsCloseContact(string targetCharacterId, int relationshipLevel)
        {
            if (relationships.ContainsKey(targetCharacterId))
            {
                var relation = relationships[targetCharacterId];
                return relation.IsCloseContact(relationshipLevel);
            }
            return false;
        }

        // ========== 记忆管理方法 ==========

        /// <summary>
        /// 添加记忆
        /// </summary>
        public void AddMemory(string memory, string type = "memory")
        {
            var timestampedMemory = $"{DateTime.Now:yyyy-MM-dd HH:mm} - {memory}";
            if (type == "memory")
            {
                memories.Add(timestampedMemory);
            }
            else if (type == "event")
            {
                importantEvents.Add(timestampedMemory);
            }
            else if (type == "secret")
            {
                personalSecrets.Add(timestampedMemory);
            }
            else if (type == "relationship")
            {
                relationshipMemories.Add(timestampedMemory);
            }
            else
            {
                GD.PrintErr("Expecting type to be memory, event, secret, or relationship, but got " + type);
            }
        }
        public void ClearMemories(string type = "memory")
        {
            if (type == "memory")
            {
                memories.Clear();
            }
            else if (type == "event")
            {
                importantEvents.Clear();
            }
            else if (type == "secret")
            {
                personalSecrets.Clear();
            }
            else if (type == "relationship")
            {
                relationshipMemories.Clear();
            }
            else
            {
                GD.PrintErr("Expecting type to be memory, event, secret, or relationship, but got " + type);
            }
        }
        public Array<string> SearchMemories(string keyword)
        {
            // 搜索所有记忆，返回包含keyword的记忆
            Array<string> result = new Array<string>();

            // 搜索通用记忆
            if (memories.Count > 0)
            {
                foreach (var memory in memories)
                {
                    if (memory.Contains(keyword))
                    {
                        result.Add(memory);
                    }
                }
            }

            // 搜索重要事件
            if (importantEvents.Count > 0)
            {
                foreach (var eventMemory in importantEvents)
                {
                    if (eventMemory.Contains(keyword))
                    {
                        result.Add(eventMemory);
                    }
                }
            }

            // 搜索个人秘密
            if (personalSecrets.Count > 0)
            {
                foreach (var secretMemory in personalSecrets)
                {
                    if (secretMemory.Contains(keyword))
                    {
                        result.Add(secretMemory);
                    }
                }
            }

            // 搜索关系记忆
            if (relationshipMemories.Count > 0)
            {
                foreach (var relationshipMemory in relationshipMemories)
                {
                    if (relationshipMemory.Contains(keyword))
                    {
                        result.Add(relationshipMemory);
                    }
                }
            }

            if (result.Count == 0)
            {
                result.Add("未找到任何记忆，或许换一个关键词试试？");
            }
            return result;
        }
        /// <summary>
        /// 添加个人秘密
        /// </summary>
        public void AddPersonalSecret(string secret)
        {
            var timestampedSecret = $"{DateTime.Now:yyyy-MM-dd HH:mm} - {secret}";
            personalSecrets.Add(timestampedSecret);
        }

        /// <summary>
        /// 获取关系摘要
        /// </summary>
        public string GetRelationshipsSummary()
        {
            if (relationships.Count == 0)
                return "暂无关系记录";

            var summary = "关系摘要:\n";
            foreach (var relation in relationships.Values)
            {
                summary += $"\n{relation.GetRelationshipSummary()}\n";
                summary += "---";
            }
            return summary;
        }

        /// <summary>
        /// 获取记忆摘要
        /// </summary>
        public string GetMemoriesSummary(int maxMemories = 10)
        {
            if (memories.Count == 0 && importantEvents.Count == 0 && personalSecrets.Count == 0 && relationshipMemories.Count == 0)
                return "暂无记忆";

            var summary = "记忆摘要:\n";

            // 通用记忆
            if (memories.Count > 0)
            {
                summary += $"\n通用记忆 ({memories.Count} 条):\n";
                var recentMemories = new Array<string>();
                for (int i = Math.Max(0, memories.Count - maxMemories); i < memories.Count; i++)
                {
                    recentMemories.Add(memories[i]);
                }
                foreach (var memory in recentMemories)
                {
                    summary += $"• {memory}\n";
                }
            }

            // 重要事件
            if (importantEvents.Count > 0)
            {
                summary += $"\n重要事件 ({importantEvents.Count} 条):\n";
                foreach (var importantEvent in importantEvents)
                {
                    summary += $"★ {importantEvent}\n";
                }
            }

            // 个人秘密
            if (personalSecrets.Count > 0)
            {
                summary += $"\n个人秘密 ({personalSecrets.Count} 条):\n";
                foreach (var secret in personalSecrets)
                {
                    summary += $"🔒 {secret}\n";
                }
            }

            // 关系记忆
            if (relationshipMemories.Count > 0)
            {
                summary += $"\n关系记忆 ({relationshipMemories.Count} 条):\n";
                foreach (var relationshipMemory in relationshipMemories)
                {
                    summary += $"👥 {relationshipMemory}\n";
                }
            }

            return summary;
        }

        public Array<ConversationMessage> GetMessageHistory()
        {
            return conversationHistory;
        }
        public void Step(int currentTurn)
        {
            // 第一步，运行所有EffectScript
            foreach (var status in currentStatus)
            {
                status.ExecuteEffectScript(this, new Dictionary<string, Variant>
                {
                    ["current_turn"] = Variant.CreateFrom(currentTurn)
                });
                if (status.CurrentDuration >= status.Duration && status.Duration > 0)
                {
                    RemoveStatus(status);
                }
            }
            // 第二部，获取环境相关Effect
            // 第三步，普通的状态更新，例如食物消耗，能量消耗，健康值消耗等
            currentThirst -= CurrentThirstConsumptionRate;
            currentSatiety -= CurrentSatietyConsumptionRate;
            if (!IsActive)
            {
                // 获取当前食物和水资源量
                var currentFood = GameManager.Instance.ResourceManager.GetResource("food").CurrentAmount;
                var currentWater = GameManager.Instance.ResourceManager.GetResource("water").CurrentAmount;

                // 修正：消耗食物和水时，确保不会消耗超过当前资源量
                if (CurrentSatietyConsumptionRate > 0 && currentFood > 0)
                {
                    var foodToConsume = Mathf.Min(CurrentSatietyConsumptionRate, currentFood);
                    if (foodToConsume > 0)
                    {
                        GameManager.Instance.ResourceManager.ConsumeResource("food", foodToConsume, $"{AgentName}消耗");
                    }
                }
                // 否则食物不足，饿着

                if (CurrentThirstConsumptionRate > 0 && currentWater > 0)
                {
                    var waterToConsume = Mathf.Min(CurrentThirstConsumptionRate, currentWater);
                    if (waterToConsume > 0)
                    {
                        GameManager.Instance.ResourceManager.ConsumeResource("water", waterToConsume, $"{AgentName}消耗");
                    }
                }
                // 否则水不足，渴着
            }
            

            // 如果小于0，则设置为0且扣除一定健康值与能量值
            if (currentThirst < 0)
            {
                currentHealth -= (int)(currentThirst * -1.0);
                currentEnergy -= (int)(currentThirst * -1.0);
                currentThirst = 0;
            }
            if (currentSatiety < 0)
            {
                currentHealth -= (int)(currentSatiety * -1.0);
                currentEnergy -= (int)(currentSatiety * -1.0);
                currentSatiety = 0;
            }
            if(currentEnergy < 0)
            {
                currentHealth -= (int)(currentEnergy * -1.0);
                currentEnergy = 0;
            }
        }

        public string GetCharacterInfo()
        {
            return $"- 角色ID: {Character.Id}\n" +
                $"- 角色名称: {Character.Name}\n" +
                $"- 职业: {Character.Profession}\n" +
                $"- 派别: {Character.Faction}\n\n" +
                $"- 当前位置: {CurrentPlace.Name}\n\n" +
                $"- 性格: {Character.Personality}\n" +
                $"- 背景故事: {Character.BackgroundStory}\n";
        }

        /// <summary>
        /// 获取同一房间内的其他Agent
        /// </summary>
        public Array<Agent> GetAgentsInSameRoom()
        {
            var agentsInSameRoom = new Array<Agent>();
            
            if (CurrentPlace == null) return agentsInSameRoom;
            
            var characterManager = GameManager.Instance?.CharacterManager;
            if (characterManager == null) return agentsInSameRoom;
            
            foreach (var otherAgent in characterManager.AllCharacters)
            {
                // 跳过自己
                if (otherAgent.AgentId == AgentId) continue;
                
                // 检查是否在同一房间
                if (otherAgent.CurrentPlace?.Id == CurrentPlace.Id)
                {
                    agentsInSameRoom.Add(otherAgent);
                }
            }
            
            return agentsInSameRoom;
        }

        /// <summary>
        /// 获取当前房间信息
        /// </summary>
        public string GetCurrentRoomInfo()
        {
            if (CurrentPlace == null) return "未在任何房间中";
            
            var agentsInRoom = GetAgentsInSameRoom();
            var roomInfo = $"房间: {CurrentPlace.Name} (ID: {CurrentPlace.Id})\n";
            roomInfo += $"描述: {CurrentPlace.Description}\n";
            roomInfo += $"房间内角色数量: {agentsInRoom.Count + 1}"; // +1 包括自己
            
            return roomInfo;
        }

        public void SetActive(bool active)
        {
            isActive = active;
        }
    }
}
