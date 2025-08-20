using System;
using System.Linq;
using Godot;
using Godot.Collections;
using Threshold.Core.Agent;
using Threshold.Core.Data;
using Threshold.Core.Enums;

namespace Threshold.Core.Agent.Functions
{
    /// <summary>
    /// 游戏世界信息查询函数
    /// </summary>
    public partial class GetWorldInfoFunction : Resource, IFunctionExecutor
    {
        public string GetFunctionName() => "get_world_info";

        public string GetFunctionDescription() => "获取游戏世界的基本信息，包括当前时间、天气、地点等";

        public Array<FunctionParameter> GetParameters()
        {
            return new Array<FunctionParameter>
            {
                new FunctionParameter("info_type", "string", "要查询的信息类型：time(时间)、weather(天气)、location(地点)、all(全部)", false)
            };
        }

        public FunctionResult Execute(Godot.Collections.Dictionary arguments, System.Collections.Generic.Dictionary<string, object> extraParams = null)
        {
            try
            {
                GD.Print($"GetWorldInfoFunction.Execute 开始执行，参数数量: {arguments?.Count ?? 0}");
                WeatherMachine weatherMachine = GameManager.Instance.WorldManager.WeatherMachine;
                string timeOfDay = "未知";
                string day = "未知";
                string weather = "未知";
                string location = "未知";
                string atmosphere = "未知";
                var infoType = "all";
                Agent currentAgent = null;
                if (extraParams != null && extraParams.ContainsKey("current_agent"))
                {
                    currentAgent = extraParams["current_agent"] as Agent;
                }
                Place currentPlace = currentAgent.CurrentPlace;
                GD.Print($"当前位置: {currentPlace}");
                GD.Print($"当前位置的CanGetTime: {currentPlace.CanGetTime}");
                GD.Print($"当前位置的CanGetWeather: {currentPlace.CanGetWeather}");
                GD.Print($"当前位置的CanGetLocation: {currentPlace.CanGetLocation}");
                GD.Print($"当前位置的CanGetAtmosphere: {currentPlace.CanGetAtmosphere}");
                if (currentPlace.CanGetTime)
                {
                    timeOfDay = TimeUtils.GetTimeOfDay(GameManager.Instance.CurrentTurn).ToString();
                    day = TimeUtils.GetDay(GameManager.Instance.CurrentTurn).ToString();
                }
                if (currentPlace.CanGetWeather)
                {
                    weather = weatherMachine.GetWeather().ToString();
                }
                if (currentPlace.CanGetLocation)
                {
                    location = currentPlace.Name;
                }
                if (currentPlace.CanGetAtmosphere)
                {
                    atmosphere = currentPlace.Description;
                }
                if (arguments != null && arguments.ContainsKey("info_type"))
                {
                    try
                    {
                        infoType = arguments["info_type"].AsString();
                        GD.Print($"解析到info_type: {infoType}");
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"解析info_type时发生错误: {ex.Message}");
                        infoType = "all";
                    }
                }

                var worldInfo = new Dictionary();
                GD.Print($"开始构建世界信息，类型: {infoType}");

                switch (infoType.ToLower())
                {
                    case "time":
                        worldInfo["time"] = $"现在是游戏时间{timeOfDay}，第{day}天";
                        break;
                    case "weather":
                        worldInfo["weather"] = $"天气：{weather}";
                        break;
                    case "location":
                        worldInfo["location"] = $"当前位置：{location}";
                        break;
                    case "atmosphere":
                        worldInfo["atmosphere"] = $"环境：{atmosphere}";
                        break;
                    case "all":
                    default:
                        worldInfo["time"] = $"现在是游戏时间{timeOfDay}，第{day}天";
                        worldInfo["weather"] = $"天气：{weather}";
                        worldInfo["location"] = $"当前位置：{location}";
                        worldInfo["atmosphere"] = $"环境：{atmosphere}";
                        break;
                }

                GD.Print($"世界信息构建完成，包含 {worldInfo.Count} 个字段");
                
                var jsonResult = Json.Stringify(worldInfo);
                GD.Print($"JSON序列化完成，长度: {jsonResult?.Length ?? 0}");
                
                var result = new FunctionResult(GetFunctionName(), jsonResult);
                GD.Print($"FunctionResult创建成功: Name={result.Name}, Success={result.Success}, Content长度={result.Content?.Length ?? 0}");
                
                return result;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"GetWorldInfoFunction.Execute 发生异常: {ex.Message}");
                GD.PrintErr($"异常堆栈: {ex.StackTrace}");
                return new FunctionResult(GetFunctionName(), "", false, $"执行get_world_info时发生错误: {ex.Message}");
            }
        }
    }



    /// <summary>
    /// 物品查询函数
    /// </summary>
    public partial class GetInventoryFunction : Resource, IFunctionExecutor
    {
        public string GetFunctionName() => "get_inventory";

        public string GetFunctionDescription() => "查询角色的物品栏，包括装备、消耗品、材料等";

        public Array<FunctionParameter> GetParameters()
        {
            return new Array<FunctionParameter>
            {
                new FunctionParameter("category", "string", "物品类别：equipment(装备)、consumables(消耗品)、materials(材料)、quest(任务)、misc(其他)、all(全部)", false)
            };
        }

        public FunctionResult Execute(Godot.Collections.Dictionary arguments, System.Collections.Generic.Dictionary<string, object> extraParams = null)
        {
            var category = arguments.ContainsKey("category") ? arguments["category"].AsString() : "all";

            var inventory = new Dictionary();

            // 尝试从Agent获取动态库存数据
            Agent currentAgent = null;
            if (extraParams != null && extraParams.ContainsKey("current_agent"))
            {
                currentAgent = extraParams["current_agent"] as Agent;
            }

            if (currentAgent != null && currentAgent.Inventory != null && currentAgent.Inventory.Count > 0)
            {
                // 使用Agent的动态库存数据
                Array<Item> agentInventory = currentAgent.Inventory;
                switch (category.ToLower())
                {
                    case "equipment":
                        inventory["equipment"] = agentInventory.Where(item => item.Type == ItemType.Weapon || item.Type == ItemType.Armor).Select(item => item.Name).ToArray();
                        break;
                    case "consumables":
                        inventory["consumables"] = agentInventory.Where(item => item.Type == ItemType.Consumable).Select(item => item.Name).ToArray();
                        break;
                    case "materials":
                        inventory["materials"] = agentInventory.Where(item => item.Type == ItemType.Material).Select(item => item.Name).ToArray();
                        break;
                    case "quest":
                        inventory["quest"] = agentInventory.Where(item => item.Type == ItemType.Quest).Select(item => item.Name).ToArray();
                        break;
                    case "misc":
                        inventory["misc"] = agentInventory.Where(item => item.Type == ItemType.Misc).Select(item => item.Name).ToArray();
                        break;
                    case "all":
                    default:
                        inventory["equipment"] = agentInventory.Where(item => item.Type == ItemType.Weapon || item.Type == ItemType.Armor).Select(item => item.Name).ToArray();
                        inventory["consumables"] = agentInventory.Where(item => item.Type == ItemType.Consumable).Select(item => item.Name).ToArray();
                        inventory["materials"] = agentInventory.Where(item => item.Type == ItemType.Material).Select(item => item.Name).ToArray();
                        inventory["quest"] = agentInventory.Where(item => item.Type == ItemType.Quest).Select(item => item.Name).ToArray();
                        inventory["misc"] = agentInventory.Where(item => item.Type == ItemType.Misc).Select(item => item.Name).ToArray();
                        break;
                }
            }

            // 如果没有则返回[]
            if (inventory.Count == 0)
            {
                inventory["equipment"] = new Array<string> { "没有装备" };
                inventory["consumables"] = new Array<string> { "没有消耗品" };
                inventory["materials"] = new Array<string> { "没有材料" };
                inventory["quest"] = new Array<string> { "没有任务" };
                inventory["misc"] = new Array<string> { "没有其他物品" };
            }

            return new FunctionResult(GetFunctionName(), Json.Stringify(inventory));
        }
    }

    /// <summary>
    /// 任务查询函数
    /// </summary>
    public partial class GetQuestsFunction : Resource, IFunctionExecutor
    {
        public string GetFunctionName() => "get_quests";

        public string GetFunctionDescription() => "查询当前可用的任务和已完成的任务";

        public Array<FunctionParameter> GetParameters()
        {
            return new Array<FunctionParameter>
            {
                new FunctionParameter("quest_type", "string", "任务类型：active(进行中)、available(可接取)、completed(已完成)、all(全部)", false)
            };
        }

        public FunctionResult Execute(Godot.Collections.Dictionary arguments, System.Collections.Generic.Dictionary<string, object> extraParams = null)
        {
            var questType = arguments.ContainsKey("quest_type") ? arguments["quest_type"].AsString() : "all";

            var quests = new Dictionary();

            switch (questType.ToLower())
            {
                case "active":
                    quests["active"] = new Array<string> { "寻找失落的魔法书", "帮助村民解决魔法问题" };
                    break;
                case "available":
                    quests["available"] = new Array<string> { "探索古代遗迹", "收集魔法材料", "护送商队" };
                    break;
                case "completed":
                    quests["completed"] = new Array<string> { "击败森林巨兽", "修复魔法阵", "寻找失踪的学徒" };
                    break;
                case "all":
                default:
                    quests["active"] = new Array<string> { "寻找失落的魔法书", "帮助村民解决魔法问题" };
                    quests["available"] = new Array<string> { "探索古代遗迹", "收集魔法材料", "护送商队" };
                    quests["completed"] = new Array<string> { "击败森林巨兽", "修复魔法阵", "寻找失踪的学徒" };
                    break;
            }

            return new FunctionResult(GetFunctionName(), Json.Stringify(quests));
        }
    }

    /// <summary>
    /// 角色关系查询函数
    /// </summary>
    public partial class GetRelationshipsFunction : Resource, IFunctionExecutor
    {
        public string GetFunctionName() => "get_relationships";

        public string GetFunctionDescription() => "查询角色与其他NPC的关系状态";

        public Array<FunctionParameter> GetParameters()
        {
            return new Array<FunctionParameter>
            {
                new FunctionParameter("npc_name", "string", "要查询的NPC名称，留空则查询所有关系", false)
            };
        }

        public FunctionResult Execute(Godot.Collections.Dictionary arguments, System.Collections.Generic.Dictionary<string, object> extraParams = null)
        {
            var npcName = arguments.ContainsKey("npc_name") ? arguments["npc_name"].AsString() : "";

            var relationships = new Dictionary();

            // 尝试从Agent获取动态关系数据
            Agent currentAgent = null;
            if (extraParams != null && extraParams.ContainsKey("current_agent"))
            {
                currentAgent = extraParams["current_agent"] as Agent;
            }

            if (currentAgent != null && currentAgent.Character != null)
            {
                // 使用Agent的动态关系数据
                if (string.IsNullOrEmpty(npcName))
                {
                    // 获取所有关系
                    var allRelations = currentAgent.Relationships;
                    var relationshipsDict = new Dictionary();
                    
                    foreach (var kvp in allRelations)
                    {
                        var relation = kvp.Value;
                        var trustDesc = GetTrustDescription(relation.TrustLevel);
                        var relationshipDesc = GetRelationshipDescription(relation.RelationshipLevel);
                        relationshipsDict[relation.TargetCharacterName] = $"{relationshipDesc}，好感度：{relation.TrustLevel}/100";
                    }
                    
                    relationships["relationships"] = relationshipsDict;
                    relationships["total_relations"] = allRelations.Count;
                }
                else
                {
                    // 获取特定NPC的关系
                    Relation relation = null;
                    foreach (var kvp in currentAgent.Relationships)
                    {
                        if (kvp.Value.TargetCharacterName == npcName)
                        {
                            relation = kvp.Value;
                            break;
                        }
                    }
                    
                    if (relation != null)
                    {
                        var trustDesc = GetTrustDescription(relation.TrustLevel);
                        var relationshipDesc = GetRelationshipDescription(relation.RelationshipLevel);
                        
                        relationships["npc"] = npcName;
                        relationships["relationship"] = $"{relationshipDesc}，好感度：{relation.TrustLevel}/100";
                        relationships["trust_level"] = relation.TrustLevel;
                        relationships["relationship_level"] = relation.RelationshipLevel;
                        relationships["interaction_count"] = relation.InteractionCount;
                        relationships["description"] = $"与{npcName}的关系：{relationshipDesc}，信任度：{trustDesc}";
                    }
                    else
                    {
                        relationships["npc"] = npcName;
                        relationships["relationship"] = "未知，好感度：0/100";
                        relationships["description"] = $"与{npcName}尚未建立关系";
                    }
                }
            }
            else
            {
                // 未找到关系
                relationships["npc"] = npcName;
                relationships["relationship"] = "未知，好感度：0/100";
                relationships["description"] = $"与{npcName}尚未建立关系";
            }

            return new FunctionResult(GetFunctionName(), Json.Stringify(relationships));
        }
        
        /// <summary>
        /// 获取信任度描述
        /// </summary>
        private string GetTrustDescription(int trustLevel)
        {
            if (trustLevel >= 90) return "极度信任";
            if (trustLevel >= 80) return "高度信任";
            if (trustLevel >= 70) return "信任";
            if (trustLevel >= 60) return "较信任";
            if (trustLevel >= 50) return "中立";
            if (trustLevel >= 40) return "较不信任";
            if (trustLevel >= 30) return "不信任";
            if (trustLevel >= 20) return "很不信任";
            return "极度不信任";
        }
        
        /// <summary>
        /// 获取关系等级描述
        /// </summary>
        private string GetRelationshipDescription(int relationshipLevel)
        {
            if (relationshipLevel >= 90) return "挚友/爱人";
            if (relationshipLevel >= 80) return "好友/伴侣";
            if (relationshipLevel >= 70) return "朋友";
            if (relationshipLevel >= 60) return "熟人";
            if (relationshipLevel >= 50) return "点头之交";
            if (relationshipLevel >= 40) return "陌生人";
            if (relationshipLevel >= 30) return "疏远";
            if (relationshipLevel >= 20) return "敌对";
            return "死敌";
        }
    }
}
