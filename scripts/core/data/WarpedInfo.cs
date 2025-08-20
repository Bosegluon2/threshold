using System;
using Godot;

namespace Threshold.Core.Data
{
    /// <summary>
    /// WARPED 属性类，方便管理Agent的WARPED属性
    /// </summary>
    public partial class WarpedInfo : Resource
    {
        
        public int Warfare;
        public int Adaptability;
        public int Reasoning;
        public int Perception;
        public int Endurance;
        public int Dexterity;
        public WarpedInfo() { }
        public WarpedInfo(WarpedInfo warpedInfo) // 拷贝构造函数
        {
            Warfare = warpedInfo.Warfare;
            Adaptability = warpedInfo.Adaptability;
            Reasoning = warpedInfo.Reasoning;
            Perception = warpedInfo.Perception;
            Endurance = warpedInfo.Endurance;
            Dexterity = warpedInfo.Dexterity;
        }
        public WarpedInfo(int warfare, int adaptability, int reasoning, int perception, int endurance, int dexterity)
        {
            Warfare = warfare;
            Adaptability = adaptability;
            Reasoning = reasoning;
            Perception = perception;
            Endurance = endurance;
            Dexterity = dexterity;
        }


    }
}
