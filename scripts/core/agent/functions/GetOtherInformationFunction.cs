using Godot;
using Godot.Collections;
using System;

namespace Threshold.Core.Agent.Functions
{
    /// <summary>
    /// 获取其他角色信息函数 - 需要权限验证
    /// </summary>
    public partial class GetOtherInformationFunction : Resource, IFunctionExecutor
    {
        public string Name => "get_other_information";
        public string Description => "获取其他角色的信息，根据权限显示不同详细程度";
        
        public FunctionResult Execute(Godot.Collections.Dictionary arguments, System.Collections.Generic.Dictionary<string, object> extraParams = null)
        {
            try
            {
                GD.Print($"=== 执行获取他人信息函数 ===");
                GD.Print($"参数: {Json.Stringify(arguments)}");
                
                // 获取参数
                var targetId = arguments.ContainsKey("target_id") ? arguments["target_id"].AsString() : "";
                var targetName = arguments.ContainsKey("target_name") ? arguments["target_name"].AsString() : "";
                var requestedInfo = arguments.ContainsKey("requested_info") ? arguments["requested_info"].AsString() : "basic";
                
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
                
                // 确定目标Agent
                Agent targetAgent = null;
                if (targetId == "player")
                {
                    targetAgent = GameManager.Instance.CharacterManager.GetPlayerAgent();
                }
                else if (!string.IsNullOrEmpty(targetId))
                {
                    targetAgent = GameManager.Instance.CharacterManager.GetCharacterById(targetId);
                }
                else if (!string.IsNullOrEmpty(targetName))
                {
                    targetAgent = GameManager.Instance.CharacterManager.GetCharacterByName(targetName);
                }
                else
                {
                    return new FunctionResult(Name, "缺少目标角色信息", false, "请提供target_id或target_name");
                }
                
                if (targetAgent == null)
                {
                    return new FunctionResult(Name, "目标角色不存在", false, "无效的目标角色");
                }
                
                // 检查是否是查看自己
                if (currentAgent.AgentId == targetAgent.AgentId)
                {
                    return new FunctionResult(Name, "不能使用此函数查看自己的信息", false, "请使用get_self_information函数查看自己的信息");
                }
                
                // 特殊处理：如果目标角色是玩家，提供更友好的信息
                if (targetAgent.Character.Id == "player")
                {
                    GD.Print($"检测到玩家角色查询，提供友好信息");
                    var playerInfo = GetPlayerFriendlyInfo(targetAgent, requestedInfo);
                    return new FunctionResult(Name, playerInfo, true, "");
                }
                
                GD.Print($"当前Agent: {currentAgent.AgentName} -> 目标Agent: {targetAgent.AgentName}");
                
                // 根据权限获取信息
                var statusInfo = GetOtherStatusInfo(currentAgent, targetAgent, requestedInfo);
                
                GD.Print($"他人信息获取完成: {targetAgent.AgentName} -> {currentAgent.AgentName}");
                return new FunctionResult(Name, statusInfo, true, "");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"获取他人信息函数执行失败: {ex.Message}");
                return new FunctionResult(Name, "", false, $"执行失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取他人的状态信息
        /// </summary>
        private string GetOtherStatusInfo(Agent requester, Agent target, string requestedInfo)
        {
            var targetCharacter = target.Character;
            var requesterCharacter = requester.Character;
            var info = $"=== {targetCharacter.Name} 的角色信息 ===\n\n";
            
            // 判断请求者的职业权限
            bool hasDoctorPrivilege = HasDoctorPrivilege(requester);
            bool hasMagePrivilege = HasMagePrivilege(requester);
            bool hasScoutPrivilege = HasScoutPrivilege(requester);
            int trustLevel = GetTrustLevel(requester, target);
            int relationshipLevel = GetRelationshipLevel(requester, target);
            // 基本信息（所有人都能看到）
            info += $"【基本信息】\n";
            if (target.CanShareInformation(requesterCharacter.Id, 30))
            {
                info += $"名称: {targetCharacter.Name}\n";
                info += $"职业: {targetCharacter.Profession}\n";
                info += $"派别: {targetCharacter.Faction}\n";
                info += $"年龄: {targetCharacter.Age}\n";
                info += $"性别: {targetCharacter.Gender}\n";
            }
            else
            {
                info += $"你们很有可能第一次见面/关系紧张，所以他没能分享自己的信息。\n";
            }


            
            // 外观信息（需要特殊权限）
            if (HasCloseContactPrivilege(requester))
            {
                if (!string.IsNullOrEmpty(targetCharacter.Appearance.ToString()))
                    info += $"外貌: {targetCharacter.Appearance}\n";
                if (!string.IsNullOrEmpty(targetCharacter.SpeechStyle.ToString()))
                    info += $"说话风格: {targetCharacter.SpeechStyle}\n";
            }
            
            info += "\n";
            
            // 根据请求的信息类型返回相应数据
            switch (requestedInfo.ToLower())
            {
                case "health":
                    info += GetOtherHealthInfo(requester, target, hasDoctorPrivilege);
                    break;
                case "combat":
                    info += GetOtherCombatInfo(requester, target, hasScoutPrivilege);
                    break;
                case "skills":
                    info += GetOtherSkillsInfo(requester, target, trustLevel, relationshipLevel);
                    break;
                case "relationships":
                    info += GetOtherRelationshipsInfo(requester, target);
                    break;
                case "full":
                    info += GetOtherFullInfo(requester, target, hasDoctorPrivilege, hasMagePrivilege, hasScoutPrivilege, trustLevel, relationshipLevel);
                    break;
                default:
                    info += GetOtherBasicInfo(requester, target, hasDoctorPrivilege, trustLevel, relationshipLevel);
                    break;
            }
            
            info += "=== 信息结束 ===";
            return info;
        }

        private int GetRelationshipLevel(Agent requester, Agent target)
        {
            var requesterCharacter = requester.Character;
            var targetCharacter = target.Character;
            var relation = target.GetRelation(requesterCharacter.Id);
            if (relation != null)
                return relation.RelationshipLevel;
            else
                return 0;
        }

        private int GetTrustLevel(Agent requester, Agent target)
        {
            var requesterCharacter = requester.Character;
            var targetCharacter = target.Character;
            var relation = target.GetRelation(requesterCharacter.Id);
            if (relation != null)
                return relation.TrustLevel;
            else
                return 0;
        }

        /// <summary>
        /// 获取他人的健康状态信息
        /// </summary>
        private string GetOtherHealthInfo(Agent requester, Agent target, bool hasDoctorPrivilege)
        {
            var targetCharacter = target.Character;
            var info = "【健康状态】\n";
            
            if (hasDoctorPrivilege)
            {
                // 医生可以看到详细数值
                info += $"健康: {target.CurrentHealth}/{targetCharacter.MaxHealth} - {Evaluation.GetHealthDescription(target.CurrentHealth, targetCharacter.MaxHealth)}\n";
                info += $"能量: {target.CurrentEnergy}/{targetCharacter.MaxEnergy} - {Evaluation.GetEnergyDescription(target.CurrentEnergy, targetCharacter.MaxEnergy)}\n";
                info += $"详细诊断: 身体状态良好，建议保持当前作息\n";
            }
            else
            {
                // 普通人只能看到大致状态
                var healthPercentage = (float)target.CurrentHealth / targetCharacter.MaxHealth;
                var energyPercentage = (float)target.CurrentEnergy / targetCharacter.MaxEnergy;
                
                if (healthPercentage > 0.8f)
                    info += $"健康: 看起来状态很好\n";
                else if (healthPercentage > 0.5f)
                    info += $"健康: 看起来有些虚弱/受伤\n";
                else
                    info += $"健康: 看起来受伤有点重\n";
                
                if (energyPercentage > 0.8f)
                    info += $"能量: 精力充沛\n";
                else if (energyPercentage > 0.5f)
                    info += $"能量: 精力一般\n";
                else
                    info += $"能量: 看起来疲惫\n";
            }
            
            return info + "\n";
        }
        
        /// <summary>
        /// 获取他人的战斗能力信息
        /// </summary>
        private string GetOtherCombatInfo(Agent requester, Agent target, bool hasScoutPrivilege)
        {
            var info = "【战斗能力】\n";
            
            if (hasScoutPrivilege)
            {
                // 侦察兵可以看到详细数值
                var currentWarpedAttributes = target.CurrentWarpedInfo;
                info += $"战斗能力 (W): {currentWarpedAttributes.Warfare}/10 - {Evaluation.GetWarfareDescription(currentWarpedAttributes.Warfare)}\n";
                info += $"适应能力 (A): {currentWarpedAttributes.Adaptability}/10 - {Evaluation.GetAdaptabilityDescription(currentWarpedAttributes.Adaptability)}\n";
                info += $"推理能力 (R): {currentWarpedAttributes.Reasoning}/10 - {Evaluation.GetReasoningDescription(currentWarpedAttributes.Reasoning)}\n";
                info += $"感知能力 (P): {currentWarpedAttributes.Perception}/10 - {Evaluation.GetPerceptionDescription(currentWarpedAttributes.Perception)}\n";
                info += $"耐力 (E): {currentWarpedAttributes.Endurance}/10 - {Evaluation.GetEnduranceDescription(currentWarpedAttributes.Endurance)}\n";
                info += $"灵巧度 (D): {currentWarpedAttributes.Dexterity}/10 - {Evaluation.GetDexterityDescription(currentWarpedAttributes.Dexterity)}\n";
                info += $"战术分析: 建议在{(currentWarpedAttributes.Warfare > 5 ? "正面战斗" : "游击战术")}中发挥优势\n";
            }
            else
            {
                // 普通人只能看到大致印象
                var currentWarpedAttributes = target.CurrentWarpedInfo;
                if (currentWarpedAttributes.Warfare > 7)
                    info += $"战斗能力: 看起来很强壮，可能是经验丰富的战士\n";
                else if (currentWarpedAttributes.Warfare > 4)
                    info += $"战斗能力: 看起来有一定的战斗经验\n";
                else
                    info += $"战斗能力: 看起来不擅长战斗\n";
            }
            
            return info + "\n";
        }
        
        /// <summary>
        /// 获取他人的技能信息
        /// </summary>
        private string GetOtherSkillsInfo(Agent requester, Agent target, int trustLevel, int relationshipLevel)
        {
            var info = "【技能信息】\n";

            // 他人只能看到明显的技能
            // if (target.Profession == "魔法师" && target.Skills.Count > 0)
            // {
            //     info += $"明显技能: 魔法相关技能\n";
            // }
            // else if (target.Profession == "战士")
            // {
            //     info += $"明显技能: 战斗技能\n";
            // }
            // else
            // {
            //     info += $"技能: 无法准确判断\n";
            // }
            if (target.CanShareInformation(requester.Character.Id, 60)) // 60是技能的保密等级
            {
                info += $"技能: {target.CurrentSkills}\n";
            }
            else
            {
                info += $"技能: 目标角色并不信任你，所以不会告诉你他的技能\n";
            }
            return info + "\n";
        }
        
        /// <summary>
        /// 获取他人的关系信息
        /// </summary>
        private string GetOtherRelationshipsInfo(Agent requester, Agent target)
        {
            var info = "【关系信息】\n";
            info += $"信任程度: {target.GetRelation(requester.Character.Id).TrustLevel} - {Evaluation.GetTrustLevelDescription(target.GetRelation(requester.Character.Id).TrustLevel)}\n";
            info += $"关系等级: {target.GetRelation(requester.Character.Id).RelationshipLevel} - {Evaluation.GetRelationshipLevelDescription(target.GetRelation(requester.Character.Id).RelationshipLevel)}\n";
            return info + "\n";
        }
        
        /// <summary>
        /// 获取他人的基本信息
        /// </summary>
        private string GetOtherBasicInfo(Agent requester, Agent target, bool hasDoctorPrivilege, int trustLevel, int relationshipLevel)
        {
            var info = "【基本状态】\n";
            info += GetOtherHealthInfo(requester, target, hasDoctorPrivilege);
            info += GetOtherSkillsInfo(requester, target, trustLevel, relationshipLevel);
            return info;
        }
        
        /// <summary>
        /// 获取他人的完整信息
        /// </summary>
        private string GetOtherFullInfo(Agent requester, Agent target, bool hasDoctorPrivilege, bool hasMagePrivilege, bool hasScoutPrivilege, int trustLevel, int relationshipLevel)
        {
            var info = "";
            info += GetOtherHealthInfo(requester, target, hasDoctorPrivilege);
            info += GetOtherCombatInfo(requester, target, hasScoutPrivilege);
            info += GetOtherSkillsInfo(requester, target, trustLevel, relationshipLevel);
            info += GetOtherRelationshipsInfo(requester, target);
            
            // 特殊信息（只有特殊职业能看到）
            if (hasMagePrivilege)
            {
                info += "【特殊信息】\n";
                if (!string.IsNullOrEmpty(target.Character.Goals.ToString()))
                    info += $"目标: {target.Character.Goals}\n";
                if (!string.IsNullOrEmpty(target.Character.Fears.ToString()))
                    info += $"恐惧: {target.Character.Fears}\n";
                info += "\n";
            }
            
            return info;
        }
        
        // 权限检查方法
        private bool HasDoctorPrivilege(Agent agent)
        {
            var character = agent.Character;
            return character.Profession == "医生" || character.Profession == "治疗师" || character.Profession == "牧师";
        }
        
        private bool HasMagePrivilege(Agent agent)
        {
            var character = agent.Character;
            return character.Profession == "魔法师" || character.Profession == "法师" || character.Profession == "巫师";
        }
        
        private bool HasScoutPrivilege(Agent agent)
        {
            var character = agent.Character;
            return character.Profession == "侦察兵" || character.Profession == "游侠" || character.Profession == "斥候";
        }
        
        private bool HasCloseContactPrivilege(Agent agent)
        {
            // 需要近距离接触才能看到外观信息
            return false; // 默认关闭，可以通过其他机制开启
        }
        
        /// <summary>
        /// 获取玩家角色的友好信息
        /// </summary>
        private string GetPlayerFriendlyInfo(Agent player, string requestedInfo)
        {
            var playerCharacter = player.Character;
            var info = $"=== {playerCharacter.Name} 的角色信息 ===\n\n";
            
            // 基本信息
            info += $"【基本信息】\n";
            info += $"名称: {playerCharacter.Name}\n";
            info += $"职业: {playerCharacter.Profession}\n";
            info += $"派别: {playerCharacter.Faction}\n";
            info += $"年龄: {playerCharacter.Age}\n";
            info += $"性别: {playerCharacter.Gender}\n";
            
            // 外观和性格信息
            if (playerCharacter.Appearance != null)
                info += $"外貌: {playerCharacter.Appearance.Content}\n";
            if (playerCharacter.SpeechStyle != null)
                info += $"说话风格: {playerCharacter.SpeechStyle.Content}\n";
            
            info += "\n";
            
            // 根据请求的信息类型返回相应数据
            switch (requestedInfo.ToLower())
            {
                case "health":
                    info += GetPlayerHealthInfo(player);
                    break;
                case "combat":
                    info += GetPlayerCombatInfo(player);
                    break;
                case "skills":
                    info += GetPlayerSkillsInfo(player);
                    break;
                case "relationships":
                    info += GetPlayerRelationshipsInfo(player);
                    break;
                case "full":
                    info += GetPlayerFullInfo(player);
                    break;
                default:
                    info += GetPlayerBasicInfo(player);
                    break;
            }
            
            info += "=== 信息结束 ===";
            return info;
        }
        
        /// <summary>
        /// 获取玩家基本状态信息
        /// </summary>
        private string GetPlayerBasicInfo(Agent player)
        {
            var info = "【基本状态】\n";
            info += GetPlayerHealthInfo(player);
            info += GetPlayerSkillsInfo(player);
            return info;
        }
        
        /// <summary>
        /// 获取玩家健康状态信息
        /// </summary>
        private string GetPlayerHealthInfo(Agent player)
        {
            var playerCharacter = player.Character;
            var info = "【健康状态】\n";
            info += $"生命值: {player.CurrentHealth}/{playerCharacter.MaxHealth}\n";
            info += $"精力值: {player.CurrentEnergy}/{playerCharacter.MaxEnergy}\n";
            info += $"状态: {string.Join(", ", player.CurrentStatus)}\n";
            info += "\n";
            return info;
        }
        
        /// <summary>
        /// 获取玩家战斗信息
        /// </summary>
        private string GetPlayerCombatInfo(Agent player)
        {
            var info = "【战斗能力】\n";
            info += $"战斗技能: {player.CurrentWarpedInfo.Warfare}\n";
            info += $"适应能力: {player.CurrentWarpedInfo.Adaptability}\n";
            info += $"推理能力: {player.CurrentWarpedInfo.Reasoning}\n";
            info += $"感知能力: {player.CurrentWarpedInfo.Perception}\n";
            info += $"耐力: {player.CurrentWarpedInfo.Endurance}\n";
            info += $"敏捷性: {player.CurrentWarpedInfo.Dexterity}\n";
            info += "\n";
            return info;
        }
        
        /// <summary>
        /// 获取玩家技能信息
        /// </summary>
        private string GetPlayerSkillsInfo(Agent player)
        {
            var info = "【技能信息】\n";
            if (player.CurrentSkills != null && player.CurrentSkills.Count > 0)
            {
                info += $"技能: {string.Join(", ", player.CurrentSkills)}\n";
            }
            else
            {
                info += "技能: 暂无记录\n";
            }
            info += "\n";
            return info;
        }
        
        /// <summary>
        /// 获取玩家关系信息
        /// </summary>
        private string GetPlayerRelationshipsInfo(Agent player)
        {
            var info = "【关系信息】\n";
            if (player.Relationships != null && player.Relationships.Count > 0)
            {
                info += "关系网络: 已建立\n";
                foreach (var relation in player.Relationships.Values)
                {
                    info += $"- 与 {relation.TargetCharacterName}: {relation.RelationshipDescription}\n";
                }
            }
            else
            {
                info += "关系网络: 尚未建立\n";
            }
            info += "\n";
            return info;
        }
        
        /// <summary>
        /// 获取玩家完整信息
        /// </summary>
        private string GetPlayerFullInfo(Agent player)
        {
            var info = "";
            info += GetPlayerHealthInfo(player);
            info += GetPlayerCombatInfo(player);
            info += GetPlayerSkillsInfo(player);
            info += GetPlayerRelationshipsInfo(player);
            
            // 目标和背景信息
            info += "【个人背景】\n";
            if (player.Character.Goals != null)
                info += $"目标: {player.Character.Goals.Content}\n";
            if (player.Character.Fears != null)
                info += $"恐惧: {player.Character.Fears.Content}\n";
            if (player.PersonalSecrets != null)
                info += $"秘密: {string.Join(", ", player.PersonalSecrets)}\n";
            info += "\n";
            
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
                new FunctionParameter("target_id", "string", "目标角色ID（必需）"),
                new FunctionParameter("target_name", "string", "目标角色名称（与target_id二选一）"),
                new FunctionParameter("requested_info", "string", "请求的信息类型：basic/health/combat/skills/relationships/full（默认basic）")
            };
        }
    }
}
