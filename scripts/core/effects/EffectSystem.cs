using Godot;
using System;
using System.Collections.Generic;

namespace Threshold.Core.Effects
{
    /// <summary>
    /// 效果类型枚举
    /// </summary>
    public enum EffectCategory
    {
        World,      // 世界效果
        Character,  // 角色效果
        Skill,      // 技能效果
        Item,       // 物品效果
        Quest,      // 任务效果
        System      // 系统效果
    }

    /// <summary>
    /// 基础效果引用类 - 用于effects命名空间中的基础定义
    /// 具体的Effect实现由各个系统（EventManager、Agent等）管理
    /// </summary>
    public class EffectReference
    {
        public string EffectId { get; set; } = "";
        public string EffectType { get; set; } = "";
        public EffectCategory Category { get; set; } = EffectCategory.World;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        // 使用路径字符串来定义效果目标
        public string TargetPath { get; set; } = "";
        
        // 效果脚本
        public string EffectScript { get; set; } = "";
        
        // 持续时间相关
        public int Duration { get; set; } = 0;
        public int RemainingTurns { get; set; } = 0;
        
        public EffectReference()
        {
            Parameters = new Dictionary<string, object>();
            RemainingTurns = Duration;
        }
        
        /// <summary>
        /// 更新效果状态
        /// </summary>
        public virtual void UpdateEffect()
        {
            if (RemainingTurns > 0)
            {
                RemainingTurns--;
            }
        }
        
        /// <summary>
        /// 检查效果是否已过期
        /// </summary>
        public virtual bool IsExpired()
        {
            return RemainingTurns <= 0;
        }
    }
}
