using System;
using Godot;

namespace Threshold.Core.Data
{
    /// <summary>
    /// OCEAN 属性类，方便管理Agent的OCEAN属性
    /// </summary>
    public partial class OceanInfo : Resource
    {
        
        public int Openness;
        public int Conscientiousness;
        public int Extraversion;
        public int Agreeableness;
        public int Neuroticism;
        public OceanInfo() { }

        public OceanInfo(int openness, int conscientiousness, int extraversion, int agreeableness, int neuroticism)
        {
            Openness = openness;
            Conscientiousness = conscientiousness;
            Extraversion = extraversion;
            Agreeableness = agreeableness;
            Neuroticism = neuroticism;
        }


    }
}
