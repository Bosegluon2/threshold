using Godot;
using Godot.Collections;
using System;
using Threshold.Core.Enums;
using Threshold.Core.Data;

namespace Threshold.Core
{
    /// <summary>
    /// 世界管理器 - 负责管理游戏世界状态
    /// </summary>
    public partial class WorldManager : Node
    {
        private ulong Seed { get; set; } = 0;
        #region Events
        [Signal] public delegate void WorldStateChangedEventHandler(WorldState newState);
        [Signal] public delegate void WeatherChangedEventHandler(WeatherType newWeather);
        #endregion

        #region World Properties
        [Export] public WorldState CurrentWorldState { get; private set; } = WorldState.Normal;
        public WeatherMachine WeatherMachine;
        public int CurrentTurn { get; private set; } = 0;
        
        [Export] public float DangerLevel { get; private set; } = 0.0f; // 危险等级 0-100
        #endregion

        #region Internal Variables
        private GameManager _gameManager;
        private RandomNumberGenerator _random;
        #endregion

        #region Map
        public Dictionary<string, Place> Places { get; private set; } = new Dictionary<string, Place>();
        #endregion

        public WorldManager(GameManager gameManager, ulong seed)
        {
            _gameManager = gameManager;
            _random = new RandomNumberGenerator();
            _random.Seed = seed;
            Seed = seed;
            WeatherMachine = new WeatherMachine(seed);
        }
        public WeatherType GetCurrentWeather()
        {
            return WeatherMachine.CurrentWeather;
        }
        public override void _Ready()
        {
            Initialize();
        }
        public TimeOfDay GetTimeOfDay()
        {
            int tod = CurrentTurn % 4;
            return (TimeOfDay)tod;
        }
        /// <summary>
        /// 初始化世界管理器
        /// </summary>
        public void Initialize()
        {
            CurrentWorldState = WorldState.Normal;
            CurrentTurn = 0;
            DangerLevel = 0.0f;
            
            GD.Print("世界管理器初始化完成");
        }

        /// <summary>
        /// 更新世界状态
        /// </summary>
        public void UpdateWorldState(int turn)
        {   
            CurrentTurn++;
            
            // 更新天气
            UpdateWeather();
            
            // 更新危险等级
            UpdateDangerLevel();
            
            // 更新世界状态
            UpdateWorldStateBasedOnConditions();
        }

        /// <summary>
        /// 更新天气
        /// </summary>
        private void UpdateWeather()
        {
            WeatherMachine.Step();
        }

        /// <summary>
        /// 获取天气变化概率
        /// </summary>
        private float GetWeatherChangeChance()
        {
            var baseChance = 0.1f; // 基础变化概率
            
            // 半夜有更高概率变化天气
            if (GetTimeOfDay() == TimeOfDay.Midnight)
            {
                baseChance *= 2.0f;
            }
            
            // 随着游戏进行，天气变化更频繁
            baseChance += CurrentTurn * 0.02f;
            
            return Mathf.Clamp(baseChance, 0.05f, 0.5f);
        }

        /// <summary>
        /// 获取随机天气
        /// </summary>
        private WeatherType GetRandomWeather()
        {
            var weathers = new[] { WeatherType.Clear, WeatherType.Cloudy, WeatherType.Rainy, WeatherType.Stormy, WeatherType.Foggy, WeatherType.Snowy };
            var weights = new float[weathers.Length];
            
            // 根据时段调整天气权重
            for (int i = 0; i < weathers.Length; i++)
            {
                weights[i] = GetWeatherWeight(weathers[i], GetTimeOfDay());
            }
            
            // 根据当前天气调整权重（避免频繁变化）
            var currentIndex = System.Array.IndexOf(weathers, GetCurrentWeather());
            if (currentIndex >= 0)
            {
                weights[currentIndex] *= 0.5f; // 降低当前天气的权重
            }
            
            // 随机选择天气
            var totalWeight = 0.0f;
            foreach (var weight in weights)
            {
                totalWeight += weight;
            }
            
            var randomValue = (float)_random.Randf() * totalWeight;
            var currentWeight = 0.0f;
            
            for (int i = 0; i < weathers.Length; i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                {
                    return weathers[i];
                }
            }
            
            return WeatherType.Clear; // 默认
        }

        /// <summary>
        /// 获取天气权重
        /// </summary>
        private float GetWeatherWeight(WeatherType weather, TimeOfDay timeOfDay)
        {
            var baseWeight = 1.0f;
            
            // 根据时段调整权重
            switch (timeOfDay)
            {
                case TimeOfDay.Morning:
                    if (weather == WeatherType.Clear) baseWeight *= 1.5f;
                    if (weather == WeatherType.Foggy) baseWeight *= 1.2f;
                    break;
                case TimeOfDay.Noon:
                    if (weather == WeatherType.Clear) baseWeight *= 1.3f;
                    if (weather == WeatherType.Cloudy) baseWeight *= 1.1f;
                    break;
                case TimeOfDay.Evening:
                    if (weather == WeatherType.Cloudy) baseWeight *= 1.2f;
                    if (weather == WeatherType.Rainy) baseWeight *= 1.1f;
                    break;
                case TimeOfDay.Midnight:
                    if (weather == WeatherType.Stormy) baseWeight *= 1.4f;
                    if (weather == WeatherType.Foggy) baseWeight *= 1.3f;
                    break;
            }
            
            return baseWeight;
        }

        /// <summary>
        /// 更新危险等级
        /// </summary>
        private void UpdateDangerLevel()
        {
            var oldDangerLevel = DangerLevel;
            
            // 基础危险等级随天数增加
            var baseDanger = CurrentTurn * 2.0f;
            
            // 时段影响
            var timeModifier = GetTimeOfDay() switch
            {
                TimeOfDay.Morning => 0.8f,
                TimeOfDay.Noon => 1.0f,
                TimeOfDay.Evening => 1.3f,
                TimeOfDay.Midnight => 2.0f, // 半夜最危险
                _ => 1.0f
            };
            
            // 天气影响
            var weatherModifier = GetCurrentWeather() switch
            {
                WeatherType.Clear => 0.8f,
                WeatherType.Cloudy => 1.0f,
                WeatherType.Rainy => 1.3f,
                WeatherType.Stormy => 1.8f,
                WeatherType.Foggy => 1.5f,
                WeatherType.Snowy => 1.2f,
                _ => 1.0f
            };
            
            // 计算新危险等级
            DangerLevel = Mathf.Clamp(baseDanger * timeModifier * weatherModifier, 0.0f, 100.0f);
            
            // 添加随机波动
            var randomVariation = (float)(_random.Randf() - 0.5) * 5.0f;
            DangerLevel = Mathf.Clamp(DangerLevel + randomVariation, 0.0f, 100.0f);
            
            if (Mathf.Abs(DangerLevel - oldDangerLevel) > 5.0f)
            {
                GD.Print($"危险等级变化: {oldDangerLevel:F1} -> {DangerLevel:F1}");
            }
        }

        /// <summary>
        /// 根据条件更新世界状态
        /// </summary>
        private void UpdateWorldStateBasedOnConditions()
        {
            var oldState = CurrentWorldState;
            
            // 根据危险等级确定世界状态
            if (DangerLevel >= 80.0f)
            {
                CurrentWorldState = WorldState.Chaos;
            }
            else if (DangerLevel >= 60.0f)
            {
                CurrentWorldState = WorldState.Critical;
            }
            else if (DangerLevel >= 40.0f)
            {
                CurrentWorldState = WorldState.Dangerous;
            }
            else if (DangerLevel >= 20.0f)
            {
                CurrentWorldState = WorldState.Normal;
            }
            else
            {
                CurrentWorldState = WorldState.Safe;
            }
            
            if (CurrentWorldState != oldState)
            {
                EmitSignal(SignalName.WorldStateChanged, CurrentWorldState.GetHashCode());
                GD.Print($"世界状态变化: {oldState} -> {CurrentWorldState}");
            }
        }

        /// <summary>
        /// 获取世界状态描述
        /// </summary>
        public string GetWorldStateDescription()
        {
            var description = $"世界状态: {CurrentWorldState}\n";
            description += $"当前天气: {GetCurrentWeather()}\n";
            description += $"危险等级: {DangerLevel:F1}/100\n";
            description += $"当前天数: 第{CurrentTurn/4}天\n";
            
            
            // 添加状态建议
            switch (CurrentWorldState)
            {
                case WorldState.Safe:
                    description += "建议: 可以安全地进行探索和收集活动";
                    break;
                case WorldState.Normal:
                    description += "建议: 保持警惕，注意周围环境";
                    break;
                case WorldState.Dangerous:
                    description += "建议: 减少外出，加强防御措施";
                    break;
                case WorldState.Critical:
                    description += "建议: 避免外出，专注于生存";
                    break;
                case WorldState.Chaos:
                    description += "建议: 寻找安全庇护所，等待危险过去";
                    break;
            }
            
            return description;
        }

        /// <summary>
        /// 获取天气对游戏的影响
        /// </summary>
        public string GetWeatherEffects()
        {
            var effects = $"天气效果 - {GetCurrentWeather()}:\n";
            
            switch (GetCurrentWeather())
            {
                case WeatherType.Clear:
                    effects += "• 视野范围: 正常\n";
                    effects += "• 移动速度: 正常\n";
                    effects += "• 资源收集: 正常\n";
                    break;
                case WeatherType.Cloudy:
                    effects += "• 视野范围: 轻微减少\n";
                    effects += "• 移动速度: 正常\n";
                    effects += "• 资源收集: 轻微减少\n";
                    break;
                case WeatherType.Rainy:
                    effects += "• 视野范围: 减少20%\n";
                    effects += "• 移动速度: 减少15%\n";
                    effects += "• 资源收集: 减少25%\n";
                    break;
                case WeatherType.Stormy:
                    effects += "• 视野范围: 减少50%\n";
                    effects += "• 移动速度: 减少30%\n";
                    effects += "• 资源收集: 减少60%\n";
                    effects += "• 危险: 高\n";
                    break;
                case WeatherType.Foggy:
                    effects += "• 视野范围: 减少70%\n";
                    effects += "• 移动速度: 减少20%\n";
                    effects += "• 资源收集: 减少40%\n";
                    break;
                case WeatherType.Snowy:
                    effects += "• 视野范围: 减少30%\n";
                    effects += "• 移动速度: 减少25%\n";
                    effects += "• 资源收集: 减少35%\n";
                    break;
            }
            
            return effects;
        }
    }
}