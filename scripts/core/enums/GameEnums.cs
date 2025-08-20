using Godot;

namespace Threshold.Core.Enums
{
    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameState
    {
        NotStarted,     // 未开始
        Playing,        // 游戏中
        Paused,         // 暂停
        GameOver,       // 游戏结束
        Victory,        // 胜利
        Defeat          // 失败
    }

    /// <summary>
    /// 一天中的时段枚举
    /// </summary>
    public enum TimeOfDay
    {
        Morning,        // 早上 (6:00-12:00)
        Noon,           // 中午 (12:00-18:00)
        Evening,        // 晚上 (18:00-24:00)
        Midnight        // 半夜 (0:00-6:00) - 特殊回合
    }

    /// <summary>
    /// 世界状态枚举
    /// </summary>
    public enum WorldState
    {
        Normal,         // 正常状态
        Dangerous,      // 危险状态
        Critical,       // 危急状态
        Chaos,          // 混乱状态
        Safe            // 安全状态
    }

    /// <summary>
    /// 事件类型枚举
    /// </summary>
    public enum EventType
    {
        Random,         // 随机事件
        Story,          // 剧情事件
        Character,      // 角色事件
        World,          // 世界事件
        System          // 系统事件
    }

    /// <summary>
    /// 事件优先级枚举
    /// </summary>
    public enum EventPriority
    {
        Low,            // 低优先级
        Normal,         // 普通优先级
        High,           // 高优先级
        Critical        // 关键优先级
    }

    /// <summary>
    /// 资源类型枚举
    /// </summary>
    public enum ResourceType
    {
        Food,           // 食物
        Water,          // 水
        Medicine,       // 药品
        Ammunition,     // 弹药
        Fuel,           // 燃料
        Materials,      // 材料
        Money           // 金钱
    }

    /// <summary>
    /// 角色状态枚举
    /// </summary>
    public enum CharacterStatus
    {
        Healthy,        // 健康
        Injured,        // 受伤
        Sick,           // 生病
        Exhausted,      // 疲惫
        Dead            // 死亡
    }

    /// <summary>
    /// 天气类型枚举
    /// </summary>
    public enum WeatherType
    {
        Clear,          // 晴朗
        Cloudy,         // 多云
        Rainy,          // 下雨
        Stormy,         // 暴风雨
        Foggy,          // 雾天
        Snowy,          // 下雪
        Hail,           // 冰雹
        Windy,          // 有风
        Thunderstorm,   // 雷暴
        Drizzle,        // 毛毛雨
        Blizzard,       // 暴风雪
        Sandstorm,      // 沙尘暴
        Overcast        // 阴天
    }

    /// <summary>
    /// 难度等级枚举
    /// </summary>
    public enum DifficultyLevel
    {
        Easy,           // 简单
        Normal,         // 普通
        Hard,           // 困难
        Nightmare       // 噩梦
    }
}
