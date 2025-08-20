using Godot;
using Godot.Collections;
using Threshold.Core;
using Threshold.Core.Agent;
using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

public partial class DataGenerator : Node
{
    public static DataGenerator Instance => _instance;
    private static DataGenerator _instance;

    public override void _Ready()
    {
        base._Ready();
        _instance = this;
    }

    /// <summary>
    /// 通过AI根据类型构造方法结构生成一个合理的实例（返回Json对象）
    /// </summary>
    /// <param name="type">要生成的类型</param>
    /// <returns>AI生成的Json对象</returns>
    public async Task<object> GenerateClass(Type type)
    {
        if (type == null)
        {
            GD.Print("type为null");
            return null;
        }

        var constructors = type.GetConstructors();
        GD.Print($"类型: {type.FullName} 的构造方法结构如下：");

        StringBuilder ctorInfoBuilder = new StringBuilder();
        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            string paramStr = string.Join(", ", System.Array.ConvertAll(parameters, p => $"{p.ParameterType.Name} {p.Name}"));

            // 枚举参数详细列出所有可能的值
            foreach (var param in parameters)
            {
                if (param.ParameterType.IsEnum)
                {
                    var enumType = param.ParameterType;
                    var enumValues = Enum.GetValues(enumType);
                    paramStr += $"\n参数 {param.Name} 的可能值: " + string.Join(", ", enumValues);
                }
            }
            GD.Print($"构造方法: {type.Name}({paramStr})");
            ctorInfoBuilder.AppendLine($"{type.Name}({paramStr})");
        }

        // 构造AI提示信息
        string prompt = $"请根据以下构造方法结构生成一个听上去合理的实例: {type.FullName}\n" +
                        $"构造方法结构:\n{ctorInfoBuilder}\n" +
                        "请挑选一个可以实现的构造方法，返回一个json字符串，每一个字段名称和类型与构造方法结构一一对应，若存在枚举，则返回对应的int index，若存在非基础类，请返回{\"status\":\"failed\"}，不要有任何其他内容";
    
        Array<ConversationMessage> messages = new Array<ConversationMessage>
        {
            new ConversationMessage("user", prompt)
        };

        // 注意：AICommunication建议作为单例或依赖注入，这里为演示直接new
        var aiCommunication = GameManager.Instance.CharacterManager.GetAICommunication();
        var resultTask = aiCommunication.GetResponse(messages);
        var result = await resultTask;
        GD.Print($"AI生成的数据: {result}");
        // 解析AI返回的json字符串
        Variant json = new Variant();
        try
        {
            json = Json.ParseString(result);
            GD.Print($"AI生成的数据: {json}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"解析AI返回的json时出错: {ex.Message}");
            GD.PrintErr($"原始AI返回: {result}");
            return null;
        }
        return json;
    }
}