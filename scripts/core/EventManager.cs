using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Threshold.Core.Conditions;
using Threshold.Core.Utils;
using Threshold.Core.Utils;

namespace Threshold.Core
{
    /// <summary>
    /// 事件状态枚举
    /// </summary>
    public enum EventStatus
    {
        Inactive,       // 未激活
        Active,         // 激活
        InProgress,     // 进行中
        Completed,      // 完成
        Failed,         // 失败
        Expired         // 过期
    }



    /// <summary>
    /// 事件类型枚举
    /// </summary>
    public enum EventType
    {
        Main,           // 主线
        Side,           // 支线
        World,          // 世界
        Daily,          // 日常
        Character       // 角色
    }

    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum TaskStatus
    {
        NotStarted,     // 未开始
        InProgress,     // 进行中
        Completed,      // 完成
        Failed          // 失败
    }

    /// <summary>
    /// 任务类型枚举
    /// </summary>
    public enum TaskType
    {
        Collection,     // 收集
        Combat,         // 战斗
        Dialogue,       // 对话
        Exploration,    // 探索
        Production,     // 生产
        Training        // 训练
    }

    /// <summary>
    /// 事件条件
    /// </summary>
    public class EventCondition
    {
        public string Type { get; set; } = ""; // "simple", "script", "and", "or", "not"
        public string Target { get; set; } = "";
        public string Property { get; set; } = "";
        public object Value { get; set; }
        public string Operator { get; set; } = "==";
        public bool Required { get; set; } = true;
        
        // 新增：脚本条件支持
        public string ConditionScript { get; set; } = ""; // GDScript脚本
        public string ConditionId { get; set; } = ""; // 条件ID
        public string Description { get; set; } = ""; // 条件描述
        
        // 新增：组合条件支持
        public List<EventCondition> SubConditions { get; set; } = new List<EventCondition>();
        
        public EventCondition()
        {
            SubConditions = new List<EventCondition>();
        }
        
        /// <summary>
        /// 评估条件
        /// </summary>
        public bool Evaluate(GameManager gameManager)
        {
            try
            {
                switch (Type.ToLower())
                {
                    case "simple":
                        return EvaluateSimpleCondition(gameManager);
                    case "script":
                        return EvaluateScriptCondition(gameManager);
                    case "and":
                        return SubConditions.All(c => c.Evaluate(gameManager));
                    case "or":
                        return SubConditions.Any(c => c.Evaluate(gameManager));
                    case "not":
                        return SubConditions.Count > 0 && !SubConditions[0].Evaluate(gameManager);
                    default:
                        GD.PrintErr($"未知的条件类型: {Type}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"评估条件时发生错误: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 评估简单条件
        /// </summary>
        private bool EvaluateSimpleCondition(GameManager gameManager)
        {
            // 这里保留原有的简单条件逻辑，用于向后兼容
            // 实际项目中建议使用脚本条件
            return true; // 简化实现
        }
        
        /// <summary>
        /// 评估脚本条件
        /// </summary>
        private bool EvaluateScriptCondition(GameManager gameManager)
        {
            if (string.IsNullOrEmpty(ConditionScript))
                return false;
                
            try
            {
                var context = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["gameManager"] = Variant.CreateFrom(gameManager),
                    ["global"] = Variant.CreateFrom(Global.Instance),
                    ["condition"] = Variant.CreateFrom(ConditionId) 
                };
                
                var result = ScriptExecutor.Instance.ExecuteScript(ConditionScript, context);
                return result.AsBool();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"执行脚本条件时发生错误: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 分支类型枚举
    /// </summary>
    public enum BranchType
    {
        Manual,         // 手动分支 - 每个选项都有enable条件
        Auto            // 自动分支 - 按index顺序评估，返回第一个满足条件的
    }
    
    /// <summary>
    /// 事件分支
    /// </summary>
    public class EventBranch
    {
        public string BranchId { get; set; } = "";
        public string BranchName { get; set; } = "";
        public BranchType Type { get; set; } = BranchType.Manual;
        public List<EventCondition> Conditions { get; set; } = new List<EventCondition>();
        public List<EventReference> NextEvents { get; set; } = new List<EventReference>();
        public float Probability { get; set; } = 1.0f;
        
        // 新增：enable条件，用于判断分支选项是否可用
        public List<BaseCondition> EnableConditions { get; set; } = new List<BaseCondition>();
        
        public EventBranch()
        {
            Conditions = new List<EventCondition>();
            NextEvents = new List<EventReference>();
            EnableConditions = new List<BaseCondition>();
        }
        
        /// <summary>
        /// 检查分支是否可用
        /// </summary>
        public bool IsEnabled(GameManager gameManager)
        {
            if (EnableConditions == null || EnableConditions.Count == 0)
                return true; // 没有条件限制，默认可用
                
            return EnableConditions.All(c => c.Evaluate(gameManager));
        }
        
        /// <summary>
        /// 检查分支条件是否满足（使用新的脚本条件系统）
        /// </summary>
        public bool AreConditionsMet(GameManager gameManager)
        {
            if (Conditions == null || Conditions.Count == 0)
                return true; // 没有条件限制，默认满足
                
            return Conditions.All(c => c.Evaluate(gameManager));
        }
    }

    /// <summary>
    /// 事件引用
    /// </summary>
    public class EventReference
    {
        public string EventId { get; set; } = "";
        public string EventName { get; set; } = "";
        public int ActivationDelay { get; set; } = 0;
    }

    /// <summary>
    /// 任务目标
    /// </summary>
    public class TaskObjective
    {
        public string Type { get; set; } = "";
        public string Target { get; set; } = "";
        public int Required { get; set; } = 1;
        public int Current { get; set; } = 0;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 奖励
    /// </summary>
    public class Reward
    {
        public string Type { get; set; } = "";
        public string Target { get; set; } = "";
        public object Value { get; set; }
        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// 效果引用 - 自包含的Effect，包含脚本和执行逻辑
    /// </summary>
    public class EffectReference
    {
        public string EffectId { get; set; } = "";
        public string EffectType { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public string EffectScript { get; set; } = ""; // 效果脚本
        public int Duration { get; set; } = 0; // 持续时间
        public int RemainingTurns { get; set; } = 0; // 剩余回合数
        
        public EffectReference()
        {
            Parameters = new Dictionary<string, object>();
            RemainingTurns = Duration;
        }
        
        /// <summary>
        /// 执行效果 - 直接执行自己的脚本
        /// </summary>
        public Variant ExecuteEffect(GameManager gameManager, object target = null)
        {
            if (string.IsNullOrEmpty(EffectScript))
                return Variant.CreateFrom("");
                
            try
            {
                var context = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["gameManager"] = Variant.CreateFrom(gameManager),
                    ["global"] = Variant.CreateFrom(Global.Instance),
                    ["effect"] = Variant.CreateFrom(EffectId),
                    ["target"] = Variant.CreateFrom(target?.ToString() ?? ""),
                    ["parameters"] = Variant.CreateFrom(Parameters?.ToString() ?? ""),
                    ["duration"] = Variant.CreateFrom(Duration),
                    ["remainingTurns"] = Variant.CreateFrom(RemainingTurns)
                };
                
                var result = ScriptExecutor.Instance.ExecuteScript(EffectScript, context);
                GD.Print($"效果执行完成: {EffectId}, 结果: {result}");
                return result;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"执行效果时发生错误: {ex.Message}");
                return Variant.CreateFrom("");
            }
        }
        
        /// <summary>
        /// 更新效果状态
        /// </summary>
        public void UpdateEffect()
        {
            if (RemainingTurns > 0)
            {
                RemainingTurns--;
            }
        }
        
        /// <summary>
        /// 检查效果是否已过期
        /// </summary>
        public bool IsExpired()
        {
            return RemainingTurns <= 0;
        }
    }

    /// <summary>
    /// 事件节点
    /// </summary>
    public class EventNode
    {
        // 基础信息
        public string EventId { get; set; } = "";
        public string EventName { get; set; } = "";
        public string Description { get; set; } = "";
        public EventType Type { get; set; } = EventType.Side;
        
        // 时间控制
        public int ActivationTurn { get; set; } = 0;
        public int ExpirationTurn { get; set; } = 0;
        public int Duration { get; set; } = 0;
        public bool IsPersistent { get; set; } = false;
        
        // 事件状态
        public EventStatus Status { get; set; } = EventStatus.Inactive;
        public int CurrentProgress { get; set; } = 0;
        public List<string> CompletedTasks { get; set; } = new List<string>();
        
        // 任务绑定
        public List<Task> RequiredTasks { get; set; } = new List<Task>();
        
        // 分支逻辑
        public List<EventBranch> Branches { get; set; } = new List<EventBranch>();
        public string CurrentBranch { get; set; } = "";
        
        // 前置条件
        public List<EventCondition> Prerequisites { get; set; } = new List<EventCondition>();
        public List<EventCondition> CompletionConditions { get; set; } = new List<EventCondition>();
        
        // 结果影响
        public List<EffectReference> WorldEffects { get; set; } = new List<EffectReference>();
        public List<EffectReference> CharacterEffects { get; set; } = new List<EffectReference>();
        
        // 循环任务
        public List<Task> CyclicTasks { get; set; } = new List<Task>();
        
        // 结束标识
        public bool IsEnding { get; set; } = false;
        public string EndingType { get; set; } = "";
        
        // 构造函数
        public EventNode()
        {
            RequiredTasks = new List<Task>();
            Branches = new List<EventBranch>();
            Prerequisites = new List<EventCondition>();
            CompletionConditions = new List<EventCondition>();
            WorldEffects = new List<EffectReference>();
            CharacterEffects = new List<EffectReference>();
            CyclicTasks = new List<Task>();
            CompletedTasks = new List<string>();
        }
        
        /// <summary>
        /// 检查事件是否可以激活
        /// </summary>
        public bool CanActivate(int currentTurn)
        {
            if (Status != EventStatus.Inactive)
                return false;
                
            if (currentTurn < ActivationTurn)
                return false;
                
            return EvaluateConditions(Prerequisites);
        }
        
        /// <summary>
        /// 检查事件是否可以完成
        /// </summary>
        public bool CanComplete()
        {
            if (Status != EventStatus.InProgress)
                return false;
                
            return EvaluateConditions(CompletionConditions);
        }
        
        /// <summary>
        /// 检查事件是否过期
        /// </summary>
        public bool IsExpired(int currentTurn)
        {
            if (IsPersistent)
                return false;
                
            return currentTurn >= ExpirationTurn;
        }
        
        /// <summary>
        /// 评估条件列表
        /// </summary>
        private bool EvaluateConditions(List<EventCondition> conditions)
        {
            if (conditions == null || conditions.Count == 0)
                return true;
                
            foreach (var condition in conditions)
            {
                if (condition.Required && !EvaluateCondition(condition))
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// 评估单个条件
        /// </summary>
        private bool EvaluateCondition(EventCondition condition)
        {
            // 暂时返回true，后续完善
            return true;
        }
    }

    /// <summary>
    /// 任务
    /// </summary>
    public class Task
    {
        // 基础信息
        public string TaskId { get; set; } = "";
        public string TaskName { get; set; } = "";
        public string Description { get; set; } = "";
        
        // 任务类型
        public TaskType Type { get; set; } = TaskType.Exploration;
        
        // 目标要求
        public List<TaskObjective> Objectives { get; set; } = new List<TaskObjective>();
        public int RequiredProgress { get; set; } = 100;
        public int CurrentProgress { get; set; } = 0;
        
        // 奖励系统
        public List<Reward> Rewards { get; set; } = new List<Reward>();
        public float VoteWeight { get; set; } = 1.0f;
        
        // 状态管理
        public TaskStatus Status { get; set; } = TaskStatus.NotStarted;
        public bool IsOptional { get; set; } = false;
        
        // 循环任务属性
        public int Cooldown { get; set; } = 0;
        public int LastExecutionTurn { get; set; } = -1;
        
        // 构造函数
        public Task()
        {
            Objectives = new List<TaskObjective>();
            Rewards = new List<Reward>();
        }
        
        /// <summary>
        /// 检查任务是否可以执行
        /// </summary>
        public bool CanExecute(int currentTurn)
        {
            if (Status == TaskStatus.Completed)
                return false;
                
            if (Cooldown > 0 && currentTurn - LastExecutionTurn < Cooldown)
                return false;
                
            return true;
        }
        
        /// <summary>
        /// 更新任务进度
        /// </summary>
        public void UpdateProgress(int progress)
        {
            CurrentProgress = Math.Min(CurrentProgress + progress, RequiredProgress);
            
            if (CurrentProgress >= RequiredProgress)
            {
                Status = TaskStatus.Completed;
            }
        }
    }

    /// <summary>
    /// 事件树
    /// </summary>
    public class EventTree
    {
        public string TreeId { get; set; } = "";
        public string TreeName { get; set; } = "";
        public EventType TreeType { get; set; } = EventType.Side;
        public int Priority { get; set; } = 50;
        public bool IsRequired { get; set; } = false;
        
        // 根事件
        public EventNode RootEvent { get; set; }
        
        // 所有事件节点
        public List<EventNode> AllEvents { get; set; } = new List<EventNode>();
        
        // 事件依赖关系
        public Dictionary<string, List<string>> EventDependencies { get; set; } = new Dictionary<string, List<string>>();
        
        // 构造函数
        public EventTree()
        {
            AllEvents = new List<EventNode>();
            EventDependencies = new Dictionary<string, List<string>>();
        }
        
        /// <summary>
        /// 添加事件节点
        /// </summary>
        public void AddEvent(EventNode eventNode)
        {
            if (!AllEvents.Any(e => e.EventId == eventNode.EventId))
            {
                AllEvents.Add(eventNode);
            }
        }
        
        /// <summary>
        /// 获取事件节点
        /// </summary>
        public EventNode GetEvent(string eventId)
        {
            return AllEvents.FirstOrDefault(e => e.EventId == eventId);
        }
    }

    /// <summary>
    /// 事件管理器
    /// </summary>
    public partial class EventManager : Node
    {
        // 核心数据
        private Dictionary<string, EventTree> allEventTrees = new Dictionary<string, EventTree>();
        private Dictionary<string, EventNode> activeEvents = new Dictionary<string, EventNode>();
        private Dictionary<string, EventNode> completedEvents = new Dictionary<string, EventNode>();
        private List<EventTree> eventForest = new List<EventTree>();

        // 随机种子管理
        private ulong worldSeed = 0;
        private RandomNumberGenerator random = new RandomNumberGenerator();

        // 回合管理
        private int currentTurn = 0;
        private const int MAX_TURNS = 120;

        // Effect管理 - 分布式管理
        private List<EffectReference> worldEffects = new List<EffectReference>();
        private List<EffectReference> characterEffects = new List<EffectReference>();

        // 事件树池
        private EventTreePool eventTreePool;

        // 信号
        [Signal]
        public delegate void EventActivatedEventHandler(string eventId, string eventName);

        [Signal]
        public delegate void EventCompletedEventHandler(string eventId, string eventName);

        [Signal]
        public delegate void EventExpiredEventHandler(string eventId, string eventName);

        [Signal]
        public delegate void TaskCompletedEventHandler(string taskId, string taskName);

        GameManager _gameManager;
        public EventManager(GameManager gameManager, ulong seed)
        {
            _gameManager = gameManager;
            worldSeed = seed;
            random = new RandomNumberGenerator();
            random.Seed = seed;
        }

        public EventManager(ulong seed)
        {
            worldSeed = seed;
            random = new RandomNumberGenerator();
            random.Seed = seed;
        }

        // 初始化
        public override void _Ready()
        {
            eventTreePool = new EventTreePool();
            GD.Print("EventManager 初始化完成");
        }

        /// <summary>
        /// 初始化事件森林
        /// </summary>
        public void InitializeEventForest()
        {
            GD.Print($"=== 开始初始化事件森林，种子: {worldSeed} ===");

            // 初始化事件树池
            eventTreePool.InitializeEventTreePool(worldSeed);

            // 获取所有事件树
            allEventTrees = eventTreePool.GetAllEventTrees();
            eventForest = allEventTrees.Values.ToList();

            GD.Print($"事件森林初始化完成，共 {eventForest.Count} 棵事件树");

            // 激活根事件
            ActivateRootEvents();
        }











        /// <summary>
        /// 激活根事件
        /// </summary>
        private void ActivateRootEvents()
        {
            foreach (var tree in eventForest)
            {
                if (tree.RootEvent != null)
                {
                    tree.RootEvent.Status = EventStatus.Active;
                    activeEvents[tree.RootEvent.EventId] = tree.RootEvent;

                    // 激活相关任务
                    ActivateEventTasks(tree.RootEvent);

                    GD.Print($"激活根事件: {tree.RootEvent.EventName}");
                    EmitSignal(SignalName.EventActivated, tree.RootEvent.EventId, tree.RootEvent.EventName);
                }
            }
        }

        /// <summary>
        /// 推进回合
        /// </summary>
        public void AdvanceTurn(int turn)
        {
            currentTurn = turn;
            // 当前需保证turn是递增的，未来可以优化

            GD.Print($"=== 回合 {currentTurn} 开始 ===");

            // 激活新事件
            ActivateEvents(currentTurn);

            // 检查过期事件
            ExpireEvents(currentTurn);

            // 评估分支条件
            EvaluateBranches();

            // 更新常驻事件
            UpdatePersistentEvents();

            // 更新效果 - 分布式Effect管理
            UpdateEffects();
            RemoveExpiredEffects();

            GD.Print($"回合 {currentTurn} 推进完成，当前活跃事件: {activeEvents.Count}, 世界效果: {worldEffects.Count}, 角色效果: {characterEffects.Count}");
        }





        /// <summary>
        /// 激活事件
        /// </summary>
        private void ActivateEvents(int turn)
        {
            var eventsToActivate = new List<EventNode>();

            foreach (var tree in eventForest)
            {
                foreach (var eventNode in tree.AllEvents)
                {
                    if (eventNode.CanActivate(turn))
                    {
                        eventsToActivate.Add(eventNode);
                    }
                }
            }

            foreach (var eventNode in eventsToActivate)
            {
                eventNode.Status = EventStatus.Active;
                activeEvents[eventNode.EventId] = eventNode;

                // 激活相关任务
                ActivateEventTasks(eventNode);

                GD.Print($"事件 {eventNode.EventName} 在第 {turn} 回合激活");
                EmitSignal(SignalName.EventActivated, eventNode.EventId, eventNode.EventName);
            }
        }

        /// <summary>
        /// 过期事件
        /// </summary>
        private void ExpireEvents(int turn)
        {
            var expiredEvents = activeEvents.Values
                .Where(e => e.IsExpired(turn) && !ShouldPreventExpiration(e))
                .ToList();

            foreach (var eventNode in expiredEvents)
            {
                eventNode.Status = EventStatus.Expired;
                activeEvents.Remove(eventNode.EventId);

                GD.Print($"事件 {eventNode.EventName} 在第 {turn} 回合过期");
                EmitSignal(SignalName.EventExpired, eventNode.EventId, eventNode.EventName);
            }
        }

        /// <summary>
        /// 检查是否应该阻止事件过期
        /// </summary>
        private bool ShouldPreventExpiration(EventNode eventNode)
        {
            // 如果事件处于InProgress状态且有分支选项，阻止过期
            // 因为用户需要做出选择
            if (eventNode.Status == EventStatus.InProgress &&
                eventNode.Branches != null &&
                eventNode.Branches.Count > 0)
            {
                GD.Print($"事件 {eventNode.EventName} 有分支选择，阻止过期");
                return true;
            }

            // 如果事件是常驻的，阻止过期
            if (eventNode.IsPersistent)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 评估分支条件
        /// </summary>
        private void EvaluateBranches()
        {
            foreach (var eventNode in activeEvents.Values)
            {
                if (eventNode.Status == EventStatus.InProgress &&
                    eventNode.Branches != null &&
                    eventNode.Branches.Count > 0)
                {
                    // 分离不同类型的分支
                    var manualBranches = eventNode.Branches.Where(b => b.Type == BranchType.Manual).ToList();
                    var autoBranches = eventNode.Branches.Where(b => b.Type == BranchType.Auto).ToList();

                    // 处理自动分支（按index顺序评估，返回第一个满足条件的）
                    if (autoBranches.Count > 0)
                    {
                        var availableAutoBranches = autoBranches
                            .Where(b => EvaluateBranchConditions(b))
                            .OrderBy(b => b.BranchId) // 按BranchId排序，确保index顺序
                            .ToList();

                        if (availableAutoBranches.Count > 0)
                        {
                            // 自动分支选择第一个满足条件的
                            var selectedBranch = availableAutoBranches.First();
                            ActivateBranch(eventNode, new List<EventBranch> { selectedBranch });
                            continue; // 自动分支已处理，跳过其他分支
                        }
                    }

                    // 手动分支不需要在这里处理，等待用户手动选择
                    if (manualBranches.Count > 0)
                    {
                        GD.Print($"事件 {eventNode.EventName} 等待用户选择分支");
                    }
                }
            }
        }

        /// <summary>
        /// 评估分支条件
        /// </summary>
        private bool EvaluateBranchConditions(EventBranch branch)
        {
            // 使用新的条件系统评估分支的enable条件
            if (branch.EnableConditions != null && branch.EnableConditions.Count > 0)
            {
                try
                {
                    return ConditionManager.Instance.EvaluateConditions(branch.EnableConditions, _gameManager);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"评估分支条件时发生错误: {ex.Message}");
                    return false; // 条件评估失败时，分支不可用
                }
            }

            // 如果没有enable条件，返回true（默认可用）
            return true;
        }

        /// <summary>
        /// 激活分支
        /// </summary>
        private void ActivateBranch(EventNode parentEvent, List<EventBranch> availableBranches)
        {
            EventBranch selectedBranch = null;

            // 根据分支类型选择分支
            if (availableBranches.Count == 1)
            {
                selectedBranch = availableBranches[0];
            }
            else if (availableBranches.All(b => b.Type == BranchType.Auto))
            {
                // 自动分支按index顺序选择第一个满足条件的
                selectedBranch = availableBranches
                    .Where(b => EvaluateBranchConditions(b))
                    .OrderBy(b => b.BranchId)
                    .FirstOrDefault();
            }
            else if (availableBranches.All(b => b.Type == BranchType.Manual))
            {
                // 手动分支选择第一个满足条件的（用于预览）
                selectedBranch = availableBranches.FirstOrDefault(b => EvaluateBranchConditions(b));
            }
            else
            {
                // 混合类型分支，优先选择自动分支，然后是手动分支
                var autoBranch = availableBranches
                    .Where(b => b.Type == BranchType.Auto && EvaluateBranchConditions(b))
                    .OrderBy(b => b.BranchId)
                    .FirstOrDefault();

                if (autoBranch != null)
                {
                    selectedBranch = autoBranch;
                }
                else
                {
                    var manualBranches = availableBranches.Where(b => b.Type == BranchType.Manual).ToList();
                    if (manualBranches.Count > 0)
                    {
                        selectedBranch = manualBranches.FirstOrDefault(b => EvaluateBranchConditions(b));
                    }
                }
            }

            if (selectedBranch == null)
            {
                GD.PrintErr($"无法为事件 {parentEvent.EventName} 选择分支");
                return;
            }

            // 激活后续事件
            foreach (var nextEventRef in selectedBranch.NextEvents)
            {
                // 找到对应的事件树
                var parentTree = eventForest.FirstOrDefault(t => t.AllEvents.Any(e => e.EventId == parentEvent.EventId));
                if (parentTree != null)
                {
                    var nextEvent = parentTree.GetEvent(nextEventRef.EventId);
                    if (nextEvent != null)
                    {
                        // 设置事件时间
                        nextEvent.ActivationTurn = currentTurn + nextEventRef.ActivationDelay;
                        nextEvent.ExpirationTurn = nextEvent.ActivationTurn + nextEvent.Duration;

                        GD.Print($"事件 {parentEvent.EventName} 选择了分支 {selectedBranch.BranchName}，后续事件 {nextEvent.EventName} 将在第 {nextEvent.ActivationTurn} 回合激活");
                    }
                }
            }
        }

        /// <summary>
        /// 手动选择分支（供外部调用）
        /// </summary>
        public void ChooseBranch(string eventId, string branchId)
        {
            if (activeEvents.TryGetValue(eventId, out var eventNode))
            {
                var branch = eventNode.Branches.FirstOrDefault(b => b.BranchId == branchId);
                if (branch != null)
                {
                    GD.Print($"手动选择分支: {eventNode.EventName} -> {branch.BranchName}");

                    // 激活选中的分支
                    ActivateBranch(eventNode, new List<EventBranch> { branch });

                    // 将当前事件标记为已完成，因为分支已经选择
                    eventNode.Status = EventStatus.Completed;
                    activeEvents.Remove(eventId);
                    completedEvents[eventId] = eventNode;

                    GD.Print($"事件 {eventNode.EventName} 因分支选择而完成");
                    EmitSignal(SignalName.EventCompleted, eventNode.EventId, eventNode.EventName);
                }
                else
                {
                    GD.PrintErr($"未找到分支: {branchId}");
                }
            }
            else
            {
                GD.PrintErr($"未找到事件: {eventId}");
            }
        }

        /// <summary>
        /// 根据概率选择分支
        /// </summary>
        private EventBranch SelectBranchByProbability(List<EventBranch> branches)
        {
            if (branches.Count == 0)
                return null;

            if (branches.Count == 1)
                return branches[0];

            // 计算总概率
            var totalProbability = branches.Sum(b => b.Probability);
            var randomValue = (float)random.Randf() * totalProbability;

            var currentProbability = 0f;
            foreach (var branch in branches)
            {
                currentProbability += branch.Probability;
                if (randomValue <= currentProbability)
                {
                    return branch;
                }
            }

            return branches.Last();
        }

        /// <summary>
        /// 更新常驻事件
        /// </summary>
        private void UpdatePersistentEvents()
        {
            foreach (var eventNode in activeEvents.Values)
            {
                if (eventNode.IsPersistent)
                {
                    // 处理循环任务
                    foreach (var task in eventNode.CyclicTasks)
                    {
                        if (task.CanExecute(currentTurn))
                        {
                            // 这里可以触发循环任务的执行
                            GD.Print($"常驻事件 {eventNode.EventName} 的循环任务 {task.TaskName} 可以执行");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 激活事件任务
        /// </summary>
        private void ActivateEventTasks(EventNode eventNode)
        {
            // 激活必需任务
            foreach (var task in eventNode.RequiredTasks)
            {
                task.Status = TaskStatus.NotStarted;
                GD.Print($"激活必需任务: {task.TaskName}");
            }

            // 激活循环任务
            foreach (var task in eventNode.CyclicTasks)
            {
                task.Status = TaskStatus.NotStarted;
                GD.Print($"激活循环任务: {task.TaskName}");
            }
        }

        /// <summary>
        /// 更新任务进度
        /// </summary>
        public void UpdateTaskProgress(string taskId, int progress)
        {
            // 查找任务
            foreach (var eventNode in activeEvents.Values)
            {
                var task = eventNode.RequiredTasks.FirstOrDefault(t => t.TaskId == taskId) ??
           eventNode.CyclicTasks.FirstOrDefault(t => t.TaskId == taskId);

                if (task != null)
                {
                    task.UpdateProgress(progress);
                    GD.Print($"任务 {task.TaskName} 进度更新: {task.CurrentProgress}/{task.RequiredProgress}");

                    // 检查任务是否完成
                    if (task.Status == TaskStatus.Completed)
                    {
                        OnTaskCompleted(task);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        public void CompleteTask(string taskId)
        {
            UpdateTaskProgress(taskId, 1000); // 给予足够大的进度值来完成任务
        }

        /// <summary>
        /// 任务完成回调
        /// </summary>
        private void OnTaskCompleted(Task task)
        {
            GD.Print($"任务 {task.TaskName} 完成！");
            EmitSignal(SignalName.TaskCompleted, task.TaskId, task.TaskName);

            // 找到相关事件
            var relatedEvents = FindEventsByTask(task.TaskId);

            // 更新事件进度
            foreach (var eventNode in relatedEvents)
            {
                UpdateEventProgress(eventNode, task.TaskId);

                // 检查是否满足推进条件
                if (CanAdvanceEvent(eventNode))
                {
                    AdvanceEvent(eventNode);
                }

                // 检查是否完成
                if (IsEventCompleted(eventNode))
                {
                    CompleteEvent(eventNode);
                }
            }
        }

        /// <summary>
        /// 查找任务相关的事件
        /// </summary>
        private List<EventNode> FindEventsByTask(string taskId)
        {
            var relatedEvents = new List<EventNode>();

            foreach (var eventNode in activeEvents.Values)
            {
                if (eventNode.RequiredTasks.Any(t => t.TaskId == taskId) ||
    eventNode.CyclicTasks.Any(t => t.TaskId == taskId))
                {
                    relatedEvents.Add(eventNode);
                }
            }

            return relatedEvents;
        }

        /// <summary>
        /// 更新事件进度
        /// </summary>
        private void UpdateEventProgress(EventNode eventNode, string completedTaskId)
        {
            // 计算任务完成度
            var totalRequiredTasks = eventNode.RequiredTasks.Count;
            if (totalRequiredTasks == 0)
                return;

            var completedRequiredTasks = eventNode.RequiredTasks.Count(t => t.Status == TaskStatus.Completed);

            // 更新进度
            eventNode.CurrentProgress = (completedRequiredTasks * 100) / totalRequiredTasks;

            // 记录已完成任务
            if (!eventNode.CompletedTasks.Contains(completedTaskId))
            {
                eventNode.CompletedTasks.Add(completedTaskId);
            }

            GD.Print($"事件 {eventNode.EventName} 进度更新: {eventNode.CurrentProgress}%");
        }

        /// <summary>
        /// 检查是否可以推进事件
        /// </summary>
        private bool CanAdvanceEvent(EventNode eventNode)
        {
            // 检查是否所有必需任务都完成
            var allRequiredTasksCompleted = eventNode.RequiredTasks.All(t => t.Status == TaskStatus.Completed);

            if (allRequiredTasksCompleted && eventNode.Status == EventStatus.Active)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 推进事件
        /// </summary>
        private void AdvanceEvent(EventNode eventNode)
        {
            eventNode.Status = EventStatus.InProgress;
            GD.Print($"事件 {eventNode.EventName} 推进到进行中状态");
        }

        /// <summary>
        /// 检查事件是否完成
        /// </summary>
        private bool IsEventCompleted(EventNode eventNode)
        {
            return eventNode.CanComplete();
        }

        /// <summary>
        /// 完成事件
        /// </summary>
        private void CompleteEvent(EventNode eventNode)
        {
            eventNode.Status = EventStatus.Completed;
            activeEvents.Remove(eventNode.EventId);
            completedEvents[eventNode.EventId] = eventNode;

            // 应用事件效果
            ApplyEventEffects(eventNode);

            GD.Print($"事件 {eventNode.EventName} 完成！");
            EmitSignal(SignalName.EventCompleted, eventNode.EventId, eventNode.EventName);
        }

        /// <summary>
        /// 应用事件效果
        /// </summary>
        private void ApplyEventEffects(EventNode eventNode)
        {
            // 应用世界效果
            foreach (var effectRef in eventNode.WorldEffects)
            {
                GD.Print($"应用世界效果: {effectRef.EffectId}");
                // 这里需要通过Library查找并应用效果
                effectRef.ExecuteEffect(GameManager.Instance);
            }

            // 应用角色效果
            foreach (var effectRef in eventNode.CharacterEffects)
            {
                GD.Print($"应用角色效果: {effectRef.EffectId}");
                // 这里需要通过Library查找并应用效果
                effectRef.ExecuteEffect(GameManager.Instance);
            }
        }

        /// <summary>
        /// 添加世界效果
        /// </summary>
        public void AddWorldEffect(EffectReference effect)
        {
            if (effect != null)
            {
                worldEffects.Add(effect);
                GD.Print($"添加世界效果: {effect.EffectId}, 持续时间: {effect.Duration}");
            }
        }

        /// <summary>
        /// 添加角色效果
        /// </summary>
        public void AddCharacterEffect(EffectReference effect)
        {
            if (effect != null)
            {
                characterEffects.Add(effect);
                GD.Print($"添加角色效果: {effect.EffectId}, 持续时间: {effect.Duration}");
            }
        }

        /// <summary>
        /// 移除过期的效果
        /// </summary>
        private void RemoveExpiredEffects()
        {
            // 移除过期的世界效果
            worldEffects.RemoveAll(effect => effect.IsExpired());
            
            // 移除过期的角色效果
            characterEffects.RemoveAll(effect => effect.IsExpired());
        }

        /// <summary>
        /// 更新所有效果
        /// </summary>
        private void UpdateEffects()
        {
            // 更新世界效果
            foreach (var effect in worldEffects)
            {
                effect.UpdateEffect();
                if (!effect.IsExpired())
                {
                    // 执行效果
                    effect.ExecuteEffect(_gameManager);
                }
            }
            
            // 更新角色效果
            foreach (var effect in characterEffects)
            {
                effect.UpdateEffect();
                if (!effect.IsExpired())
                {
                    // 执行效果
                    effect.ExecuteEffect(_gameManager);
                }
            }
        }


        /// <summary>
        /// 获取当前回合
        /// </summary>
        public int GetCurrentTurn()
        {
            return currentTurn;
        }

        /// <summary>
        /// 获取活跃事件数量
        /// </summary>
        public int GetActiveEventCount()
        {
            return activeEvents.Count;
        }

        /// <summary>
        /// 获取已完成事件数量
        /// </summary>
        public int GetCompletedEventCount()
        {
            return completedEvents.Count;
        }

        /// <summary>
        /// 获取所有活跃事件
        /// </summary>
        public Dictionary<string, EventNode> GetActiveEvents()
        {
            return new Dictionary<string, EventNode>(activeEvents);
        }
        public void Step(int turn)
        {
            currentTurn = turn;
            AdvanceTurn(turn);
        }
    }
}
