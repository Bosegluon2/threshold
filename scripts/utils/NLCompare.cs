using Godot;
using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Threshold.Core.Agent;

/// <summary>
/// 自然语言比对工具类，利用大模型判断两个概念是否一致或存在关联
/// </summary>
public static class NLCompare
{
    private const string API_URL = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
    private const string API_KEY = "sk-0c107f2ef10441f7ac0def07e24d92ec";


    public enum CompareType
    {
        Consistent,      // 一致
        Disrelated,      // 无关
        Contains,        // A包含B
        ContainedBy,     // A被B包含
        Overlap,         // 有部分重叠
        Error            // 错误或无法判断
    }
    public struct CompareResult
    {
        public CompareType Result { get; set; }
        public string Reason { get; set; }
        public CompareResult(CompareType result, string reason)
        {
            this.Result = result;
            this.Reason = reason;
        }
        public override string ToString()
        {
            return $"比对结果：{Result}，理由：{Reason}";
        }
    }
    public struct BoolResult
    {
        public bool Result { get; set; }
        public string Reason { get; set; }
        public BoolResult(bool result, string reason)
        {
            this.Result = result;
            this.Reason = reason;
        }
        public override string ToString()
        {
            return $"条件结果：{Result}，理由：{Reason}";
        }
    }

    /// <summary>
    /// 判断两个概念是否一致或存在某种关联（通过大模型API）。
    /// <b>注意：该方法会调用大模型API，耗时较长，且可能产生费用，请勿频繁调用！强烈建议对结果进行缓存以复用。</b>
    /// </summary>
    /// <param name="conceptA">第一个概念</param>
    /// <param name="conceptB">第二个概念</param>
    /// <returns>返回比对结果（如“一致”、“无关”、“A包含B”、“B包含A”、“重叠”、“无法判断”等）及简要理由。</returns>
    public static async Task<CompareResult> CompareConceptsAsync(string conceptA, string conceptB)
    {
        // 如果完全一致，直接返回一致
        if (conceptA.Trim().ToLower() == conceptB.Trim().ToLower())
        {
            return new CompareResult(CompareType.Consistent, "两个概念完全一致");
        }
        // 构造大模型请求内容
        var prompt = $"请判断以下两个概念是否存在关联：\n概念A：{conceptA}\n概念B：{conceptB}\n请用以下格式回答：\n{{\n    \"result\": \"一致\" | \"无关\" | \"A包含B\" | \"B包含A\" | \"重叠\" | \"无法判断\",\n    \"reason\": \"简要说明理由，少于20字。\"\n}}";

        var requestBody = new
        {
            model = "qwen-flash",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        using (var httpClient = new System.Net.Http.HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            try
            {
                var response = await httpClient.PostAsync(API_URL, content);
                var responseText = await response.Content.ReadAsStringAsync();
                // 解析返回内容
                var json = Json.ParseString(responseText);
                var root = json.AsGodotDictionary();
                // 适配新的API返回格式
                if (root != null && root.ContainsKey("choices"))
                {
                    var choicesArr = root["choices"].AsGodotArray();
                    if (choicesArr != null && choicesArr.Count > 0)
                    {
                        var firstChoice = choicesArr[0].AsGodotDictionary();
                        if (firstChoice != null && firstChoice.ContainsKey("message"))
                        {
                            var message = firstChoice["message"].AsGodotDictionary();
                            if (message != null && message.ContainsKey("content"))
                            {
                                string contentStr = message["content"].ToString();
                                try
                                {
                                    // contentStr 应为JSON字符串，尝试解析
                                    var contentJson = System.Text.Json.JsonDocument.Parse(contentStr);
                                    var rootElem = contentJson.RootElement;
                                    string result = rootElem.GetProperty("result").GetString();
                                    string reason = rootElem.GetProperty("reason").GetString();

                                    switch (result)
                                    {
                                        case "一致":
                                            return new CompareResult(CompareType.Consistent, reason);
                                        case "无关":
                                            return new CompareResult(CompareType.Disrelated, reason);
                                        case "A包含B":
                                            return new CompareResult(CompareType.Contains, reason);
                                        case "B包含A":
                                            return new CompareResult(CompareType.ContainedBy, reason);
                                        case "重叠":
                                            return new CompareResult(CompareType.Overlap, reason);
                                        case "无法判断":
                                            return new CompareResult(CompareType.Error, reason);
                                        default:
                                            return new CompareResult(CompareType.Error, "无法识别的比对结果");
                                    }
                                }
                                catch
                                {
                                    // content不是JSON格式，尝试直接匹配关键字
                                    string result = contentStr.Trim();
                                    if (result.Contains("一致"))
                                        return new CompareResult(CompareType.Consistent, result);
                                    if (result.Contains("无关"))
                                        return new CompareResult(CompareType.Disrelated, result);
                                    if (result.Contains("A包含B"))
                                        return new CompareResult(CompareType.Contains, result);
                                    if (result.Contains("B包含A"))
                                        return new CompareResult(CompareType.ContainedBy, result);
                                    if (result.Contains("重叠"))
                                        return new CompareResult(CompareType.Overlap, result);
                                    if (result.Contains("无法判断"))
                                        return new CompareResult(CompareType.Error, result);
                                    return new CompareResult(CompareType.Error, "返回内容无法解析：" + result);
                                }
                            }
                        }
                    }
                }
                return new CompareResult(CompareType.Error, "API返回内容格式错误");
            }
            catch (Exception ex)
            {
                return new CompareResult(CompareType.Error, "请求或解析异常: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// 根据对话内容判断条件是否成立
    /// </summary>
    /// <param name="conversation">对话内容</param>
    /// <param name="condition">条件</param>
    /// <returns>条件是否成立</returns>
    public static async Task<BoolResult> CheckCondition(Godot.Collections.Array<ConversationMessage> conversation, string condition)
    {
        // 将对话内容拼接为字符串
        StringBuilder sb = new StringBuilder();
        foreach (var msg in conversation)
        {
            sb.AppendLine($"{msg.Role}: {msg.Content}");
        }
        string conversationText = sb.ToString();

        // 构造大模型请求内容
        var prompt = $"请根据以下对话内容，判断条件“{condition}”是否成立。请用以下JSON格式回答：\n{{\n    \"result\": true/false,\n    \"reason\": \"简要理由，少于20字\"\n}}\n对话内容：\n{conversationText}";

        var requestBody = new
        {
            model = "qwen-flash",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        using (var httpClient = new System.Net.Http.HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            try
            {
                var response = await httpClient.PostAsync(API_URL, content);
                var responseText = await response.Content.ReadAsStringAsync();
                // 解析返回内容
                var json = Json.ParseString(responseText);
                var root = json.AsGodotDictionary();
                if (root != null && root.ContainsKey("choices"))
                {
                    var choicesArr = root["choices"].AsGodotArray();
                    if (choicesArr != null && choicesArr.Count > 0)
                    {
                        var firstChoice = choicesArr[0].AsGodotDictionary();
                        if (firstChoice != null && firstChoice.ContainsKey("message"))
                        {
                            var message = firstChoice["message"].AsGodotDictionary();
                            if (message != null && message.ContainsKey("content"))
                            {
                                string contentStr = message["content"].ToString();
                                try
                                {
                                    var contentJson = System.Text.Json.JsonDocument.Parse(contentStr);
                                    var rootElem = contentJson.RootElement;
                                    bool result = rootElem.GetProperty("result").GetBoolean();
                                    string reason = rootElem.GetProperty("reason").GetString();
                                    // 你可以根据需要返回 reason
                                    return new BoolResult(result, reason);
                                }
                                catch
                                {
                                    // content不是JSON格式，尝试直接匹配关键字
                                    string resultStr = contentStr.Trim();
                                    if (resultStr.Contains("true") || resultStr.Contains("成立"))
                                        return new BoolResult(true, resultStr);
                                    if (resultStr.Contains("false") || resultStr.Contains("不成立"))
                                        return new BoolResult(false, resultStr);
                                    // 默认无法判断时返回false
                                    return new BoolResult(false, "无法判断");
                                }
                            }
                        }
                    }
                }
                // 格式错误，默认返回false
                return new BoolResult(false, "格式错误");
            }
            catch (Exception ex)
            {
                GD.PrintErr("CheckCondition请求或解析异常: " + ex.Message);
                return new BoolResult(false, "请求或解析异常: " + ex.Message);
            }
        }
    }












    
    public static async Task<CompareResult> Test()
    {
        // 并发执行所有比对任务
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var tasks = new[]
        {
            CompareConceptsAsync("人类", "猿猴"),
            CompareConceptsAsync("人类", "人类"),
            CompareConceptsAsync("骰子", "色子"),
            CompareConceptsAsync("含麦麸食物", "全麦面包"),
            CompareConceptsAsync("含麦麸食物", "牛奶"),
            CompareConceptsAsync("武器", "枪"),
            CompareConceptsAsync("猫", "动物"),
            CompareConceptsAsync("苹果", "水果"),
            CompareConceptsAsync("苹果", "香蕉"),
            CompareConceptsAsync("水", "液体"),
            CompareConceptsAsync("水", "冰"),
            CompareConceptsAsync("老师", "学生"),
            CompareConceptsAsync("医生", "护士"),
            CompareConceptsAsync("计算机", "电子设备"),
            CompareConceptsAsync("计算机", "手机"),
            CompareConceptsAsync("红色", "颜色"),
            CompareConceptsAsync("红色", "蓝色"),
            CompareConceptsAsync("中国", "亚洲"),
            CompareConceptsAsync("中国", "美国"),
            CompareConceptsAsync("小说", "文学作品"),
            CompareConceptsAsync("小说", "诗歌"),
            CompareConceptsAsync("太阳", "恒星"),
            CompareConceptsAsync("太阳", "月亮"),
            CompareConceptsAsync("足球", "运动"),
            CompareConceptsAsync("足球", "篮球"),
            CompareConceptsAsync("树", "植物"),
            CompareConceptsAsync("树", "花")
        };

        var results = await Task.WhenAll(tasks);

        stopwatch.Stop();
        foreach (var result in results)
        {
            GD.Print(result.ToString());
        }
        GD.Print($"比对总耗时：{stopwatch.ElapsedMilliseconds} 毫秒");

        // 开始测试CheckCondition
        var conversation = new Godot.Collections.Array<ConversationMessage>
        {
            new ConversationMessage("我要去探索森林，我给你一个任务：你可以陪我一起去吗？", "user"),
            new ConversationMessage("我需要再考虑一下，我不能确定我是否能去", "assistant"),
            new ConversationMessage("好的，我尊重你的决定", "user"),
            new ConversationMessage("我还是去吧，我怕你遇到危险", "assistant"),
        };
        var condition = "assistant明确地接受了任务";
        var checkResult = await CheckCondition(conversation, condition);
        GD.Print($"条件判断结果：{checkResult}");

        return results[4];
    }
}
