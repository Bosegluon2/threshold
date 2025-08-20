using Threshold.Core.Agent;
using Threshold.Core.Data;
using Threshold.Core.Enums;
using Threshold.Core.Utils;
using Godot;
using Godot.Collections;
using System.Linq;
using System.Threading.Tasks;
using System;
namespace Threshold.Core
{
    public enum PolicyStatusType
    {
        Approved,
        Rejected,
        // 弃权
        Abstain
        }
    public partial class Policy : Resource
    {
        public string id { get; set; }
        public string content { get; set; }
        public Array<string> modifier = new Array<string>();
        public int duration = 0;
        public int priority = 0;




        public Policy(string id, string content, Array<string> modifier, int duration, int priority)
        {
            this.id = id;
            this.content = content;
            this.modifier = modifier;
            this.duration = duration;
            this.priority = priority;
        }
        public bool IsExpired()
        {
            return duration <= 0;
        }
        public void Update(int turn)
        {
            duration--;
        }
        public override string ToString()
        {
            return $"Policy: {id}\n{content}\nModifier: {string.Join(", ", modifier)}\nDuration: {duration}\nPriority: {priority}";
        }
    }
    public partial class PolicyPersonalOpinion : Resource
    {

        public Policy policy { get; set; }
        public Agent.Agent agent { get; set; }
        public PolicyStatusType status { get; set; }
        public string reason { get; set; }
        public PolicyPersonalOpinion(Policy policy, Agent.Agent agent, PolicyStatusType status, string reason)
        {
            this.policy = policy;
            this.agent = agent;
            this.status = status;
            this.reason = reason;
        }
    }
    public partial class PolicyStatus : Resource
    {
        public PolicyStatus(Policy policy, PolicyStatusType status)
        {
            this.policy = policy;
            this.status = status;
        }

        public Policy policy { get; set; }
        public PolicyStatusType status { get; set; }
        public override string ToString()
        {
            return $"Policy: {policy.id}\nStatus: {status}";
        }
        
    }
    public partial class CommitteeManager : Node
    {
        private Array<Agent.Agent> availableAgents = new Array<Agent.Agent>();
        private GameManager gameManager;
        private Array<Policy> policiesInEffect = new Array<Policy>();
        private int maxTurns = 1;
        #region Events
        [Signal]
        public delegate void PolicyExpiredEventHandler(Policy policy);
        [Signal]
        public delegate void PolicyAddedEventHandler(Policy policy);
        [Signal]
        public delegate void PolicyRemovedEventHandler(Policy policy);
        [Signal]
        public delegate void PolicyNegotiatedEventHandler(Policy policy, Agent.Agent agent, PolicyPersonalOpinion opinion);
        [Signal]
        public delegate void PolicySettledEventHandler(Policy policy, PolicyStatusType status);
        #endregion
        public CommitteeManager(GameManager gameManager)
        {
            this.gameManager = gameManager;
        }
        public void SetMaxTurns(int maxTurns)
        {
            this.maxTurns = maxTurns;
        }
        public int GetMaxTurns()
        {
            return maxTurns;
        }
        public void UpdateAvailableAgents()
        {
            GD.Print("UpdateAvailableAgents");
            // 清空所有空闲的agent
            availableAgents.Clear();
            foreach (Agent.Agent agent in GameManager.Instance.CharacterManager.GetAllAgents())
            {
                GD.Print($"Checking agent {agent.AgentName}");
                if (agent.CanAttendCommittee() && agent.AgentId != "player")
                {
                    availableAgents.Add(agent);
                    GD.Print($"Adding agent {agent.AgentName}");
                }
            }
            GD.Print($"Available agents: {availableAgents.Count}");
        }
        public bool CheckAvailable()
        {
            //议会只在中午开始
            if (TimeUtils.GetTimeOfDay(gameManager.CurrentTurn) != TimeOfDay.Noon)
            {
                GD.Print("Not noon");
                return false;
            }
            // 少于2人，停止议会阶段
            if (availableAgents.Count < 2)
            {
                GD.Print("Less than 2 agents");
                return false;
            }
            GD.Print("Enough agents and right time");
            return true;
        }
        public void AddPolicy(Policy policy)
        {
            policiesInEffect.Add(policy);
        }
        public void RemovePolicy(Policy policy)
        {
            policiesInEffect.Remove(policy);
        }
        public void UpdatePolicies(int turn)
        {
            foreach (Policy policy in policiesInEffect)
            {
                policy.Update(turn);
                if (policy.IsExpired())
                {
                    RemovePolicy(policy);
                    GD.Print($"Policy {policy.id} expired at turn {turn}");
                    EmitSignal(SignalName.PolicyExpired, policy);
                }
            }
        }
        public async Task<PolicyPersonalOpinion> NegotiatePolicy(Policy policy, Agent.Agent agent)
        {
            GD.Print($"Negotiating policy {policy.id} with {agent.AgentName}");
            // 获取agent的性格，政治观点，记忆等
            var basicInfo = GameManager.Instance.WorldABC.GetEssentialInfo();
            var character = agent.GetCharacter();
            var personality = character.Personality;
            var coreViews = character.CoreValues;
            var memory = agent.Memories;
            var relationship = new Relation("player", GameManager.Instance.CharacterManager.GetPlayerAgent().AgentName, 60);
            if (agent.Relationships.ContainsKey("player"))
            {
                relationship = agent.Relationships["player"];
            }
            // TODO: 也许需要考虑其他关系
            // 异步等待1秒


            string prompt = $@"
请根据以下信息，判断是否同意该政策：
世界背景：
{basicInfo}
角色职业：
{character.Profession}
角色性格：
{personality}
角色核心价值观：
{coreViews}
角色记忆：
{string.Join("\n", memory)}
角色关系：
{relationship.RelationshipDescription}
政策内容：
{policy.content}
政策修改器：
{string.Join(", ", policy.modifier)}
政策优先级：
{policy.priority}
政策持续时间：
{policy.duration}

请根据以上信息，返回纯json的格式，包含以下字段：
{{
    ""reason"": ""string"",
    ""result"": ""approved"" 或 ""rejected"" 或 ""abstain""
}}
其中reason是你的理由，result是你的态度（同意为""approved""，反对为""rejected""，弃权为""abstain""）。
";
            var messages = new Array<ConversationMessage>
            {
                new ConversationMessage(prompt, "system")
            };
            // 修正：方法需要声明为async，否则不能使用await
            // 此外，返回类型应为Task<PolicyPersonalOpinion>，return时需要直接返回对象
            GD.Print($"Sending prompt to {agent.AgentName}");
            string response = await GameManager.Instance.CharacterManager.GetAICommunication().GetResponse(messages);
            // GD.Print(response);
            // 清理response中代码块遗留 ```json和```
            response = response.Replace("```json", "").Replace("```", "");
            // 清理换行符
            response = response.Replace("\n", "");
            // 解析json
            var json = Json.ParseString(response).AsGodotDictionary();
            var reason = json["reason"].AsString();
            var result = json["result"].AsString();
            PolicyStatusType status = PolicyStatusType.Abstain;
            if (result == "approved")
            {
                status = PolicyStatusType.Approved;
            }
            else if (result == "rejected")
            {
                status = PolicyStatusType.Rejected;
            }
            GD.Print($"Policy {policy.id} negotiated with {agent.AgentName}: {reason}, {result}");
            EmitSignal(SignalName.PolicyNegotiated, policy, agent, new PolicyPersonalOpinion(policy, agent, status, reason));
            return new PolicyPersonalOpinion(policy, agent, status, reason);
        }
        public async Task<PolicyStatus> VotePolicy(Policy policy, bool isTest = false)
        {
            UpdateAvailableAgents();
            if (!CheckAvailable() && !isTest)
            {
                GD.Print("No available agents");
                return new PolicyStatus(policy, PolicyStatusType.Rejected);
            }
            GD.Print($"Voting policy {policy.id} with {availableAgents.Count} agents");
            // 并行获取每个agent的投票
            Task<PolicyPersonalOpinion>[] tasks = new Task<PolicyPersonalOpinion>[availableAgents.Count];
            for (int i = 0; i < availableAgents.Count; i++)
            {
                tasks[i] = NegotiatePolicy(policy, availableAgents[i]);
            }
            // 等待所有投票完成
            PolicyPersonalOpinion[] results = await System.Threading.Tasks.Task.WhenAll(tasks);

            // 统计投票结果
            float approvedCount = 0;
            float rejectedCount = 0;
            float abstainCount = 0;
            foreach (var result in results)
            {
                if (result.status == PolicyStatusType.Approved)
                {
                    approvedCount += result.agent.CurrentVoteWeight;
                }
                else if (result.status == PolicyStatusType.Abstain)
                {
                    abstainCount += result.agent.CurrentVoteWeight;
                }
                else
                {
                    rejectedCount += result.agent.CurrentVoteWeight;
                }
            }

            // 更科学的通过方法：采用“绝对多数制”
            // 绝对多数：同意票数 > (总有效票数/2)，弃权不计入有效票
            float validVotes = approvedCount + rejectedCount;
            PolicyStatusType finalStatus;
            if (validVotes == 0)
            {
                // 全部弃权
                finalStatus = PolicyStatusType.Abstain;
            }
            else if (approvedCount > validVotes / 2)
            {
                finalStatus = PolicyStatusType.Approved;
            }
            else if (rejectedCount > validVotes / 2)
            {
                finalStatus = PolicyStatusType.Rejected;
            }
            else
            {
                // 没有一方获得绝对多数，视为弃权
                finalStatus = PolicyStatusType.Abstain;
            }

            PolicyStatus policyStatus = new PolicyStatus(policy, finalStatus);
            EmitSignal(SignalName.PolicySettled, policy, finalStatus.GetHashCode());
            return policyStatus;
        }
        public async Task<PolicyStatus> Test()
        {
            var policy = new Policy("科技而非军事", "科技是第一生产力，我们应该大力发展科技，而非在军事上投入资源", new Array<string>(), 10, 1);
            var opinion = await VotePolicy(policy, true);
            return opinion;
        }
        public Array<Agent.Agent> GetAvailableAgents()
        {
            return availableAgents;
        }
        public float GetTotalVoteWeight()
        {
            float total = 0;
            foreach (var agent in availableAgents)
            {
                total += agent.CurrentVoteWeight;
            }
            return total;
        }

        public Policy GetPolicy(string title)
        {
            return policiesInEffect.FirstOrDefault(p => p.id == title);
        }
        public Array<Policy> GetPolicies()
        {
            return policiesInEffect;
        }
    }
}