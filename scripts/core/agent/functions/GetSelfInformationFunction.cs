using Godot;
using Godot.Collections;
using System;

namespace Threshold.Core.Agent.Functions
{
    /// <summary>
    /// 获取自己的角色信息函数 - 不需要权限验证
    /// </summary>
    public partial class GetSelfInformationFunction : Resource, IFunctionExecutor
    {
        public string Name => "get_self_information";
        public string Description => "获取自己的角色信息，包括状态、技能、关系等";
        
        public FunctionResult Execute(Godot.Collections.Dictionary arguments, System.Collections.Generic.Dictionary<string, object> extraParams = null)
        {
            try
            {

                GD.Print($"=== 执行获取自己信息函数 ===");
                GD.Print($"参数: {Json.Stringify(arguments)}");
                
                // 获取请求的信息类型
                var requestedInfo = arguments.ContainsKey("requested_info") ? arguments["requested_info"].AsString() : "basic";
                
                // 优先从extraParams获取当前Agent
                Agent currentAgent = null;
                Agent targetAgent = null;
                if (extraParams != null)
                {
                    if (extraParams.ContainsKey("current_agent"))
                    {
                        currentAgent = extraParams["current_agent"] as Agent;
                        GD.Print($"从extraParams获取到Agent: {currentAgent?.AgentName ?? "null"}");
                    }
                    if (extraParams.ContainsKey("conversation_target"))
                    {
                        targetAgent = extraParams["conversation_target"] as Agent;
                        GD.Print($"从extraParams获取到目标Agent: {targetAgent?.AgentName ?? "null"}");
                    }
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
                
                GD.Print($"当前Agent: {currentAgent.AgentName} (ID: {currentAgent.AgentId})");
                
                // 获取自己的信息（不需要权限验证）
                var statusInfo = GetSelfStatusInfo(currentAgent, requestedInfo, targetAgent);
                
                GD.Print($"自己信息获取完成: {currentAgent.AgentName}");
                return new FunctionResult(Name, statusInfo, true, "");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"获取自己信息函数执行失败: {ex.Message}");
                return new FunctionResult(Name, "", false, $"执行失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取自己的状态信息
        /// </summary>
        private string GetSelfStatusInfo(Agent agent, string requestedInfo, Agent targetAgent)
        {
            var character = agent.Character; // 获取静态数据
            var info = $"=== {character.Name} 的角色信息 ===\n\n";
            
            // 从Agent获取动态数据
            var currentHealth = agent.CurrentHealth;
            var currentEnergy = agent.CurrentEnergy;
            var currentStatus = agent.CurrentStatus;
            var currentSkills = agent.CurrentSkills;
            var currentWarpedAttributes = agent.CurrentWarpedInfo;
            
            int trustLevel = 0;
            int relationshipLevel = 0;
            if (targetAgent != null && targetAgent.Character != null)
            {
                var relation = agent.GetRelation(targetAgent.Character.Id);
                if (relation != null)
                {
                    trustLevel = relation.TrustLevel;
                    relationshipLevel = relation.RelationshipLevel;
                }
            }
            // 基本信息
            info += $"【基本信息】\n";
            if (targetAgent != null && agent.CanShareInformation(targetAgent.Character.Id, 30))
            {
                info += $"名称: {character.Name}\n";
                info += $"职业: {character.Profession}\n";
                info += $"派别: {character.Faction}\n";
                info += $"年龄: {character.Age}\n";
                info += $"性别: {character.Gender}\n";
            }
            else
            {
                info += $"你们很有可能第一次见面/关系紧张，所以尽量不要分享自己的信息。如果你的性格不保守，你可以分享一些信息。\n";
            }

            
            if (!string.IsNullOrEmpty(character.Appearance.ToString()))
                info += $"外貌: {character.Appearance}\n";
            if (!string.IsNullOrEmpty(character.SpeechStyle.ToString()))
                info += $"说话风格: {character.SpeechStyle}\n";

            if (!string.IsNullOrEmpty(character.BackgroundStory))
            {
                if (targetAgent != null && agent.CanShareInformation(targetAgent.Character.Id, 60))
                {
                    info += $"背景故事: {character.BackgroundStory}\n";
                }
                else
                {
                    info += $"背景故事: [已隐藏，防止你泄露给其他角色]\n";
                }
            }
            
            info += "\n";
            
            // 根据请求的信息类型返回相应数据
            switch (requestedInfo.ToLower())
            {
                case "health":
                    info += GetSelfHealthInfo(agent);
                    break;
                case "combat":
                    info += GetSelfCombatInfo(agent);
                    break;
                case "skills":
                    info += GetSelfSkillsInfo(agent);
                    break;
                case "relationships":
                    info += GetSelfRelationshipsInfo(agent);
                    break;
                case "full":
                    info += GetSelfFullInfo(agent);
                    break;
                case "response_style":
                    info += GetSelfResponseStyleInfo(agent);
                    break;
                default:
                    info += GetSelfBasicInfo(agent);
                    break;
            }
            
            info += "=== 信息结束 ===";
            return info;
        }

        private string GetSelfResponseStyleInfo(Agent agent)
        {
            var character = agent.Character;
            var info = "【响应风格】\n";
            info += $"语言风格: {character.SpeechStyle}\n";
            info += $"价值观: {character.GetValuesDescription()}\n";
            return info + "\n";
        }


        /// <summary>
        /// 获取自己的健康状态信息
        /// </summary>
        private string GetSelfHealthInfo(Agent agent)
        {
            var character = agent.Character;
            var info = "【健康状态】\n";
            info += $"健康: {agent.CurrentHealth}/{character.MaxHealth} - {Evaluation.GetHealthDescription(agent.CurrentHealth, character.MaxHealth)}\n";
            info += $"能量: {agent.CurrentEnergy}/{character.MaxEnergy} - {Evaluation.GetEnergyDescription(agent.CurrentEnergy, character.MaxEnergy)}\n";
            info += "详细诊断: 身体状态良好，建议保持当前作息\n";
            return info + "\n";
        }
        
        /// <summary>
        /// 获取自己的战斗能力信息
        /// </summary>
        private string GetSelfCombatInfo(Agent agent)
        {
            var character = agent.Character;
            var currentWarpedAttributes = agent.CurrentWarpedInfo;
            var info = "【战斗能力】\n";
            info += $"战斗能力 (W): {currentWarpedAttributes.Warfare}/10 - {Evaluation.GetWarfareDescription(currentWarpedAttributes.Warfare)}\n";
            info += $"适应能力 (A): {currentWarpedAttributes.Adaptability}/10 - {Evaluation.GetAdaptabilityDescription(currentWarpedAttributes.Adaptability)}\n";
            info += $"推理能力 (R): {currentWarpedAttributes.Reasoning}/10 - {Evaluation.GetReasoningDescription(currentWarpedAttributes.Reasoning)}\n";
            info += $"感知能力 (P): {currentWarpedAttributes.Perception}/10 - {Evaluation.GetPerceptionDescription(currentWarpedAttributes.Perception)}\n";
            info += $"耐力 (E): {currentWarpedAttributes.Endurance}/10 - {Evaluation.GetEnduranceDescription(currentWarpedAttributes.Endurance)}\n";
            info += $"灵巧度 (D): {currentWarpedAttributes.Dexterity}/10 - {Evaluation.GetDexterityDescription(currentWarpedAttributes.Dexterity)}\n";
            info += $"战术分析: 建议在{(currentWarpedAttributes.Warfare > 5 ? "正面战斗" : "游击战术")}中发挥优势\n";
            return info + "\n";
        }
        
        /// <summary>
        /// 获取自己的技能信息
        /// </summary>
        private string GetSelfSkillsInfo(Agent agent)
        {
            var character = agent.Character;
            var currentSkills = agent.CurrentSkills;
            var info = "【技能信息】\n";
            if (character.Skills.Count > 0)
            {
                info += $"技能: {string.Join("、", character.Skills)}\n";
            }
            // 显示当前技能等级
            if (currentSkills.Count > 0)
            {
                info += "当前技能等级:\n";
                foreach (var skill in currentSkills)
                {
                    info += $"  {skill.Key}: 等级 {skill.Value}\n";
                }
            }
            info += $"知识水平: {Evaluation.KnowledgeLevelToDescription(character.KnowledgeLevel)}\n";
            return info + "\n";
        }
        
        /// <summary>
        /// 获取自己的关系信息
        /// </summary>
        private string GetSelfRelationshipsInfo(Agent agent)
        {
            var character = agent.Character;
            var info = "【关系信息】\n";
            if (agent.Relationships.Count > 0)
            {
                foreach (var relationship in agent.Relationships)
                {
                    info += $"{relationship.Value.TargetCharacterName}: {relationship.Value.GetRelationshipSummary()}\n";
                }
            }
            else
            {
                info += "暂无关系记录\n";
            }
            return info + "\n";
        }
        
        /// <summary>
        /// 获取自己的基本信息
        /// </summary>
        private string GetSelfBasicInfo(Agent agent)
        {
            var info = "【基本状态】\n";
            info += GetSelfHealthInfo(agent);
            info += GetSelfSkillsInfo(agent);
            return info;
        }
        private string GetSelfPersonalInfo(Agent agent)
        {
            var character = agent.Character;
            var info = "【个人偏好】\n";
            info += $"生活习惯: {character.GetLifestyleDescription()}\n";
            info += $"兴趣爱好: {character.Likes}\n";
            return info + "\n";
        }
        /// <summary>
        /// 获取自己的完整信息
        /// </summary>
        private string GetSelfFullInfo(Agent agent)
        {
            var character = agent.Character;
            var info = "";
            info += GetSelfHealthInfo(agent);
            info += GetSelfCombatInfo(agent);
            info += GetSelfSkillsInfo(agent);
            info += GetSelfRelationshipsInfo(agent);

            info += "【特殊信息】\n";
            if (!string.IsNullOrEmpty(character.Goals.ToString()))
                info += $"目标: {character.Goals}\n";
            if (!string.IsNullOrEmpty(character.Fears.ToString()))
                info += $"恐惧: {character.Fears}\n";
            if (!string.IsNullOrEmpty(character.Secrets.ToString()))
                info += $"秘密: [已隐藏，防止你泄露给其他角色]\n"; // 即使是自己，秘密也不完全显示
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
                new FunctionParameter("requested_info", "string", "请求的信息类型：basic/health/combat/skills/relationships/response_style/full（默认basic）")
            };
        }
    }
}
