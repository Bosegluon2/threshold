using Godot;
using Godot.Collections;
using System;

namespace Threshold.Core.Agent.Functions
{
    /// <summary>
    /// 允许Agent自主调控记忆的函数
    /// </summary>
    public partial class UpdateMemoriesFunction : Resource, IFunctionExecutor
    {
        public string Name => "update_memories";
        public string Description => "允许Agent自主添加、删除或标记记忆。";

        public FunctionResult Execute(Dictionary arguments, System.Collections.Generic.Dictionary<string, object> extraParams = null)
        {
            try
            {
                GD.Print("=== 执行UpdateMemoriesFunction ===");
                // 获取当前Agent
                Agent currentAgent = null;
                if (extraParams != null && extraParams.ContainsKey("current_agent"))
                {
                    currentAgent = extraParams["current_agent"] as Agent;
                }
                if (currentAgent == null)
                {
                    return new FunctionResult(Name, "无法获取当前Agent", false, "请确保Agent已正确设置");
                }

                // 操作类型: write/delete/clear/search
                string operation = arguments.ContainsKey("operation") ? arguments["operation"].AsString() : "write";
                string memoryContent = arguments.ContainsKey("memory_content") ? arguments["memory_content"].AsString() : "";
                string type = arguments.ContainsKey("type") ? arguments["type"].AsString() : "memory";

                // 根据操作类型执行不同的操作
                switch (operation)
                {
                    case "write":
                        // 写入记忆
                        currentAgent.AddMemory(memoryContent, type);
                        break;
                    case "clear":
                        // 清空记忆
                        currentAgent.ClearMemories(type);
                        break;
                    case "search":
                        // 搜索记忆 返回格式为JSON Array<string>
                        Array<string> result = currentAgent.SearchMemories(memoryContent);
                        return new FunctionResult(Name, Json.Stringify(result), true, "搜索记忆成功");

                }
                return new FunctionResult(Name, "", true, $"操作成功: {operation}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"UpdateMemoriesFunction 执行失败: {ex.Message}");
                return new FunctionResult(Name, "", false, $"执行失败: {ex.Message}");
            }
        }

        public string GetFunctionName() => Name;

        public string GetFunctionDescription() => Description;

        public Array<FunctionParameter> GetParameters()
        {
            return new Array<FunctionParameter>
            {
                new FunctionParameter("operation", "string", "操作类型：write（写入）/clear（清空）/search（搜索）", false),
                new FunctionParameter("memory_content", "string", "记忆内容/搜索关键词（空则为获取所有记忆）", true),
                new FunctionParameter("type", "string", "记忆类型：memory（记忆）/event（事件）/secret（秘密），默认memory", false),
            };
        }
    }
}
