using System;
using Godot;
using Godot.Collections;
using Threshold.Core.Agent;
using Threshold.Core.Utils;

namespace Threshold.Core.Data
{
    /// <summary>
    /// 表示一个地点/场所
    /// </summary>
    public partial class Place : Resource
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public bool IsAccessible { get; set; } = true;
        public string ParentId { get; set; }
        public Vector3 Position { get; set; } = Vector3.Zero;
        public int Capacity { get; set; } = 10;
        public int Level { get; set; } = 1;
        public Array<string> Tags { get; set; } = new Array<string>();
        public string State { get; set; } = "normal";
        public string Environment { get; set; } = "indoor";
        public bool CanGetTime { get; set; } = false;
        public bool CanGetWeather { get; set; } = false;
        public bool CanGetLocation { get; set; } = false;
        public bool CanGetAtmosphere { get; set; } = false;
        public float LightLevel { get; set; } = 1.0f;
        public float Temperature { get; set; } = 20.0f;
        public Array<Agent.Agent> Agents { get; set; } = new Array<Agent.Agent>();
        
        // 脚本驱动的效果 - 替代PlaceEffect
        public string OnAgentEnterScript { get; set; } = ""; // Agent进入时执行的脚本
        public string OnAgentLeaveScript { get; set; } = ""; // Agent离开时执行的脚本
        
        public Place() { }

        public Place(string id, string name, string description, string type, bool isAccessible = true, string parentId = null, Vector3? position = null, bool canGetTime = false, bool canGetWeather = false, bool canGetLocation = false, bool canGetAtmosphere = false)
        {
            Id = id;
            Name = name;
            Description = description;
            Type = type;
            IsAccessible = isAccessible;
            ParentId = parentId;
            Position = position ?? Vector3.Zero;
            CanGetTime = canGetTime;
            CanGetWeather = canGetWeather;
            CanGetLocation = canGetLocation;
            CanGetAtmosphere = canGetAtmosphere;
        }

        public override string ToString()
        {
            return $"{Name}（{Type}）: {Description}";
        }

        public override bool Equals(object obj)
        {
            return obj is Place place && Id == place.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public bool AddAgent(Agent.Agent agent)
        {
            if (ContainsAgent(agent) || Agents.Count >= Capacity)
            {
                return false;
            }
            Agents.Add(agent);
            
            // 执行进入时脚本
            ExecuteOnAgentEnterScript(agent);
            
            return true;
        }

        public bool RemoveAgent(Agent.Agent agent)
        {
            if (!ContainsAgent(agent))
            {
                return false;
            }
            
            // 执行离开时脚本
            ExecuteOnAgentLeaveScript(agent);
            
            Agents.Remove(agent);
            return true;
        }
        
        /// <summary>
        /// 执行Agent进入时脚本
        /// </summary>
        private void ExecuteOnAgentEnterScript(Agent.Agent agent)
        {
            if (string.IsNullOrEmpty(OnAgentEnterScript))
                return;
                
            try
            {
                var context = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["agent"] = Variant.CreateFrom(agent),
                    ["place"] = Variant.CreateFrom(this),
                    ["gameManager"] = Variant.CreateFrom(GameManager.Instance),
                    ["global"] = Variant.CreateFrom(Global.Instance)
                };
                
                var result = ScriptExecutor.Instance.ExecuteScript(OnAgentEnterScript, context);
                GD.Print($"地点 {Name} 执行进入脚本，结果: {result}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"执行地点进入脚本时发生错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 执行Agent离开时脚本
        /// </summary>
        private void ExecuteOnAgentLeaveScript(Agent.Agent agent)
        {
            if (string.IsNullOrEmpty(OnAgentLeaveScript))
                return;
                
            try
            {
                var context = new Godot.Collections.Dictionary<string, Variant>
                {
                    ["agent"] = Variant.CreateFrom(agent),
                    ["place"] = Variant.CreateFrom(this),
                    ["gameManager"] = Variant.CreateFrom(GameManager.Instance),
                    ["global"] = Variant.CreateFrom(Global.Instance)
                };
                
                var result = ScriptExecutor.Instance.ExecuteScript(OnAgentLeaveScript, context);
                GD.Print($"地点 {Name} 执行离开脚本，结果: {result}");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"执行地点离开脚本时发生错误: {ex.Message}");
            }
        }

        public int GetAgentCount()
        {
            return Agents.Count;
        }

        public Array<Agent.Agent> GetAllAgents()
        {
            return Agents;
        }

        public bool ContainsAgent(Agent.Agent agent)
        {
            return Agents.Contains(agent);
        }

        public string GetFullDescription()
        {
            var description = $"{Name} ({Type})\n";
            description += $"{Description}\n";
            description += $"状态: {State}\n";
            description += $"环境: {Environment}\n";
            description += $"容量: {Agents.Count}/{Capacity}\n";
            description += $"等级: {Level}\n";
            description += $"光照: {LightLevel:F1}\n";
            description += $"温度: {Temperature:F1}°C\n";
            
            if (Tags.Count > 0)
            {
                description += $"标签: {string.Join(", ", Tags)}\n";
            }
            
            return description;
        }

        public bool IsSafe()
        {
            return State == "safe" || State == "normal";
        }

        public bool IsDangerous()
        {
            return State == "dangerous" || State == "hostile";
        }

        public bool IsFull()
        {
            return Agents.Count >= Capacity;
        }
    }
} 
