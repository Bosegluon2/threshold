using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Threshold.Core.Enums;
using Threshold.Core.Agent;
using Threshold.Core.Data;
using Threshold.Core.Agent.Functions;


namespace Threshold.Core
{
    /// <summary>
    /// 角色管理器 - 负责管理游戏中的角色状态、函数调用、RAG系统等
    /// </summary>
    public partial class CharacterManager : Node
    {
        #region Events
        [Signal] public delegate void CharacterDiedEventHandler(Agent.Agent agent);
        [Signal] public delegate void CharacterStatusChangedEventHandler(Agent.Agent agent, CharacterStatus oldStatus, CharacterStatus newStatus);
        #endregion

        #region Character Collections
        [Export] public Array<Agent.Agent> AllCharacters { get; private set; } = new Array<Agent.Agent>();
        [Export] public Array<Agent.Agent> AliveCharacters { get; private set; } = new Array<Agent.Agent>();
        [Export] public Array<Agent.Agent> DeadCharacters { get; private set; } = new Array<Agent.Agent>();
        [Export] public Array<Agent.Agent> AvailableCharacters { get; private set; } = new Array<Agent.Agent>(); // 空闲人员列表
        
        // 专门的Player Agent，不包含在AllCharacters中
        [Export] public Agent.Agent PlayerAgent { get; private set; }
        #endregion

        #region Internal Variables
        private GameManager _gameManager;
        private AICommunication sharedAICommunication;
        private FunctionManager functionManager;
        private WorldLoreManager worldLoreManager;
        private SimpleRAG simpleRAG;
        private System.Collections.Generic.Dictionary<string, Place> placesById = new System.Collections.Generic.Dictionary<string, Place>();
        private int nextRoomId = 1;
        #endregion

        public CharacterManager(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public override void _EnterTree()
        {
            Initialize();
        }
        public Agent.Agent GetPlayerAgent()
        {
            return PlayerAgent;
        }
        

        /// <summary>
        /// 初始化角色管理器
        /// </summary>
        public void Initialize()
        {
            AllCharacters.Clear();
            AliveCharacters.Clear();
            DeadCharacters.Clear();

            // 初始化核心系统
            InitializeCoreSystems();

            // 验证初始化结果
            ValidateInitialization();

            GD.Print("角色管理器初始化完成");
        }

        /// <summary>
        /// 验证初始化结果
        /// </summary>
        private void ValidateInitialization()
        {
            GD.Print("=== 验证初始化结果 ===");
            
            if (functionManager == null)
            {
                GD.PrintErr("❌ 函数管理器未初始化");
            }
            else
            {
                var functionCount = functionManager.GetAvailableFunctions().Count;
                GD.Print($"✅ 函数管理器已初始化，可用函数: {functionCount}");
            }
            
            if (worldLoreManager == null)
            {
                GD.PrintErr("❌ 世界观管理器未初始化");
            }
            else
            {
                var entryCount = worldLoreManager.GetTotalEntries();
                GD.Print($"✅ 世界观管理器已初始化，条目数量: {entryCount}");
            }
            
            if (sharedAICommunication == null)
            {
                GD.PrintErr("❌ AI通信模块未初始化");
            }
            else
            {
                GD.Print("✅ AI通信模块已初始化");
            }
            
            if (simpleRAG == null)
            {
                GD.PrintErr("❌ RAG系统未初始化");
            }
            else
            {
                GD.Print("✅ RAG系统已初始化");
            }
            
            GD.Print($"✅ 角色列表已初始化，当前角色数量: {AllCharacters.Count}");
            GD.Print($"✅ Player角色: {(PlayerAgent != null ? PlayerAgent.AgentName : "未设置")}");
            GD.Print("=== 初始化验证完成 ===");
        }

        /// <summary>
        /// 初始化核心系统
        /// </summary>
        private void InitializeCoreSystems()
        {
            try
            {
                GD.Print("=== 初始化角色管理器核心系统 ===");
                
                // 初始化世界设定管理器
                InitializeWorldLoreManager();
                
                // 初始化函数管理器
                InitializeFunctions();
                
                // 初始化AI通信
                InitializeAICommunication();
                
                // 初始化RAG系统
                InitializeRAGSystem();
                
                // 初始化默认角色
                InitializeDefaultCharacters();
                
                GD.Print("=== 角色管理器核心系统初始化完成 ===");
            }
            catch (Exception e)
            {
                GD.PrintErr($"初始化核心系统时发生错误: {e}");
                GD.PrintErr($"堆栈跟踪: {e.StackTrace}");
            }
        }

        /// <summary>
        /// 初始化世界设定管理器
        /// </summary>
        private void InitializeWorldLoreManager()
        {
            GD.Print("正在初始化世界设定管理器...");
            worldLoreManager = new WorldLoreManager();
            try
            {
                worldLoreManager.InitializeDefaultLore();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"世界设定初始化失败: {ex.Message}");
                GD.PrintErr("继续初始化其他系统...");
            }
        }

        /// <summary>
        /// 初始化AI通信
        /// </summary>
        private void InitializeAICommunication()
        {
            GD.Print("正在初始化AI通信模块...");
            try
            {
                if (functionManager == null)
                {
                    GD.PrintErr("函数管理器未初始化，无法初始化AI通信模块");
                    return;
                }
                
                sharedAICommunication = new AICommunication();
                sharedAICommunication.SetFunctionManager(functionManager);
                AddChild(sharedAICommunication);
                
                GD.Print("AI通信模块初始化成功");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"AI通信模块初始化失败: {ex.Message}");
                GD.PrintErr($"堆栈跟踪: {ex.StackTrace}");
                GD.PrintErr("继续初始化其他系统...");
            }
        }

        /// <summary>
        /// 初始化函数
        /// </summary>
        private void InitializeFunctions()
        {
            try
            {
                GD.Print("正在初始化函数管理器...");
                
                // 首先初始化functionManager
                functionManager = new FunctionManager();
                
                if (functionManager == null)
                {
                    GD.PrintErr("函数管理器创建失败");
                    return;
                }
                
                GD.Print("开始注册函数...");
                
                // 注册所有函数
                try
                {
                    functionManager.RegisterFunction(new GetSelfInformationFunction());
                    GD.Print("GetSelfInformationFunction 注册成功");
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"GetSelfInformationFunction 注册失败: {ex.Message}");
                }
                
                try
                {
                    functionManager.RegisterFunction(new GetOtherInformationFunction());
                    GD.Print("GetOtherInformationFunction 注册成功");
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"GetOtherInformationFunction 注册失败: {ex.Message}");
                }
                try
                {
                    functionManager.RegisterFunction(new GetPlaceInformationFunction());
                    GD.Print("GetPlaceInformationFunction 注册成功");
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"GetPlaceInformationFunction 注册失败: {ex.Message}");
                }
                try
                {
                    functionManager.RegisterFunction(new UpdateRelationshipFunction());
                    GD.Print("UpdateRelationshipFunction 注册成功");
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"UpdateRelationshipFunction 注册失败: {ex.Message}");
                }
                try
                {
                    functionManager.RegisterFunction(new UpdateMemoriesFunction());
                    GD.Print("UpdateMemoriesFunction 注册成功");
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"UpdateMemoriesFunction 注册失败: {ex.Message}");
                }
                
                // 游戏相关函数
                try
                {
                    functionManager.RegisterFunction(new GetInventoryFunction());
                    GD.Print("GetInventoryFunction 注册成功");
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"GetInventoryFunction 注册失败: {ex.Message}");
                }
                
                try
                {
                    functionManager.RegisterFunction(new GetQuestsFunction());
                    GD.Print("GetQuestsFunction 注册成功");
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"GetQuestsFunction 注册失败: {ex.Message}");
                }
                
                try
                {
                    functionManager.RegisterFunction(new GetRelationshipsFunction());
                    GD.Print("GetRelationshipsFunction 注册成功");
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"GetRelationshipsFunction 注册失败: {ex.Message}");
                }
                
                try
                {
                    functionManager.RegisterFunction(new GetWorldInfoFunction());
                    GD.Print("GetWorldInfoFunction 注册成功");
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"GetWorldInfoFunction 注册失败: {ex.Message}");
                }
                
                // RAG相关函数
                if (worldLoreManager != null)
                {
                    try
                    {
                        var worldLoreRetrieval = new WorldLoreRetrievalFunction(worldLoreManager);
                        functionManager.RegisterFunction(worldLoreRetrieval);
                        GD.Print("WorldLoreRetrievalFunction 注册成功");
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"WorldLoreRetrievalFunction 注册失败: {ex.Message}");
                    }
                    
                    try
                    {
                        var characterLore = new CharacterLoreFunction(worldLoreManager);
                        functionManager.RegisterFunction(characterLore);
                        GD.Print("CharacterLoreFunction 注册成功");
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"CharacterLoreFunction 注册失败: {ex.Message}");
                    }
                }
                else
                {
                    GD.PrintErr("世界观管理器未初始化，跳过RAG函数注册");
                }
                
                var functionCount = functionManager.GetAvailableFunctions().Count;
                GD.Print($"函数系统初始化完成，共 {functionCount} 个函数");
                
                if (functionCount == 0)
                {
                    GD.PrintErr("警告：没有成功注册任何函数");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"函数初始化失败: {ex.Message}");
                GD.PrintErr($"堆栈跟踪: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 初始化RAG系统
        /// </summary>
        private void InitializeRAGSystem()
        {
            GD.Print("正在初始化RAG系统...");
            try
            {
                if (worldLoreManager != null)
                {
                    simpleRAG = new SimpleRAG(worldLoreManager);
                    GD.Print("RAG系统初始化完成");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"RAG系统初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化默认角色
        /// </summary>
        private void InitializeDefaultCharacters()
        {
            GD.Print("正在初始化默认角色...");
            
            try
            {
                // 从YAML文件加载所有人物
                var loadedCharacters = AgentLoader.LoadAllAgents();
                
                if (loadedCharacters.Count > 0)
                {
                    // 将加载的人物添加到管理器中，但player角色单独处理
                    foreach (var agent in loadedCharacters)
                    {
                        if (agent.AgentId == "player")
                        {
                            // Player角色单独处理
                            PlayerAgent = agent;
                            GD.Print($"Player角色已设置: {agent.AgentName}");
                            
                            // 设置AI通信
                            if (sharedAICommunication != null)
                            {
                                agent.SetAICommunication(sharedAICommunication);
                            }
                        }
                        else
                        {
                            // 其他角色添加到常规角色列表
                            AddCharacter(agent);
                            
                            // 设置AI通信
                            if (sharedAICommunication != null)
                            {
                                agent.SetAICommunication(sharedAICommunication);
                            }
                        }
                    }
                    
                    GD.Print($"角色加载完成，共 {AllCharacters.Count} 个常规角色，Player角色: {(PlayerAgent != null ? PlayerAgent.AgentName : "无")}");
                }
                else
                {
                    GD.PrintErr("未找到任何人物YAML文件，将创建默认角色");
                    CreateFallbackCharacters();
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载人物YAML文件失败: {ex.Message}");
                GD.PrintErr("将创建默认角色作为后备方案");
                CreateFallbackCharacters();
            }
        }

        /// <summary>
        /// 创建后备默认角色（当YAML加载失败时使用）
        /// </summary>
        private void CreateFallbackCharacters()
        {
            GD.Print("正在创建后备默认角色...");
            
            // 检查是否已有角色，如果有则不创建后备角色
            if (AllCharacters.Count > 0)
            {
                GD.Print("已有角色存在，跳过后备角色创建");
                return;
            }
            
            // 创建默认角色（不包含player角色）
            var mage = CreateCharacter(
                "艾莉娅",
                "聪明、好奇、善良但有些傲慢",
                "女",
                25,
                "魔法师",
                "魔法学院",
                "艾莉娅是魔法学院的天才学生，拥有强大的魔法天赋。她从小就对魔法充满好奇，经过多年的学习和实践，已经成为了一名出色的魔法师。"
            );
            
            var warrior = CreateCharacter(
                "雷克斯",
                "勇敢、忠诚、直率、重视荣誉",
                "男",
                32,
                "战士",
                "皇家卫队",
                "雷克斯是皇家卫队的精英战士，经历过无数战斗。他性格直率，重视荣誉和忠诚，为了保护国家和人民愿意付出一切。"
            );
            
            GD.Print($"后备默认角色创建完成，共创建 {AllCharacters.Count} 个常规角色");
        }

        /// <summary>
        /// 创建新角色
        /// </summary>
        public Agent.Agent CreateCharacter(string name, string personality, string gender, int age, string profession, string faction, string backgroundStory)
        {
            // 保护玩家ID不被使用
            if (name.ToLower() == "player" || name.ToLower() == "探索者查理")
            {
                GD.PrintErr("角色名称不能使用 'player' 或 '探索者查理'，这些名称已被保留");
                return null;
            }
            
            var characterId = GenerateCharacterId();
            var character = new CharacterCard();
            character.Id = characterId;
            character.Name = name;
            character.Personality = personality;
            character.Gender = gender;
            character.Age = age;
            character.Profession = profession;
            character.Faction = faction;
            character.BackgroundStory = backgroundStory;
            
            // 创建对应的Agent
            var agentId = GenerateAgentId();
            var agentName = $"Agent_{name}";
            Agent.Agent newAgent = new Agent.Agent(agentId, agentName);
            
            // 先添加到场景树，确保_Ready被调用
            AddChild(newAgent);
            
            // 然后设置角色
            newAgent.SetCharacter(character);
            
            // 设置AI通信
            if (sharedAICommunication != null)
            {
                newAgent.SetAICommunication(sharedAICommunication);
            }
            
            // 添加到管理器
            AddCharacter(newAgent);
            
            return newAgent;
        }

        /// <summary>
        /// 生成角色ID
        /// </summary>
        private string GenerateCharacterId()
        {
            return $"char_{AllCharacters.Count + 1:D4}";
        }

        /// <summary>
        /// 生成Agent ID
        /// </summary>
        private string GenerateAgentId()
        {
            return $"agent_{AllCharacters.Count + 1:D4}";
        }

        /// <summary>
        /// 更新角色状态
        /// </summary>
        public void UpdateCharacters(int turn)
        {
            int day = TimeUtils.GetDay(turn);
            TimeOfDay timeOfDay = TimeUtils.GetTimeOfDay(turn);
            foreach (var agent in AliveCharacters)
            {
                agent.Step(turn);
            }
        }

        /// <summary>
        /// 添加角色
        /// </summary>
        public void AddCharacter(Agent.Agent agent)
        {
            if (agent == null) return;
            
            // 检查是否已存在相同名称的角色
            if (agent.Character != null)
            {
                var existingAgent = GetCharacterByName(agent.Character.Name);
                if (existingAgent != null)
                {
                    GD.Print($"角色名称 '{agent.Character.Name}' 已存在，跳过添加");
                    return;
                }
            }
            
            // 检查是否已存在相同ID的角色
            if (!AllCharacters.Contains(agent))
            {
                AllCharacters.Add(agent);
                AliveCharacters.Add(agent);
                
                GD.Print($"添加角色: {agent.AgentName} (ID: {agent.AgentId})");
            }
            else
            {
                GD.Print($"角色已存在于列表中: {agent.AgentName}");
            }
        }

        /// <summary>
        /// 移除角色
        /// </summary>
        public void RemoveCharacter(Agent.Agent agent)
        {
            if (agent != null)
            {
                AllCharacters.Remove(agent);
                AliveCharacters.Remove(agent);
                DeadCharacters.Remove(agent);
                
                GD.Print($"移除角色: {agent.AgentName}");
            }
        }

        /// <summary>
        /// 获取角色统计信息
        /// </summary>
        public string GetCharacterStatistics()
        {
            var stats = $"角色统计:\n";
            stats += $"总角色数: {AllCharacters.Count}\n";
            stats += $"存活角色: {AliveCharacters.Count}\n";
            stats += $"死亡角色: {DeadCharacters.Count}\n";
            stats += $"Player角色: {(PlayerAgent != null ? PlayerAgent.AgentName : "未设置")}\n";
            
            return stats;
        }

        /// <summary>
        /// 获取所有角色（兼容性方法）
        /// </summary>
        public Array<Agent.Agent> GetAllAgents()
        {
            return AllCharacters;
        }

        /// <summary>
        /// 获取角色数量
        /// </summary>
        public int GetCharacterCount()
        {
            return AllCharacters.Count;
        }

        /// <summary>
        /// 根据ID查找角色
        /// </summary>
        public Agent.Agent GetCharacterById(string id)
        {
            foreach (var agent in AllCharacters)
            {
                if (agent.AgentId == id)
                {
                    return agent;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据名称查找角色
        /// </summary>
        public Agent.Agent GetCharacterByName(string name)
        {
            foreach (var agent in AllCharacters)
            {
                if (agent.AgentName == name)
                {
                    return agent;
                }
            }
            return null;
        }

        #region 系统管理方法

        /// <summary>
        /// 获取世界观管理器
        /// </summary>
        public WorldLoreManager GetWorldLoreManager()
        {
            return worldLoreManager;
        }

        /// <summary>
        /// 获取函数管理器
        /// </summary>
        public FunctionManager GetFunctionManager()
        {
            return functionManager;
        }

        /// <summary>
        /// 安全执行函数调用
        /// </summary>
        public FunctionResult SafeExecuteFunction(FunctionCall functionCall, System.Collections.Generic.Dictionary<string, object> extraParams = null)
        {
            if (functionManager == null)
            {
                GD.PrintErr("函数管理器未初始化，无法执行函数");
                return new FunctionResult(functionCall.Name, "", false, "函数管理器未初始化");
            }
            
            try
            {
                return functionManager.ExecuteFunction(functionCall, extraParams);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"执行函数时发生异常: {ex.Message}");
                GD.PrintErr($"堆栈跟踪: {ex.StackTrace}");
                return new FunctionResult(functionCall.Name, "", false, $"执行函数时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查函数系统是否可用
        /// </summary>
        public bool IsFunctionSystemAvailable()
        {
            return functionManager != null && functionManager.HasFunctions();
        }

        /// <summary>
        /// 获取AI通信模块
        /// </summary>
        public AICommunication GetAICommunication()
        {
            return sharedAICommunication;
        }

        /// <summary>
        /// 获取RAG系统
        /// </summary>
        public SimpleRAG GetRAGSystem()
        {
            return simpleRAG;
        }

        #endregion

        #region 房间管理方法

        /// <summary>
        /// 创建新房间
        /// </summary>
        public Place CreateRoom(string roomName, string description = "")
        {
            var roomId = GenerateRoomId();
            var room = new Place(roomId, roomName, description, "room");
            
            placesById[roomId] = room;
            return room;
        }

        /// <summary>
        /// 根据ID获取房间
        /// </summary>
        public Place GetRoomById(string roomId)
        {
            return placesById.ContainsKey(roomId) ? placesById[roomId] : null;
        }

        /// <summary>
        /// 获取所有房间
        /// </summary>
        public Array<Place> GetAllRooms()
        {
            var result = new Array<Place>();
            foreach (var room in placesById.Values)
            {
                result.Add(room);
            }
            return result;
        }

        /// <summary>
        /// 删除房间
        /// </summary>
        public bool DeleteRoom(string roomId)
        {
            if (placesById.ContainsKey(roomId))
            {
                var room = placesById[roomId];
                
                // 让所有Agent离开房间
                var agents = room.GetAllAgents();
                foreach (var agent in agents)
                {
                    if (agent != null)
                    {
                        agent.LeavePlace();
                    }
                }
                
                placesById.Remove(roomId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 生成房间ID
        /// </summary>
        private string GenerateRoomId()
        {
            return $"room_{nextRoomId++:D4}";
        }

        /// <summary>
        /// 获取房间统计信息
        /// </summary>
        public string GetRoomStats()
        {
            var stats = $"房间统计信息:\n";
            stats += $"总房间数: {placesById.Count}\n";
            
            foreach (var room in placesById.Values)
            {
                stats += $"  - {room.Name}: {room.GetAgentCount()} 个Agent\n";
            }
            
            return stats;
        }

        #endregion

        #region 统计和状态方法

        /// <summary>
        /// 获取系统统计信息
        /// </summary>
        public string GetSystemStats()
        {
            var totalAgents = AllCharacters.Count;
            var activeAgents = 0;
            var totalMessages = 0;
            
            foreach (var agent in AllCharacters)
            {
                if (agent.IsActive)
                    activeAgents++;
                totalMessages += agent.ConversationHistory.Count;
            }
            
            var stats = $"系统统计信息:\n";
            stats += $"总Agent数: {totalAgents}\n";
            stats += $"活跃Agent数: {activeAgents}\n";
            stats += $"总消息数: {totalMessages}\n";
            stats += $"可用角色: {AllCharacters.Count}\n";
            
            if (worldLoreManager != null)
                stats += $"世界观条目: {worldLoreManager.GetTotalEntries()}\n";
            
            if (functionManager != null)
                stats += $"可用函数: {functionManager.GetAvailableFunctions().Count}";
            
            return stats;
        }

        /// <summary>
        /// 获取所有Agent的详细状态
        /// </summary>
        public string GetAllAgentsStatus()
        {
            var status = "所有Agent状态:\n\n";
            
            foreach (var agent in AllCharacters)
            {
                status += $"--- {agent.AgentName} ---\n";
                status += agent.GetStatusInfo();
                status += "\n\n";
            }
            
            return status;
        }

        /// <summary>
        /// 清空所有Agent
        /// </summary>
        public void ClearAllAgents()
        {
            foreach (var agent in AllCharacters)
            {
                agent.QueueFree();
            }
            
            AllCharacters.Clear();
            AliveCharacters.Clear();
            DeadCharacters.Clear();
            GD.Print("所有Agent已清空");
        }

        #endregion

        #region 角色管理方法

        /// <summary>
        /// 删除角色
        /// </summary>
        public void DeleteCharacter(string characterId)
        {
            var agent = GetCharacterById(characterId);
            if (agent != null)
            {
                RemoveCharacter(agent);
                agent.QueueFree();
                GD.Print($"角色 {agent.AgentName} 已删除");
            }
        }

        /// <summary>
        /// 获取角色名称列表
        /// </summary>
        public Array<string> GetCharacterNames()
        {
            var result = new Array<string>();
            foreach (var agent in AllCharacters)
            {
                result.Add(agent.AgentName);
            }
            return result;
        }

        /// <summary>
        /// 检查角色是否存在
        /// </summary>
        public bool CharacterExists(string characterId)
        {
            return GetCharacterById(characterId) != null;
        }

        /// <summary>
        /// 检查角色名称是否存在
        /// </summary>
        public bool CharacterNameExists(string characterName)
        {
            return GetCharacterByName(characterName) != null;
        }

        #endregion

        /// <summary>
        /// 更新可用角色列表
        /// </summary>
        public void UpdateAvailableCharacters()
        {
            AvailableCharacters.Clear();
            
            foreach (var agent in AllCharacters)
            {
                if (agent != null && agent.IsActive == false && !agent.IsDead())
                {
                    AvailableCharacters.Add(agent);
                }
            }
            
            GD.Print($"可用角色列表已更新，共 {AvailableCharacters.Count} 个空闲人员");
        }

        /// <summary>
        /// 获取可用角色统计信息
        /// </summary>
        public string GetAvailableCharactersInfo()
        {
            var info = $"可用角色信息:\n";
            info += $"总角色数: {AllCharacters.Count}\n";
            info += $"存活角色: {AliveCharacters.Count}\n";
            info += $"死亡角色: {DeadCharacters.Count}\n";
            info += $"空闲角色: {AvailableCharacters.Count}\n";
            info += $"Player角色: {(PlayerAgent != null ? PlayerAgent.AgentName : "未设置")}\n";
            
            return info;
        }
    }
}