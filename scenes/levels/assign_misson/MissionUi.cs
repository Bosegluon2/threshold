using Godot;
using Threshold.Core;
using Threshold.Core.Agent;
using System;
using System.Linq;
using System.Runtime.Serialization;
using Threshold.UI.MissionAssignment;

public partial class MissionUi : Control
{
	// 当节点第一次进入场景树时调用
	Label statusLabel;
	RichTextLabel assistantLabel;
	Label infoLabel;
	Label userLabel;
	Agent selectedAgent;
	Button messageSendButton;
	Button missionAssignmentButton;
	Button mapButton;
	VBoxContainer talkPanel;
	Button nextTurnButton;
	TextEdit messageTextEdit;
	CharacterButton CharacterButtonTemplate;
	[Export]
	public MissionAssignmentUI missionAssignmentUI;
	public override void _Ready()
	{
		Fader.Instance.FadeIn(1.0f);
		int current_turn = GameManager.Instance.CurrentTurn;
		string missionTitle;
		var timeOfDay = TimeUtils.GetTimeOfDay(current_turn);
		if (timeOfDay == Threshold.Core.Enums.TimeOfDay.Morning)
		{
			missionTitle = "晨间任务分配";
		}
		else if (timeOfDay == Threshold.Core.Enums.TimeOfDay.Evening)
		{
			missionTitle = "傍晚任务分配";
		}
		else
		{
			missionTitle = "错误时间，游戏出错，提交issue";
		}

		GetNode<Label>("Info").Text = missionTitle;
		statusLabel = GetNode<Label>("S/S/Talk/Split/TalkPanel/V/CharacterStatus");
		statusLabel.Text = "讯息未连接。";

		talkPanel = GetNode<VBoxContainer>("S/S/Talk/Split/TalkPanel/V/S/Content");
		assistantLabel = (RichTextLabel)talkPanel.GetNode("Assistant").Duplicate();
		infoLabel = (Label)talkPanel.GetNode("Info").Duplicate();
		userLabel = (Label)talkPanel.GetNode("User").Duplicate();
		talkPanel.GetNode("Assistant").QueueFree();
		talkPanel.GetNode("Info").QueueFree();
		talkPanel.GetNode("User").QueueFree();

		nextTurnButton = GetNode<Button>("Next");
		nextTurnButton.Pressed += () =>
		{
			Fader.Instance.FadeOut(1.0f, Callable.From(() =>
			{
				GetTree().ChangeSceneToPacked(FastLoader.Instance.files["Rollover"]);
			}));
		};

		messageTextEdit = GetNode<TextEdit>("S/S/Talk/Split/TextEdit");
		messageTextEdit.PlaceholderText = "请输入讯息";

		messageSendButton = GetNode<Button>("S/S/Talk/Split/TextEdit/H/Send");
		messageSendButton.Pressed += () => SendMessage();
		missionAssignmentUI = GetNode<MissionAssignmentUI>("S/MissionAssignmentUI");
		missionAssignmentUI.MissionAssigned += () =>
		{
			InitTalkPanel();
			// 刷新角色按钮列表
			RefreshCharacterList();
		};

		missionAssignmentButton = GetNode<Button>("S/S/Talk/Split/TextEdit/H/Assign");
		missionAssignmentButton.Pressed += () => missionAssignmentUI.AddAgent(selectedAgent);
		// 尝试获取角色列表容器
		var characterList = GetNodeOrNull<HBoxContainer>("S/S/S/H");
		if (characterList == null)
		{
			GD.PrintErr("未找到角色列表节点 S/S/S/H，请检查场景结构。");
			return;
		}

		// 检查容器下是否有子节点
		if (characterList.GetChildCount() == 0)
		{
			GD.PrintErr("角色列表容器下没有子节点，无法复制角色按钮模板。");
			return;
		}

		// 尝试获取第一个角色按钮模板
		var templateButton = characterList.GetChild(0) as CharacterButton;
		if (templateButton == null)
		{
			GD.PrintErr("第一个子节点不是 CharacterButton 类型，请检查节点类型。");
			return;
		}

		// 复制模板并移除原模板
		CharacterButtonTemplate = (CharacterButton)templateButton.Duplicate();
		templateButton.QueueFree();

		// 检查GameManager和CharacterManager是否可用
		if (GameManager.Instance == null)
		{
			GD.PrintErr("GameManager实例不可用");
			return;
		}

		if (GameManager.Instance.CharacterManager == null)
		{
			GD.PrintErr("CharacterManager不可用");
			return;
		}

		GD.Print("AliveCharacters: " + GameManager.Instance.CharacterManager.AliveCharacters.Count);

		// 打印所有角色信息，排除player
		var validCharacters = 0;
		var nullCharacterAgents = 0;

		foreach (var agent in GameManager.Instance.CharacterManager.AliveCharacters)
		{
			try
			{
				if (agent == null)
				{
					GD.PrintErr("发现null agent");
					continue;
				}

				if (agent.Character == null)
				{
					GD.PrintErr($"Agent {agent.AgentId} ({agent.AgentName}) 的Character为null");
					nullCharacterAgents++;
					continue;
				}


				GD.Print($"有效角色: {agent.Character.Name} (ID: {agent.Character.Id})");
				validCharacters++;
			}
			catch (Exception e)
			{
				GD.PrintErr($"处理角色时出错: {e.Message}");
				GD.PrintErr($"堆栈: {e.StackTrace}");
			}
		}

		GD.Print($"统计: 总角色={GameManager.Instance.CharacterManager.AliveCharacters.Count}, " +
				 $"有效角色={validCharacters}, " +
				 $"nullCharacter={nullCharacterAgents}");

		// 使用新的方法创建角色按钮
		CreateCharacterButtons(characterList);
	}

	void RefreshCharacterList()
	{
		// 清空角色按钮列表
		var characterList = GetNodeOrNull<HBoxContainer>("S/S/S/H");
		if (characterList == null)
		{
			GD.PrintErr("未找到角色列表节点 S/S/S/H，请检查场景结构。");
			return;
		}
		
		// 清空所有子节点
		foreach (Node n in characterList.GetChildren())
		{
			n.QueueFree();
		}
		
		// 重新创建角色按钮列表
		CreateCharacterButtons(characterList);
	}

    private void CreateCharacterButtons(HBoxContainer characterList)
    {
		if (characterList == null)
		{
			GD.PrintErr("角色列表容器为null");
			return;
		}
		
		// 检查GameManager和CharacterManager是否可用
		if (GameManager.Instance?.CharacterManager == null)
		{
			GD.PrintErr("GameManager或CharacterManager不可用");
			return;
		}

		GD.Print("刷新角色列表，当前存活角色数量: " + GameManager.Instance.CharacterManager.AliveCharacters.Count);

		// 遍历所有存活角色，创建按钮
		foreach (var agent in GameManager.Instance.CharacterManager.AliveCharacters)
		{
			try
			{
				if (agent?.Character == null)
				{
					GD.PrintErr($"跳过无效的agent: {agent?.AgentId}");
					continue;
				}

				GD.Print($"创建角色按钮: {agent.Character.Name} (ID: {agent.Character.Id})");
				var newCharacterButton = (CharacterButton)CharacterButtonTemplate.Duplicate();
				newCharacterButton.SetCharacterId(agent.Character.Id);
				
				// 设置按钮状态和颜色
				if (agent.IsDead())
				{
					newCharacterButton.Disabled = true;
					newCharacterButton.Modulate = new Color(0.8f, 0.1f, 0.1f, 1f); // 死亡：猩红色
				}
				else if (agent.IsActive)
				{
					newCharacterButton.Disabled = true;
					newCharacterButton.Modulate = new Color(0.55f, 0.55f, 0.55f, 1); // 正忙：淡灰色
				}
				else
				{
					newCharacterButton.Disabled = false;
					newCharacterButton.Modulate = new Color(1, 1, 1, 1); // 空闲：正常白色
				}
				
				// 绑定点击事件
				newCharacterButton.Pressed += () =>
				{
					selectedAgent = GameManager.Instance.CharacterManager.GetCharacterById(newCharacterButton.CharacterId);
					if (selectedAgent != null)
					{
						statusLabel.Text = "当前讯息：" + selectedAgent.Character.Name;
						InitTalkPanel();
					}
				};
				
				characterList.AddChild(newCharacterButton);
			}
			catch (Exception e)
			{
				GD.PrintErr($"为角色 {agent?.Character?.Name} 创建按钮时出错: {e.Message}");
			}
		}
	}

    void InitTalkPanel()
	{
		// 先断开旧的信号连接（如果存在）
		if (selectedAgent != null)
		{
			// 断开所有可能的信号连接
			selectedAgent.AIResponseReceived -= AddAssistantMessage;
			GD.Print($"已断开 {selectedAgent.AgentName} 的信号连接");
		}
		
		var currentTurnMessage = (Label)infoLabel.Duplicate();
		foreach (Node n in talkPanel.GetChildren())
		{
			GD.Print("QueueFree: " + n.Name);
			n.QueueFree();
		}
		talkPanel.AddChild(currentTurnMessage);
		
		GD.Print($"{selectedAgent.AgentName} 的对话历史：");
		foreach (var message in selectedAgent.ConversationHistory)
		{
			GD.Print( message.ToString());
		}
		GD.Print("以上。");
		
		foreach (ConversationMessage message in selectedAgent.GetMessageHistory())
		{
			if (message.Role == "user")
			{
				GD.Print("AddUserMessage: " + message.Content);
				var currentUserLabel = (Label)userLabel.Duplicate();
				currentUserLabel.Text = message.Content;
				talkPanel.AddChild(currentUserLabel);
			}
			else if (message.Role == "assistant")
			{
				GD.Print("AddAssistantMessage: " + message.Content);
				var currentAssistantLabel = (RichTextLabel)assistantLabel.Duplicate();
				currentAssistantLabel.Text = message.Content;
				talkPanel.AddChild(currentAssistantLabel);
			}
		}
		
		currentTurnMessage.Text = "当前回合：" + GameManager.Instance.CurrentTurn + "，" + TimeUtils.GetTimeOfDay(GameManager.Instance.CurrentTurn);
		messageTextEdit.Text = "";
		
		// 连接新的信号（只连接当前选中的Agent）
		if (selectedAgent != null)
		{
			selectedAgent.AIResponseReceived += AddAssistantMessage;
			GD.Print($"已连接 {selectedAgent.AgentName} 的信号：AIResponseReceived");
		}
	}

	void SendMessage()
	{
		if (messageTextEdit.Text == "" || selectedAgent == null)
		{
			return;
		}
		GD.Print(messageTextEdit.Text);
		
		// 在UI中显示用户消息（Agent.SendMessageToTarget会自动添加到历史）
		var currentUserLabel = (Label)userLabel.Duplicate();
		currentUserLabel.Text = messageTextEdit.Text;
		talkPanel.AddChild(currentUserLabel);

		// 使用新的方法发送私聊消息
		selectedAgent.SendMessageToTarget(messageTextEdit.Text, GameManager.Instance.CharacterManager.GetPlayerAgent());
		messageTextEdit.Text = "";
	}

	void SpeakInRoom()
	{
		if (messageTextEdit.Text == "" || selectedAgent == null)
		{
			return;
		}
		GD.Print($"[房间对话] {messageTextEdit.Text}");
		
		// 在UI中显示房间消息（Agent.SpeakInRoom会自动添加到历史）
		var currentUserLabel = (Label)userLabel.Duplicate();
		currentUserLabel.Text = $"[房间] {messageTextEdit.Text}";
		talkPanel.AddChild(currentUserLabel);

		// 使用房间对话方法
		selectedAgent.SpeakInRoom(messageTextEdit.Text);
		messageTextEdit.Text = "";
	}
	void AddAssistantMessage(string message, Agent senderAgent)
	{
		if (selectedAgent == null) 
		{
			GD.PrintErr("AddAssistantMessage: selectedAgent 为 null");
			return;
		}
		
		// 检查发送者Agent是否与当前选中的Agent匹配
		if (senderAgent == null || senderAgent.AgentId != selectedAgent.AgentId)
		{
			GD.Print($"AddAssistantMessage: 忽略来自 {senderAgent?.AgentName ?? "未知"} 的回复，当前选中: {selectedAgent.AgentName}");
			return;
		}
		
		GD.Print($"=== AddAssistantMessage 开始 ===");
		GD.Print($"当前选中的Agent: {selectedAgent.AgentName} (ID: {selectedAgent.AgentId})");
		GD.Print($"消息内容: {message}");
		GD.Print($"发送者Agent: {senderAgent.AgentName} (ID: {senderAgent.AgentId})");
		
		// 创建AI消息并添加到Agent历史
		var aiMessage = new ConversationMessage(message, "assistant", selectedAgent.AgentId);
		selectedAgent.AddMessageToHistory(aiMessage);
		
		GD.Print($"消息已添加到 {selectedAgent.AgentName} 的历史，当前历史长度: {selectedAgent.ConversationHistory.Count}");
		
		// 在UI中显示消息
		var currentAssistantLabel = (RichTextLabel)assistantLabel.Duplicate();
		currentAssistantLabel.Text = message;
		talkPanel.AddChild(currentAssistantLabel);
		
		GD.Print($"AddAssistantMessage: {message} (已添加到 {selectedAgent.AgentName} 的历史)");
		GD.Print($"=== AddAssistantMessage 完成 ===");
	}

	// 每帧调用
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("multiline_enter"))
		{
			// Shift+Enter 触发房间对话
			SpeakInRoom();
		}
		else if (Input.IsActionJustPressed("ui_accept"))
		{
			// Enter 触发私聊
			SendMessage();
		}
	}
}
