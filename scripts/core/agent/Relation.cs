using Godot;
using Godot.Collections;
using System;

namespace Threshold.Core.Agent
{
    /// <summary>
    /// 角色关系类 - 包含信任程度、印象、关系描述等信息
    /// </summary>
    public partial class Relation : Resource
    {
        [Export] public string TargetCharacterId { get; set; } = "";
        [Export] public string TargetCharacterName { get; set; } = "";
        [Export] public int TrustLevel { get; set; } = 50; // 信任程度 0-100
        [Export] public int RelationshipLevel { get; set; } = 50; // 关系等级 0-100
        [Export] public string Impression { get; set; } = ""; // 对目标角色的印象
        [Export] public string RelationshipDescription { get; set; } = ""; // 关系描述
        [Export] public string FirstMeeting { get; set; } = ""; // 初次见面的描述
        [Export] public string LastInteraction { get; set; } = ""; // 最后一次互动的描述
        [Export] public int InteractionCount { get; set; } = 0; // 互动次数
        [Export] public int SharedSecretsCount { get; set; } = 0; // 已分享的秘密数量
        [Export] public int FavorsCount { get; set; } = 0; // 互相帮助的次数
        [Export] public int ConflictsCount { get; set; } = 0; // 冲突次数
        [Export] public string RelationshipType { get; set; } = "陌生人"; // 关系类型：陌生人、熟人、朋友、密友、恋人、敌人等
        
        public Relation() { }

        public Relation(string targetCharacterId, string targetCharacterName, int initialTrust = 50)
        {
            TargetCharacterId = targetCharacterId;
            TargetCharacterName = targetCharacterName;
            TrustLevel = Mathf.Clamp(initialTrust, 0, 100);
            RelationshipLevel = 50;
            RelationshipDescription = "陌生人";
            FirstMeeting = "初次见面";
            LastInteraction = "初次见面";
            InteractionCount = 0;
            SharedSecretsCount = 0;
            FavorsCount = 0;
            ConflictsCount = 0;
        }
        
        /// <summary>
        /// 更新信任程度
        /// </summary>
        public void UpdateTrust(int change, string reason = "")
        {
            var oldTrust = TrustLevel;
            TrustLevel = Mathf.Clamp(TrustLevel + change, 0, 100);
            
            if (!string.IsNullOrEmpty(reason))
            {
                GD.Print($"角色 {TargetCharacterName} 信任度变化: {oldTrust} -> {TrustLevel} (原因: {reason})");
            }
            
            // 根据信任度自动调整关系类型
            UpdateRelationshipType();
        }
        
        /// <summary>
        /// 更新关系等级
        /// </summary>
        public void UpdateRelationship(int change, string reason = "")
        {
            var oldRelationship = RelationshipLevel;
            RelationshipLevel = Mathf.Clamp(RelationshipLevel + change, 0, 100);
            
            if (!string.IsNullOrEmpty(reason))
            {
                GD.Print($"角色 {TargetCharacterName} 关系等级变化: {oldRelationship} -> {RelationshipLevel} (原因: {reason})");
            }
            
            // 根据关系等级自动调整关系类型
            UpdateRelationshipType();
        }
        
        /// <summary>
        /// 记录互动
        /// </summary>
        public void RecordInteraction(string description, int trustChange = 0, int relationshipChange = 0)
        {
            LastInteraction = description;
            InteractionCount++;
            
            if (trustChange != 0)
            {
                UpdateTrust(trustChange, description);
            }
            
            if (relationshipChange != 0)
            {
                UpdateRelationship(relationshipChange, description);
            }
        }
        
        /// <summary>
        /// 记录分享的秘密
        /// </summary>
        public void RecordSharedSecret(string secretType, string secretContent)
        {
            SharedSecretsCount++;
            
            // 分享秘密通常会增加信任和关系
            UpdateTrust(5, $"分享了{secretType}");
            UpdateRelationship(3, $"分享了{secretType}");
        }
        
        /// <summary>
        /// 记录帮助行为
        /// </summary>
        public void RecordFavor(string favorDescription, bool isGiving = true)
        {
            FavorsCount++;
            
            // 帮助行为会增加信任和关系
            var trustChange = isGiving ? 3 : 5;
            var relationshipChange = isGiving ? 2 : 4;
            
            UpdateTrust(trustChange, favorDescription);
            UpdateRelationship(relationshipChange, favorDescription);
        }
        
        /// <summary>
        /// 记录冲突
        /// </summary>
        public void RecordConflict(string conflictDescription, int severity = 1)
        {
            ConflictsCount++;
            
            // 冲突会减少信任和关系
            var trustChange = -severity * 5;
            var relationshipChange = -severity * 3;
            
            UpdateTrust(trustChange, conflictDescription);
            UpdateRelationship(relationshipChange, conflictDescription);
        }
        
        /// <summary>
        /// 更新关系类型
        /// </summary>
        private void UpdateRelationshipType()
        {
            var combinedScore = (TrustLevel + RelationshipLevel) / 2.0f;
            
            if (combinedScore >= 90)
                RelationshipType = "密友";
            else if (combinedScore >= 80)
                RelationshipType = "好朋友";
            else if (combinedScore >= 70)
                RelationshipType = "朋友";
            else if (combinedScore >= 60)
                RelationshipType = "熟人";
            else if (combinedScore >= 40)
                RelationshipType = "点头之交";
            else if (combinedScore >= 20)
                RelationshipType = "陌生人";
            else if (combinedScore >= 10)
                RelationshipType = "不信任";
            else
                RelationshipType = "敌人";
        }
        
        /// <summary>
        /// 获取关系摘要
        /// </summary>
        public string GetRelationshipSummary()
        {
            var summary = $"与 {TargetCharacterName} 的关系:\n";
            summary += $"关系类型: {RelationshipType}\n";
            summary += $"信任程度: {TrustLevel}/100\n";
            summary += $"关系等级: {RelationshipLevel}/100\n";
            summary += $"互动次数: {InteractionCount}\n";
            
            if (!string.IsNullOrEmpty(Impression))
                summary += $"印象: {Impression}\n";
            
            if (!string.IsNullOrEmpty(RelationshipDescription))
                summary += $"关系描述: {RelationshipDescription}\n";
            
            if (SharedSecretsCount > 0)
                summary += $"已分享秘密: {SharedSecretsCount} 个\n";
            
            if (FavorsCount > 0)
                summary += $"互相帮助: {FavorsCount} 次\n";
            
            if (ConflictsCount > 0)
                summary += $"冲突记录: {ConflictsCount} 次\n";
            
            return summary;
        }
        
        /// <summary>
        /// 获取信任描述
        /// </summary>
        public string GetTrustDescription()
        {
            if (TrustLevel >= 90) return "完全信任";
            if (TrustLevel >= 80) return "高度信任";
            if (TrustLevel >= 70) return "比较信任";
            if (TrustLevel >= 60) return "一般信任";
            if (TrustLevel >= 50) return "中立";
            if (TrustLevel >= 40) return "略有怀疑";
            if (TrustLevel >= 30) return "比较怀疑";
            if (TrustLevel >= 20) return "高度怀疑";
            if (TrustLevel >= 10) return "极度怀疑";
            else return "完全不信任";
        }
        
        /// <summary>
        /// 获取关系描述
        /// </summary>
        public string GetRelationshipDescription()
        {
            if (RelationshipLevel >= 90) return "生死之交";
            if (RelationshipLevel >= 80) return "亲密无间";
            if (RelationshipLevel >= 70) return "关系密切";
            if (RelationshipLevel >= 60) return "关系良好";
            if (RelationshipLevel >= 50) return "关系一般";
            if (RelationshipLevel >= 40) return "关系疏远";
            if (RelationshipLevel >= 30) return "关系冷淡";
            if (RelationshipLevel >= 20) return "关系紧张";
            if (RelationshipLevel >= 10) return "关系恶劣";
            else return "势不两立";
        }
        
        /// <summary>
        /// 检查是否可以分享特定类型的信息
        /// </summary>
        public bool CanShareInformation(int informationSecrecyLevel)
        {
            // 使用sigmoid函数计算分享概率
            var combinedScore = (TrustLevel + RelationshipLevel) / 2.0f;
            var probability = CalculateShareProbability(combinedScore, informationSecrecyLevel);
            
            // 使用随机数决定是否分享
            var random = new Random();
            return random.NextDouble() < probability;
        }
        
        
        public bool IsCloseContact(int relationshipLevel)
        {
            return RelationshipLevel >= relationshipLevel;
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
    }
}
