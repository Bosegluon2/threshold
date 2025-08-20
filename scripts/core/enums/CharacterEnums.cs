using Godot;
namespace Threshold.Core.Enums
{
    public enum Mood
    {
        // 正面情绪（由小到大）
        Calm,           // 平静
        Relaxed,        // 放松
        Hopeful,        // 充满希望
        Grateful,       // 感激
        Proud,          // 自豪
        Happy,          // 开心
        Excited,        // 兴奋
        Euphoric,       // 极度愉悦
        Ecstatic,       // 欣喜若狂

        // 中性/复杂情绪
        Surprised,      // 惊讶
        Confused,       // 困惑
        Bored,          // 无聊
        Tired,          // 疲惫
        Embarrassed,    // 尴尬
        Lonely,         // 孤独
        Nervous,        // 紧张
        Anxious,        // 焦虑

        // 负面情绪（由小到大）
        Sad,            // 悲伤
        Disgusted,      // 厌恶
        Ashamed,        // 羞愧
        Angry,          // 生气
        Fearful,        // 害怕
        Furious,        // 暴怒
        Overwhelmed,    // 不堪重负
        Terrified,      // 极度恐惧
        Hysterical,     // 歇斯底里
        Manic,          // 狂躁
        Despair,        // 绝望
        Devastated,     // 极度崩溃
        Catatonic       // 木僵
    }
}