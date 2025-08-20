using Godot;
using Threshold.Core.Enums;
public static class TimeUtils
{
    /// <summary>
    /// 获取当前时段，0-Based
    /// </summary>
    /// <param name="turn"></param>
    /// <returns></returns>
    public static TimeOfDay GetTimeOfDay(int turn)
    {
        return (TimeOfDay)(turn % 4);
    }
    /// <summary>
    /// 获取当前天数，0-Based
    /// </summary>
    /// <param name="turn"></param>
    /// <returns></returns>
    public static int GetDay(int turn)
    {
        return turn / 4;
    }
    /// <summary>
    /// 获取当前回合，0-Based
    /// </summary>
    /// <param name="day"></param>
    /// <param name="timeOfDay"></param>
    /// <returns></returns>
    public static int GetTurn(int day,TimeOfDay timeOfDay)
    {
        return day * 4 + (int)timeOfDay;
    }
}