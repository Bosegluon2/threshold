using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using Threshold.Core.Enums;

namespace Threshold.Core
{
    /// <summary>
    /// 游戏资源类
    /// </summary>
    public partial class GameResource:Resource
    {
        public string GameResourceId { get; set; } = "";
        public string GameResourceName { get; set; } = "";
        public ResourceType ResourceType { get; set; } = ResourceType.Materials;
        public int CurrentAmount { get; set; } = 0;
        public int MaxAmount { get; set; } = 100;
        public float DecayRate { get; set; } = 0.0f; // 每日衰减率
        public bool IsEssential { get; set; } = false; // 是否必需资源
        public string Description { get; set; } = "";
        
        public GameResource(string resourceId, string resourceName, ResourceType resourceType)
        {
            GameResourceId = resourceId;
            GameResourceName = resourceName;
            ResourceType = resourceType;
        }
        
        /// <summary>
        /// 检查资源是否充足
        /// </summary>
        public bool IsSufficient(int requiredAmount = 1)
        {
            return CurrentAmount >= requiredAmount;
        }
        
        /// <summary>
        /// 获取资源状态描述
        /// </summary>
        public string GetStatusDescription()
        {
            var percentage = (float)CurrentAmount / MaxAmount;
            var status = percentage switch
            {
                >= 0.8f => "充足",
                >= 0.5f => "正常",
                >= 0.3f => "偏低",
                >= 0.1f => "不足",
                _ => "紧缺"
            };
            
            return $"{GameResourceName}({GameResourceId}): {CurrentAmount}/{MaxAmount} ({status})";
        }
    }

    /// <summary>
    /// 资源管理器 - 负责管理游戏中的各种资源
    /// </summary>
    public partial class ResourceManager : Node
    {
        #region Events
        [Signal] public delegate void ResourceChangedEventHandler(string resourceId, int oldAmount, int newAmount);
        [Signal] public delegate void ResourceDepletedEventHandler(string resourceId);
        [Signal] public delegate void ResourceCriticalEventHandler(string resourceId);
        [Signal] public delegate void ResourceOverflowEventHandler(string resourceId);
        #endregion

        #region Resource Collections
        [Export] public Godot.Collections.Dictionary<string, GameResource> Resources { get; private set; } = new Godot.Collections.Dictionary<string, GameResource>();
        [Export] public Array<GameResource> EssentialResources { get; private set; } = new Array<GameResource>();
        [Export] public Array<GameResource> CriticalResources { get; private set; } = new Array<GameResource>();
        #endregion

        #region Resource Settings
        [Export] public bool EnableResourceDecay { get; set; } = false; // 是否启用资源衰减
        [Export] public float GlobalDecayMultiplier { get; set; } = 1.0f; // 全局衰减倍数
        [Export] public int CriticalThreshold { get; set; } = 20; // 资源紧缺阈值
        [Export] public int OverflowThreshold { get; set; } = 90; // 资源溢出阈值
        #endregion

        #region Internal Variables
        private GameManager _gameManager;
        private Godot.Collections.Dictionary<string, int> _resourceHistory;
        private Godot.Collections.Dictionary<string, int> _lastUpdateTime;
        #endregion

        public ResourceManager(GameManager gameManager)
        {
            _gameManager = gameManager;
            _resourceHistory = new Godot.Collections.Dictionary<string, int>();
            _lastUpdateTime = new Godot.Collections.Dictionary<string, int>();
        }

        public override void _Ready()
        {
            Initialize();
        }

        /// <summary>
        /// 初始化资源管理器
        /// </summary>
        public void Initialize()
        {
            // 清空所有资源集合
            Resources.Clear();
            EssentialResources.Clear();
            CriticalResources.Clear();
            
            // 创建初始资源
            CreateInitialResources();
            
            // 初始化资源历史
            foreach (var resource in Resources.Values)
            {
                _resourceHistory[resource.GameResourceId] = resource.CurrentAmount;
                _lastUpdateTime[resource.GameResourceId] = _gameManager.CurrentTurn;
            }
            
            GD.Print("资源管理器初始化完成");
        }

        /// <summary>
        /// 创建初始资源
        /// </summary>
        private void CreateInitialResources()
        {
            // 基础生存资源
            CreateResource("food", "食物", ResourceType.Food, 50, 200, 5, 0, 0.05f, true, "维持生命的基本食物");
            CreateResource("water", "水", ResourceType.Water, 60, 200, 8, 0, 0.03f, true, "维持生命的基本水源");
            CreateResource("medicine", "药品", ResourceType.Medicine, 20, 50, 1, 0, 0.0f, false, "治疗伤病的重要药品");
            
            // 战斗资源
            CreateResource("ammunition", "弹药", ResourceType.Ammunition, 30, 200, 2, 0, 0.0f, false, "用于战斗的弹药");
            CreateResource("fuel", "燃料", ResourceType.Fuel, 40, 100, 3, 0, 0.02f, false, "用于发电和运输的燃料");
            
            // 材料资源
            CreateResource("materials", "材料", ResourceType.Materials, 100, 500, 0, 2, 0.01f, false, "用于建造和维修的材料");
            
            GD.Print($"创建了 {Resources.Count} 种初始资源");
        }

        /// <summary>
        /// 创建资源
        /// </summary>
        private void CreateResource(string resourceId, string resourceName, ResourceType resourceType, 
            int initialAmount, int maxAmount, int dailyConsumption, int dailyProduction, 
            float decayRate, bool isEssential, string description)
        {
            var resource = new GameResource(resourceId, resourceName, resourceType)
            {
                CurrentAmount = initialAmount,
                MaxAmount = maxAmount,
                DecayRate = decayRate,
                IsEssential = isEssential,
                Description = description
            };
            
            Resources[resourceId] = resource;
            
            if (isEssential)
            {
                EssentialResources.Add(resource);
            }
            
            GD.Print($"创建资源: {resourceName} (初始数量: {initialAmount})");
        }

        /// <summary>
        /// 更新资源状态
        /// </summary>
        public void UpdateResources(int turn)
        {
            int day = TimeUtils.GetDay(turn);
            TimeOfDay timeOfDay = TimeUtils.GetTimeOfDay(turn);

            foreach (var resource in Resources.Values)
            {
                var oldAmount = resource.CurrentAmount;
                _gameManager.CharacterManager.UpdateAvailableCharacters();
                // 每一个不Active的且在基地的Agent消耗

                // 处理资源衰减
                if (EnableResourceDecay && resource.DecayRate > 0)
                {
                    var decayAmount = (int)(resource.CurrentAmount * resource.DecayRate * GlobalDecayMultiplier);
                    if (decayAmount > 0)
                    {
                        ConsumeResource(resource.GameResourceId, decayAmount, "自然衰减");
                    }
                }

                // 检查资源状态变化
                CheckResourceStatus(resource, oldAmount);
            }
            
            // 更新资源历史
            UpdateResourceHistory();
        }

        /// <summary>
        /// 添加资源
        /// </summary>
        public bool AddResource(string resourceId, int amount, string reason = "")
        {
            if (!Resources.ContainsKey(resourceId) || amount <= 0)
            {
                return false;
            }
            
            var resource = Resources[resourceId];
            var oldAmount = resource.CurrentAmount;
            var newAmount = Mathf.Min(resource.CurrentAmount + amount, resource.MaxAmount);
            
            if (newAmount != oldAmount)
            {
                resource.CurrentAmount = newAmount;
                
                // 发出资源变化信号
                EmitSignal(SignalName.ResourceChanged, resourceId, oldAmount, newAmount);
                
                // 检查是否溢出
                if (newAmount >= resource.MaxAmount * OverflowThreshold / 100)
                {
                    EmitSignal(SignalName.ResourceOverflow, resourceId);
                }
                
                GD.Print($"资源增加: {resource.GameResourceName} +{amount} = {newAmount} (原因: {reason})");
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 消耗资源
        /// </summary>
        public bool ConsumeResource(string resourceId, int amount, string reason = "")
        {
            if (!Resources.ContainsKey(resourceId) || amount <= 0)
            {
                return false;
            }
            
            var resource = Resources[resourceId];
            var oldAmount = resource.CurrentAmount;
            var newAmount = Mathf.Max(resource.CurrentAmount - amount, 0);
            
            if (newAmount != oldAmount)
            {
                resource.CurrentAmount = newAmount;
                
                // 发出资源变化信号
                EmitSignal(SignalName.ResourceChanged, resourceId, oldAmount, newAmount);
                
                // 检查资源状态
                CheckResourceStatus(resource, oldAmount);
                
                GD.Print($"资源消耗: {resource.GameResourceName} -{amount} = {newAmount} (原因: {reason})");
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 检查资源状态
        /// </summary>
        private void CheckResourceStatus(GameResource resource, int oldAmount)
        {
            var percentage = (float)resource.CurrentAmount / resource.MaxAmount;
            
            // 检查是否紧缺
            if (percentage <= CriticalThreshold / 100.0f)
            {
                if (!CriticalResources.Contains(resource))
                {
                    CriticalResources.Add(resource);
                    EmitSignal(SignalName.ResourceCritical, resource.GameResourceId);
                    GD.Print($"资源紧缺警告: {resource.GameResourceName}");
                }
            }
            else
            {
                CriticalResources.Remove(resource);
            }
            
            // 检查是否耗尽
            if (resource.CurrentAmount <= 0 && oldAmount > 0)
            {
                EmitSignal(SignalName.ResourceDepleted, resource.GameResourceId);
                GD.Print($"资源耗尽: {resource.GameResourceName}");
            }
        }

        /// <summary>
        /// 获取资源
        /// </summary>
        public GameResource GetResource(string resourceId)
        {
            return Resources.ContainsKey(resourceId) ? Resources[resourceId] : null;
        }

        /// <summary>
        /// 检查资源是否充足
        /// </summary>
        public bool HasSufficientResource(string resourceId, int requiredAmount)
        {
            var resource = GetResource(resourceId);
            return resource != null && resource.IsSufficient(requiredAmount);
        }

        /// <summary>
        /// 获取资源统计信息
        /// </summary>
        public string GetResourceStatistics()
        {
            var stats = $"资源统计:\n";
            stats += $"总资源种类: {Resources.Count}\n";
            stats += $"必需资源: {EssentialResources.Count}\n";
            stats += $"紧缺资源: {CriticalResources.Count}\n";
            stats += $"资源衰减: {(EnableResourceDecay ? "启用" : "禁用")}\n";
            stats += $"全局衰减倍数: {GlobalDecayMultiplier:F2}\n\n";
            
            foreach (var resource in Resources.Values)
            {
                stats += $"{resource.GetStatusDescription()}\n";
            }
            
            return stats;
        }

        /// <summary>
        /// 更新资源历史
        /// </summary>
        private void UpdateResourceHistory()
        {
            foreach (var resource in Resources.Values)
            {
                _resourceHistory[resource.GameResourceId] = resource.CurrentAmount;
                _lastUpdateTime[resource.GameResourceId] = _gameManager.CurrentTurn;
            }
        }

        /// <summary>
        /// 获取资源变化趋势
        /// </summary>
        public string GetResourceTrend(string resourceId)
        {
            if (!_resourceHistory.ContainsKey(resourceId))
            {
                return "无历史数据";
            }
            
            var resource = GetResource(resourceId);
            if (resource == null) return "资源不存在";
            
            var currentAmount = resource.CurrentAmount;
            var historicalAmount = _resourceHistory[resourceId];
            var change = currentAmount - historicalAmount;
            
            var trend = change switch
            {
                > 0 => "上升",
                < 0 => "下降",
                _ => "稳定"
            };
            
            return $"{resource.GameResourceName}: {trend} ({change:+0;-0})";
        }

        /// <summary>
        /// 设置资源上限
        /// </summary>
        public void SetResourceMaxAmount(string resourceId, int newMaxAmount)
        {
            if (Resources.ContainsKey(resourceId) && newMaxAmount > 0)
            {
                var resource = Resources[resourceId];
                var oldMax = resource.MaxAmount;
                resource.MaxAmount = newMaxAmount;
                
                // 如果当前数量超过新上限，进行调整
                if (resource.CurrentAmount > newMaxAmount)
                {
                    resource.CurrentAmount = newMaxAmount;
                }
                
                GD.Print($"资源上限调整: {resource.GameResourceName} {oldMax} -> {newMaxAmount}");
            }
        }

        /// <summary>
        /// 重置资源到初始状态
        /// </summary>
        public void ResetResources()
        {
            foreach (var resource in Resources.Values)
            {
                // 这里可以根据需要设置初始值
                resource.CurrentAmount = Mathf.Min(resource.CurrentAmount, resource.MaxAmount);
            }
            
            GD.Print("所有资源已重置");
        }
    }
}
