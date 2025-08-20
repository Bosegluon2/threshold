using Godot;
using Godot.Collections;
using System;

namespace Threshold.Core.Agent.Functions
{
    /// <summary>
    /// 关系更新函数 - 让AI自主更新与其他角色的关系和记忆
    /// </summary>
    public partial class UpdateRelationshipFunction : Resource, IFunctionExecutor
    {
        public string Name => "update_relationship";
        public string Description => "更新与目标角色的关系状态，记录互动、信任变化、关系变化等，并自动更新记忆系统";
        
        public FunctionResult Execute(Godot.Collections.Dictionary arguments, System.Collections.Generic.Dictionary<string, object> extraParams = null)
        {
            try
            {
                GD.Print($"=== 执行关系更新函数 ===");
                GD.Print($"参数: {Json.Stringify(arguments)}");
                
                // 获取参数
                var targetId = arguments.ContainsKey("target_id") ? arguments["target_id"].AsString() : "";
                var targetName = arguments.ContainsKey("target_name") ? arguments["target_name"].AsString() : "";
                var interactionType = arguments.ContainsKey("interaction_type") ? arguments["interaction_type"].AsString() : "";
                var description = arguments.ContainsKey("description") ? arguments["description"].AsString() : "";
                var trustChange = arguments.ContainsKey("trust_change") ? arguments["trust_change"].AsInt32() : 0;
                var relationshipChange = arguments.ContainsKey("relationship_change") ? arguments["relationship_change"].AsInt32() : 0;
                var severity = arguments.ContainsKey("severity") ? arguments["severity"].AsInt32() : 1;
                var isImportant = arguments.ContainsKey("is_important") ? arguments["is_important"].AsBool() : false;
                
                // 验证参数
                if (string.IsNullOrEmpty(targetId) && string.IsNullOrEmpty(targetName))
                {
                    return new FunctionResult(Name, "false", false, "缺少目标角色信息");
                }
                
                if (string.IsNullOrEmpty(interactionType))
                {
                    return new FunctionResult(Name, "false", false, "缺少互动类型参数");
                }
                
                if (string.IsNullOrEmpty(description))
                {
                    return new FunctionResult(Name, "false", false, "缺少互动描述");
                }
                
                // 获取当前Agent
                Agent currentAgent = null;
                if (extraParams != null && extraParams.ContainsKey("current_agent"))
                {
                    currentAgent = extraParams["current_agent"] as Agent;
                    GD.Print($"从extraParams获取到Agent: {currentAgent?.AgentName ?? "null"}");
                }
                
                if (currentAgent == null)
                {
                    return new FunctionResult(Name, "false", false, "无法获取当前Agent信息");
                }
                
                // 获取目标角色
                Agent targetCharacter = null;
                if (!string.IsNullOrEmpty(targetId))
                {
                    targetCharacter = GameManager.Instance.CharacterManager.GetCharacterById(targetId);
                }
                else if (!string.IsNullOrEmpty(targetName))
                {
                    targetCharacter = GameManager.Instance.CharacterManager.GetCharacterByName(targetName);
                }
                
                // 如果没有指定目标，默认使用玩家角色
                if (targetCharacter == null)
                {
                    if (extraParams != null && extraParams.ContainsKey("conversation_target"))
                    {
                        targetCharacter = extraParams["conversation_target"] as Agent;
                        GD.Print($"使用默认对话目标: {targetCharacter?.Name ?? "null"}");
                    }
                }
                
                if (targetCharacter == null)
                {
                    return new FunctionResult(Name, "false", false, "目标角色不存在");
                }
                
                GD.Print($"当前Agent: {currentAgent.AgentName}");
                GD.Print($"目标角色: {targetCharacter.Name}");
                GD.Print($"互动类型: {interactionType}");
                GD.Print($"互动描述: {description}");
                GD.Print($"信任变化: {trustChange}");
                GD.Print($"关系变化: {relationshipChange}");
                
                // 根据互动类型执行相应的更新
                var result = ExecuteInteractionUpdate(
                    currentAgent, 
                    targetCharacter, 
                    interactionType, 
                    description, 
                    trustChange, 
                    relationshipChange, 
                    severity, 
                    isImportant
                );
                
                GD.Print($"关系更新结果: {result}");
                return new FunctionResult(Name, "true", true, result);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"关系更新函数执行失败: {ex.Message}");
                return new FunctionResult(Name, "false", false, $"执行失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 执行互动更新
        /// </summary>
        private string ExecuteInteractionUpdate(
            Agent currentAgent, 
            Agent targetCharacter, 
            string interactionType, 
            string description, 
            int trustChange, 
            int relationshipChange, 
            int severity, 
            bool isImportant)
        {
            var targetId = targetCharacter.Character.Id;
            var targetName = targetCharacter.Name;
            
            // 获取或创建关系
            var relation = currentAgent.GetOrCreateRelation(targetId, targetName);
            var oldTrust = relation.TrustLevel;
            var oldRelationship = relation.RelationshipLevel;
            var oldType = relation.RelationshipType;
            
            switch (interactionType.ToLower())
            {
                case "general_interaction":
                    // 一般互动
                    currentAgent.UpdateRelation(targetId, description, trustChange, relationshipChange);
                    currentAgent.AddMemory($"与 {targetName}: {description}", isImportant ? "event" : "memory");
                    break;
                    
                case "share_secret":
                    // 分享秘密
                    currentAgent.RecordSharedSecret(targetId, "秘密", description);
                    currentAgent.AddMemory($"向 {targetName} 分享了秘密: {description}", "secret"); 
                    break;
                    
                case "give_favor":
                    // 给予帮助
                    currentAgent.RecordFavor(targetId, description, true);
                    currentAgent.AddMemory($"帮助了 {targetName}: {description}", "event");
                    break;
                    
                case "receive_favor":
                    // 接受帮助
                    currentAgent.RecordFavor(targetId, description, false);
                    currentAgent.AddMemory($"接受了 {targetName} 的帮助: {description}", "event");
                    break;
                    
                case "conflict":
                    // 冲突
                    currentAgent.RecordConflict(targetId, description, severity);
                    currentAgent.AddMemory($"与 {targetName} 发生冲突: {description}", severity >= 2 ? "event" : "memory");
                    break;
                    
                case "reconciliation":
                    // 和解
                    currentAgent.UpdateRelation(targetId, description, trustChange, relationshipChange);
                    currentAgent.AddMemory($"与 {targetName} 和解: {description}", "event");
                    break;
                    
                case "betrayal":
                    // 背叛
                    currentAgent.UpdateRelation(targetId, description, -20, -15);
                    currentAgent.AddMemory($"被 {targetName} 背叛: {description}", "event");
                    break;
                    
                case "loyalty":
                    // 忠诚表现
                    currentAgent.UpdateRelation(targetId, description, 10, 8);
                    currentAgent.AddMemory($"{targetName} 表现出忠诚: {description}", "event");
                    break;
                    
                case "gift":
                    // 礼物/馈赠
                    currentAgent.UpdateRelation(targetId, description, 5, 4);
                    currentAgent.AddMemory($"收到 {targetName} 的礼物: {description}", "event");
                    break;
                    
                case "training":
                    // 训练/学习
                    currentAgent.UpdateRelation(targetId, description, 3, 2);
                    currentAgent.AddMemory($"与 {targetName} 一起训练: {description}", "event");
                    break;
                    
                case "adventure":
                    // 冒险/任务
                    currentAgent.UpdateRelation(targetId, description, 4, 3);
                    currentAgent.AddMemory($"与 {targetName} 一起冒险: {description}", "event");
                    break;
                    
                case "celebration":
                    // 庆祝/聚会
                    currentAgent.UpdateRelation(targetId, description, 2, 3);
                    currentAgent.AddMemory($"与 {targetName} 一起庆祝: {description}", "event");
                    break;
                    
                case "sorrow":
                    // 悲伤/安慰
                    currentAgent.UpdateRelation(targetId, description, 6, 5);
                    currentAgent.AddMemory($"安慰 {targetName}: {description}", "event");
                    break;
                    
                case "competition":
                    // 竞争/比赛
                    var competitionTrustChange = trustChange != 0 ? trustChange : -2;
                    var competitionRelationshipChange = relationshipChange != 0 ? relationshipChange : -1;
                    currentAgent.UpdateRelation(targetId, description, competitionTrustChange, competitionRelationshipChange);
                    currentAgent.AddMemory($"与 {targetName} 竞争: {description}", "event");
                    break;
                    
                case "cooperation":
                    // 合作
                    var cooperationTrustChange = trustChange != 0 ? trustChange : 4;
                    var cooperationRelationshipChange = relationshipChange != 0 ? relationshipChange : 3;
                    currentAgent.UpdateRelation(targetId, description, cooperationTrustChange, cooperationRelationshipChange);
                    currentAgent.AddMemory($"与 {targetName} 合作: {description}", "event");
                    break;
                    
                case "custom":
                    // 自定义互动
                    currentAgent.UpdateRelation(targetId, description, trustChange, relationshipChange);
                    currentAgent.AddMemory($"与 {targetName}: {description}", "event");
                    break;
                    
                default:
                    // 默认处理
                    currentAgent.UpdateRelation(targetId, description, trustChange, relationshipChange);
                    currentAgent.AddMemory($"与 {targetName}: {description}", "event");
                    break;
            }
            
            // 获取更新后的关系信息
            var newTrust = relation.TrustLevel;
            var newRelationship = relation.RelationshipLevel;
            var newType = relation.RelationshipType;
            
            // 构建结果描述
            var result = $"关系更新完成！\n";
            result += $"目标角色: {targetName}\n";
            result += $"互动类型: {interactionType}\n";
            result += $"互动描述: {description}\n";
            result += $"信任变化: {oldTrust} → {newTrust} ({GetChangeDescription(oldTrust, newTrust)})\n";
            result += $"关系变化: {oldRelationship} → {newRelationship} ({GetChangeDescription(oldRelationship, newRelationship)})\n";
            result += $"关系类型: {oldType} → {newType}\n";
            result += $"互动次数: {relation.InteractionCount}\n";
            
            // if (relation.SharedSecrets.Count > 0)
            //     result += $"已分享秘密: {relation.SharedSecrets.Count} 个\n";
            // if (relation.Favors.Count > 0)
            //     result += $"互相帮助: {relation.Favors.Count} 次\n";
            // if (relation.Conflicts.Count > 0)
            //     result += $"冲突记录: {relation.Conflicts.Count} 次\n";
            
            return result;
        }
        
        /// <summary>
        /// 获取变化描述
        /// </summary>
        private string GetChangeDescription(int oldValue, int newValue)
        {
            var change = newValue - oldValue;
            if (change > 0)
                return $"+{change} (提升)";
            else if (change < 0)
                return $"{change} (下降)";
            else
                return "0 (无变化)";
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
                new FunctionParameter("target_id", "string", "目标角色ID（与target_name二选一）"),
                new FunctionParameter("target_name", "string", "目标角色名称（与target_id二选一）"),
                new FunctionParameter("interaction_type", "string", "互动类型：general_interaction/share_secret/give_favor/receive_favor/conflict/reconciliation/betrayal/loyalty/gift/training/adventure/celebration/sorrow/competition/cooperation/custom", true),
                new FunctionParameter("description", "string", "互动的详细描述", true),
                new FunctionParameter("trust_change", "int", "信任程度变化（可选，某些类型会自动计算）", false),
                new FunctionParameter("relationship_change", "int", "关系等级变化（可选，某些类型会自动计算）", false),
                new FunctionParameter("severity", "int", "冲突严重程度（仅用于conflict类型，默认1）", false),
                new FunctionParameter("is_important", "bool", "是否为重要事件（影响记忆分类，默认false）", false)
            };
        }
    }
}
