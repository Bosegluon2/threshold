using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Threshold.Core.Utils;

namespace Threshold.Core.Conditions
{
    /// <summary>
    /// 条件类型枚举
    /// </summary>
    public enum ConditionType
    {
        Simple,         // 简单条件
        And,            // 与条件组合
        Or,             // 或条件组合
        Not,            // 非条件
        Script          // 脚本条件
    }

    /// <summary>
    /// 条件操作符枚举
    /// </summary>
    public enum ConditionOperator
    {
        Equals,         // ==
        NotEquals,      // !=
        GreaterThan,    // >
        LessThan,       // <
        GreaterEqual,   // >=
        LessEqual,      // <=
        Contains,       // 包含
        NotContains,    // 不包含
        Exists,         // 存在
        NotExists       // 不存在
    }

    /// <summary>
    /// 条件目标类型枚举
    /// </summary>
    public enum ConditionTargetType
    {
        WorldState,     // 世界状态
        Character,      // 角色属性
        SystemVariable, // 系统变量
        Item,           // 物品
        Skill,          // 技能
        Quest,          // 任务
        Event           // 事件
    }

    /// <summary>
    /// 基础条件类
    /// </summary>
    [Obsolete("已弃用，请使用ScriptExecutor替代")]
    public abstract class BaseCondition
    {
        public string ConditionId { get; set; } = "";
        public ConditionType Type { get; set; }
        public string Description { get; set; } = "";
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 评估条件
        /// </summary>
        public abstract bool Evaluate(GameManager gameManager);

        /// <summary>
        /// 获取条件的字符串表示
        /// </summary>
        public abstract string GetDisplayString();
    }

    /// <summary>
    /// 简单条件类
    /// </summary>
    [Obsolete("已弃用，请使用ScriptExecutor替代")]
    public class SimpleCondition : BaseCondition
    {
        public ConditionTargetType TargetType { get; set; }
        public string TargetPath { get; set; } = ""; // 例如: "character:char_0001.skills.magic_theory"
        public ConditionOperator Operator { get; set; }
        public object ExpectedValue { get; set; }

        public SimpleCondition()
        {
            Type = ConditionType.Simple;
        }

        public override bool Evaluate(GameManager gameManager)
        {
            if (!IsEnabled) return false;

            try
            {
                // 获取目标值
                object actualValue = GetTargetValue(gameManager);

                // 执行比较操作
                return CompareValues(actualValue, ExpectedValue, Operator);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"评估条件时发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取目标值
        /// </summary>
        private object GetTargetValue(GameManager gameManager)
        {
            try
            {
                // 使用PathResolver来获取目标值
                return ScriptExecutor.GetValue(gameManager, TargetPath);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"通过PathResolver获取目标值时发生错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 比较值
        /// </summary>
        private bool CompareValues(object actual, object expected, ConditionOperator op)
        {
            if (actual == null && expected == null)
                return op == ConditionOperator.Equals;

            if (actual == null || expected == null)
                return op == ConditionOperator.NotEquals;

            try
            {
                switch (op)
                {
                    case ConditionOperator.Equals:
                        return actual.Equals(expected);

                    case ConditionOperator.NotEquals:
                        return !actual.Equals(expected);

                    case ConditionOperator.GreaterThan:
                        return Convert.ToDouble(actual) > Convert.ToDouble(expected);

                    case ConditionOperator.LessThan:
                        return Convert.ToDouble(actual) < Convert.ToDouble(expected);

                    case ConditionOperator.GreaterEqual:
                        return Convert.ToDouble(actual) >= Convert.ToDouble(expected);

                    case ConditionOperator.LessEqual:
                        return Convert.ToDouble(actual) <= Convert.ToDouble(expected);

                    case ConditionOperator.Contains:
                        return actual.ToString().Contains(expected.ToString());

                    case ConditionOperator.NotContains:
                        return !actual.ToString().Contains(expected.ToString());

                    case ConditionOperator.Exists:
                        return actual != null;

                    case ConditionOperator.NotExists:
                        return actual == null;

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public override string GetDisplayString()
        {
            var operatorText = Operator switch
            {
                ConditionOperator.Equals => "==",
                ConditionOperator.NotEquals => "!=",
                ConditionOperator.GreaterThan => ">",
                ConditionOperator.LessThan => "<",
                ConditionOperator.GreaterEqual => ">=",
                ConditionOperator.LessEqual => "<=",
                ConditionOperator.Contains => "包含",
                ConditionOperator.NotContains => "不包含",
                ConditionOperator.Exists => "存在",
                ConditionOperator.NotExists => "不存在",
                _ => "未知"
            };

            return $"{TargetPath} {operatorText} {ExpectedValue}";
        }
    }

    /// <summary>
    /// 条件组合类
    /// </summary>
    [Obsolete("已弃用，请使用ScriptExecutor替代")]
    public class ConditionCombination : BaseCondition
    {
        public List<BaseCondition> SubConditions { get; set; } = new List<BaseCondition>();

        public ConditionCombination()
        {
            SubConditions = new List<BaseCondition>();
        }

        public override bool Evaluate(GameManager gameManager)
        {
            if (!IsEnabled || SubConditions.Count == 0) return false;

            switch (Type)
            {
                case ConditionType.And:
                    return SubConditions.All(c => c.Evaluate(gameManager));

                case ConditionType.Or:
                    return SubConditions.Any(c => c.Evaluate(gameManager));

                case ConditionType.Not:
                    return SubConditions.Count > 0 && !SubConditions[0].Evaluate(gameManager);

                default:
                    return false;
            }
        }

        public override string GetDisplayString()
        {
            var operatorText = Type switch
            {
                ConditionType.And => "AND",
                ConditionType.Or => "OR",
                ConditionType.Not => "NOT",
                _ => "未知"
            };

            if (Type == ConditionType.Not)
            {
                return $"NOT ({SubConditions.FirstOrDefault()?.GetDisplayString() ?? "无条件"})";
            }

            var conditionStrings = SubConditions.Select(c => c.GetDisplayString());
            return $"({string.Join($" {operatorText} ", conditionStrings)})";
        }
    }

    /// <summary>
    /// 条件管理器
    /// </summary>
    [Obsolete("已弃用，请使用ScriptExecutor替代")]
    public class ConditionManager
    {
        private static ConditionManager instance;
        public static ConditionManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ConditionManager();
                return instance;
            }
        }

        private Dictionary<string, BaseCondition> conditionTemplates = new Dictionary<string, BaseCondition>();

        private ConditionManager() { }

        /// <summary>
        /// 注册条件模板
        /// </summary>
        public void RegisterConditionTemplate(string templateId, BaseCondition condition)
        {
            conditionTemplates[templateId] = condition;
        }

        /// <summary>
        /// 获取条件模板
        /// </summary>
        public BaseCondition GetConditionTemplate(string templateId)
        {
            return conditionTemplates.TryGetValue(templateId, out var condition) ? condition : null;
        }

        /// <summary>
        /// 评估条件
        /// </summary>
        public bool EvaluateCondition(BaseCondition condition, GameManager gameManager)
        {
            if (condition == null) return false;
            return condition.Evaluate(gameManager);
        }

        /// <summary>
        /// 评估条件列表
        /// </summary>
        public bool EvaluateConditions(List<BaseCondition> conditions, GameManager gameManager)
        {
            if (conditions == null || conditions.Count == 0) return true;

            // 默认使用AND逻辑
            return conditions.All(c => EvaluateCondition(c, gameManager));
        }

        /// <summary>
        /// 创建简单条件
        /// </summary>
        public static SimpleCondition CreateSimpleCondition(
            string targetPath,
            ConditionOperator op,
            object expectedValue)
        {
            var condition = new SimpleCondition
            {
                TargetPath = targetPath,
                Operator = op,
                ExpectedValue = expectedValue
            };

            // 自动推断目标类型
            if (targetPath.StartsWith("character:"))
                condition.TargetType = ConditionTargetType.Character;
            else if (targetPath.StartsWith("world:"))
                condition.TargetType = ConditionTargetType.WorldState;
            else if (targetPath.StartsWith("system:"))
                condition.TargetType = ConditionTargetType.SystemVariable;
            else if (targetPath.StartsWith("item:"))
                condition.TargetType = ConditionTargetType.Item;
            else if (targetPath.StartsWith("skill:"))
                condition.TargetType = ConditionTargetType.Skill;
            else if (targetPath.StartsWith("quest:"))
                condition.TargetType = ConditionTargetType.Quest;
            else if (targetPath.StartsWith("event:"))
                condition.TargetType = ConditionTargetType.Event;

            return condition;
        }

        /// <summary>
        /// 创建AND条件组合
        /// </summary>
        public static ConditionCombination CreateAndCondition(params BaseCondition[] conditions)
        {
            var combination = new ConditionCombination
            {
                Type = ConditionType.And
            };

            combination.SubConditions.AddRange(conditions);
            return combination;
        }

        /// <summary>
        /// 创建OR条件组合
        /// </summary>
        public static ConditionCombination CreateOrCondition(params BaseCondition[] conditions)
        {
            var combination = new ConditionCombination
            {
                Type = ConditionType.Or
            };

            combination.SubConditions.AddRange(conditions);
            return combination;
        }

        /// <summary>
        /// 创建NOT条件
        /// </summary>
        public static ConditionCombination CreateNotCondition(BaseCondition condition)
        {
            var combination = new ConditionCombination
            {
                Type = ConditionType.Not
            };

            combination.SubConditions.Add(condition);
            return combination;
        }
    }
}
