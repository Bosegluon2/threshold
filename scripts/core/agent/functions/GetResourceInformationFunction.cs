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
    public partial class GetResourceInformationFunction : Resource, IFunctionExecutor
    {
        public string Name => "get_resource_info";
        public string Description => "获取仓储资源信息";

        public FunctionResult Execute(Godot.Collections.Dictionary arguments, System.Collections.Generic.Dictionary<string, object> extraParams = null)
        {
            try
            {
                GD.Print($"=== 执行获取位置信息函数 ===");
                GD.Print($"参数: {Json.Stringify(arguments)}");

                // 获取参数
                var requestedInfo = arguments.ContainsKey("requested_info") ? arguments["requested_info"].AsString() : "all";


                if (requestedInfo == "all")
                {
                    var resources = GameManager.Instance.ResourceManager.Resources.Values;
                    var info = $"=== 仓储资源信息 ===\n\n";
                    StringBuilder sb = new StringBuilder();
                    foreach (var resource in resources)
                    {
                        info += $"名称: {resource.GameResourceName}:目前{resource.CurrentAmount}/最大容量{resource.MaxAmount}\n";

                    }
                    // 对于水和食物，计算消耗
                    int totalFoodRate = 0;
                    int totalWaterRate = 0;
                    foreach (var agent in GameManager.Instance.CharacterManager.AvailableCharacters)
                    {
                        totalFoodRate += agent.CurrentSatietyConsumptionRate;
                    }
                    foreach (var agent in GameManager.Instance.CharacterManager.AvailableCharacters)
                    {
                        totalWaterRate += agent.CurrentThirstConsumptionRate;
                    }
                    info += $"目前食物消耗: {totalFoodRate} 单位/天\n";
                    info += $"目前饮水消耗: {totalWaterRate} 单位/天\n";
                    return new FunctionResult(Name, info, true, "获取仓储资源信息成功");

                }
                else
                {
                    var resource = GameManager.Instance.ResourceManager.GetResource(requestedInfo);
                    if (resource == null)
                    {
                        return new FunctionResult(Name, "无效的请求信息类型", false, "请确保requested_info为all/food/water/medicine/ammunition/fuel/materials/zpe");
                    }
                    var info = $"=== {resource.GameResourceName} 的资源信息 ===\n\n";
                    info += $"名称: {resource.GameResourceName}\n";
                    info += $"目前: {resource.CurrentAmount}\n";
                    info += $"最大容量: {resource.MaxAmount}\n";
                    info += $"描述: {resource.Description}\n";
                    // 如果是food 或water的话计算消耗
                    if (resource.GameResourceId == "food")
                    {
                        int totalFoodRate = 0;
                        // 更新所有Agent的饥饿值
                        GameManager.Instance.CharacterManager.UpdateAvailableCharacters();
                        foreach (var agent in GameManager.Instance.CharacterManager.AvailableCharacters)
                        {
                            totalFoodRate += agent.CurrentSatietyConsumptionRate;
                        }
                        info += $"目前食物消耗: {totalFoodRate} 单位/天\n";
                    }
                    else if (resource.GameResourceId == "water")
                    {
                        int totalWaterRate = 0;
                        foreach (var agent in GameManager.Instance.CharacterManager.AvailableCharacters)
                        {
                            totalWaterRate += agent.CurrentThirstConsumptionRate;
                        }
                        info += $"目前饮水消耗: {totalWaterRate} 单位/天\n";
                    }
                    return new FunctionResult(Name, info, true, "获取仓储资源信息成功");
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
                new FunctionParameter("requested_info", "string", "请求的信息类型：all/food/water/medicine/ammunition/fuel/materials/zpe，默认all，zpe指的是零点能"),
            
            };
        }
    }
}
