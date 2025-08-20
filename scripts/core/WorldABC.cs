using Godot;
using Threshold.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Threshold.Core.Agent;
using System;

/// <summary>
/// 世界基础知识，用来让Agent快速了解世界
/// </summary>
public partial class WorldABC : Node
{
    private GameManager gameManager;
    public string BackgroundStory { get; set; }
    public string WorldStyle { get; set; }
    public string WorldRules { get; set; }
    public string WorldHistory { get; set; }
    public string WorldCulture { get; set; }
    public string WorldEconomy { get; set; }
    public string WorldPolitics { get; set; }
    public string WorldReligion { get; set; }
    public WorldABC()
    {
    }
    public WorldABC(GameManager gameManager)
    {

        this.gameManager = gameManager;
        try
        {
            var yaml = FileAccess.Open("./data/world_abc.yaml", FileAccess.ModeFlags.Read).GetAsText();
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            // 直接反序列化到当前对象，避免手动赋值
            var loaded = deserializer.Deserialize<WorldABC>(yaml);

            // 只赋值非Godot自带属性，避免设置Godot内部属性如Name导致报错
            var props = typeof(WorldABC).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var prop in props)
            {
                // 排除Godot自带的Name属性
                if (prop.CanWrite && prop.Name != "Name")
                {
                    var value = prop.GetValue(loaded);
                    prop.SetValue(this, value);
                }
            }
            GD.Print("WorldABC 初始化完成");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"WorldABC 初始化失败: {ex.Message}");
        }
    }
    public override void _Ready()
    {

    }
    public string GetEssentialInfo()
    {
        return $"世界背景: {BackgroundStory}";
    }

    public string GetWorldInfo()
    {
        return $"世界背景: {BackgroundStory}" +
        $"世界风格: {WorldStyle}" +
        $"世界规则: {WorldRules}" +
        $"世界历史: {WorldHistory}" +
        $"世界文化: {WorldCulture}" +
        $"世界经济: {WorldEconomy}" +
        $"世界政治: {WorldPolitics}" +
        $"世界宗教: {WorldReligion}";
    }
}