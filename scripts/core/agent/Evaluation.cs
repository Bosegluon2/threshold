using System;

namespace Threshold.Core.Agent
{
    /// <summary>
    /// 评价工具类：将数值转换为自然语言描述
    /// </summary>
    public static class Evaluation
    {
        /// <summary>
        /// 将数值（0-100）转换为自然语言描述
        /// </summary>
        /// <param name="value">数值（0-100）</param>
        /// <param name="type">可选，描述类型（如"健康"、"能量"等）</param>
        /// <returns>自然语言描述</returns>
        public static string GetHealthDescription(int value, int max)
        {
            float percentage = (float)value / max;
            if (percentage >= 0.95f)
                return "你目前精力充沛，身体几乎没有任何不适，状态极佳。";
            else if (percentage >= 0.85f)
                return "你感觉良好，只有些许疲劳或小伤，对行动几乎没有影响。";
            else if (percentage >= 0.7f)
                return "你有一些疲惫或轻微的伤势，但整体状态还算不错。";
            else if (percentage >= 0.55f)
                return "你感到明显的疲劳或有一些伤口，行动略受影响，需要注意休息。";
            else if (percentage >= 0.4f)
                return "你身体状况较差，伤势或虚弱感影响了你的行动效率。";
            else if (percentage >= 0.25f)
                return "你非常虚弱，伤势严重，行动变得吃力，急需治疗或休息。";
            else if (percentage >= 0.1f)
                return "你濒临崩溃，身体极度虚弱，几乎无法支撑自己，随时可能倒下。";
            else
                return "你处于濒死的边缘，生命垂危，任何轻微的打击都可能导致死亡。";
        }

        public static string GetEnergyDescription(int value, int max)
        {
            float percentage = (float)value / max;
            if (percentage >= 0.95f)
                return "你精力充沛，精神饱满，完全没有疲惫感，随时可以投入任何活动。";
            else if (percentage >= 0.85f)
                return "你感觉精力充足，只有轻微的疲劳，完全不影响你的行动。";
            else if (percentage >= 0.7f)
                return "你有些疲惫，但依然能够顺利完成大部分任务。";
            else if (percentage >= 0.55f)
                return "你感到明显的疲劳，精力有所下降，长时间活动会感到吃力。";
            else if (percentage >= 0.4f)
                return "你的精力消耗较大，精神有些恍惚，需要适当休息，否则会影响表现。";
            else if (percentage >= 0.25f)
                return "你非常疲惫，精力几乎耗尽，难以集中注意力，行动变得迟缓。";
            else if (percentage >= 0.1f)
                return "你精疲力竭，几乎无法支撑自己，随时可能因过度疲劳而倒下。";
            else
                return "你处于极度虚弱的状态，精力枯竭，任何活动都可能导致昏厥。";
        }

        // Warped System
        public static string GetWarfareDescription(int value)
        {
            // 0-10，0为极弱，10为战神
            if (value >= 10)
                return "你的战斗能力堪称战神，几乎无人能敌。";
            else if (value >= 8)
                return "你的战斗能力非常强大，能够轻松击败大多数敌人。";
            else if (value >= 6)
                return "你的战斗能力较强，面对一般敌人有明显优势。";
            else if (value >= 4)
                return "你的战斗能力一般，能够应付普通的冲突，但遇到强敌需谨慎。";
            else if (value >= 2)
                return "你的战斗能力较弱，面对危险时需要依靠技巧或他人帮助。";
            else
                return "你几乎没有战斗能力，手无缚鸡之力，极易被敌人击败。";
        }

        public static string GetAdaptabilityDescription(int value)
        {
            // 0-10，0为极差，10为极强
            if (value >= 10)
                return "你的适应能力极强，能在任何环境下如鱼得水。";
            else if (value >= 8)
                return "你的适应能力非常出色，面对变化总能迅速调整自己。";
            else if (value >= 6)
                return "你的适应能力较好，能较快适应新环境和挑战。";
            else if (value >= 4)
                return "你的适应能力一般，面对新情况需要一定时间调整。";
            else if (value >= 2)
                return "你的适应能力较弱，面对变化容易感到不适应。";
            else
                return "你几乎无法适应环境的变化，容易陷入困境。";
        }

        public static string GetReasoningDescription(int value)
        {
            // 0-10，0为极差，10为极强
            if (value >= 10)
                return "你的推理能力堪比天才，能轻松洞察复杂问题的本质。";
            else if (value >= 8)
                return "你的推理能力非常出色，能迅速分析并解决难题。";
            else if (value >= 6)
                return "你的推理能力较强，能理清大多数问题的逻辑。";
            else if (value >= 4)
                return "你的推理能力一般，能处理常见问题，但遇到复杂情况会有些吃力。";
            else if (value >= 2)
                return "你的推理能力较弱，面对复杂问题容易迷惑。";
            else
                return "你几乎没有推理能力，难以理解和分析问题。";
        }

        public static string GetPerceptionDescription(int value)
        {
            // 0-10，0为极差，10为极强
            if (value >= 10)
                return "你的感知力极为敏锐，几乎没有任何细节能逃过你的眼睛。";
            else if (value >= 8)
                return "你的感知力非常强，能轻易察觉到环境中的细微变化。";
            else if (value >= 6)
                return "你的感知力较好，能注意到大部分重要线索。";
            else if (value >= 4)
                return "你的感知力一般，能发现常见的异常，但容易忽略细节。";
            else if (value >= 2)
                return "你的感知力较弱，常常错过重要信息。";
            else
                return "你几乎没有感知力，周围发生的事情很难引起你的注意。";
        }

        public static string GetEnduranceDescription(int value)
        {
            // 0-10，0为极差，10为极强
            if (value >= 10)
                return "你的耐力惊人，能够长时间承受极端的体力和精神考验。";
            else if (value >= 8)
                return "你的耐力非常强，能轻松应对长时间的高强度活动。";
            else if (value >= 6)
                return "你的耐力较好，能完成大部分需要持久力的任务。";
            else if (value >= 4)
                return "你的耐力一般，长时间活动后会感到疲惫。";
            else if (value >= 2)
                return "你的耐力较弱，容易因体力不支而中断活动。";
            else
                return "你几乎没有耐力，稍微活动就会感到极度疲惫。";
        }

        public static string GetDexterityDescription(int value)
        {
            // 0-10，0为极差，10为极强
            if (value >= 10)
                return "你的灵巧度堪称完美，动作迅捷且极为精准。";
            else if (value >= 8)
                return "你的灵巧度非常高，能够轻松完成各种精细操作。";
            else if (value >= 6)
                return "你的灵巧度较好，动作协调，反应灵敏。";
            else if (value >= 4)
                return "你的灵巧度一般，完成日常操作没有问题。";
            else if (value >= 2)
                return "你的灵巧度较低，动作有些笨拙，容易出错。";
            else
                return "你几乎没有灵巧度，动作迟缓且不协调。";
        }

        public static string GetTrustLevelDescription(int value)
        {
            if (value >= 90)
                return "你对对方极度信任，几乎无条件相信对方。";
            else if (value >= 70)
                return "你非常信任对方，愿意与对方分享重要信息。";
            else if (value >= 50)
                return "你比较信任对方，愿意合作但仍有保留。";
            else if (value >= 30)
                return "你对对方有一定信任，但不会轻易托付重要事务。";
            else if (value >= 10)
                return "你对对方基本不信任，保持警惕。";
            else
                return "你完全不信任对方，甚至怀疑对方有恶意。";
        }
        public static string GetRelationshipLevelDescription(int value)
        {
            if (value >= 90)
                return "你们的关系极为亲密，几乎无话不谈。甚至可以称得上是恋人关系。";
            else if (value >= 70)
                return "你们关系很好，是亲密的朋友或伙伴。";
            else if (value >= 50)
                return "你们关系较好，能够互相支持。";
            else if (value >= 30)
                return "你们关系一般，属于普通熟人。";
            else if (value >= 10)
                return "你们关系较差，存在隔阂或矛盾。";
            else
                return "你们几乎没有关系，甚至互为敌人。";
        }




        /// <summary>
        /// 将技能等级枚举转换为自然语言描述
        /// </summary>
        /// <param name="level">技能等级</param>
        /// <returns>自然语言描述</returns>
        public static string KnowledgeLevelToDescription(CharacterCard.Knowledge level)
        {
            switch (level)
            {
                case CharacterCard.Knowledge.None:
                    return "无相关知识";
                case CharacterCard.Knowledge.Basic:
                    return "基础水平";
                case CharacterCard.Knowledge.Intermediate:
                    return "中等水平";
                case CharacterCard.Knowledge.Advanced:
                    return "高级水平";
                case CharacterCard.Knowledge.Expert:
                    return "专家级";
                default:
                    return "未知水平";
            }
        }
    }
}
