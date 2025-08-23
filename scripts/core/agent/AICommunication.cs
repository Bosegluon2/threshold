using Godot;
using Godot.Collections;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Threshold.Core.Agent
{
    /// <summary>
    /// AI通信模块，负责与DashScope API通信
    /// </summary>
    public partial class AICommunication : Node
    {
        private const string API_URL = "https://dashscope.aliyuncs.com/compatible-mode/v1"; //任何支持function-call模型的提供商都可以
        private const string API_KEY = "YOUR_API_KEY"; // 请替换为你的API_KEY
        
        private const string MODEL = "qwen-plus"; //任何支持function-call模型的提供商都可以
        private System.Net.Http.HttpClient httpClient;
        private FunctionManager functionManager;
        private Agent currentAgent; // 当前Agent引用
        
        // 敏感回复检测
        private static readonly string[] SENSITIVE_RESPONSES = {
            "由于法律法规拒绝回答",
            "抱歉，我不能回答这个问题",
            "这个问题我无法回答",
            "我不能提供这个信息",
            "根据规定，我不能回答"
        };
        
        [Signal]
        public delegate void ResponseReceivedEventHandler(string response);
        
        [Signal]
        public delegate void ErrorOccurredEventHandler(string error);
        
        public override void _Ready()
        {
            httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");
            httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            // 注意：Content-Type 头应由 HttpContent 设置，而不是 DefaultRequestHeaders
        }
        
        /// <summary>
        /// 设置函数管理器
        /// </summary>
        public void SetFunctionManager(FunctionManager manager)
        {
            functionManager = manager;
        }
        
        /// <summary>
        /// 设置当前Agent
        /// </summary>
        public void SetCurrentAgent(Agent agent)
        {
            currentAgent = agent;
        }
        
        /// <summary>
        /// 获取当前Agent
        /// </summary>
        public Agent GetCurrentAgent()
        {
            return currentAgent;
        }
        
        /// <summary>
        /// 检测敏感回复
        /// </summary>
        private bool IsSensitiveResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return false;
                
            foreach (var sensitive in SENSITIVE_RESPONSES)
            {
                if (response.Contains(sensitive))
                {
                    GD.PrintErr($"检测到敏感回复: {sensitive}");
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 回退对话历史
        /// </summary>
        private void RollbackConversation(Array<ConversationMessage> conversationHistory)
        {
            if (conversationHistory != null && conversationHistory.Count > 0)
            {
                // 移除最后一条消息
                conversationHistory.RemoveAt(conversationHistory.Count - 1);
                GD.Print("已回退对话历史，移除最后一条消息");
            }
        }
        
        public override void _ExitTree()
        {
            httpClient?.Dispose();
        }
        
        /// <summary>
        /// 发送消息到AI
        /// </summary>
        /// <param name="message">用户消息</param>
        /// <param name="agent">发送消息的Agent</param>
        /// <param name="targetAgent">目标Agent（私聊时）或null（房间对话时）</param>
        /// <param name="conversationHistory">对话历史</param>
        /// <param name="isRoomMessage">是否为房间消息（true=房间对话，false=私聊）</param>
        public async void SendMessage(string message, Agent agent, Agent targetAgent, Godot.Collections.Array<ConversationMessage> conversationHistory = null, bool isRoomMessage = false)
        {
            try
            {
                // 设置当前Agent引用（用于函数调用等）
                SetCurrentAgent(agent);
                
                var response = await SendMessageAsync(message, agent, targetAgent, conversationHistory, isRoomMessage);
                
                // 将AI回复添加到发送消息的Agent的对话历史中
                if (agent != null && !string.IsNullOrEmpty(response))
                {
                    // 如果是房间消息，广播给房间内的其他Agent
                    if (isRoomMessage)
                    {
                        BroadcastToRoom(agent, response);
                    }
                    
                    // 直接通知发送消息的Agent，传递发送者Agent参数
                    agent.EmitSignal(Agent.SignalName.AIResponseReceived, response, agent);
                }
                else
                {
                    GD.PrintErr("无法处理AI回复：agent为null或response为空");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"AI通信错误: {ex.Message}");
                if (agent != null)
                {
                    agent.EmitSignal(nameof(Agent.ErrorOccurred), ex.Message);
                }
            }
        }

        /// <summary>
        /// 广播消息到房间内的其他Agent
        /// </summary>
        private void BroadcastToRoom(Agent speaker, string message)
        {
            if (speaker?.CurrentPlace == null) return;
            
            var speakerName = speaker.Character?.Name ?? "未知角色";
            var roomId = speaker.CurrentPlace.Id;
            var roomName = speaker.CurrentPlace.Name;
            
            GD.Print($"=== 房间广播 ===");
            GD.Print($"发言者: {speakerName} (ID: {speaker.AgentId})");
            GD.Print($"房间: {roomName} (ID: {roomId})");
            GD.Print($"消息内容: {message}");
            
            // 获取同一房间内的其他Agent
            var agentsInSameRoom = speaker.GetAgentsInSameRoom();
            var broadcastCount = 0;
            
            foreach (var otherAgent in agentsInSameRoom)
            {
                // 创建特殊标记的消息
                var heardMessage = $"[你听见{speakerName}在{roomName}中大声说了一句] {message}";
                var heardConversationMessage = new ConversationMessage(heardMessage, "system", otherAgent.AgentId);
                
                // 添加到其他Agent的对话历史中
                otherAgent.AddMessageToHistory(heardConversationMessage);
                
                GD.Print($"广播给: {otherAgent.Character?.Name ?? "未知角色"} (ID: {otherAgent.AgentId})");
                broadcastCount++;
            }
            
            GD.Print($"房间广播完成，共广播给 {broadcastCount} 个Agent");
        }
        /// <summary>
        /// 临时实现：发送消息到AI（无关Agent，仅用于测试/临时用途）
        /// </summary>
        public async Task<string> GetResponse(Array<ConversationMessage> messages)
        {
            // 构造请求体
            var requestData = new Godot.Collections.Dictionary
            {
                ["model"] = MODEL,
                ["messages"] = SimpleBuildMessages(messages),
                ["temperature"] = 0.7,
                ["max_tokens"] = 1000,
                ["stream"] = false
            };

            string jsonContent = Json.Stringify(requestData);
            GD.Print($"GetResponse请求数据: {jsonContent}");
            // 发送HTTP请求
            using (var request = new HttpRequestMessage(HttpMethod.Post, API_URL+"/chat/completions"))
            {
                request.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                if (!string.IsNullOrEmpty(API_KEY))
                {
                    request.Headers.Add("Authorization", $"Bearer {API_KEY}");
                }

                try
                {
                    var response = await httpClient.SendAsync(request);
                    var responseString = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        GD.PrintErr($"GetResponse请求失败: {response.StatusCode} {responseString}");
                        return "";
                    }
                    // 解析AI回复
                    var json = Json.ParseString(responseString);
                    if (json.AsGodotDictionary() is Dictionary dict && dict.ContainsKey("choices"))
                    {
                        var choices = dict["choices"].AsGodotArray();
                        if (choices != null && choices.Count > 0)
                        {
                            var first = choices[0].AsGodotDictionary();
                            if (first != null && first.ContainsKey("message"))
                            {
                                var message = first["message"].AsGodotDictionary();
                                if (message != null && message.ContainsKey("content"))
                                {
                                    return message["content"].ToString();
                                }
                            }
                        }
                    }
                    return "GetResponse异常: 未知错误";
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"GetResponse异常: {ex.Message}");
                    return "GetResponse异常: " + ex.Message;
                }
            }
        }
        /// <summary>
        /// 异步发送消息
        /// </summary>
        private async Task<string> SendMessageAsync(string message, Agent agent, Agent targetAgent, Array<ConversationMessage> conversationHistory, bool isRoomMessage)
        {
            GD.Print("=== 开始发送AI消息 ===");
            GD.Print($"用户消息: {message}");
            GD.Print($"角色: {agent.AgentName}");
            GD.Print($"对话历史长度: {conversationHistory?.Count ?? 0}");

            // 使用Godot的Dictionary和Array构建请求体
            var requestData = new Godot.Collections.Dictionary
            {
                ["model"] = MODEL,
                ["messages"] = BuildMessages(message, agent, targetAgent, conversationHistory, isRoomMessage),
                ["temperature"] = 0.7,
                ["max_tokens"] = 1000,
                ["stream"] = false
            };

            // 如果支持函数调用，添加函数定义
            if (functionManager != null && functionManager.HasFunctions())
            {
                var functions = functionManager.GetAvailableFunctions();
                var functionArray = new Array<Variant>();

                GD.Print($"支持函数调用，可用函数数量: {functions.Count}");
                var functionNames = new Array<string>();
                foreach (var function in functions)
                {
                    var functionDef = function.GetOpenAIFormat();
                    functionArray.Add(functionDef);
                    functionNames.Add(function.Name);
                }
                if (functionNames.Count > 0)
                {
                    GD.Print(string.Join("、", functionNames));
                }

                requestData["functions"] = functionArray;
                requestData["function_call"] = "auto";
                GD.Print("已启用函数调用功能");
            }
            else
            {
                GD.Print("函数调用功能未启用");
            }

            // 使用Godot的JSON API进行序列化
            string jsonContent = Json.Stringify(requestData);
            // GD.Print($"请求数据: {jsonContent}");

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            // Content-Type 头由 StringContent 构造函数自动设置，无需手动添加

            GD.Print("正在发送HTTP请求...");
            var response = await httpClient.PostAsync(API_URL+"/chat/completions", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            GD.Print($"HTTP响应状态: {response.StatusCode}");
            GD.Print($"HTTP响应内容: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                GD.PrintErr($"API请求失败: {response.StatusCode} - {responseContent}");
                throw new Exception($"API请求失败: {response.StatusCode} - {responseContent}");
            }

            // 解析AI响应，使用Godot的JSON API
            var jsonResult = Json.ParseString(responseContent);
            if (jsonResult.AsGodotDictionary() is not Dictionary resultDict)
            {
                return "抱歉，我没有收到有效的回复。";
            }
            if (!resultDict.ContainsKey("choices"))
            {
                return "抱歉，我没有收到有效的回复。";
            }
            var choices = resultDict["choices"].AsGodotArray();
            if (choices == null || choices.Count == 0)
            {
                return "抱歉，我没有收到有效的回复。";
            }
            var firstChoice = choices[0].AsGodotDictionary();
            if (firstChoice == null || !firstChoice.ContainsKey("message"))
            {
                return "抱歉，我没有收到有效的回复。";
            }
            var messageDict = firstChoice["message"].AsGodotDictionary();

            // 检查是否有函数调用
            if (messageDict.ContainsKey("function_call"))
            {
                return await HandleFunctionCall(messageDict, agent, conversationHistory);
            }

            if (messageDict == null || !messageDict.ContainsKey("content"))
            {
                return "抱歉，我没有收到有效的回复。";
            }

            var aiResponse = messageDict["content"].AsString() ?? "抱歉，我没有收到有效的回复。";

            // 检测敏感回复
            if (IsSensitiveResponse(aiResponse))
            {
                GD.PrintErr("检测到敏感回复，正在回退对话历史...");
                RollbackConversation(conversationHistory);
                return "抱歉，我无法回答这个问题。请换个方式提问。";
            }

            return aiResponse;
        }
        private Array<Variant> SimpleBuildMessages(Array<ConversationMessage> conversations)
        {
            var messages = new Array<Variant>();
            foreach (var conversation in conversations)
            {
                messages.Add(new Godot.Collections.Dictionary
                {
                    ["role"] = conversation.Role,
                    ["content"] = conversation.Content
                });
            }
            return messages;
        }
        /// <summary>
        /// 构建消息列表
        /// </summary>
        private Array<Variant> BuildMessages(string message, Agent agent, Agent targetAgent,Array<ConversationMessage> conversationHistory, bool isRoomMessage)
        {
            var messages = new Array<Variant>();
            
            // 根据消息类型构建不同的系统提示词
            string systemContent;
            if (isRoomMessage)
            {
                // 房间对话：在公共房间中公开说话
                var roomName = agent.CurrentPlace?.Name ?? "未知房间";
                var roomId = agent.CurrentPlace?.Id ?? "未知";

                systemContent = "【房间对话指令】\n" +
                    $"你现在扮演角色：{agent.Character.Name} (ID: {agent.Character.Id})\n" +
                    $"你正在房间 {roomName} (ID: {roomId}) 中公开说话，所有人都能听到\n\n" +
                    $"{agent.Character.GetRoleplayPrompt()}\n\n" +
                    "【房间对话规则】\n" +
                    "1. 这是公开对话，房间内的其他角色都能听到\n" +
                    "2. 请使用适合公开场合的语气和内容\n" +
                    "3. 可以与其他角色互动，但要注意场合\n" +
                    "4. 当前世界观：\n" +
                    $"{GameManager.Instance.WorldABC.GetWorldInfo()}\n\n";
            }
            else
            {
                // 私聊：与特定角色对话
                systemContent = "【私聊指令】\n" +
                    $"你现在扮演角色：{agent.Character.Name} (ID: {agent.Character.Id})\n" +
                    $"你正在与 {targetAgent.Character.Name} 进行私密对话\n\n" +
                    $"{agent.Character.GetRoleplayPrompt()}\n\n" +
                    "【私聊规则】\n" +
                    "1. 这是私密对话，只有你和对方能听到\n" +
                    "2. 可以分享更私密的信息和想法\n" +
                    "3. 当前用户身份信息：\n" +
                    $"{agent.GetCharacterInfo()}\n\n" +
                    "4. 对方角色信息：\n" +
                    $"{targetAgent.GetCharacterInfo()}\n\n" +
                    "5. 当前世界观：\n" +
                    $"{GameManager.Instance.WorldABC.GetWorldInfo()}\n\n" +
                    "【关键规则】\n" +
                    "1. 当需要获取角色信息时，请使用以下函数：\n" +
                    "   - get_self_information: 获取你自己的信息（不需要额外参数）\n" +
                    "   - get_other_information: 获取其他角色的信息（需要target_id或target_name）\n\n" + 
                    "2. 只有当需要获取你自己的信息时，才使用get_self_information\n\n" +
                    
                    "【重要提醒】\n" +
                    $"- 你始终是 {agent.Character.Name}，不是 {targetAgent.Character.Name}\n" +
                    $"- 请严格按照 {agent.Character.Name} 的性格和背景来回应\n" +
                    "- 任何信息都不要无中生有，均需要有根据\n\n例如物品、百科、世界观、角色信息等，均需要通过function-call获取\n\n";
            }
            
            GD.Print($"=== 构建系统提示词 ===");
            GD.Print($"消息类型: {(isRoomMessage ? "房间对话" : "私聊")}");
            GD.Print($"当前Agent: {agent.AgentName} (ID: {agent.AgentId})");
            GD.Print($"当前角色: {agent.Character.Name} (ID: {agent.Character.Id})");
            if (!isRoomMessage && targetAgent != null)
            {
                GD.Print($"目标Agent: {targetAgent.AgentName} (ID: {targetAgent.AgentId})");
                GD.Print($"目标角色: {targetAgent.Character.Name} (ID: {targetAgent.Character.Id})");
            }
            GD.Print($"对话历史长度: {conversationHistory?.Count ?? 0}");
            
            // 添加系统角色设定
            messages.Add(new Godot.Collections.Dictionary
            {
                ["role"] = "system",
                ["content"] = systemContent
            });
            
            // 添加对话历史（只包含当前Agent的消息）
            if (conversationHistory != null)
            {
                foreach (var historyMessage in conversationHistory)
                {
                    // 只添加当前Agent的消息，避免其他Agent的消息干扰
                    if (historyMessage.AgentId == agent.AgentId)
                    {
                        messages.Add(new Godot.Collections.Dictionary
                        {
                            ["role"] = historyMessage.Role,
                            ["content"] = historyMessage.Content
                        });
                    }
                }
            }
            
            // 添加当前用户消息
            messages.Add(new Godot.Collections.Dictionary
            {
                ["role"] = "user",
                ["content"] = message
            });
            
            GD.Print($"构建了 {messages.Count} 条消息，准备发送第二次请求...");
            
            return messages;
        }
        
        /// <summary>
        /// 处理函数调用
        /// </summary>
        private async Task<string> HandleFunctionCall(Dictionary messageDict, Agent agent, Array<ConversationMessage> conversationHistory)
        {
            GD.Print("=== 开始处理函数调用 ===");
            
            if (functionManager == null)
            {
                GD.PrintErr("函数管理器未初始化");
                return "抱歉，函数调用功能不可用。";
            }
            
            var functionCall = messageDict["function_call"].AsGodotDictionary();
            if (functionCall == null)
            {
                GD.PrintErr("函数调用格式无效");
                return "抱歉，函数调用格式无效。";
            }
            
            var functionName = functionCall.ContainsKey("name") ? functionCall["name"].AsString() : "";
            var arguments = functionCall.ContainsKey("arguments") ? functionCall["arguments"].AsString() : "{}";
            
            GD.Print($"函数名称: {functionName}");
            GD.Print($"函数参数: {arguments}");
            
            if (string.IsNullOrEmpty(functionName))
            {
                GD.PrintErr("函数名称为空");
                return "抱歉，函数名称无效。";
            }
            
            try
            {
                GD.Print("正在解析函数参数...");
                // 解析函数参数
                var argsJson = Json.ParseString(arguments);
                var argsDict = argsJson.AsGodotDictionary() ?? new Dictionary();
                
                GD.Print($"解析后的参数: {Json.Stringify(argsDict)}");
                
                GD.Print($"正在执行函数: {functionName}");
                
                // 准备额外参数
                var extraParams = new System.Collections.Generic.Dictionary<string, object>();
                if (agent != null)
                {
                    extraParams["current_agent"] = agent;
                    extraParams["current_agent_id"] = agent.AgentId;
                    
                    // 安全地获取位置ID
                    if (agent.CurrentPlace != null)
                    {
                        extraParams["current_place_id"] = agent.CurrentPlace.Id;
                        GD.Print($"添加位置参数: {agent.CurrentPlace.Id}");
                    }
                    else
                    {
                        extraParams["current_position_id"] = "未分配";
                        GD.Print("警告: Agent没有分配位置");
                    }
                    
                    
                    GD.Print($"添加额外参数: current_agent={agent.AgentName}, agent_id={agent.AgentId}");
                }
                else
                {
                    GD.Print("警告: Agent为空，无法提供额外参数");
                }
                
                // 执行函数
                GD.Print($"准备执行函数: {functionName}");
                GD.Print($"functionManager状态: {(functionManager != null ? "可用" : "null")}");
                GD.Print($"argsDict状态: {(argsDict != null ? $"可用，包含{argsDict.Count}个参数" : "null")}");
                GD.Print($"extraParams状态: {(extraParams != null ? $"可用，包含{extraParams.Count}个参数" : "null")}");
                
                if (functionManager == null)
                {
                    GD.PrintErr("functionManager为null，无法执行函数");
                    return "函数管理器未初始化，无法执行函数";
                }
                
                // 检查函数是否已注册
                var availableFunctions = functionManager.GetAvailableFunctions();
                GD.Print($"可用函数数量: {availableFunctions?.Count ?? 0}");
                if (availableFunctions != null)
                {
                    foreach (var func in availableFunctions)
                    {
                        GD.Print($"  - {func.Name}: {func.Description}");
                    }
                }
                
                // 检查函数是否存在
                var hasFunction = functionManager.HasFunctions();
                GD.Print($"函数管理器是否有函数: {hasFunction}");
                
                // 检查参数Dictionary
                GD.Print($"argsDict类型: {argsDict?.GetType().Name ?? "null"}");
                GD.Print($"argsDict内容: {Json.Stringify(argsDict)}");
                
                // 尝试创建FunctionCall
                GD.Print("准备创建FunctionCall...");
                FunctionCall functionCallObj = null;
                try
                {
                    functionCallObj = new FunctionCall(functionName, argsDict);
                    GD.Print($"✅ FunctionCall创建成功: Name={functionCallObj.Name}, Arguments数量={functionCallObj.Arguments?.Count ?? 0}");
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"❌ FunctionCall创建失败: {ex.Message}");
                    GD.PrintErr($"堆栈跟踪: {ex.StackTrace}");
                    return $"创建函数调用对象失败: {ex.Message}";
                }
                
                FunctionResult functionResult = null;
                try
                {
                    functionResult = functionManager.ExecuteFunction(functionCallObj, extraParams);
                    
                    if (functionResult == null)
                    {
                        GD.PrintErr("函数执行返回null结果");
                        return "函数执行失败：返回了null结果";
                    }
                    
                    GD.Print($"函数执行结果: 成功={functionResult.Success}, 内容长度={functionResult.Content?.Length ?? 0}");
                    if (!functionResult.Success)
                    {
                        GD.PrintErr($"函数执行失败: {functionResult.ErrorMessage}");
                        return $"函数执行失败: {functionResult.ErrorMessage}";
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"执行函数时发生异常: {ex.Message}");
                    GD.PrintErr($"异常堆栈: {ex.StackTrace}");
                    return $"执行函数时发生错误: {ex.Message}";
                }
                
                if (functionResult != null && functionResult.Success)
                {
                    GD.Print("函数执行成功，正在构建函数结果消息...");
                    // 将函数结果发送回AI，获取最终回复
                    var functionMessage = new Dictionary
                    {
                        ["role"] = "function",
                        ["name"] = functionName,
                        ["content"] = functionResult.Content
                    };
                    
                    GD.Print($"函数结果消息: {Json.Stringify(functionMessage)}");
                    
                    // 构建新的消息列表，包含函数结果
                    var newMessages = new Array<Variant>();
                    
                    // 添加系统角色设定
                    newMessages.Add(new Dictionary
                    {
                        ["role"] = "system",
                        ["content"] = agent.Character.GetRoleplayPrompt()
                    });
                    
                    // 添加对话历史
                    if (conversationHistory != null)
                    {
                        foreach (var historyMessage in conversationHistory)
                        {
                            newMessages.Add(new Dictionary
                            {
                                ["role"] = historyMessage.Role,
                                ["content"] = historyMessage.Content
                            });
                        }
                    }
                    
                    // 添加用户消息
                    newMessages.Add(new Dictionary
                    {
                        ["role"] = "user",
                        ["content"] = "请根据函数调用的结果，以角色身份给出合适的回复。"
                    });
                    
                    // 添加函数结果
                    newMessages.Add(functionMessage);
                    
                    GD.Print($"构建了 {newMessages.Count} 条消息，准备发送第二次请求...");
                    
                    // 发送第二次请求获取最终回复
                    var finalRequestData = new Dictionary
                    {
                        ["model"] = MODEL,
                        ["messages"] = newMessages,
                        ["temperature"] = 0.7,
                        ["max_tokens"] = 1000,
                        ["stream"] = false
                    };
                    
                    var finalJsonContent = Json.Stringify(finalRequestData);
                    // GD.Print($"第二次请求数据: {finalJsonContent}");
                    
                    var finalHttpContent = new StringContent(finalJsonContent, Encoding.UTF8, "application/json");
                    
                    GD.Print("正在发送第二次请求...");
                    var finalResponse = await httpClient.PostAsync(API_URL+"/chat/completions", finalHttpContent);
                    var finalResponseContent = await finalResponse.Content.ReadAsStringAsync();
                    
                    GD.Print($"第二次请求响应状态: {finalResponse.StatusCode}");
                    GD.Print($"第二次请求响应内容: {finalResponseContent}");
                    
                    if (!finalResponse.IsSuccessStatusCode)
                    {
                        GD.PrintErr($"第二次请求失败: {finalResponse.StatusCode} - {finalResponseContent}");
                        return $"函数执行成功，但获取最终回复失败: {finalResponse.StatusCode}";
                    }
                    
                    // 解析最终回复
                    var finalJsonResult = Json.ParseString(finalResponseContent);
                    if (finalJsonResult.AsGodotDictionary() is not Dictionary finalResultDict)
                    {
                        GD.PrintErr("无法解析第二次请求的响应");
                        return "函数执行成功，但无法解析最终回复。";
                    }
                    
                    var finalChoices = finalResultDict["choices"].AsGodotArray();
                    if (finalChoices == null || finalChoices.Count == 0)
                    {
                        GD.PrintErr("第二次请求响应中没有choices");
                        return "函数执行成功，但无法获取最终回复。";
                    }
                    
                    var finalFirstChoice = finalChoices[0].AsGodotDictionary();
                    if (finalFirstChoice == null || !finalFirstChoice.ContainsKey("message"))
                    {
                        GD.PrintErr("第二次请求响应中没有message");
                        return "函数执行成功，但无法获取最终回复。";
                    }
                    
                    var finalMessageDict = finalFirstChoice["message"].AsGodotDictionary();
                    if (finalMessageDict == null || !finalMessageDict.ContainsKey("content"))
                    {
                        GD.PrintErr("第二次请求响应中没有content");
                        return "函数执行成功，但无法获取最终回复。";
                    }
                    
                    var finalResponseText = finalMessageDict["content"].AsString();
                    GD.Print($"获取到最终回复，长度: {finalResponseText?.Length ?? 0}");
                    GD.Print("=== 函数调用处理完成 ===");
                    
                    return finalResponseText ?? "函数执行成功，但无法获取最终回复。";
                }
                else
                {
                    return $"函数执行失败: {functionResult.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                return $"执行函数时发生错误: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// 对话消息结构
    /// </summary>
    public partial class ConversationMessage : Resource
    {
        public string Content { get; set; }
        public string Role { get; set; }
        public string AgentId { get; set; }  // 新增：标识发言者的Agent ID
        public DateTime Timestamp { get; set; }

        public ConversationMessage(string content, string role, string agentId = "")
        {
            Content = content;
            Role = role;
            AgentId = agentId;
            Timestamp = DateTime.Now;
        }
        public override string ToString()
        {
            return $"[{Role}] {Content} (AgentId: {AgentId})";
        }
    }
    
    // 以下AIResponse等结构体已不再直接用于JSON反序列化，保留以兼容其他用法
    public partial class AIResponse:Resource
    {
        public Array<Choice> choices { get; set; }
        public Usage usage { get; set; }
    }
    
    public partial class Choice:Resource
    {
        public Message message { get; set; }
        public int index { get; set; }
        public string finish_reason { get; set; }
    }
    
    public partial class Message:Resource
    {
        public string role { get; set; }
        public string content { get; set; }
    }
    
    public partial class Usage:Resource
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }
}
