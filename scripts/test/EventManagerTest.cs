using Godot;
using Threshold.Core;
using Threshold.Core.Utils;
using System.Collections.Generic;

namespace Threshold.Test
{
    /// <summary>
    /// 事件管理器测试脚本
    /// </summary>
    public partial class EventManagerTest : Node
    {
        private EventManager eventManager;
        private Button testButton;
        private Button advanceTurnButton;
        private Button explorePathsButton;
        private Label statusLabel;
        private VBoxContainer eventListContainer;
        private ScrollContainer scrollContainer;
        
        // 存储事件UI组件的字典
        private Dictionary<string, EventUIComponent> eventUIComponents = new Dictionary<string, EventUIComponent>();
        
        public override void _Ready()
        {
            GD.Print("=== EventManagerTest 初始化 ===");
            
            // 创建事件管理器
            eventManager = new EventManager(12345);
            AddChild(eventManager);
            
            // 连接信号
            eventManager.EventActivated += OnEventActivated;
            eventManager.EventCompleted += OnEventCompleted;
            eventManager.EventExpired += OnEventExpired;
            eventManager.TaskCompleted += OnTaskCompleted;
            
            // 创建UI
            CreateTestUI();
            
            // 初始化事件森林
            InitializeEventForest();
        }
        
        /// <summary>
        /// 创建测试UI
        /// </summary>
        private void CreateTestUI()
        {
            // 创建状态标签
            statusLabel = new Label();
            statusLabel.Text = "事件系统状态: 未初始化";
            statusLabel.Position = new Vector2(10, 10);
            AddChild(statusLabel);
            
            // 创建测试按钮
            testButton = new Button();
            testButton.Text = "测试事件系统";
            testButton.Position = new Vector2(10, 50);
            testButton.Pressed += OnTestButtonPressed;
            AddChild(testButton);
            
            // 创建推进回合按钮
            advanceTurnButton = new Button();
            advanceTurnButton.Text = "推进回合";
            advanceTurnButton.Position = new Vector2(10, 90);
            advanceTurnButton.Pressed += OnAdvanceTurnButtonPressed;
            AddChild(advanceTurnButton);
            
            // 创建探索路径按钮
            explorePathsButton = new Button();
            explorePathsButton.Text = "探索数据路径";
            explorePathsButton.Position = new Vector2(10, 130);
            explorePathsButton.Pressed += OnExplorePathsButtonPressed;
            AddChild(explorePathsButton);
            
            // 创建滚动容器
            scrollContainer = new ScrollContainer();
            scrollContainer.Position = new Vector2(10, 170);
            scrollContainer.Size = new Vector2(800, 600);
            AddChild(scrollContainer);
            
            // 创建事件列表容器
            eventListContainer = new VBoxContainer();
            eventListContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            eventListContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            scrollContainer.AddChild(eventListContainer);
        }
        
        /// <summary>
        /// 初始化事件森林
        /// </summary>
        private void InitializeEventForest()
        {
            try
            {
                GD.Print("开始初始化事件森林...");
                eventManager.InitializeEventForest();
                statusLabel.Text = $"事件系统状态: 已初始化，种子: 12345";
                GD.Print("事件森林初始化完成");
                
                // 更新事件列表显示
                UpdateEventListDisplay();
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"初始化事件森林时发生错误: {ex.Message}");
                statusLabel.Text = $"事件系统状态: 初始化失败 - {ex.Message}";
            }
        }
        
        /// <summary>
        /// 更新事件列表显示
        /// </summary>
        private void UpdateEventListDisplay()
        {
            // 清空现有的事件UI组件
            foreach (var component in eventUIComponents.Values)
            {
                component.QueueFree();
            }
            eventUIComponents.Clear();
            
            // 获取活跃事件
            var activeEvents = eventManager.GetActiveEvents();
            
            // 为每个活跃事件创建UI组件
            foreach (var kvp in activeEvents)
            {
                var eventNode = kvp.Value;
                var eventUI = CreateEventUIComponent(eventNode);
                eventListContainer.AddChild(eventUI);
                eventUIComponents[eventNode.EventId] = eventUI;
            }
            
            // 如果没有活跃事件，显示提示
            if (activeEvents.Count == 0)
            {
                var noEventsLabel = new Label();
                noEventsLabel.Text = "当前没有活跃事件";
                noEventsLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
                eventListContainer.AddChild(noEventsLabel);
            }
        }
        
        /// <summary>
        /// 创建事件UI组件
        /// </summary>
        private EventUIComponent CreateEventUIComponent(EventNode eventNode)
        {
            var component = new EventUIComponent(eventNode, this);
            return component;
        }
        
        /// <summary>
        /// 测试按钮事件
        /// </summary>
        private void OnTestButtonPressed()
        {
            GD.Print("=== 开始测试事件系统 ===");
            
            // 获取系统状态
            var currentTurn = eventManager.GetCurrentTurn();
            var activeEventCount = eventManager.GetActiveEventCount();
            var completedEventCount = eventManager.GetCompletedEventCount();
            
            GD.Print($"当前回合: {currentTurn}");
            GD.Print($"活跃事件数量: {activeEventCount}");
            GD.Print($"已完成事件数量: {completedEventCount}");
            
            // 获取活跃事件
            var activeEvents = eventManager.GetActiveEvents();
            foreach (var kvp in activeEvents)
            {
                var eventNode = kvp.Value;
                GD.Print($"活跃事件: {eventNode.EventName} (ID: {eventNode.EventId})");
                GD.Print($"  状态: {eventNode.Status}");
                GD.Print($"  进度: {eventNode.CurrentProgress}%");
                GD.Print($"  必需任务: {eventNode.RequiredTasks.Count}");
                GD.Print($"  循环任务: {eventNode.CyclicTasks.Count}");
                GD.Print($"  分支数量: {eventNode.Branches.Count}");
                GD.Print($"  是否结束节点: {eventNode.IsEnding}");
                if (eventNode.IsEnding)
                {
                    GD.Print($"  结束类型: {eventNode.EndingType}");
                }
                
                // 显示任务详情
                foreach (var task in eventNode.RequiredTasks)
                {
                    GD.Print($"    必需任务: {task.TaskName} - 状态: {task.Status}, 进度: {task.CurrentProgress}/{task.RequiredProgress}");
                }
                
                foreach (var task in eventNode.CyclicTasks)
                {
                    GD.Print($"    循环任务: {task.TaskName} - 状态: {task.Status}, 进度: {task.CurrentProgress}/{task.RequiredProgress}");
                }
                
                // 显示分支详情
                foreach (var branch in eventNode.Branches)
                {
                    var branchTypeText = branch.Type switch
                    {
                        BranchType.Manual => "手动分支",
                        BranchType.Auto => "自动分支",
                        _ => "未知类型"
                    };
                    
                    GD.Print($"    分支: {branch.BranchName} (ID: {branch.BranchId}) - 类型: {branchTypeText} - 概率: {branch.Probability}");
                    
                    // 显示enable条件
                    if (branch.EnableConditions != null && branch.EnableConditions.Count > 0)
                    {
                        GD.Print($"      Enable条件:");
                        foreach (var condition in branch.EnableConditions)
                        {
                            GD.Print($"        - {condition.GetDisplayString()}");
                        }
                    }
                    
                    foreach (var nextEvent in branch.NextEvents)
                    {
                        GD.Print($"      后续事件: {nextEvent.EventName} - 延迟: {nextEvent.ActivationDelay} 回合");
                    }
                }
            }
            
            statusLabel.Text = $"测试完成 - 回合: {currentTurn}, 活跃事件: {activeEventCount}";
            
            // 更新事件列表显示
            UpdateEventListDisplay();
        }
        
        /// <summary>
        /// 推进回合按钮事件
        /// </summary>
        private void OnAdvanceTurnButtonPressed()
        {
            GD.Print("=== 推进回合 ===");
            eventManager.Step(eventManager.GetCurrentTurn() + 1);
            
            var currentTurn = eventManager.GetCurrentTurn();
            var activeEventCount = eventManager.GetActiveEventCount();
            
            statusLabel.Text = $"回合: {currentTurn}, 活跃事件: {activeEventCount}";
            
            // 更新事件列表显示
            UpdateEventListDisplay();
        }
        
        /// <summary>
        /// 探索路径按钮事件
        /// </summary>
        private void OnExplorePathsButtonPressed()
        {
            GD.Print("=== 开始探索数据路径（深度递归测试） ===");
            
            // 创建一个模拟的GameManager来探索路径
            var mockGameManager = CreateMockGameManager();
            
            // 使用DataPathExplorer探索路径（现在支持更深递归）
            
            // 直接测试Global.Instance的路径（这是真实存在的数据）
            GD.Print("\n=== 测试Global.Instance路径（真实数据） ===");
            TestGlobalInstancePaths();
            
            // 测试GameManager的路径
            GD.Print("\n=== 测试GameManager路径 ===");
            ScriptExecutor.ExplorePaths(mockGameManager);
            
            statusLabel.Text = "数据路径深度探索完成，查看控制台输出";
        }
        
        /// <summary>
        /// 测试Global.Instance的路径（直接访问，不使用PathResolver）
        /// </summary>
        private void TestGlobalInstancePaths()
        {
            try
            {
                if (Global.Instance == null)
                {
                    GD.Print("Global.Instance为空，无法测试");
                    return;
                }
                
                GD.Print("=== 直接测试Global.Instance路径 ===");
                
                // 测试基础路径
                var globalVars = Global.Instance.globalVariables;
                GD.Print($"Global.Instance.globalVariables: {globalVars.Count} 个键值对");
                
                // 测试具体键值
                if (globalVars.ContainsKey("test_key"))
                {
                    var testValue = globalVars["test_key"];
                    GD.Print($"Global.Instance.globalVariables[\"test_key\"]: {testValue}");
                }
                
                if (globalVars.ContainsKey("nested_data"))
                {
                    var nestedData = globalVars["nested_data"];
                    GD.Print($"Global.Instance.globalVariables[\"nested_data\"]: {nestedData}");
                    
                                    // 测试嵌套数据
                if (nestedData.VariantType == Variant.Type.Dictionary)
                {
                    var nestedDict = nestedData.As<Godot.Collections.Dictionary<string, Variant>>();
                    if (nestedDict.ContainsKey("level2"))
                    {
                        var level2 = nestedDict["level2"];
                        GD.Print($"Global.Instance.globalVariables[\"nested_data\"][\"level2\"]: {level2}");
                        
                        if (level2.VariantType == Variant.Type.Dictionary)
                        {
                            var level2Dict = level2.As<Godot.Collections.Dictionary<string, Variant>>();
                            if (level2Dict.ContainsKey("deep_nested"))
                            {
                                var deepNested = level2Dict["deep_nested"];
                                GD.Print($"Global.Instance.globalVariables[\"nested_data\"][\"level2\"][\"deep_nested\"]: {deepNested}");
                            }
                        }
                    }
                }
                }
                
                // 显示所有键
                GD.Print("Global.Instance.globalVariables 中的所有键:");
                foreach (var key in globalVars.Keys)
                {
                    var value = globalVars[key];
                    var typeName = value.VariantType.ToString();
                    GD.Print($"  {key}: {value} (类型: {typeName})");
                }
                
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"测试Global.Instance路径时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建模拟的GameManager用于路径探索
        /// </summary>
        private GameManager CreateMockGameManager()
        {
            var gameManager = new GameManager();
            
            // 设置一些模拟数据
            // 注意：这里我们使用反射来设置私有属性，仅用于测试
            var currentTurnProperty = typeof(GameManager).GetProperty("CurrentTurn");
            if (currentTurnProperty != null && currentTurnProperty.CanWrite)
            {
                currentTurnProperty.SetValue(gameManager, 5);
            }
            
            // 使用真实存在的Global.Instance.globalVariables来测试深度递归
            try
            {
                // 确保Global.Instance存在
                if (Global.Instance == null)
                {
                    GD.Print("Global.Instance为空，创建新的Global实例");
                    var globalNode = new Global();
                    globalNode.Name = "Global";
                    AddChild(globalNode);
                }
                
                // 设置一些测试数据到Global.Instance.globalVariables
                Global.Instance.globalVariables["test_key"] = "test_value";
                Global.Instance.globalVariables["magic_academy_available"] = true;
                Global.Instance.globalVariables["warrior_guild_available"] = true;
                Global.Instance.globalVariables["quest_progress"] = 75;
                
                // 创建嵌套的测试数据结构
                var nestedData = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["level1"] = "value1",
                    ["level2"] = new Godot.Collections.Dictionary<string, Variant>
                    {
                        ["deep_nested"] = "deep_value",
                        ["number"] = 42,
                        ["boolean"] = true
                    }
                };
                Global.Instance.globalVariables["nested_data"] = nestedData;
                
                GD.Print("成功设置Global.Instance.globalVariables测试数据");
            }
            catch (System.Exception ex)
            {
                GD.Print($"设置Global.Instance.globalVariables失败: {ex.Message}");
            }
            
            return gameManager;
        }
        
        /// <summary>
        /// 事件激活回调
        /// </summary>
        private void OnEventActivated(string eventId, string eventName)
        {
            GD.Print($"事件激活: {eventName} (ID: {eventId})");
            UpdateEventListDisplay();
        }
        
        /// <summary>
        /// 事件完成回调
        /// </summary>
        private void OnEventCompleted(string eventId, string eventName)
        {
            GD.Print($"事件完成: {eventName} (ID: {eventId})");
            UpdateEventListDisplay();
        }
        
        /// <summary>
        /// 事件过期回调
        /// </summary>
        private void OnEventExpired(string eventId, string eventName)
        {
            GD.Print($"事件过期: {eventName} (ID: {eventId})");
            UpdateEventListDisplay();
        }
        
        /// <summary>
        /// 任务完成回调
        /// </summary>
        private void OnTaskCompleted(string taskId, string taskName)
        {
            GD.Print($"任务完成: {taskName} (ID: {taskId})");
            UpdateEventListDisplay();
        }
        
        /// <summary>
        /// 完成任务（供EventUIComponent调用）
        /// </summary>
        public void CompleteTask(string taskId)
        {
            eventManager.CompleteTask(taskId);
        }
        
        /// <summary>
        /// 选择分支（供EventUIComponent调用）
        /// </summary>
        public void ChooseBranch(string eventId, string branchId)
        {
            eventManager.ChooseBranch(eventId, branchId);
        }
    }
    
    /// <summary>
    /// 事件UI组件
    /// </summary>
    public partial class EventUIComponent : PanelContainer
    {
        private EventNode eventNode;
        private EventManagerTest testManager;
        
        private Label eventInfoLabel;
        private Label taskInfoLabel;
        private Label branchInfoLabel;
        private Button completeTasksButton;
        private VBoxContainer branchButtonsContainer;
        
        public EventUIComponent(EventNode eventNode, EventManagerTest testManager)
        {
            this.eventNode = eventNode;
            this.testManager = testManager;
            
            CreateUI();
        }
        
        /// <summary>
        /// 创建UI
        /// </summary>
        private void CreateUI()
        {
            // 设置面板样式
            var styleBox = new StyleBoxFlat();
            styleBox.BgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            styleBox.BorderColor = new Color(0.5f, 0.5f, 0.5f);
            styleBox.BorderWidthLeft = 2;
            styleBox.BorderWidthRight = 2;
            styleBox.BorderWidthTop = 2;
            styleBox.BorderWidthBottom = 2;
            styleBox.CornerRadiusTopLeft = 5;
            styleBox.CornerRadiusTopRight = 5;
            styleBox.CornerRadiusBottomLeft = 5;
            styleBox.CornerRadiusBottomRight = 5;
            AddThemeStyleboxOverride("panel", styleBox);
            
            // 创建主容器
            var mainContainer = new VBoxContainer();
            mainContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddChild(mainContainer);
            
            // 事件信息标签
            eventInfoLabel = new Label();
            eventInfoLabel.Text = $"事件: {eventNode.EventName}\n状态: {eventNode.Status}\n进度: {eventNode.CurrentProgress}%";
            eventInfoLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
            mainContainer.AddChild(eventInfoLabel);
            
            // 任务信息标签
            taskInfoLabel = new Label();
            UpdateTaskInfo();
            mainContainer.AddChild(taskInfoLabel);
            
            // 满足条件按钮（只有当事件处于Active状态且有必需任务时才显示）
            if (eventNode.Status == EventStatus.Active && eventNode.RequiredTasks.Count > 0)
            {
                completeTasksButton = new Button();
                completeTasksButton.Text = "满足所有条件（完成任务）";
                completeTasksButton.Pressed += OnCompleteTasksButtonPressed;
                mainContainer.AddChild(completeTasksButton);
            }
            
            // 分支信息标签
            if (eventNode.Branches.Count > 0)
            {
                branchInfoLabel = new Label();
                UpdateBranchInfo();
                mainContainer.AddChild(branchInfoLabel);
                
                // 分支选择按钮容器
                branchButtonsContainer = new VBoxContainer();
                mainContainer.AddChild(branchButtonsContainer);
                
                // 只有当事件处于InProgress状态时才显示分支选择按钮
                if (eventNode.Status == EventStatus.InProgress)
                {
                    CreateBranchButtons();
                }
            }
            
            // 设置大小
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ShrinkBegin;
        }
        
        /// <summary>
        /// 更新任务信息
        /// </summary>
        private void UpdateTaskInfo()
        {
            var taskText = "任务信息:\n";
            
            if (eventNode.RequiredTasks.Count > 0)
            {
                taskText += "必需任务:\n";
                foreach (var task in eventNode.RequiredTasks)
                {
                    var statusColor = task.Status == TaskStatus.Completed ? "绿色" : 
                                    task.Status == TaskStatus.InProgress ? "黄色" : "红色";
                    taskText += $"  • {task.TaskName}: {task.Status} ({task.CurrentProgress}/{task.RequiredProgress})\n";
                }
            }
            
            if (eventNode.CyclicTasks.Count > 0)
            {
                taskText += "循环任务:\n";
                foreach (var task in eventNode.CyclicTasks)
                {
                    var statusColor = task.Status == TaskStatus.Completed ? "绿色" : 
                                    task.Status == TaskStatus.InProgress ? "黄色" : "红色";
                    taskText += $"  • {task.TaskName}: {task.Status} ({task.CurrentProgress}/{task.RequiredProgress})\n";
                }
            }
            
            if (eventNode.RequiredTasks.Count == 0 && eventNode.CyclicTasks.Count == 0)
            {
                taskText += "无任务";
            }
            
            taskInfoLabel.Text = taskText;
        }
        
        /// <summary>
        /// 更新分支信息
        /// </summary>
        private void UpdateBranchInfo()
        {
            var branchText = "分支选项:\n";
            foreach (var branch in eventNode.Branches)
            {
                var branchTypeText = branch.Type switch
                {
                    BranchType.Manual => "手动分支",
                    BranchType.Auto => "自动分支",
                    _ => "未知类型"
                };
                
                branchText += $"• {branch.BranchName} [{branchTypeText}] (概率: {branch.Probability})\n";
                
                // 显示enable条件
                if (branch.EnableConditions != null && branch.EnableConditions.Count > 0)
                {
                    branchText += $"  Enable条件:\n";
                    foreach (var condition in branch.EnableConditions)
                    {
                        branchText += $"    - {condition.GetDisplayString()}\n";
                    }
                }
                
                foreach (var nextEvent in branch.NextEvents)
                {
                    branchText += $"  后续: {nextEvent.EventName} (延迟: {nextEvent.ActivationDelay} 回合)\n";
                }
            }
            branchInfoLabel.Text = branchText;
        }
        
        /// <summary>
        /// 创建分支选择按钮
        /// </summary>
        private void CreateBranchButtons()
        {
            // 清空现有按钮
            foreach (var child in branchButtonsContainer.GetChildren())
            {
                child.QueueFree();
            }
            
            // 为每个分支创建选择按钮
            foreach (var branch in eventNode.Branches)
            {
                var branchButton = new Button();
                branchButton.Text = $"选择分支: {branch.BranchName}";
                branchButton.Pressed += () => OnBranchButtonPressed(branch.BranchId);
                branchButtonsContainer.AddChild(branchButton);
            }
        }
        
        /// <summary>
        /// 满足条件按钮事件
        /// </summary>
        private void OnCompleteTasksButtonPressed()
        {
            GD.Print($"为事件 {eventNode.EventName} 满足所有条件");
            
            // 完成所有必需任务
            foreach (var task in eventNode.RequiredTasks)
            {
                if (task.Status != TaskStatus.Completed)
                {
                    testManager.CompleteTask(task.TaskId);
                }
            }
            
            // 更新显示
            UpdateTaskInfo();
            
            // 如果事件现在处于InProgress状态且有分支，显示分支选择按钮
            if (eventNode.Status == EventStatus.InProgress && eventNode.Branches.Count > 0)
            {
                CreateBranchButtons();
            }
        }
        
        /// <summary>
        /// 分支选择按钮事件
        /// </summary>
        private void OnBranchButtonPressed(string branchId)
        {
            GD.Print($"事件 {eventNode.EventName} 选择分支: {branchId}");
            testManager.ChooseBranch(eventNode.EventId, branchId);
        }
        
        /// <summary>
        /// 更新显示
        /// </summary>
        public void UpdateDisplay()
        {
            // 更新事件信息
            eventInfoLabel.Text = $"事件: {eventNode.EventName}\n状态: {eventNode.Status}\n进度: {eventNode.CurrentProgress}%";
            
            // 更新任务信息
            UpdateTaskInfo();
            
            // 更新分支信息
            if (eventNode.Branches.Count > 0)
            {
                UpdateBranchInfo();
                
                // 如果事件现在处于InProgress状态且有分支，显示分支选择按钮
                if (eventNode.Status == EventStatus.InProgress)
                {
                    CreateBranchButtons();
                }
            }
        }
    }
}
