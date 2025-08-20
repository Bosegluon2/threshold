using System;
using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;
using Threshold.Core.Utils;

namespace Threshold.Core.Data
{
    public partial class Status : Resource
    {
        [Export] public string Id { get; set; }
        [Export] public string Name { get; set; }
        [Export] public string Description { get; set; }
        [Export] public string Category { get; set; } = "physical";
        [Export] public int Priority { get; set; } = 1;
        [Export] public int Duration { get; set; } = -1; // -1表示永久状态
        [Export] public bool Stackable { get; set; } = false;
        [Export] public bool Removable { get; set; } = true;
        [Export] public string Icon { get; set; } = "";
        
        // 效果系统 - 改为script实现
        [Export] public string EffectScript { get; set; } = "";
        
        // 状态管理
        public int CurrentDuration { get; private set; } = 0;
        private bool isActive = true;
        
        public Status() { }
        public Status(Status status)
        {
            Id = status.Id;
            Name = status.Name;
            Description = status.Description;
            CurrentDuration = status.CurrentDuration;
            Duration = status.Duration;
            Stackable = status.Stackable;
            Removable = status.Removable;
            Icon = status.Icon;
            EffectScript = status.EffectScript;
            Category = status.Category;
            Priority = status.Priority;
        }
        public Status(string id, string name, string description, int duration)
        {
            Id = id;
            Name = name;
            Description = description;
            CurrentDuration = duration;
        }

        /// <summary>
        /// 创建完整的Status对象
        /// </summary>
        public Status(string id, string name, string description, string category, int priority, 
                     int duration, bool stackable, bool removable, string icon, string effectScript = "")
        {
            Id = id;
            Name = name;
            Description = description;
            Category = category;
            Priority = priority;
            CurrentDuration = duration;
            Duration = duration;
            Stackable = stackable;
            Removable = removable;
            Icon = icon;
            EffectScript = effectScript;
        }

        /// <summary>
        /// 状态过期
        /// </summary>
        public void Expire()
        {
            isActive = false;
        }

        /// <summary>
        /// 激活状态
        /// </summary>
        public void Activate()
        {
            isActive = true;
            CurrentDuration = 0;
        }

        
        /// <summary>
        /// 执行状态效果脚本
        /// </summary>
        public Variant ExecuteEffectScript(Agent.Agent target, Dictionary<string, Variant> context = null)
        {
            if (string.IsNullOrEmpty(EffectScript))
            {
                return Variant.CreateFrom("");
            }

            try
            {
                CurrentDuration++;
                var scriptContext = new Dictionary<string, Variant>
                {
                    ["status"] = Variant.CreateFrom(this),
                    ["target"] = Variant.CreateFrom(target),
                    ["current_duration"] = Variant.CreateFrom(CurrentDuration),
                    ["is_active"] = Variant.CreateFrom(isActive)
                };

                // 合并传入的上下文
                if (context != null)
                {
                    foreach (var kvp in context)
                    {
                        scriptContext[kvp.Key] = kvp.Value;
                    }
                }

                var result = ScriptExecutor.Instance.ExecuteScript(EffectScript, scriptContext);
                GD.Print($"状态效果脚本执行完成: {Id}, 结果: {result}");
                return result;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"执行状态效果脚本时发生错误: {ex.Message}");
                return Variant.CreateFrom("");
            }
        }

        /// <summary>
        /// 获取效果描述
        /// </summary>
        public string GetEffectsDescription()
        {
            if (string.IsNullOrEmpty(EffectScript))
                return "无特殊效果";
            
            return $"效果脚本: {EffectScript}";
        }

        public bool Is(string statusId)
        {
            return Id == statusId;
        }

        public override bool Equals(object obj)
        {
            return obj is Status status &&
                   Id == status.Id;
        }

        public override string ToString()
        {
            return $"{Name}: {Description} (当前值: {CurrentDuration}/{Duration}, 优先级: {Priority})";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
}