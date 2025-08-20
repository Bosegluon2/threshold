using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Threshold.Core.Conditions
{
    /// <summary>
    /// 条件加载器
    /// </summary>
    public class ConditionLoader
    {
        private static ConditionLoader instance;
        public static ConditionLoader Instance
        {
            get
            {
                if (instance == null)
                    instance = new ConditionLoader();
                return instance;
            }
        }
        
        private ConditionLoader() { }
        
        /// <summary>
        /// 从YAML数据创建条件
        /// </summary>
        public BaseCondition CreateConditionFromData(Dictionary<string, object> data)
        {
            if (data == null) return null;
            
            var conditionType = data.GetValueOrDefault("type", "simple").ToString();
            
            switch (conditionType.ToLower())
            {
                case "simple":
                    return CreateSimpleConditionFromData(data);
                    
                case "and":
                    return CreateCombinationConditionFromData(data, ConditionType.And);
                    
                case "or":
                    return CreateCombinationConditionFromData(data, ConditionType.Or);
                    
                case "not":
                    return CreateCombinationConditionFromData(data, ConditionType.Not);
                    
                case "script":
                    return CreateScriptConditionFromData(data);
                    
                default:
                    GD.PrintErr($"不支持的条件类型: {conditionType}");
                    return null;
            }
        }
        
        /// <summary>
        /// 从YAML数据创建简单条件
        /// </summary>
        private SimpleCondition CreateSimpleConditionFromData(Dictionary<string, object> data)
        {
            var condition = new SimpleCondition();
            
            // 设置基本属性
            condition.ConditionId = data.GetValueOrDefault("condition_id", "").ToString();
            condition.Description = data.GetValueOrDefault("description", "").ToString();
            condition.IsEnabled = data.GetValueOrDefault("is_enabled", true).ToString().ToLower() == "true";
            
            // 设置目标路径
            condition.TargetPath = data.GetValueOrDefault("target_path", "").ToString();
            
            // 设置操作符
            var operatorStr = data.GetValueOrDefault("operator", "equals").ToString().ToLower();
            condition.Operator = ParseOperator(operatorStr);
            
            // 设置期望值
            condition.ExpectedValue = data.GetValueOrDefault("expected_value", "");
            
            // 自动推断目标类型
            condition.TargetType = InferTargetType(condition.TargetPath);
            
            return condition;
        }
        
        /// <summary>
        /// 从YAML数据创建组合条件
        /// </summary>
        private ConditionCombination CreateCombinationConditionFromData(Dictionary<string, object> data, ConditionType type)
        {
            var combination = new ConditionCombination();
            
            // 设置基本属性
            combination.ConditionId = data.GetValueOrDefault("condition_id", "").ToString();
            combination.Description = data.GetValueOrDefault("description", "").ToString();
            combination.IsEnabled = data.GetValueOrDefault("is_enabled", true).ToString().ToLower() == "true";
            combination.Type = type;
            
            // 解析子条件
            if (data.TryGetValue("sub_conditions", out var subConditionsData))
            {
                if (subConditionsData is List<object> subConditionsList)
                {
                    foreach (var subConditionData in subConditionsList)
                    {
                        if (subConditionData is Dictionary<string, object> subConditionDict)
                        {
                            var subCondition = CreateConditionFromData(subConditionDict);
                            if (subCondition != null)
                            {
                                combination.SubConditions.Add(subCondition);
                            }
                        }
                    }
                }
            }
            
            return combination;
        }
        
        /// <summary>
        /// 从YAML数据创建脚本条件
        /// </summary>
        private ScriptCondition CreateScriptConditionFromData(Dictionary<string, object> data)
        {
            var condition = new ScriptCondition("");
            
            // 设置基本属性
            condition.ConditionId = data.GetValueOrDefault("condition_id", "").ToString();
            condition.Description = data.GetValueOrDefault("description", "").ToString();
            condition.IsEnabled = data.GetValueOrDefault("is_enabled", true).ToString().ToLower() == "true";
            
            // 设置脚本内容
            condition.Script = data.GetValueOrDefault("script", "").ToString();
            
            return condition;
        }
        
        /// <summary>
        /// 解析操作符字符串
        /// </summary>
        private ConditionOperator ParseOperator(string operatorStr)
        {
            return operatorStr switch
            {
                "equals" => ConditionOperator.Equals,
                "not_equals" => ConditionOperator.NotEquals,
                "greater_than" => ConditionOperator.GreaterThan,
                "less_than" => ConditionOperator.LessThan,
                "greater_equal" => ConditionOperator.GreaterEqual,
                "less_equal" => ConditionOperator.LessEqual,
                "contains" => ConditionOperator.Contains,
                "not_contains" => ConditionOperator.NotContains,
                "exists" => ConditionOperator.Exists,
                "not_exists" => ConditionOperator.NotExists,
                _ => ConditionOperator.Equals
            };
        }
        
        /// <summary>
        /// 推断目标类型
        /// </summary>
        private ConditionTargetType InferTargetType(string targetPath)
        {
            if (string.IsNullOrEmpty(targetPath)) return ConditionTargetType.WorldState;
            
            var prefix = targetPath.Split(':')[0].ToLower();
            
            return prefix switch
            {
                "world" => ConditionTargetType.WorldState,
                "character" => ConditionTargetType.Character,
                "system" => ConditionTargetType.SystemVariable,
                "item" => ConditionTargetType.Item,
                "skill" => ConditionTargetType.Skill,
                "quest" => ConditionTargetType.Quest,
                "event" => ConditionTargetType.Event,
                _ => ConditionTargetType.WorldState
            };
        }
        
        /// <summary>
        /// 从YAML数据创建条件列表
        /// </summary>
        public List<BaseCondition> CreateConditionsFromData(List<object> conditionsData)
        {
            var conditions = new List<BaseCondition>();
            
            if (conditionsData == null) return conditions;
            
            foreach (var conditionData in conditionsData)
            {
                if (conditionData is Dictionary<string, object> conditionDict)
                {
                    var condition = CreateConditionFromData(conditionDict);
                    if (condition != null)
                    {
                        conditions.Add(condition);
                    }
                }
            }
            
            return conditions;
        }
        
        /// <summary>
        /// 从YAML数据创建条件列表（重载版本）
        /// </summary>
        public List<BaseCondition> CreateConditionsFromData(object conditionsData)
        {
            if (conditionsData is List<object> list)
            {
                return CreateConditionsFromData(list);
            }
            
            return new List<BaseCondition>();
        }
    }
    
    /// <summary>
    /// 字典扩展方法
    /// </summary>
    public static class DictionaryExtensions
    {
        public static T GetValueOrDefault<T>(this Dictionary<string, object> dict, string key, T defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is T typedValue)
                        return typedValue;
                    
                    // 尝试转换
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            
            return defaultValue;
        }
    }
}
