using Godot;
using System;

namespace Threshold.Core.Agent
{
    /// <summary>
    /// 信息类 - 包含具体内容和秘密程度
    /// </summary>
    public partial class Information : Resource
    {
        [Export] public string Content { get; set; } = "";
        [Export] public int SecrecyLevel { get; set; } = 0; // 0-100，0表示完全公开，100表示最高机密

        public Information() { }

        public Information(string content, int secrecyLevel = 0)
        {
            Content = content;
            SecrecyLevel = Mathf.Clamp(secrecyLevel, 0, 100);
        }

        /// <summary>
        /// 获取信息的描述（根据秘密程度可能隐藏部分内容）
        /// </summary>
        public string GetDescription(int trustLevel = 0)
        {
            if (trustLevel >= SecrecyLevel)
            {
                return Content; // 完全信任，显示全部内容
            }
            else if (trustLevel >= SecrecyLevel * 0.7f)
            {
                // 较高信任，显示大部分内容
                var words = Content.Split(' ');
                var visibleCount = Mathf.Max(1, (int)(words.Length * 0.8f));
                var visibleWords = new string[visibleCount];
                Array.Copy(words, visibleWords, visibleCount);
                return string.Join(" ", visibleWords) + "...";
            }
            else if (trustLevel >= SecrecyLevel * 0.4f)
            {
                // 中等信任，显示部分内容
                var words = Content.Split(' ');
                var visibleCount = Mathf.Max(1, (int)(words.Length * 0.5f));
                var visibleWords = new string[visibleCount];
                Array.Copy(words, visibleWords, visibleCount);
                return string.Join(" ", visibleWords) + "...";
            }
            else
            {
                // 低信任，只显示基本信息
                return "[信息保密]";
            }
        }

        /// <summary>
        /// 检查是否应该分享此信息
        /// </summary>
        public bool ShouldShare(int trustLevel, int relationshipLevel = 50)
        {
            // 使用sigmoid函数计算分享概率
            var combinedScore = (trustLevel + relationshipLevel) / 2.0f;
            var probability = CalculateShareProbability(combinedScore, SecrecyLevel);

            // 使用随机数决定是否分享
            var random = new Random();
            return random.NextDouble() < probability;
        }

        /// <summary>
        /// 使用sigmoid函数计算分享概率
        /// </summary>
        private double CalculateShareProbability(double trustScore, int secrecyLevel)
        {
            // 归一化信任分数到0-1范围
            var normalizedTrust = Mathf.Clamp(trustScore / 100.0, 0.0, 1.0);

            // 归一化秘密程度到0-1范围
            var normalizedSecrecy = secrecyLevel / 100.0;

            // 计算信任与秘密的差值
            var difference = normalizedTrust - normalizedSecrecy;

            // 使用sigmoid函数，设置较小的过渡区间
            var steepness = 15.0; // 控制sigmoid的陡峭程度
            var sigmoid = 1.0 / (1.0 + Math.Exp(-steepness * difference));

            // 确保在极端情况下有明确的边界
            if (difference < -0.3) return 0.01; // 几乎不可能分享
            if (difference > 0.3) return 0.99;  // 几乎肯定会分享

            return sigmoid;
        }

        /// <summary>
        /// 获取信息的公开程度描述
        /// </summary>
        public string GetSecrecyDescription()
        {
            if (SecrecyLevel <= 10) return "完全公开";
            if (SecrecyLevel <= 25) return "基本公开";
            if (SecrecyLevel <= 40) return "一般信息";
            if (SecrecyLevel <= 60) return "内部信息";
            if (SecrecyLevel <= 80) return "机密信息";
            if (SecrecyLevel <= 95) return "高度机密";
            else return "绝密信息";
        }
        public override string ToString()
        {
            return $" {Content}";
        }
    }
}
