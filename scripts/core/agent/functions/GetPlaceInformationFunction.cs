using Godot;
using Godot.Collections;
using Threshold.Core.Data;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Threshold.Core.Agent.Functions
{
    /// <summary>
    /// 获取其他角色信息函数 - 需要权限验证
    /// </summary>
    public partial class GetPlaceInformationFunction : Resource, IFunctionExecutor
    {
        public string Name => "get_place_information";
        public string Description => "获取当前位置的信息";

        public FunctionResult Execute(Godot.Collections.Dictionary arguments, System.Collections.Generic.Dictionary<string, object> extraParams = null)
        {
            try
            {
                GD.Print($"=== 执行获取位置信息函数 ===");
                GD.Print($"参数: {Json.Stringify(arguments)}");

                // 获取参数
                var requestedInfo = arguments.ContainsKey("requested_info") ? arguments["requested_info"].AsString() : "current_place";
                var requestedInfoPlaceIdOrName = arguments.ContainsKey("place_id_or_name") ? arguments["place_id_or_name"].AsString() : "";

                // 优先从extraParams获取当前Agent
                Agent currentAgent = null;
                if (extraParams != null && extraParams.ContainsKey("current_agent"))
                {
                    currentAgent = extraParams["current_agent"] as Agent;
                    GD.Print($"从extraParams获取到Agent: {currentAgent?.AgentName ?? "null"}");
                }

                // 如果extraParams中没有，则从AI通信上下文获取
                if (currentAgent == null)
                {
                    currentAgent = GetCurrentAgentFromContext();
                    GD.Print($"从上下文获取到Agent: {currentAgent?.AgentName ?? "null"}");
                }

                if (currentAgent == null)
                {
                    return new FunctionResult(Name, "无法获取当前Agent信息", false, "请确保Agent已正确设置");
                }

                // 确定目标位置 
                if (requestedInfo == "current_place")
                {
                    var place = currentAgent.GetCurrentPlace();
                    if (place == null)
                    {
                        return new FunctionResult(Name, "无法获取当前位置信息", false, "请确保Agent已正确设置");
                    }
                    GD.Print($"当前Agent: {currentAgent.AgentName} -> 目标位置: {place.Name}");

                    // 根据权限获取信息
                    var statusInfo = GetPlaceStatusInfo(currentAgent, place);

                    GD.Print($"位置信息获取完成: {place.Name} -> {currentAgent.AgentName}");
                    return new FunctionResult(Name, statusInfo, true, "");
                }
                else if (requestedInfo == "all_places")
                {
                    var places = GameManager.Instance.Library.GetAllPlaces();
                    StringBuilder sb = new StringBuilder();
                    foreach (var place in places)
                    {
                        sb.Append($"id: {place.Id} 名称: {place.Name} \n");
                    }
                    return new FunctionResult(Name, sb.ToString(), true, "");
                }
                else if (requestedInfo == "search_place")
                {
                    // 先搜索位置
                    Place place = null;
                    var places = GameManager.Instance.Library.SearchPlaces(requestedInfoPlaceIdOrName);
                    if (places.Count != 0)
                    {
                        place = places[0];
                        var info = GetPlaceStatusInfo(currentAgent, place);
                        return new FunctionResult(Name, info, true, "");
                    }
                    else
                    {
                        // 搜索不到则用id搜索
                        place = GameManager.Instance.Library.GetPlace(requestedInfoPlaceIdOrName);
                        if (place == null)
                        {
                            return new FunctionResult(Name, "无法获取位置信息", false, "请确保位置ID或名称正确");
                        }
                        var info = GetPlaceStatusInfo(currentAgent, place);
                        return new FunctionResult(Name, info, true, "");
                    }
                }
                else
                {
                    return new FunctionResult(Name, "无效的请求信息类型", false, "请确保requested_info为current/all/search");
                }



            }
            catch (Exception ex)
            {
                GD.PrintErr($"获取位置信息函数执行失败: {ex.Message}");
                return new FunctionResult(Name, "", false, $"执行失败: {ex.Message}");
            }
        }

        private string GetPlaceStatusInfo(Agent currentAgent, Place place)
        {
            var info = $"=== {place.Name} 的位置信息 ===\n\n";
            info += $"名称: {place.Name}\n";
            info += $"描述: {place.Description}\n";
            info += $"类型: {place.Type}\n";
            return info;
        }



        /// <summary>
        /// 从当前AI通信上下文获取Agent信息
        /// </summary>
        private Agent GetCurrentAgentFromContext()
        {
            // 尝试从AgentManager获取当前活跃的Agent
            var agentManager = GameManager.Instance.CharacterManager;
            if (agentManager != null)
            {
                var agents = agentManager.GetAllAgents();
                if (agents != null && agents.Count > 0)
                {
                    // 返回第一个有角色的Agent
                    foreach (var agent in agents)
                    {
                        if (agent.Character != null)
                        {
                            return agent;
                        }
                    }
                }
            }

            GD.PrintErr("无法获取当前Agent信息");
            return null;
        }

        public string GetFunctionName()
        {
            return Name;
        }

        public string GetFunctionDescription()
        {
            return Description;
        }

        public Array<FunctionParameter> GetParameters()
        {
            return new Array<FunctionParameter>
            {
                new FunctionParameter("requested_info", "string", "请求的信息类型：current_place/all_places/search_place，（默认current_place）"),
                new FunctionParameter("place_id_or_name", "string", "当requested_info为search时，请求的位置ID或名称，（默认当前位置）")
            };
        }
    }
}
