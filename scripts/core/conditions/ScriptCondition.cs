using Godot;
using Threshold.Core.Utils;

namespace Threshold.Core.Conditions
{
    /// <summary>
    /// 脚本条件
    /// 使用ScriptExecutor执行脚本作为条件判断
    /// 可以替代复杂的条件配置，直接写函数调用链
    /// </summary>
    public class ScriptCondition : BaseCondition
    {
        /// <summary>
        /// 脚本内容，用分号分隔多个语句
        /// </summary>
        public string Script { get; set; }
        public ConditionType ConditionType { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ScriptCondition(string script)
        {
            Script = script;
            ConditionType = ConditionType.Script;
        }

        /// <summary>
        /// 评估条件
        /// </summary>
        public override bool Evaluate(GameManager gameManager)
        {
            if (string.IsNullOrWhiteSpace(Script))
            {
                GD.PrintErr("脚本条件为空");
                return false;
            }

            try
            {
                // 使用ScriptExecutor执行脚本
                var result = ScriptExecutor.Instance.ExecuteScript<bool>(Script, new Godot.Collections.Dictionary<string, Variant>
                {
                    ["gameManager"] = Variant.CreateFrom(gameManager)
                });
                GD.Print($"脚本条件执行结果: {result}");
                return result;
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"脚本条件执行失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取显示字符串
        /// </summary>
        public override string GetDisplayString()
        {
            return $"脚本条件: {Script}";
        }

        /// <summary>
        /// 验证脚本语法
        /// </summary>
    }
}
