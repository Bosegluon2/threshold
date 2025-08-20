using Godot;
using Godot.Collections;
using Threshold.Core;
using Threshold.Core.Agent;
using Threshold.Core.Data;
using System;

/// <summary>
/// 这是一个简单的任务模拟器，暂时用于测试
/// </summary>
public enum MissionType
{
    Production,      // 生产任务
    Exploration,     // 探索任务
    Combat,          // 战斗任务
    Rescue,          // 救援任务
    Delivery,        // 运输任务
    Investigation,   // 调查任务
}

public enum MissionPhase
{
    Planning,        // 规划阶段
    Traveling,       // 前往目标地点
    Executing,       // 执行任务
    Returning,       // 返回基地
    Completed,       // 任务完成
    Failed           // 任务失败
}

public enum MissionOutcome
{
    Running,         // 任务进行中，并未结束
    Success,         // 任务成功且全部/部分Agent返回
    PartialSuccess,  // 任务失败但全部/部分Agent返回
    TotalFailure     // 任务失败且所有Agent死亡
}

public partial class MissionSimulator : Node
{
    public string MissionId { get; set; }
    public string MissionName { get; set; }
    public string MissionDescription { get; set; }
    public MissionType MissionType { get; set; }
    public int MissionMaxTurn { get; set; }
    public int MissionCurrentTurn { get; set; }
    public float MissionDangerLevel { get; set; }
    public Place MissionTargetPlace { get; set; }
    public Place MissionBasePlace { get; set; }  // 任务基地
    public Array<Agent> MissionAgents { get; set; }
    public EventNode MissionRelatedEvent { get; set; }
    public int granularity { get; set; } = 12;  // 每回合的小事件数量（30分钟一个细粒度）
    
    // 细粒度时间相关
    private int currentGranularityStep = 0;  // 当前细粒度步骤（0-11）
    private bool isProcessingGranularity = false;  // 是否正在处理细粒度时间
    
    // 任务状态
    public MissionPhase CurrentPhase { get; set; } = MissionPhase.Planning;
    public int PhaseProgress { get; set; } = 0;  // 当前阶段进度 (0-100)
    public int PhaseRequiredTurns { get; set; } = 0;  // 当前阶段需要的回合数
    public int PhaseCurrentTurn { get; set; } = 0;  // 当前阶段已进行的回合数
    
    // 任务结果
    public bool IsCompleted { get; set; } = false;
    public bool IsFailed { get; set; } = false;
    public string FailureReason { get; set; } = "";
    public float SuccessRate { get; set; } = 0.0f;  // 成功率 (0.0-1.0)
    public MissionOutcome FinalOutcome { get; set; } = MissionOutcome.Success;
    
    // 资源配备
    public int FoodSupply { get; set; } = 100;  // 食物配备
    public int WaterSupply { get; set; } = 100;  // 水资源配备
    public int FoodConsumptionPerStep { get; set; } = 5;  // 每步食物消耗
    public int WaterConsumptionPerStep { get; set; } = 3;  // 每步水资源消耗
    
    // 遭遇战系统
    public float EncounterChance { get; set; } = 0.1f;  // 遭遇战概率 (0.0-1.0)
    public float EncounterIntensity { get; set; } = 0.5f;  // 遭遇战强度 (0.0-1.0)
    public RandomNumberGenerator rng = new RandomNumberGenerator();  // 随机数生成器
    
    public override void _Ready()
    {
        MissionAgents = new Array<Agent>();
        MissionCurrentTurn = 0;
    }
    
    public void AddAgent(Agent agent)
    {
        if (agent == null)
        {
            GD.PrintErr("无法添加Agent：Agent为null");
            return;
        }
        
        if (MissionAgents == null)
        {
            MissionAgents = new Array<Agent>();
        }
        
        if (!MissionAgents.Contains(agent))
    {
        MissionAgents.Add(agent);
            GD.Print($"Agent {agent.AgentName ?? "未知"} 已添加到任务 {MissionName ?? "未命名"}");
        }
    }
    
    public void RemoveAgent(Agent agent)
    {
        if (MissionAgents.Contains(agent))
        {
            MissionAgents.Remove(agent);
            GD.Print($"Agent {agent.AgentName} 已从任务 {MissionName} 中移除");
        }
    }
    
    /// <summary>
    /// 计算基于WARPED属性的资源上限
    /// </summary>
    private void CalculateResourceLimits()
    {
        if (MissionAgents == null || MissionAgents.Count == 0) return;
        
        int totalEndurance = 0;
        int totalAdaptability = 0;
        int agentCount = 0;
        
        // 计算所有Agent的WARPED属性总和
        foreach (var agent in MissionAgents)
        {
            if (agent?.Character == null) continue;
            
            try
            {
                var warpedInfo = agent.Character.BaseWarpedInfo;
                if (warpedInfo != null)
                {
                    totalEndurance += warpedInfo.Endurance;
                    totalAdaptability += warpedInfo.Adaptability;
                    agentCount++;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"获取Agent {agent?.AgentName ?? "未知"} 的WARPED属性时出错: {ex.Message}");
            }
        }
        
        if (agentCount == 0) return;
        
        // 计算平均WARPED属性
        float avgEndurance = (float)totalEndurance / agentCount;
        float avgAdaptability = (float)totalAdaptability / agentCount;
        
        // 基于WARPED属性计算资源上限
        // 耐力影响食物上限，适应度影响水资源上限
        int baseFoodLimit = 50;  // 基础食物上限
        int baseWaterLimit = 30; // 基础水资源上限
        
        // 耐力加成：每点耐力增加2点食物上限
        int enduranceBonus = (int)(avgEndurance * 2);
        
        // 适应度加成：每点适应度增加1.5点水资源上限
        int adaptabilityBonus = (int)(avgAdaptability * 1.5f);
        
        // 计算最终资源上限
        int maxFoodSupply = baseFoodLimit + enduranceBonus;
        int maxWaterSupply = baseWaterLimit + adaptabilityBonus;
        
        // 设置资源上限
        FoodSupply = Math.Min(FoodSupply, maxFoodSupply);
        WaterSupply = Math.Min(WaterSupply, maxWaterSupply);
        
        GD.Print($"=== 资源上限计算 ===");
        GD.Print($"平均耐力: {avgEndurance:F1}");
        GD.Print($"平均适应度: {avgAdaptability:F1}");
        GD.Print($"食物上限: {maxFoodSupply} (基础: {baseFoodLimit} + 耐力加成: {enduranceBonus})");
        GD.Print($"水资源上限: {maxWaterSupply} (基础: {baseWaterLimit} + 适应度加成: {adaptabilityBonus})");
        GD.Print($"实际配备: 食物 {FoodSupply}, 水资源 {WaterSupply}");
    }

    /// <summary>
    /// 获取基于WARPED属性的资源上限
    /// </summary>
    /// <returns>包含食物和水资源上限的结构体</returns>
    public ResourceLimits GetResourceLimits()
    {
        if (MissionAgents == null || MissionAgents.Count == 0)
        {
            return new ResourceLimits { MaxFood = 50, MaxWater = 30 }; // 返回基础值
        }
        
        int totalEndurance = 0;
        int totalAdaptability = 0;
        int agentCount = 0;
        
        // 计算所有Agent的WARPED属性总和
        foreach (var agent in MissionAgents)
        {
            if (agent?.Character == null) continue;
            
            try
            {
                var warpedInfo = agent.Character.BaseWarpedInfo;
                if (warpedInfo != null)
                {
                    totalEndurance += warpedInfo.Endurance;
                    totalAdaptability += warpedInfo.Adaptability;
                    agentCount++;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"获取Agent {agent?.AgentName ?? "未知"} 的WARPED属性时出错: {ex.Message}");
            }
        }
        
        if (agentCount == 0)
        {
            return new ResourceLimits { MaxFood = 50, MaxWater = 30 }; // 返回基础值
        }
        
        // 计算平均WARPED属性
        float avgEndurance = (float)totalEndurance / agentCount;
        float avgAdaptability = (float)totalAdaptability / agentCount;
        
        // 基于WARPED属性计算资源上限
        int baseFoodLimit = 50;  // 基础食物上限
        int baseWaterLimit = 30; // 基础水资源上限
        
        // 耐力加成：每点耐力增加2点食物上限
        int enduranceBonus = (int)(avgEndurance * 2);
        
        // 适应度加成：每点适应度增加1.5点水资源上限
        int adaptabilityBonus = (int)(avgAdaptability * 1.5f);
        
        // 计算最终资源上限
        int maxFoodSupply = baseFoodLimit + enduranceBonus;
        int maxWaterSupply = baseWaterLimit + adaptabilityBonus;
        
        return new ResourceLimits 
        { 
            MaxFood = maxFoodSupply, 
            MaxWater = maxWaterSupply,
            AvgEndurance = avgEndurance,
            AvgAdaptability = avgAdaptability,
            EnduranceBonus = enduranceBonus,
            AdaptabilityBonus = adaptabilityBonus
        };
    }

    /// <summary>
    /// 资源上限结构体
    /// </summary>
    public struct ResourceLimits
    {
        public int MaxFood;           // 食物上限
        public int MaxWater;          // 水资源上限
        public float AvgEndurance;    // 平均耐力
        public float AvgAdaptability; // 平均适应度
        public int EnduranceBonus;    // 耐力加成
        public int AdaptabilityBonus; // 适应度加成
        
        public override string ToString()
        {
            return $"食物上限: {MaxFood}, 水资源上限: {MaxWater} (耐力加成: {EnduranceBonus}, 适应度加成: {AdaptabilityBonus})";
        }
    }

    /// <summary>
    /// 开始任务
    /// </summary>
    public void StartMission()
    {
        if (MissionAgents == null || MissionAgents.Count == 0)
        {
            GD.PrintErr("无法开始任务：没有分配Agent");
            return;
        }
        
        if (MissionTargetPlace == null)
        {
            GD.PrintErr("无法开始任务：未设置目标地点");
            return;
        }
        
        if (MissionBasePlace == null)
        {
            GD.PrintErr("无法开始任务：未设置基地地点");
            return;
        }

        // 检查任务类型与目标地点是否匹配
        if (!IsMissionTypeCompatibleWithPlace(MissionType, MissionTargetPlace))
        {
            GD.PrintErr($"无法开始任务：任务类型 {MissionType} 与目标地点 {MissionTargetPlace.Name} 不兼容");
            return;
        }
        
        // 确保细粒度时间设置正确
        granularity = 12; // 每回合12个细粒度时间（30分钟一个）
        GD.Print($"任务细粒度时间设置: {granularity} (每30分钟一个时间段)");

        // 计算基于WARPED属性的资源上限
        CalculateResourceLimits();
        
        // 动态计算任务难度 - 基于路径上附近地点的难度等级
        float dynamicDifficulty = CalculateDynamicMissionDifficulty();
        GD.Print($"动态计算的任务难度: {dynamicDifficulty:F2} (原始难度: {MissionDangerLevel})");
        
        // 设置遭遇战参数 - 使用动态计算的难度
        // 遭遇战概率现在表示每回合的总概率，而不是每个细粒度时间的概率
        EncounterChance = Math.Min(15.0f, dynamicDifficulty * 2.0f); // 从3降到2，最大15%
        EncounterIntensity = Math.Min(30.0f, dynamicDifficulty * 3.0f); // 从4降到3，最大30%
        
        // 根据Agent数量调整资源配备（但不超过WARPED属性计算的上限）
        int baseFoodPerAgent = 50;  // 每个Agent基础食物配备
        int baseWaterPerAgent = 30; // 每个Agent基础水资源配备
        
        FoodSupply = Math.Min(MissionAgents.Count * baseFoodPerAgent, FoodSupply);
        WaterSupply = Math.Min(MissionAgents.Count * baseWaterPerAgent, WaterSupply);
        
        // 设置资源消耗率（任务中的消耗应该比正常情况低一些）
        // 由于现在每个step包含12个细粒度时间，所以总的消耗率需要乘以12
        FoodConsumptionPerStep = 0;
        WaterConsumptionPerStep = 0;
        foreach (var agent in MissionAgents)
        {
            // 修正：食物消耗对应饱食度，水资源消耗对应口渴度
            FoodConsumptionPerStep += agent.CurrentSatietyConsumptionRate;
            WaterConsumptionPerStep += agent.CurrentThirstConsumptionRate;
        }
        
        // 任务中的消耗率可以适当降低（比如降低到正常消耗的70%）
        // 注意：这里不应该乘以granularity，因为ConsumeResources()只在Step()中调用一次
        // 如果乘以granularity，会导致一个step消耗12倍资源，这是错误的
        FoodConsumptionPerStep = (int)(FoodConsumptionPerStep * 0.7f);
        WaterConsumptionPerStep = (int)(WaterConsumptionPerStep * 0.7f);
        
        
        // 初始化任务状态
        CurrentPhase = MissionPhase.Planning;
        PhaseProgress = 0;
        PhaseCurrentTurn = 0;
        MissionCurrentTurn = 0;
        IsCompleted = false;
        IsFailed = false;
        FinalOutcome = MissionOutcome.Running;
        // 更新CharacterManager中的初始状态
        UpdateCharacterManagerStatus();
        
        // 设置所有Agent为活跃状态
        SetAgentsActive(true);
    }
    
    /// <summary>
    /// 检查任务类型与地点是否兼容
    /// </summary>
    private bool IsMissionTypeCompatibleWithPlace(MissionType missionType, Place targetPlace)
    {
        if (targetPlace == null) return false;

        return missionType switch
        {
            MissionType.Production => targetPlace.Type == "production",
            MissionType.Exploration => targetPlace.Type == "exploration" || targetPlace.Type == "landmark",
            MissionType.Combat => targetPlace.Type == "combat" || targetPlace.Type == "dangerous",
            MissionType.Rescue => targetPlace.Type == "rescue" || targetPlace.Type == "medical",
            MissionType.Delivery => targetPlace.Type == "delivery" || targetPlace.Type == "transport",
            MissionType.Investigation => targetPlace.Type == "investigation" || targetPlace.Type == "research",
            _ => true // 其他类型默认允许
        };
    }

    /// <summary>
    /// 计算各阶段所需回合数
    /// </summary>
    private void CalculatePhaseRequirements()
    {
        var baseAgent = MissionAgents[0];  // 使用第一个Agent作为基准
        
        // 计算前往目标地点所需回合数
        float distanceToTarget = MissionBasePlace.Position.DistanceTo(MissionTargetPlace.Position);
        int travelTurns = CalculateTravelTurns(distanceToTarget, baseAgent);
        
        // 计算执行任务所需回合数
        int executionTurns = CalculateExecutionTurns(baseAgent);
        
        // 计算返回基地所需回合数
        float distanceToBase = MissionTargetPlace.Position.DistanceTo(MissionBasePlace.Position);
        int returnTurns = CalculateTravelTurns(distanceToBase, baseAgent);
        
        GD.Print($"任务阶段预估:");
        GD.Print($"  前往目标: {travelTurns} 回合");
        GD.Print($"  执行任务: {executionTurns} 回合");
        GD.Print($"  返回基地: {returnTurns} 回合");
        GD.Print($"  总计: {travelTurns + executionTurns + returnTurns} 回合");
    }
    
    /// <summary>
    /// 计算旅行所需回合数
    /// </summary>
    private int CalculateTravelTurns(float distance, Agent agent)
    {
        // 基于Agent的敏捷度计算（现在速度值是在一个细粒度时间中的总距离）
        // 由于一个step包含12个细粒度时间，所以总速度是单个细粒度时间的12倍
        float baseSpeed = agent.CurrentWarpedInfo.Dexterity; // 不再乘以granularity
        float terrainModifier = GetTerrainModifier(agent.CurrentPlace);
        float weatherModifier = GetWeatherModifier();
        
        float effectiveSpeed = baseSpeed * terrainModifier * weatherModifier;
        // 计算需要多少个细粒度时间才能到达目标
        int granularityTurns = Math.Max(1, (int)(distance / effectiveSpeed));
        // 转换为大回合数（每个大回合包含12个细粒度时间）
        int turns = Math.Max(1, (int)(granularityTurns / granularity));
        
        return turns;
    }
    
    /// <summary>
    /// 计算执行任务所需回合数
    /// </summary>
    private int CalculateExecutionTurns(Agent agent)
    {
        // 基于任务类型和Agent能力计算
        int baseTurns = MissionType switch
        {
            MissionType.Production => 3,
            MissionType.Exploration => 5,
            MissionType.Combat => 2,
            MissionType.Rescue => 4,
            MissionType.Delivery => 1,
            MissionType.Investigation => 6,
            _ => 3
        };
        
        // 根据Agent技能调整
        float skillModifier = GetSkillModifier(agent);
        int adjustedTurns = Math.Max(1, (int)(baseTurns / skillModifier));
        
        return adjustedTurns;
    }
    
    /// <summary>
    /// 获取地形修正系数
    /// </summary>
    private float GetTerrainModifier(Place place)
    {
        return place?.Environment switch
        {
            "indoor" => 1.0f,
            "outdoor" => 0.8f,
            "mountain" => 0.6f,
            "water" => 0.5f,
            "forest" => 0.7f,
            _ => 1.0f
        };
    }
    
    /// <summary>
    /// 获取天气修正系数
    /// </summary>
    private float GetWeatherModifier()
    {
        // 这里可以接入天气系统，暂时返回固定值
        return 1.0f;
    }
    
    /// <summary>
    /// 获取技能修正系数
    /// </summary>
    private float GetSkillModifier(Agent agent)
    {
        float modifier = 1.0f;
        
        // 根据任务类型检查相关技能
        switch (MissionType)
        {
            case MissionType.Production:
                if (agent.Character.Skills.Contains("crafting")) modifier += 0.3f;
                if (agent.Character.Skills.Contains("engineering")) modifier += 0.2f;
                break;
            case MissionType.Exploration:
                if (agent.Character.Skills.Contains("survival")) modifier += 0.3f;
                if (agent.Character.Skills.Contains("navigation")) modifier += 0.2f;
                break;
            case MissionType.Combat:
                if (agent.Character.Skills.Contains("combat")) modifier += 0.4f;
                if (agent.Character.Skills.Contains("tactics")) modifier += 0.2f;
                break;
            case MissionType.Rescue:
                if (agent.Character.Skills.Contains("medicine")) modifier += 0.3f;
                if (agent.Character.Skills.Contains("first_aid")) modifier += 0.2f;
                break;
            case MissionType.Delivery:
                if (agent.Character.Skills.Contains("logistics")) modifier += 0.3f;
                break;
            case MissionType.Investigation:
                if (agent.Character.Skills.Contains("investigation")) modifier += 0.3f;
                if (agent.Character.Skills.Contains("analysis")) modifier += 0.2f;
                break;
        }
        
        return modifier;
    }
    
    /// <summary>
    /// 任务步进（一个大回合，包含12个细粒度时间）
    /// </summary>
    public void Step(int currentGlobalTurn) // 意味着每一回合发生12次小事件
    {
        // 如果任务已经完成，直接返回，避免继续执行
        if (IsCompleted || IsFailed || FinalOutcome != MissionOutcome.Running)
        {
            GD.Print($"任务已结束，跳过步进。状态: {FinalOutcome}");
            return;
        }
        
        MissionCurrentTurn++;
        PhaseCurrentTurn++;
        
        GD.Print($"=== 任务步进 (回合 {MissionCurrentTurn}) ===");
        GD.Print($"当前阶段: {CurrentPhase}, 进度: {PhaseProgress}%");
        
        // 消耗资源（只在step中调用一次）
        ConsumeResources();
        
        // 补充Agent的饱食度和水资源（只在step中调用一次）
        ReplenishAgentResources();
        
        // 更新Agent位置信息（只在step中调用一次）
        UpdateAgentLocations();
        
        // 每回合都同步CharacterManager状态
        UpdateCharacterManagerStatus();
        
        // 开始处理细粒度时间
        ProcessGranularityTime();
        
        // 检查任务状态
        CheckMissionStatus();
    }
    
    /// <summary>
    /// 处理细粒度时间（12个30分钟的时间段）
    /// </summary>
    private void ProcessGranularityTime()
    {
        if (isProcessingGranularity)
        {
            GD.Print("正在处理细粒度时间，跳过重复调用");
            return;
        }
        
        isProcessingGranularity = true;
        currentGranularityStep = 0;
        
        GD.Print($"开始处理细粒度时间，共{granularity}个时间段（每段30分钟）");
        
        // 处理12个细粒度时间
        for (int i = 0; i < granularity; i++)
        {
            currentGranularityStep = i;
            ProcessSingleGranularityStep();
        }
        
        currentGranularityStep = 0;
        isProcessingGranularity = false;
        
        GD.Print("细粒度时间处理完成");
    }
    
    /// <summary>
    /// 处理单个细粒度时间（30分钟）
    /// </summary>
    private void ProcessSingleGranularityStep()
    {
        // 计算当前细粒度时间（0-11对应0:00-5:30，每30分钟一个）
        int hour = currentGranularityStep / 2;
        int minute = (currentGranularityStep % 2) * 30;
        string timeString = $"{hour:D2}:{minute:D2}";
        
        GD.Print($"  细粒度时间 {currentGranularityStep + 1}/{granularity}: {timeString}");
        
        // 检查随机遭遇战（每个细粒度时间都有机会）
        if (ShouldTriggerEncounter())
        {
            TriggerRandomEncounter();
        }
        
        // 资源获取（每个细粒度时间都有机会）
        ProcessResourceGathering();
        
        // 根据当前阶段执行相应逻辑
        switch (CurrentPhase)
        {
            case MissionPhase.Planning:
                ExecutePlanningPhaseGranularity();
                break;
            case MissionPhase.Traveling:
                ExecuteTravelingPhaseGranularity();
                break;
            case MissionPhase.Executing:
                ExecuteExecutionPhaseGranularity();
                break;
            case MissionPhase.Returning:
                ExecuteReturningPhaseGranularity();
                break;
            case MissionPhase.Completed:
                // 已完成，不需要执行
                break;
        }
    }
    
    /// <summary>
    /// 处理资源获取（细粒度时间）
    /// </summary>
    private void ProcessResourceGathering()
    {
        if (MissionAgents == null || MissionAgents.Count == 0) return;
        
        // 根据当前阶段和地点类型决定资源获取
        switch (CurrentPhase)
        {
            case MissionPhase.Traveling:
                // 旅行中可能发现资源
                if (rng.Randf() < 0.15f) // 15%概率发现资源
                {
                    ProcessTravelResourceDiscovery();
                }
                break;
            case MissionPhase.Executing:
                // 执行任务时获取资源
                ProcessMissionResourceGathering();
                break;
            case MissionPhase.Returning:
                // 返回途中可能发现资源
                if (rng.Randf() < 0.1f) // 10%概率发现资源
                {
                    ProcessTravelResourceDiscovery();
                }
                break;
        }
    }
    
    /// <summary>
    /// 处理旅行中的资源发现
    /// </summary>
    private void ProcessTravelResourceDiscovery()
    {
        // 随机发现一些资源
        var resourceTypes = new[] { "food", "water", "materials" };
        var discoveredResource = resourceTypes[rng.RandiRange(0, resourceTypes.Length-1)];
        var amount = rng.RandiRange(1, 5); // 1-5单位
        
        GD.Print($"    资源发现: 发现了 {amount} 单位 {discoveredResource}");
        
        // 这里可以添加实际的资源获取逻辑
        // 比如更新任务配备的资源或通知ResourceManager
    }
    
    /// <summary>
    /// 处理任务执行中的资源获取
    /// </summary>
    private void ProcessMissionResourceGathering()
    {
        if (MissionTargetPlace == null) return;
        
        // 根据地点类型和任务类型获取资源
        switch (MissionTargetPlace.Type)
        {
            case "production":
                // 生产地点：获取生产资源
                if (rng.Randf() < 0.4f) // 40%概率获取资源
                {
                    foreach (var agent in MissionAgents)
                    {
                        var amount = rng.RandiRange(2, 8)+agent.Character.BaseWarpedInfo.Adaptability/2;
                        GD.Print($"    生产资源: 获得了 {amount} 单位生产材料");
                        foreach (var resourceId in MissionTargetPlace.Tags)
                        {
                            GameManager.Instance.ResourceManager.AddResource(resourceId, amount, $"任务生产: {MissionName}");
                        }
                    }

                }
                break;
            case "exploration":
                // 探索地点：获取探索资源
                if (rng.Randf() < 0.5f) // 50%概率获取资源
                {
                    var amount = rng.RandiRange(1, 5); // 1-4单位
                    GD.Print($"    探索发现: 获得了 {amount} 单位探索资源");
                }
                break;
            case "combat":
                // 战斗地点：获取战利品
                if (rng.Randf() < 0.3f) // 30%概率获取资源
                {
                    var amount = rng.RandiRange(1, 4); // 1-3单位
                    GD.Print($"    战利品: 获得了 {amount} 单位战利品");
                }
                break;
            default:
                // 其他类型地点
                if (rng.Randf() < 0.2f) // 20%概率获取资源
                {
                    var amount = rng.RandiRange(1, 3); // 1-2单位
                    GD.Print($"    资源获取: 获得了 {amount} 单位通用资源");
                }
                break;
        }
    }
    
    /// <summary>
    /// 执行规划阶段（细粒度时间版本）
    /// </summary>
    private void ExecutePlanningPhaseGranularity()
    {
        // 检查是否有存活的Agent，如果没有则跳过规划
        bool hasAliveAgent = false;
        foreach (var agent in MissionAgents)
        {
            if (agent != null && !agent.IsDead())
            {
                hasAliveAgent = true;
                break;
            }
        }
        
        if (!hasAliveAgent)
        {
            GD.Print("    所有Agent都已死亡，跳过规划");
            return;
        }
        
        // 每个细粒度时间完成8%的规划进度（从2%提升到8%）
        // 这样只需要12个细粒度时间（6小时）就能完成规划，而不是之前的50个细粒度时间（25小时）
        PhaseProgress += 8;
        GD.Print($"    规划进度: {PhaseProgress}%");
        
        if (PhaseProgress >= 100)
        {
            // 规划完成，进入旅行阶段
            CurrentPhase = MissionPhase.Traveling;
            PhaseProgress = 0;
            PhaseCurrentTurn = 0;
            GD.Print("规划完成，开始前往目标地点");
        }
    }
    
    /// <summary>
    /// 执行旅行阶段
    /// </summary>
    private void ExecuteTravelingPhase()
    {
        // 检查是否有存活的Agent，如果没有则跳过旅行
        bool hasAliveAgent = false;
        foreach (var agent in MissionAgents)
        {
            if (agent != null && !agent.IsDead())
            {
                hasAliveAgent = true;
                break;
            }
        }
        
        if (!hasAliveAgent)
        {
            GD.Print("    所有Agent都已死亡，跳过旅行");
            return;
        }
        
        // 每个细粒度时间推进旅行进度
        // 计算每个细粒度时间应该推进的距离
        float totalDistance = MissionBasePlace.Position.DistanceTo(MissionTargetPlace.Position);
        // 修正：旅行应该在1-2个step内完成，而不是之前的2个step
        // 每个细粒度时间推进总距离的1/12，这样12个细粒度时间就能完成旅行
        float distancePerGranularity = totalDistance / granularity;
        
        // 更新Agent位置（每个细粒度时间移动一小段距离）
        UpdateAgentTravelProgress(distancePerGranularity);
        
        // 计算旅行进度（基于细粒度时间，而不是大回合）
        float travelProgress = (float)(currentGranularityStep + 1) / granularity;
        PhaseProgress = (int)(travelProgress * 100);
        
        GD.Print($"    旅行进度: {PhaseProgress}% (距离目标: {totalDistance - GetCurrentTravelDistance():F1})");
        
        if (PhaseProgress >= 100)
        {
            // 到达目标地点，进入执行阶段
            CurrentPhase = MissionPhase.Executing;
            PhaseProgress = 0;
            PhaseCurrentTurn = 0;
            GD.Print("已到达目标地点，开始执行任务");
        }
    }
    
    /// <summary>
    /// 执行任务执行阶段
    /// </summary>
    private void ExecuteExecutionPhase()
    {
        if (PhaseProgress >= 100)
        {
            // 执行完成，进入返回阶段
            CurrentPhase = MissionPhase.Returning;
            PhaseProgress = 0;
            PhaseCurrentTurn = 0;
            GD.Print("任务执行完成，开始返回基地");
            return;
        }

        // 根据任务类型执行不同的逻辑
        switch (MissionType)
        {
            case MissionType.Production:
                ExecuteProductionPhase();
                break;
            case MissionType.Exploration:
                ExecuteExplorationPhase();
                break;
            case MissionType.Combat:
                ExecuteCombatPhase();
                break;
            case MissionType.Rescue:
                ExecuteRescuePhase();
                break;
            case MissionType.Delivery:
                ExecuteDeliveryPhase();
                break;
            case MissionType.Investigation:
                ExecuteInvestigationPhase();
                break;
            default:
                // 默认执行逻辑
                PhaseProgress += 25;
                GD.Print($"执行阶段进度: {PhaseProgress}%");
                break;
        }
    }

    /// <summary>
    /// 执行生产阶段
    /// </summary>
    private void ExecuteProductionPhase()
    {
        // 检查目标地点是否为生产类型
        if (MissionTargetPlace?.Type != "production")
        {
            GD.PrintErr($"目标地点 {MissionTargetPlace?.Name ?? "未知"} 不是生产类型，无法执行生产任务");
            PhaseProgress += 10; // 少量进度
            return;
        }

        // 获取可生产的资源列表
        var producibleResources = GetProducibleResources();
        if (producibleResources.Count == 0)
        {
            GD.PrintErr($"目标地点 {MissionTargetPlace.Name} 没有可生产的资源");
            PhaseProgress += 10;
            return;
        }

        // 计算生产量（基于Agent数量和技能）
        int productionAmount = CalculateProductionAmount();
        
        // 执行生产
        foreach (var resourceId in producibleResources)
        {
            if (resourceId == "food" || resourceId == "water")
            {
                // 生产食物和水资源，直接添加到仓库
                var success = GameManager.Instance?.ResourceManager?.AddResource(resourceId, productionAmount, $"任务生产: {MissionName}");
                if (success == true)
                {
                    GD.Print($"成功生产 {resourceId}: +{productionAmount}");
                }
                else
                {
                    GD.PrintErr($"生产 {resourceId} 失败");
                }
            }
        }

        // 更新进度
        PhaseProgress += 33;
        GD.Print($"生产阶段进度: {PhaseProgress}% (生产量: {productionAmount})");
    }

    /// <summary>
    /// 获取可生产的资源列表
    /// </summary>
    private Array<string> GetProducibleResources()
    {
        var resources = new Array<string>();
        
        if (MissionTargetPlace?.Tags == null) return resources;
        
        foreach (var tag in MissionTargetPlace.Tags)
        {
            // 检查标签是否为有效的资源ID
            if (IsValidResourceId(tag))
            {
                resources.Add(tag);
            }
        }
        
        return resources;
    }

    /// <summary>
    /// 检查是否为有效的资源ID
    /// </summary>
    private bool IsValidResourceId(string resourceId)
    {
        // 这里可以根据实际需要扩展
        return resourceId == "food" || resourceId == "water" || resourceId == "energy" || resourceId == "materials";
    }

    /// <summary>
    /// 计算生产量
    /// </summary>
    private int CalculateProductionAmount()
    {
        if (MissionAgents == null || MissionAgents.Count == 0) return 0;
        
        int baseProduction = 8; // 基础生产量提升到8
        int agentBonus = MissionAgents.Count * 2; // 每个Agent提供2点加成
        
        // 基于Agent的技能计算额外加成
        int skillBonus = 0;
        foreach (var agent in MissionAgents)
        {
            if (agent?.Character?.BaseWarpedInfo != null)
            {
                // 推理能力影响生产效率
                skillBonus += agent.Character.BaseWarpedInfo.Reasoning / 3; // 从/4改回/3
            }
        }
        
        int totalProduction = baseProduction + agentBonus + skillBonus;
        GD.Print($"生产量计算: 基础{baseProduction} + Agent加成{agentBonus} + 技能加成{skillBonus} = {totalProduction}");
        
        return totalProduction;
    }

    /// <summary>
    /// 执行探索阶段
    /// </summary>
    private void ExecuteExplorationPhase()
    {
        // 检查目标地点是否适合探索
        if (MissionTargetPlace?.Type != "exploration" && MissionTargetPlace?.Type != "landmark")
        {
            GD.PrintErr($"目标地点 {MissionTargetPlace?.Name ?? "未知"} 不适合执行探索任务");
            PhaseProgress += 10; // 少量进度
            return;
        }

        // 探索任务：发现新地点或资源
        PhaseProgress += 20;
        GD.Print($"探索阶段进度: {PhaseProgress}%");
    }

    /// <summary>
    /// 执行战斗阶段
    /// </summary>
    private void ExecuteCombatPhase()
    {
        // 检查目标地点是否适合战斗
        if (MissionTargetPlace?.Type != "combat" && MissionTargetPlace?.Type != "dangerous")
        {
            GD.PrintErr($"目标地点 {MissionTargetPlace?.Name ?? "未知"} 不适合执行战斗任务");
            PhaseProgress += 10; // 少量进度
            return;
        }

        // 战斗任务：与敌人战斗
        PhaseProgress += 50;
        GD.Print($"战斗阶段进度: {PhaseProgress}%");
    }

    /// <summary>
    /// 执行救援阶段
    /// </summary>
    private void ExecuteRescuePhase()
    {
        // 检查目标地点是否适合救援
        if (MissionTargetPlace?.Type != "rescue" && MissionTargetPlace?.Type != "medical")
        {
            GD.PrintErr($"目标地点 {MissionTargetPlace?.Name ?? "未知"} 不适合执行救援任务");
            PhaseProgress += 10; // 少量进度
            return;
        }

        // 救援任务：营救目标
        PhaseProgress += 25;
        GD.Print($"救援阶段进度: {PhaseProgress}%");
    }

    /// <summary>
    /// 执行运输阶段
    /// </summary>
    private void ExecuteDeliveryPhase()
    {
        // 检查目标地点是否适合运输
        if (MissionTargetPlace?.Type != "delivery" && MissionTargetPlace?.Type != "transport")
        {
            GD.PrintErr($"目标地点 {MissionTargetPlace?.Name ?? "未知"} 不适合执行运输任务");
            PhaseProgress += 10; // 少量进度
            return;
        }

        // 运输任务：运送物品
        PhaseProgress += 40;
        GD.Print($"运输阶段进度: {PhaseProgress}%");
    }

    /// <summary>
    /// 执行调查阶段
    /// </summary>
    private void ExecuteInvestigationPhase()
    {
        // 检查目标地点是否适合调查
        if (MissionTargetPlace?.Type != "investigation" && MissionTargetPlace?.Type != "research")
        {
            GD.PrintErr($"目标地点 {MissionTargetPlace?.Name ?? "未知"} 不适合执行调查任务");
            PhaseProgress += 10; // 少量进度
            return;
        }

        // 调查任务：收集信息
        PhaseProgress += 16;
        GD.Print($"调查阶段进度: {PhaseProgress}%");
    }
    
    /// <summary>
    /// 执行返回阶段
    /// </summary>
    private void ExecuteReturningPhase()
    {
        // 模拟返回进度
        float returnProgress = (float)PhaseCurrentTurn / CalculateTravelTurns(
            MissionTargetPlace.Position.DistanceTo(MissionBasePlace.Position), 
            MissionAgents[0]
        );
        
        PhaseProgress = (int)(returnProgress * 100);
        
        if (PhaseProgress >= 100)
        {
            // 返回完成，进入完成阶段
            CurrentPhase = MissionPhase.Completed;
            PhaseProgress = 0;
            PhaseCurrentTurn = 0;
            GD.Print("返回完成，任务阶段结束，等待所有Agent返回基地");
        }
    }
    
    /// <summary>
    /// 计算任务成功率
    /// </summary>
    private float CalculateSuccessRate()
    {
        float baseRate = 0.8f;  // 基础成功率80%
        
        // 根据Agent数量调整
        float agentBonus = Math.Min(0.1f, MissionAgents.Count * 0.02f);
        
        // 根据危险等级调整
        float dangerPenalty = MissionDangerLevel * 0.1f;
        
        // 根据回合效率调整
        float efficiencyBonus = Math.Max(0, (MissionMaxTurn - MissionCurrentTurn) * 0.01f);
        
        float finalRate = baseRate + agentBonus - dangerPenalty + efficiencyBonus;
        return Math.Max(0.0f, Math.Min(1.0f, finalRate));
    }
    
    /// <summary>
    /// 分配任务奖励
    /// </summary>
    private void DistributeRewards()
    {
        // 这里可以实现具体的奖励分配逻辑
        GD.Print("分配任务奖励...");
        
        foreach (var agent in MissionAgents)
        {
            // 增加经验值、物品等
            GD.Print($"Agent {agent.AgentName} 获得任务奖励");
        }
    }
    
    /// <summary>
    /// 评估任务可行性
    /// </summary>
    public string EvaluateMission(int currentGlobalTurn, Agent agent)
    {
        if (MissionTargetPlace == null || agent.CurrentPlace == null)
        {
            return "无法评估：缺少必要信息";
        }
        
        float distance = MissionTargetPlace.Position.DistanceTo(agent.CurrentPlace.Position);
        int turnToTarget = CalculateTravelTurns(distance, agent);
        int turnOnMission = CalculateExecutionTurns(agent);
        int turnToReturn = CalculateTravelTurns(distance, agent);
        
        int totalTurn = turnToTarget + turnOnMission + turnToReturn;
        
        if (totalTurn > MissionMaxTurn)
        {
            return $"距离太远，需要 {totalTurn} 回合，超过限制 {MissionMaxTurn} 回合";
        }
        
        // 检查Agent状态
        if (agent.CurrentHealth < 50)
        {
            return "健康状态不佳，不建议执行任务";
        }
        
        if (agent.CurrentEnergy < 30)
        {
            return "能量不足，需要休息";
        }
        
        return $"任务可行，预计需要 {totalTurn} 回合";
    }
    
    /// <summary>
    /// 获取任务状态摘要
    /// </summary>
    public string GetMissionStatus()
    {
        int aliveCount = 0;
        int returnedCount = 0;
        int deadCount = 0;

        if (MissionAgents != null)
        {
            for (int i = 0; i < MissionAgents.Count; i++)
            {
                var agent = MissionAgents[i];
                if (agent == null)
                {
                    continue;
                }

                try
                {
                    string agentName = "未知Agent";
                    try
                    {
                        agentName = agent.AgentName ?? "未知Agent";
                    }
                    catch (Exception)
                    {
                    }

                    string location = "未知";
                    // 获取位置信息
                    try
                    {
                        if (agent.CurrentPlace != null)
                        {
                            location = agent.CurrentPlace.Name ?? "未知地点";
                        }
                        else
                        {
                            location = "未知地点";
                        }
                    }
                    catch (Exception)
                    {
                        location = "未知地点";
                    }

                    bool isAlive = false;
                    string agentStatus = "未知";
                    try
                    {
                        isAlive = !agent.IsDead();

                        if (isAlive)
                        {
                            aliveCount++;
                            try
                            {
                                if (agent.CurrentPlace?.Id == MissionBasePlace?.Id)
                                {
                                    agentStatus = "已返回";
                                    returnedCount++;
                                }
                                else
                                {
                                    agentStatus = "存活";
                                }
                            }
                            catch (Exception)
                            {
                                agentStatus = "存活";
                            }
                        }
                        else
                        {
                            agentStatus = "死亡";
                            deadCount++;
                        }
                    }
                    catch (Exception)
                    {
                        agentStatus = "状态检查失败";
                        deadCount++;
                    }
                }
                catch (Exception)
                {
                    deadCount++;
                }
            }
        }

        var status = "";

        try
        {
            try
            {
                status += $"任务: {MissionName ?? "未命名"}\n";
            }
            catch (Exception)
            {
                status += "任务: 获取失败\n";
            }

            try
            {
                status += $"状态: {CurrentPhase}\n";
            }
            catch (Exception)
            {
                status += "状态: 获取失败\n";
            }

            try
            {
                status += $"进度: {PhaseProgress}%\n";
            }
            catch (Exception)
            {
                status += "进度: 获取失败\n";
            }

            try
            {
                status += $"当前回合: {MissionCurrentTurn}/{MissionMaxTurn}\n";
            }
            catch (Exception)
            {
                status += "当前回合: 获取失败\n";
            }

            try
            {
                status += $"参与Agent: {MissionAgents?.Count ?? 0} 个\n";
            }
            catch (Exception)
            {
                status += "参与Agent: 获取失败\n";
            }

            // 添加资源信息
            try
            {
                var resourceLimits = GetResourceLimits();
                status += $"食物配备: {FoodSupply}/{resourceLimits.MaxFood}\n";
                status += $"水资源配备: {WaterSupply}/{resourceLimits.MaxWater}\n";
                status += $"遭遇战概率: {EncounterChance:F1}%\n";
                status += $"遭遇战强度: {EncounterIntensity:F1}%\n";
            }
            catch (Exception ex)
            {
                GD.PrintErr($"添加资源信息时出错: {ex.Message}");
                status += $"食物配备: {FoodSupply}\n";
                status += $"水资源配备: {WaterSupply}\n";
                status += $"遭遇战概率: {EncounterChance:F1}%\n";
                status += $"遭遇战强度: {EncounterIntensity:F1}%\n";
            }

            try
            {
                status += $"任务结局: {FinalOutcome}\n";
            }
            catch (Exception)
            {
                status += "任务结局: 获取失败\n";
            }

            try
            {
                if (FinalOutcome == MissionOutcome.Running)
                {
                    status += "任务状态: 进行中\n";
                }
                else if (IsCompleted)
                {
                    status += $"结果: 成功 (成功率: {SuccessRate:P1})\n";
                }
                else if (IsFailed)
                {
                    status += $"结果: 失败 (原因: {FailureReason ?? "未知"})\n";
                }
            }
            catch (Exception)
            {
                status += "任务状态: 获取失败\n";
            }

            status += "\nAgent状态:\n";

            if (MissionAgents != null)
            {
                for (int i = 0; i < MissionAgents.Count; i++)
                {
                    var agent = MissionAgents[i];

                    if (agent == null)
                    {
                        status += $"  Agent[{i}]: null\n";
                        continue;
                    }

                    try
                    {
                        string agentName = "未知Agent";
                        string location = "未知";
                        string agentStatus = "未知";

                        try
                        {
                            agentName = agent.AgentName ?? "未知Agent";
                        }
                        catch (Exception)
                        {
                            agentName = "未知Agent";
                        }

                        try
                        {
                            if (agent.CurrentPlace != null)
                            {
                                location = agent.CurrentPlace.Name ?? "未知地点";
                            }
                            else
                            {
                                location = "未知地点";
                            }
                        }
                        catch (Exception)
                        {
                            location = "未知地点";
                        }

                        try
                        {
                            if (!agent.IsDead())
                            {
                                if (agent.CurrentPlace?.Id == MissionBasePlace?.Id)
                                {
                                    agentStatus = "已返回";
                                }
                                else
                                {
                                    agentStatus = "存活";
                                }
                            }
                            else
                            {
                                agentStatus = "死亡";
                            }
                        }
                        catch (Exception)
                        {
                            agentStatus = "状态未知";
                        }

                        status += $"  {agentName}: {agentStatus} - {location}\n";
                    }
                    catch (Exception)
                    {
                        status += $"  Agent[{i}]: 处理失败\n";
                    }
                }
            }
            else
            {
                status += "  无Agent信息\n";
            }

            try
            {
                status += $"\n统计: 存活 {aliveCount}, 返回 {returnedCount}, 死亡 {deadCount}\n";
            }
            catch (Exception)
            {
                status += "\n统计: 信息获取失败\n";
            }
        }
        catch (Exception ex)
        {
            status = $"任务状态获取失败: {ex.Message}";
        }

        return status;
    }

    /// <summary>
    /// 消耗资源
    /// </summary>
    private void ConsumeResources()
    {
        if (MissionAgents == null || MissionAgents.Count == 0) return;
        
        // 计算总消耗
        int totalFoodConsumption = MissionAgents.Count * FoodConsumptionPerStep;
        int totalWaterConsumption = MissionAgents.Count * WaterConsumptionPerStep;
        
        // 检查资源是否足够
        bool foodShortage = FoodSupply < totalFoodConsumption;
        bool waterShortage = WaterSupply < totalWaterConsumption;
        
        // 消耗资源（不能低于0）
        int oldFood = FoodSupply;
        int oldWater = WaterSupply;
        
        FoodSupply = Math.Max(0, FoodSupply - totalFoodConsumption);
        WaterSupply = Math.Max(0, WaterSupply - totalWaterConsumption);
        
        GD.Print($"任务资源消耗: 食物 -{totalFoodConsumption} (剩余: {FoodSupply}), 水资源 -{totalWaterConsumption} (剩余: {WaterSupply})");
        
        // 资源不足警告
        if (foodShortage)
        {
            GD.PrintErr($"  WARNING: 任务食物不足！需要 {totalFoodConsumption}，但只有 {oldFood}");
        }
        if (waterShortage)
        {
            GD.PrintErr($"  WARNING: 任务水资源不足！需要 {totalWaterConsumption}，但只有 {oldWater}");
        }
        
        // 资源耗尽警告（但任务继续）
        if (FoodSupply <= 0 && WaterSupply <= 0)
        {
            GD.PrintErr($"  ERROR: 任务资源耗尽！但任务将继续运行");
        }
        
        // 只有当任务配备的资源不足时，才考虑从Agent的个人状态中消耗
        // 这样可以避免与正常的饥饿机制完全冲突
        if (foodShortage || waterShortage)
        {
            GD.Print("任务资源不足，Agent将开始消耗个人资源（饥饿/口渴）");
        }
    }
    
    /// <summary>
    /// 检查是否应该触发遭遇战
    /// </summary>
    private bool ShouldTriggerEncounter()
    {
        // 在旅行和执行阶段有遭遇战概率
        if (CurrentPhase == MissionPhase.Traveling || CurrentPhase == MissionPhase.Executing)
        {
            // 由于现在每个细粒度时间（30分钟）都检查一次，所以概率需要大幅降低
            // 基础概率应该是每回合（12个细粒度时间）的总概率，然后平均分配到每个细粒度时间
            // 例如：如果希望每回合有10%概率遭遇战，那么每个细粒度时间应该是 10% / 12 = 0.83%
            float baseEncounterChancePerTurn = EncounterChance / 100.0f; // 转换为0-1范围
            float adjustedEncounterChance = baseEncounterChancePerTurn / granularity;
            
            // 添加遭遇战冷却机制，避免连续触发
            if (currentGranularityStep < 2) // 前两个细粒度时间不触发遭遇战
            {
                return false;
            }
            
            // 添加额外的随机性，避免过于规律
            if (currentGranularityStep % 3 == 0) // 每3个细粒度时间才检查一次
            {
                float roll = rng.Randf();
                return roll < adjustedEncounterChance;
            }
            
            return false;
        }
        return false;
    }
    
    /// <summary>
    /// 触发随机遭遇战
    /// </summary>
    private void TriggerRandomEncounter()
    {
        if (MissionAgents == null || MissionAgents.Count == 0)
        {
            GD.Print("无Agent参与，跳过遭遇战");
            return;
        }
        
        GD.Print("=== 遭遇战触发 ===");
        
        // 根据遭遇战强度计算损失（大幅降低伤害）
        float baseEnergyLoss = EncounterIntensity * 0.05f; // 基础精力损失为强度的5%（从10%降到5%）
        float baseHealthLoss = EncounterIntensity * 0.025f; // 基础生命损失为强度的2.5%（从5%降到2.5%）
        
        foreach (var agent in MissionAgents)
        {
            if (agent == null) continue;
            
            // 检查Agent是否已经死亡，死亡的不参与遭遇战
            if (agent.IsDead())
            {
                GD.Print($"Agent {agent.AgentName ?? "未知"} 已死亡，跳过遭遇战");
                continue;
            }
            
            try
            {
                // 计算精力损失（2-8点之间，从5-15降到2-8）
                float energyLoss = Math.Max(2, Math.Min(8, baseEnergyLoss + (float)(rng.Randf() * 3)));
                int actualEnergyLoss = (int)energyLoss;
                
                // 计算生命损失（1-4点之间，从2-8降到1-4）
                float healthLoss = Math.Max(1, Math.Min(4, baseHealthLoss + (float)(rng.Randf() * 2)));
                int actualHealthLoss = (int)healthLoss;
                
                // 应用损失
                agent.UpdateEnergy(Math.Max(0, agent.CurrentEnergy - actualEnergyLoss));
                agent.UpdateHealth(Math.Max(0, agent.CurrentHealth - actualHealthLoss));
                
                GD.Print($"Agent {agent.AgentName ?? "未知"} 遭遇战损失: 能量 -{actualEnergyLoss}, 生命 -{actualHealthLoss} (当前: 能量{agent.CurrentEnergy}, 生命{agent.CurrentHealth})");
                
                // 检查Agent是否因遭遇战死亡
                if (agent.IsDead())
                {
                    GD.PrintErr($"Agent {agent.AgentName ?? "未知"} 因遭遇战死亡！");
                }
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"遭遇战中处理Agent {agent?.AgentName ?? "未知"} 时出错: {ex.Message}");
            }
        }
        
        GD.Print("遭遇战结束");
    }
    
    /// <summary>
    /// 补充Agent的饱食度和水资源
    /// </summary>
    private void ReplenishAgentResources()
    {
        if (MissionAgents == null || MissionAgents.Count == 0)
        {
            GD.Print("无Agent参与，跳过资源补充");
            return;
        }
        
        foreach (var agent in MissionAgents)
        {
            if (agent == null) continue;
            
            try
            {
                // 只在资源不足时才补充，而不是强制设置为满值
                // 这样可以避免与正常的饥饿机制冲突
                
                // 检查饱食度是否过低（低于30%时补充）
                if (agent.Character != null && agent.CurrentSatiety < agent.Character.MaxSatiety * 0.3f)
                {
                    // 补充到50%，而不是满值
                    int targetSatiety = (int)(agent.Character.MaxSatiety * 0.5f);
                    var satietyProperty = agent.GetType().GetProperty("CurrentSatiety");
                    if (satietyProperty != null && satietyProperty.CanWrite)
                    {
                        satietyProperty.SetValue(agent, targetSatiety);
                        GD.Print($"Agent {agent.AgentName} 饱食度补充到 {targetSatiety}");
                    }
                }
                
                // 检查口渴度是否过低（低于30%时补充）
                if (agent.Character != null && agent.CurrentThirst < agent.Character.MaxThirst * 0.3f)
                {
                    // 补充到50%，而不是满值
                    int targetThirst = (int)(agent.Character.MaxThirst * 0.5f);
                    var thirstProperty = agent.GetType().GetProperty("CurrentThirst");
                    if (thirstProperty != null && thirstProperty.CanWrite)
                    {
                        thirstProperty.SetValue(agent, targetThirst);
                        GD.Print($"Agent {agent.AgentName} 口渴度补充到 {targetThirst}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"补充Agent {agent?.AgentName ?? "未知"} 资源时出错: {ex.Message}");
            }
        }
        
        GD.Print("任务中Agent的资源补充完成（仅在必要时补充）");
    }
    
    /// <summary>
    /// 检查任务状态
    /// </summary>
    private void CheckMissionStatus()
    {
        if (MissionAgents == null || MissionAgents.Count == 0)
        {
            GD.Print("无Agent参与，任务状态检查跳过");
            return;
        }
        
        // 检查Agent状态
        int aliveAgents = 0;
        int returnedAgents = 0;
        int deadAgents = 0;
        
        foreach (var agent in MissionAgents)
        {
            if (agent == null) continue;
            
            try
            {
                if (!agent.IsDead()) // 不要自己判断死亡，使用IsDead()方法
                {
                    aliveAgents++;
                    
                    // 检查是否已返回基地
                    if (agent.CurrentPlace?.Id == MissionBasePlace?.Id)
                    {
                        returnedAgents++;
                    }
                }
                else
                {
                    deadAgents++;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"检查Agent {agent?.AgentName ?? "未知"} 状态时出错: {ex.Message}");
                deadAgents++; // 出错时当作死亡处理
            }
        }
        
        GD.Print($"任务状态检查: 存活 {aliveAgents}, 返回 {returnedAgents}, 死亡 {deadAgents}");
        
        // 更新CharacterManager中的Agent状态
        UpdateCharacterManagerStatus();
        
        // 如果所有Agent都死亡，立即结束任务
        if (deadAgents == MissionAgents.Count)
        {
            FinalOutcome = MissionOutcome.TotalFailure;
            IsFailed = true;
            GD.Print("=== 任务结束：所有Agent死亡 ===");
            
            // 计算最终成功率
            SuccessRate = CalculateSuccessRate();
            
            GD.Print($"最终结局: {FinalOutcome}");
            GD.Print($"成功率: {SuccessRate:P1}");
            GD.Print($"返回Agent数量: {returnedAgents}/{MissionAgents.Count}");
            GD.Print($"死亡Agent数量: {deadAgents}/{MissionAgents.Count}");
            
            // 分配奖励
            DistributeRewards();
            
            // 最终更新CharacterManager状态
            UpdateCharacterManagerStatus();
            
            // 设置所有Agent为非活跃状态
            SetAgentsActive(false);
            return;
        }
        
        // 只有当任务阶段完成（Completed）且所有Agent都回到起点或死亡时，任务才结束
        if (CurrentPhase == MissionPhase.Completed && returnedAgents + deadAgents == MissionAgents.Count)
        {
            // 任务结束，确定最终结局
            if (returnedAgents == MissionAgents.Count)
            {
                // 所有Agent都返回
                FinalOutcome = MissionOutcome.Success;
                IsCompleted = true;
                GD.Print("=== 任务结束：所有Agent成功返回 ===");
            }
            else
            {
                // 部分Agent返回，部分死亡
                FinalOutcome = MissionOutcome.PartialSuccess;
                IsCompleted = true;
                GD.Print("=== 任务结束：部分Agent返回，部分死亡 ===");
            }
            
            // 计算最终成功率
            SuccessRate = CalculateSuccessRate();
            
            GD.Print($"最终结局: {FinalOutcome}");
            GD.Print($"成功率: {SuccessRate:P1}");
            GD.Print($"返回Agent数量: {returnedAgents}/{MissionAgents.Count}");
            GD.Print($"死亡Agent数量: {deadAgents}/{MissionAgents.Count}");
            
            // 分配奖励
            DistributeRewards();
            
            // 最终更新CharacterManager状态
            UpdateCharacterManagerStatus();
            
            // 设置所有Agent为非活跃状态
            SetAgentsActive(false);
        }
        else
        {
            // 任务仍在进行中
            FinalOutcome = MissionOutcome.Running;
            GD.Print("任务仍在进行中...");
            
            // 确保Agent保持活跃状态
            SetAgentsActive(true);
        }
    }

    /// <summary>
    /// 检查任务是否真正结束
    /// </summary>
    public bool IsMissionFinished()
    {
        return FinalOutcome != MissionOutcome.Running;
    }
    
    /// <summary>
    /// 获取任务运行状态
    /// </summary>
    public string GetMissionRunningStatus()
    {
        if (IsMissionFinished())
        {
            return $"任务已结束 - {FinalOutcome}";
        }
        
        return $"任务进行中 - 回合 {MissionCurrentTurn}";
    }

    /// <summary>
    /// 更新Agent位置信息
    /// </summary>
    private void UpdateAgentLocations()
    {
        if (MissionAgents == null || MissionAgents.Count == 0) return;
        
        foreach (var agent in MissionAgents)
        {
            if (agent == null || agent.IsDead()) continue;
            
            try
            {
                switch (CurrentPhase)
                {
                    case MissionPhase.Planning:
                        // 规划阶段：在基地
                        if (agent.CurrentPlace?.Id != MissionBasePlace?.Id)
                        {
                            agent.CurrentPlace = MissionBasePlace;
                            GD.Print($"Agent {agent.AgentName} 位置更新: 在基地进行任务规划");
                        }
                        break;
                        
                    case MissionPhase.Traveling:
                        // 旅行阶段：在前往目标的路上
                        float travelProgress = (float)PhaseCurrentTurn / CalculateTravelTurns(
                            MissionBasePlace.Position.DistanceTo(MissionTargetPlace.Position), 
                            agent
                        );
                        int progressPercent = (int)(travelProgress * 100);
                        
                        // 创建一个临时的"在路上"位置
                        var travelingPlace = new Place
                        {
                            Id = "traveling",
                            Name = $"前往{MissionTargetPlace.Name}的路上 ({progressPercent}%)",
                            Environment = "outdoor"
                        };
                        
                        if (agent.CurrentPlace?.Id != travelingPlace.Id)
                        {
                            agent.CurrentPlace = travelingPlace;
                            GD.Print($"Agent {agent.AgentName} 位置更新: 前往{MissionTargetPlace.Name}的路上 ({progressPercent}%)");
                        }
                        break;
                        
                    case MissionPhase.Executing:
                        // 执行阶段：在目标地点
                        if (agent.CurrentPlace?.Id != MissionTargetPlace?.Id)
                        {
                            agent.CurrentPlace = MissionTargetPlace;
                            GD.Print($"Agent {agent.AgentName} 位置更新: 到达{MissionTargetPlace.Name}，正在执行任务");
                        }
                        break;
                        
                    case MissionPhase.Returning:
                        // 返回阶段：在返回基地的路上
                        float returnProgress = (float)PhaseCurrentTurn / CalculateTravelTurns(
                            MissionTargetPlace.Position.DistanceTo(MissionBasePlace.Position), 
                            agent
                        );
                        int returnPercent = (int)(returnProgress * 100);
                        
                        var returningPlace = new Place
                        {
                            Id = "returning",
                            Name = $"返回{MissionBasePlace.Name}的路上 ({returnPercent}%)",
                            Environment = "outdoor"
                        };
                        
                        if (agent.CurrentPlace?.Id != returningPlace.Id)
                        {
                            agent.CurrentPlace = returningPlace;
                            GD.Print($"Agent {agent.AgentName} 位置更新: 返回{MissionBasePlace.Name}的路上 ({returnPercent}%)");
                        }
                        break;
                        
                    case MissionPhase.Completed:
                        // 完成阶段：回到基地
                        if (agent.CurrentPlace?.Id != MissionBasePlace?.Id)
                        {
                            agent.CurrentPlace = MissionBasePlace;
                            GD.Print($"Agent {agent.AgentName} 位置更新: 已返回基地");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"更新Agent {agent?.AgentName ?? "未知"} 位置时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 更新CharacterManager中的Agent状态
    /// </summary>
    private void UpdateCharacterManagerStatus()
    {
        if (GameManager.Instance?.CharacterManager == null) return;
        
        foreach (var agent in MissionAgents)
        {
            if (agent == null) continue;
            
            try
            {
                // 根据Agent状态更新CharacterManager中的列表
                if (agent.IsDead())
                {
                    // 如果Agent死亡，从AliveCharacters移除，添加到DeadCharacters
                    if (GameManager.Instance.CharacterManager.AliveCharacters.Contains(agent))
                    {
                        GameManager.Instance.CharacterManager.AliveCharacters.Remove(agent);
                    }
                    
                    if (!GameManager.Instance.CharacterManager.DeadCharacters.Contains(agent))
                    {
                        GameManager.Instance.CharacterManager.DeadCharacters.Add(agent);
                    }
                    
                    GD.Print($"Agent {agent.AgentName} 状态已更新为死亡");
                }
                else
                {
                    // 如果Agent存活，确保在AliveCharacters中
                    if (!GameManager.Instance.CharacterManager.AliveCharacters.Contains(agent))
                    {
                        GameManager.Instance.CharacterManager.AliveCharacters.Add(agent);
                    }
                    
                    if (GameManager.Instance.CharacterManager.DeadCharacters.Contains(agent))
                    {
                        GameManager.Instance.CharacterManager.DeadCharacters.Remove(agent);
                    }
                    
                    GD.Print($"Agent {agent.AgentName} 状态已更新为存活");
                }
                
                // 发出状态变化信号
                GameManager.Instance.CharacterManager.EmitSignal(nameof(CharacterManager.CharacterStatusChanged), agent);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"更新Agent {agent?.AgentName ?? "未知"} 状态时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 设置所有Agent的活跃状态
    /// </summary>
    /// <param name="isActive">是否活跃</param>
    private void SetAgentsActive(bool isActive)
    {
        if (MissionAgents == null || MissionAgents.Count == 0) return;
        
        foreach (var agent in MissionAgents)
        {
            if (agent == null) continue;
            
            try
            {
                agent.SetActive(isActive);
                
                GD.Print($"Agent {agent.AgentName} 设置为{(isActive ? "活跃" : "非活跃")}状态");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"设置Agent {agent?.AgentName ?? "未知"} 活跃状态时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 更新Agent的旅行进度
    /// </summary>
    private void UpdateAgentTravelProgress(float distancePerGranularity)
    {
        foreach (var agent in MissionAgents)
        {
            if (agent == null || agent.IsDead()) continue;
            
            try
            {
                // 计算当前Agent在旅行中的总距离
                float currentTravelDistance = 0;
                if (MissionBasePlace != null && MissionTargetPlace != null)
                {
                    currentTravelDistance = MissionBasePlace.Position.DistanceTo(agent.CurrentPlace?.Position ?? MissionBasePlace.Position);
                }
                
                // 计算目标距离
                float targetDistance = MissionBasePlace.Position.DistanceTo(MissionTargetPlace.Position);
                
                // 计算当前Agent在旅行中的总距离与目标距离的比率
                float travelProgressRatio = currentTravelDistance / targetDistance;
                
                // 计算当前Agent在旅行中的总距离加上本次细粒度时间推进的距离
                float newTravelDistance = currentTravelDistance + distancePerGranularity;
                
                // 计算新的旅行进度比率
                float newTravelProgressRatio = newTravelDistance / targetDistance;
                
                // 如果Agent的当前位置不是在前往目标的路上，则更新其位置
                if (agent.CurrentPlace?.Id != "traveling")
                {
                    var travelingPlace = new Place
                    {
                        Id = "traveling",
                        Name = $"前往{MissionTargetPlace.Name}的路上 ({Math.Min(100, (int)(newTravelProgressRatio * 100))}%)",
                        Environment = "outdoor"
                    };
                    agent.CurrentPlace = travelingPlace;
                    GD.Print($"Agent {agent.AgentName} 位置更新: 前往{MissionTargetPlace.Name}的路上 ({Math.Min(100, (int)(newTravelProgressRatio * 100))}%)");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"更新Agent {agent?.AgentName ?? "未知"} 旅行进度时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 获取当前Agent在旅行中的总距离
    /// </summary>
    private float GetCurrentTravelDistance()
    {
        if (MissionBasePlace == null || MissionTargetPlace == null) return 0;
        
        float currentTravelDistance = 0;
        if (MissionBasePlace.Position != null && MissionTargetPlace.Position != null)
        {
            currentTravelDistance = MissionBasePlace.Position.DistanceTo(MissionTargetPlace.Position);
        }
        
        return currentTravelDistance;
    }

    /// <summary>
    /// 更新Agent的返回进度
    /// </summary>
    private void UpdateAgentReturnProgress(float distancePerGranularity)
    {
        foreach (var agent in MissionAgents)
        {
            if (agent == null || agent.IsDead()) continue;
            
            try
            {
                // 计算当前Agent在返回中的总距离
                float currentReturnDistance = 0;
                if (MissionTargetPlace != null && MissionBasePlace != null)
                {
                    currentReturnDistance = MissionTargetPlace.Position.DistanceTo(agent.CurrentPlace?.Position ?? MissionTargetPlace.Position);
                }
                
                // 计算目标距离
                float targetDistance = MissionTargetPlace.Position.DistanceTo(MissionBasePlace.Position);
                
                // 计算当前Agent在返回中的总距离与目标距离的比率
                float returnProgressRatio = currentReturnDistance / targetDistance;
                
                // 计算当前Agent在返回中的总距离加上本次细粒度时间推进的距离
                float newReturnDistance = currentReturnDistance + distancePerGranularity;
                
                // 计算新的返回进度比率
                float newReturnProgressRatio = newReturnDistance / targetDistance;
                
                // 如果Agent的当前位置不是在返回基地的路上，则更新其位置
                if (agent.CurrentPlace?.Id != "returning")
                {
                    var returningPlace = new Place
                    {
                        Id = "returning",
                        Name = $"返回{MissionBasePlace.Name}的路上 ({Math.Min(100, (int)(newReturnProgressRatio * 100))}%)",
                        Environment = "outdoor"
                    };
                    agent.CurrentPlace = returningPlace;
                    GD.Print($"Agent {agent.AgentName} 位置更新: 返回{MissionBasePlace.Name}的路上 ({Math.Min(100, (int)(newReturnProgressRatio * 100))}%)");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"更新Agent {agent?.AgentName ?? "未知"} 返回进度时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 获取当前Agent在返回中的总距离
    /// </summary>
    private float GetCurrentReturnDistance()
    {
        if (MissionTargetPlace == null || MissionBasePlace == null) return 0;
        
        float currentReturnDistance = 0;
        if (MissionTargetPlace.Position != null && MissionBasePlace.Position != null)
        {
            currentReturnDistance = MissionTargetPlace.Position.DistanceTo(MissionBasePlace.Position);
        }
        
        return currentReturnDistance;
    }

    /// <summary>
    /// 执行生产阶段（细粒度时间版本）
    /// </summary>
    private void ExecuteProductionPhaseGranularity()
    {
        // 检查是否有存活的Agent，如果没有则跳过生产
        bool hasAliveAgent = false;
        foreach (var agent in MissionAgents)
        {
            if (agent != null && !agent.IsDead())
            {
                hasAliveAgent = true;
                break;
            }
        }
        
        if (!hasAliveAgent)
        {
            GD.Print("    所有Agent都已死亡，跳过生产");
            return;
        }
        
        // 每个细粒度时间完成8%的生产进度（从2%提升到8%）
        // 这样只需要12个细粒度时间（6小时）就能完成生产，而不是之前的50个细粒度时间（25小时）
        PhaseProgress += 8;
        GD.Print($"    生产进度: {PhaseProgress}%");
        
        // 生产资源（每个细粒度时间都有机会）
        if (MissionTargetPlace?.Type == "production")
        {
            var producibleResources = GetProducibleResources();
            if (producibleResources.Count > 0)
            {
                // 只计算存活Agent的生产量
                int aliveAgentCount = 0;
                foreach (var agent in MissionAgents)
                {
                    if (agent != null && !agent.IsDead())
                    {
                        aliveAgentCount++;
                    }
                }
                
                if (aliveAgentCount > 0)
                {
                    int baseProduction = 8; // 基础生产量
                    int agentBonus = aliveAgentCount * 2; // 每个存活Agent提供2点加成
                    int skillBonus = 0;
                    
                    // 基于存活Agent的技能计算额外加成
                    foreach (var agent in MissionAgents)
                    {
                        if (agent?.Character?.BaseWarpedInfo != null && !agent.IsDead())
                        {
                            skillBonus += agent.Character.BaseWarpedInfo.Reasoning / 3;
                        }
                    }
                    
                    int totalProduction = baseProduction + agentBonus + skillBonus;
                    int productionAmount = totalProduction / granularity; // 分配到每个细粒度时间
                    
                    if (productionAmount > 0)
                    {
                        GD.Print($"    生产资源: 获得了 {productionAmount} 单位生产材料");
                        // 这里可以添加实际的资源生产逻辑
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 执行探索阶段（细粒度时间版本）
    /// </summary>
    private void ExecuteExplorationPhaseGranularity()
    {
        // 检查是否有存活的Agent，如果没有则跳过探索
        bool hasAliveAgent = false;
        foreach (var agent in MissionAgents)
        {
            if (agent != null && !agent.IsDead())
            {
                hasAliveAgent = true;
                break;
            }
        }
        
        if (!hasAliveAgent)
        {
            GD.Print("    所有Agent都已死亡，跳过探索");
            return;
        }
        
        // 每个细粒度时间完成8%的探索进度（从8%保持不变，确保12个细粒度时间完成）
        PhaseProgress += 8;
        GD.Print($"    探索进度: {PhaseProgress}%");
        
        // 探索发现（每个细粒度时间都有机会）
        if (rng.Randf() < 0.3f) // 30%概率发现新内容
        {
            GD.Print($"    探索发现: 发现了新的信息或资源");
            // 这里可以添加实际的探索发现逻辑
        }
    }
    
    /// <summary>
    /// 执行战斗阶段（细粒度时间版本）
    /// </summary>
    private void ExecuteCombatPhaseGranularity()
    {
        // 检查是否有存活的Agent，如果没有则跳过战斗
        bool hasAliveAgent = false;
        foreach (var agent in MissionAgents)
        {
            if (agent != null && !agent.IsDead())
            {
                hasAliveAgent = true;
                break;
            }
        }
        
        if (!hasAliveAgent)
        {
            GD.Print("    所有Agent都已死亡，跳过战斗");
            return;
        }
        
        // 每个细粒度时间完成8%的战斗进度（从8%保持不变，确保12个细粒度时间完成）
        PhaseProgress += 8;
        GD.Print($"    战斗进度: {PhaseProgress}%");
        
        // 战斗事件（每个细粒度时间都有机会）
        if (rng.Randf() < 0.4f) // 40%概率发生战斗事件
        {
            GD.Print($"    战斗事件: 遭遇敌人，进行战斗");
            // 这里可以添加实际的战斗逻辑
        }
    }
    
    /// <summary>
    /// 执行救援阶段（细粒度时间版本）
    /// </summary>
    private void ExecuteRescuePhaseGranularity()
    {
        // 检查是否有存活的Agent，如果没有则跳过救援
        bool hasAliveAgent = false;
        foreach (var agent in MissionAgents)
        {
            if (agent != null && !agent.IsDead())
            {
                hasAliveAgent = true;
                break;
            }
        }
        
        if (!hasAliveAgent)
        {
            GD.Print("    所有Agent都已死亡，跳过救援");
            return;
        }
        
        // 每个细粒度时间完成8%的救援进度（从8%保持不变，确保12个细粒度时间完成）
        PhaseProgress += 8;
        GD.Print($"    救援进度: {PhaseProgress}%");
        
        // 救援行动（每个细粒度时间都有机会）
        if (rng.Randf() < 0.25f) // 25%概率进行救援行动
        {
            GD.Print($"    救援行动: 进行救援操作");
            // 这里可以添加实际的救援逻辑
        }
    }
    
    /// <summary>
    /// 执行运输阶段（细粒度时间版本）
    /// </summary>
    private void ExecuteDeliveryPhaseGranularity()
    {
        // 检查是否有存活的Agent，如果没有则跳过运输
        bool hasAliveAgent = false;
        foreach (var agent in MissionAgents)
        {
            if (agent != null && !agent.IsDead())
            {
                hasAliveAgent = true;
                break;
            }
        }
        
        if (!hasAliveAgent)
        {
            GD.Print("    所有Agent都已死亡，跳过运输");
            return;
        }
        
        // 每个细粒度时间完成8%的运输进度（从8%保持不变，确保12个细粒度时间完成）
        PhaseProgress += 8;
        GD.Print($"    运输进度: {PhaseProgress}%");
        
        // 运输检查（每个细粒度时间都有机会）
        if (rng.Randf() < 0.2f) // 20%概率进行运输检查
        {
            GD.Print($"    运输检查: 检查运输物品状态");
            // 这里可以添加实际的运输检查逻辑
        }
    }
    
    /// <summary>
    /// 执行调查阶段（细粒度时间版本）
    /// </summary>
    private void ExecuteInvestigationPhaseGranularity()
    {
        // 检查是否有存活的Agent，如果没有则跳过调查
        bool hasAliveAgent = false;
        foreach (var agent in MissionAgents)
        {
            if (agent != null && !agent.IsDead())
            {
                hasAliveAgent = true;
                break;
            }
        }
        
        if (!hasAliveAgent)
        {
            GD.Print("    所有Agent都已死亡，跳过调查");
            return;
        }
        
        // 每个细粒度时间完成8%的调查进度（从8%保持不变，确保12个细粒度时间完成）
        PhaseProgress += 8;
        GD.Print($"    调查进度: {PhaseProgress}%");
        
        // 调查发现（每个细粒度时间都有机会）
        if (rng.Randf() < 0.35f) // 35%概率发现调查线索
        {
            GD.Print($"    调查发现: 发现了新的线索");
            // 这里可以添加实际的调查发现逻辑
        }
    }
    
    /// <summary>
    /// 执行旅行阶段（细粒度时间版本）
    /// </summary>
    private void ExecuteTravelingPhaseGranularity()
    {
        // 检查是否有存活的Agent，如果没有则跳过旅行
        bool hasAliveAgent = false;
        foreach (var agent in MissionAgents)
        {
            if (agent != null && !agent.IsDead())
            {
                hasAliveAgent = true;
                break;
            }
        }
        
        if (!hasAliveAgent)
        {
            GD.Print("    所有Agent都已死亡，跳过旅行");
            return;
        }
        
        // 每个细粒度时间推进旅行进度
        // 计算每个细粒度时间应该推进的距离
        float totalDistance = MissionBasePlace.Position.DistanceTo(MissionTargetPlace.Position);
        // 修正：旅行应该在1-2个step内完成，而不是之前的2个step
        // 每个细粒度时间推进总距离的1/12，这样12个细粒度时间就能完成旅行
        float distancePerGranularity = totalDistance / granularity;
        
        // 更新Agent位置（每个细粒度时间移动一小段距离）
        UpdateAgentTravelProgress(distancePerGranularity);
        
        // 计算旅行进度（基于细粒度时间，而不是大回合）
        float travelProgress = (float)(currentGranularityStep + 1) / granularity;
        PhaseProgress = (int)(travelProgress * 100);
        
        GD.Print($"    旅行进度: {PhaseProgress}% (距离目标: {totalDistance - GetCurrentTravelDistance():F1})");
        
        if (PhaseProgress >= 100)
        {
            // 到达目标地点，进入执行阶段
            CurrentPhase = MissionPhase.Executing;
            PhaseProgress = 0;
            PhaseCurrentTurn = 0;
            GD.Print("已到达目标地点，开始执行任务");
        }
    }
    
    /// <summary>
    /// 执行任务执行阶段（细粒度时间版本）
    /// </summary>
    private void ExecuteExecutionPhaseGranularity()
    {
        if (PhaseProgress >= 100)
        {
            // 执行完成，进入返回阶段
            CurrentPhase = MissionPhase.Returning;
            PhaseProgress = 0;
            PhaseCurrentTurn = 0;
            GD.Print("任务执行完成，开始返回基地");
            return;
        }

        // 检查是否有存活的Agent，如果没有则跳过执行
        bool hasAliveAgent = false;
        foreach (var agent in MissionAgents)
        {
            if (agent != null && !agent.IsDead())
            {
                hasAliveAgent = true;
                break;
            }
        }
        
        if (!hasAliveAgent)
        {
            GD.Print("所有Agent都已死亡，跳过任务执行");
            return;
        }

        // 每个细粒度时间推进执行进度
        // 根据任务类型执行不同的逻辑
        switch (MissionType)
        {
            case MissionType.Production:
                ExecuteProductionPhaseGranularity();
                break;
            case MissionType.Exploration:
                ExecuteExplorationPhaseGranularity();
                break;
            case MissionType.Combat:
                ExecuteCombatPhaseGranularity();
                break;
            case MissionType.Rescue:
                ExecuteRescuePhaseGranularity();
                break;
            case MissionType.Delivery:
                ExecuteDeliveryPhaseGranularity();
                break;
            case MissionType.Investigation:
                ExecuteInvestigationPhaseGranularity();
                break;
            default:
                // 默认执行逻辑
                PhaseProgress += 8; // 每个细粒度时间8%，12个细粒度时间完成96%
                GD.Print($"    执行阶段进度: {PhaseProgress}%");
                break;
        }
    }
    
    /// <summary>
    /// 执行返回阶段（细粒度时间版本）
    /// </summary>
    private void ExecuteReturningPhaseGranularity()
    {
        // 检查是否有存活的Agent，如果没有则跳过返回
        bool hasAliveAgent = false;
        foreach (var agent in MissionAgents)
        {
            if (agent != null && !agent.IsDead())
            {
                hasAliveAgent = true;
                break;
            }
        }
        
        if (!hasAliveAgent)
        {
            GD.Print("    所有Agent都已死亡，跳过返回");
            return;
        }
        
        // 每个细粒度时间推进返回进度
        // 计算每个细粒度时间应该推进的距离
        float totalDistance = MissionTargetPlace.Position.DistanceTo(MissionBasePlace.Position);
        // 修正：返回应该在1-2个step内完成，而不是之前的2个step
        // 每个细粒度时间推进总距离的1/12，这样12个细粒度时间就能完成返回
        float distancePerGranularity = totalDistance / granularity;
        
        // 更新Agent返回进度（每个细粒度时间移动一小段距离）
        UpdateAgentReturnProgress(distancePerGranularity);
        
        // 计算返回进度（基于细粒度时间，而不是大回合）
        float returnProgress = (float)(currentGranularityStep + 1) / granularity;
        PhaseProgress = (int)(returnProgress * 100);
        
        GD.Print($"    返回进度: {PhaseProgress}% (距离基地: {totalDistance - GetCurrentReturnDistance():F1})");
        
        if (PhaseProgress >= 100)
        {
            // 返回基地，任务完成
            CurrentPhase = MissionPhase.Completed;
            PhaseProgress = 100;
            GD.Print("已返回基地，任务完成");
        }
    }

            /// <summary>
        /// 动态计算任务难度 - 基于路径上附近地点的难度等级
        /// </summary>
        private float CalculateDynamicMissionDifficulty()
        {
            if (MissionBasePlace == null || MissionTargetPlace == null) return 1.0f;
            
            var allPlaces = GameManager.Instance?.Library?.GetAllPlaces();
            if (allPlaces == null) return 1.0f;
            
            float totalDifficulty = 0.0f;
            float totalWeight = 0.0f;
            const float searchRadius = 50.0f; // 50单位搜索半径
            
            // 计算路径方向向量
            Vector3 pathDirection = (MissionTargetPlace.Position - MissionBasePlace.Position).Normalized();
            float pathLength = MissionBasePlace.Position.DistanceTo(MissionTargetPlace.Position);
            
            // 如果路径太短，直接返回目标地点难度
            if (pathLength < 10.0f)
            {
                return Math.Max(1.0f, MissionTargetPlace.Level);
            }
            
            // 沿着路径采样多个点，检查附近的地点
            int samplePoints = Math.Max(3, (int)(pathLength / 20.0f)); // 每20单位采样一个点
            
            for (int i = 0; i <= samplePoints; i++)
            {
                // 计算路径上的采样点
                float t = (float)i / samplePoints;
                Vector3 samplePoint = MissionBasePlace.Position.Lerp(MissionTargetPlace.Position, t);
                
                // 检查这个采样点附近的所有地点
                foreach (var place in allPlaces)
                {
                    if (place == null || place == MissionBasePlace || place == MissionTargetPlace) continue;
                    
                    float distance = samplePoint.DistanceTo(place.Position);
                    if (distance <= searchRadius)
                    {
                        // 距离越近，权重越大
                        float weight = 1.0f - (distance / searchRadius);
                        weight = Math.Max(0.1f, weight); // 最小权重0.1
                        
                        // 地点难度等级
                        float placeDifficulty = Math.Max(1.0f, place.Level);
                        
                        // 根据地点类型调整难度
                        float typeMultiplier = GetPlaceTypeDifficultyMultiplier(place.Type);
                        
                        totalDifficulty += placeDifficulty * weight * typeMultiplier;
                        totalWeight += weight;
                    }
                }
            }
            
            // 如果没有找到附近地点，使用起点和终点的平均难度
            if (totalWeight < 0.1f)
            {
                float baseDifficulty = Math.Max(1.0f, MissionBasePlace.Level);
                float targetDifficulty = Math.Max(1.0f, MissionTargetPlace.Level);
                return (baseDifficulty + targetDifficulty) / 2.0f;
            }
            
            // 计算加权平均难度
            float averageDifficulty = totalDifficulty / totalWeight;
            
            // 确保最小难度为1.0
            return Math.Max(1.0f, averageDifficulty);
        }
    
    /// <summary>
    /// 获取地点类型的难度乘数
    /// </summary>
    private float GetPlaceTypeDifficultyMultiplier(string placeType)
    {
        return placeType switch
        {
            "dangerous" => 1.5f,      // 危险地点难度更高
            "combat" => 1.4f,         // 战斗地点
            "hostile" => 1.6f,        // 敌对地点
            "exploration" => 1.2f,    // 探索地点
            "landmark" => 1.1f,       // 地标地点
            "production" => 0.8f,     // 生产地点相对安全
            "safe" => 0.7f,           // 安全地点
            "medical" => 0.9f,        // 医疗地点
            "rescue" => 1.3f,         // 救援地点
            "research" => 1.1f,       // 研究地点
            "transport" => 0.9f,      // 运输地点
            "delivery" => 0.8f,       // 配送地点
            _ => 1.0f                 // 默认乘数
        };
    }
}