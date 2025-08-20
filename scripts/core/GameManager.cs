using Godot;
using Godot.Collections;
using System;
using Threshold.Core.Agent;
using Threshold.Core.Enums;
using Threshold.Core;
using Threshold.Core.Utils;

namespace Threshold.Core
{
    /// <summary>
    /// 总游戏管理器 - 单例模式，管理所有游戏系统
    /// </summary>
    public partial class GameManager : Node
    {
        #region Singleton
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GD.PrintErr("GameManager实例未初始化！");
                }
                return _instance;
            }
        }
        #endregion
        #region Seed
        [Export] public ulong Seed { get; private set; } = 0;
        #endregion
        public int CurrentTurn { get; private set; } = 0;
        #region World Info
        [Export] public WorldABC WorldABC { get; private set; }
        #endregion
        #region Core Managers
        [Export] public CharacterManager CharacterManager { get; private set; }
        [Export] public Library Library { get; private set; }
        [Export] public ScriptExecutor ScriptExecutor { get; private set; }
        [Export] public ResourceManager ResourceManager { get; private set; }
        [Export] public MissionManager MissionManager { get; private set; }
        [Export] public WorldManager WorldManager { get; private set; }
        [Export] public CommitteeManager CommitteeManager { get; private set; }
        [Export] public SaveManager SaveManager { get; private set; }
        #endregion

        #region Game State
        [Export] public GameState CurrentGameState { get; private set; } = GameState.NotStarted;
        [Export] public bool IsGamePaused { get; private set; } = false;
        [Export] public bool IsGameOver { get; private set; } = false;
        #endregion

        #region Game Settings
        [Export] public int TotalDays { get; private set; } = 10;
        [Export] public int TurnsPerDay { get; private set; } = 4;
        [Export] public int TotalTurns { get; private set; } = 40;
        #endregion

        #region Events
        [Signal] public delegate void GameStartedEventHandler();
        [Signal] public delegate void GamePausedEventHandler();
        [Signal] public delegate void GameResumedEventHandler();
        [Signal] public delegate void GameOverEventHandler();
        [Signal] public delegate void TurnChangedEventHandler(int turn);
        [Signal] public delegate void GameStateChangedEventHandler(int newState);
        #endregion

        public override void _EnterTree()
        {
            // 单例模式实现
            if (_instance != null && _instance != this)
            {
                GD.PrintErr("GameManager实例已存在，销毁重复实例");
                QueueFree();
                return;
            }
            
            _instance = this;
            GD.Print("GameManager实例创建成功");
        }

        public override void _Ready()
        {
            InitializeManagers();
            ConnectSignals();
        }

        /// <summary>
        /// 初始化所有管理器
        /// </summary>
        private void InitializeManagers()
        {
            // 创建并初始化各个管理器
            //EventManager = new EventManager(this, Seed);
            ResourceManager = new ResourceManager(this);
            WorldManager = new WorldManager(this, Seed);
            MissionManager = new MissionManager();
            //SaveManager = new SaveManager(this, Seed);
            Library = new Library();
            WorldABC = new WorldABC(this);
            CommitteeManager = new CommitteeManager(this);
            // 先添加Library，确保数据加载
            AddChild(Library);

            // 加载Library数据
            LibraryLoader.LoadAllDataToLibrary();

            // 然后创建CharacterManager（因为它依赖Library数据）
            CharacterManager = new CharacterManager(this);

            //AddChild(EventManager);
            AddChild(ResourceManager);
            AddChild(CharacterManager);
            AddChild(WorldManager);
            AddChild(MissionManager);
            //AddChild(SaveManager);
            AddChild(WorldABC);
            AddChild(CommitteeManager);
            GD.Print("所有管理器初始化完成");
            GD.Print(WorldABC.GetEssentialInfo());
        }
        public bool Step()
        {
            // 将回合数+1 并且更新世界状态
            CurrentTurn++;
            WorldManager.UpdateWorldState(CurrentTurn);
            // EventManager.TriggerEvents(CurrentTurn);
            CharacterManager.UpdateCharacters(CurrentTurn);
            ResourceManager.UpdateResources(CurrentTurn);
            foreach (var mission in MissionManager.ActiveMissions)
            {
                var result = MissionManager.StepMission(mission, CurrentTurn);
                if (!result)
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 连接信号
        /// </summary>
        private void ConnectSignals()
        {
            // 连接各个管理器的信号
            // EventManager.EventTriggered += OnEventTriggered;
            CharacterManager.CharacterDied += OnCharacterDied;
            WorldManager.WorldStateChanged += OnWorldStateChanged;
        }

        /// <summary>
        /// 开始新游戏
        /// </summary>
        public void StartNewGame()
        {
            try
            {
                GD.Print("开始新游戏...");
                
                // 重置游戏状态
                ResetGameState();
                
                // 初始化游戏数据
                InitializeGameData();
                
                
                // 更新游戏状态
                SetGameState(GameState.Playing);
                
                // 发出游戏开始信号
                EmitSignal(SignalName.GameStarted);
                
                GD.Print("新游戏开始成功");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"开始新游戏失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载游戏
        /// </summary>
        public void LoadGame(string saveSlot)
        {
            try
            {
                GD.Print($"加载游戏存档: {saveSlot}");
                
                if (SaveManager.LoadGame(saveSlot))
                {
                    SetGameState(GameState.Playing);
                    EmitSignal(SignalName.GameStarted);
                    GD.Print("游戏加载成功");
                }
                else
                {
                    GD.PrintErr("游戏加载失败");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"加载游戏失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存游戏
        /// </summary>
        public void SaveGame(string saveSlot)
        {
            try
            {
                GD.Print($"保存游戏到存档: {saveSlot}");
                
                if (SaveManager.SaveGame(saveSlot))
                {
                    GD.Print("游戏保存成功");
                }
                else
                {
                    GD.PrintErr("游戏保存失败");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"保存游戏失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            if (CurrentGameState == GameState.Playing)
            {
                IsGamePaused = true;
                EmitSignal(SignalName.GamePaused);
                GD.Print("游戏已暂停");
            }
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            if (IsGamePaused)
            {
                IsGamePaused = false;
                EmitSignal(SignalName.GameResumed);
                GD.Print("游戏已恢复");
            }
        }

        /// <summary>
        /// 结束游戏
        /// </summary>
        public void EndGame()
        {
            IsGameOver = true;
            SetGameState(GameState.GameOver);
            EmitSignal(SignalName.GameOver);
            GD.Print("游戏结束");
        }

        /// <summary>
        /// 重置游戏状态
        /// </summary>
        private void ResetGameState()
        {
            CurrentGameState = GameState.NotStarted;
            IsGamePaused = false;
            IsGameOver = false;
        }

        /// <summary>
        /// 初始化游戏数据
        /// </summary>
        private void InitializeGameData()
        {
            // 初始化各个管理器的数据
            //EventManager.InitializeEventForest();
            ResourceManager.Initialize();
            CharacterManager.Initialize();
            WorldManager.Initialize();
            
            GD.Print("游戏数据初始化完成");
        }

        /// <summary>
        /// 设置游戏状态
        /// </summary>
        private void SetGameState(GameState newState)
        {
            if (CurrentGameState != newState)
            {
                var oldState = CurrentGameState;
                CurrentGameState = newState;
                
                EmitSignal(SignalName.GameStateChanged, newState.GetHashCode());
                GD.Print($"游戏状态变化: {oldState} -> {newState}");
            }
        }


        private void OnEventTriggered(EventNode gameEvent)
        {
            GD.Print($"事件触发: {gameEvent.EventName}");
            // 处理事件逻辑
        }

        private void OnCharacterDied(Agent.Agent agent)
        {
            GD.Print($"角色死亡: {agent.AgentName}");
            // 处理角色死亡逻辑
        }

        private void OnWorldStateChanged(WorldState newState)
        {
            GD.Print($"世界状态变化: {newState}");
            // 处理世界状态变化逻辑
        }


        /// <summary>
        /// 处理回合变化
        /// </summary>
        private void HandleTurnChange(int turn)
        {
            // 根据时段触发不同的事件
            // EventManager.TriggerEvents(turn);
            
            // 更新世界状态
            WorldManager.UpdateWorldState(turn);
            
            // 更新角色状态
            CharacterManager.UpdateCharacters(turn);
            
            // 更新资源状态
            ResourceManager.UpdateResources(turn);
        }

        /// <summary>
        /// 获取当前游戏信息
        /// </summary>
        public string GetGameInfo()
        {
            var info = $"游戏状态: {CurrentGameState}\n";
            info += $"当前回合: 第{WorldManager.CurrentTurn}回合\n";
            info += $"当前时段: {WorldManager.GetTimeOfDay()}\n";
            info += $"游戏暂停: {IsGamePaused}\n";
            info += $"游戏结束: {IsGameOver}\n";
            
            return info;
        }

        public override void _ExitTree()
        {
            // 清理资源
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
