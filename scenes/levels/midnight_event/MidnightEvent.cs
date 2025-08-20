using Godot;
using Godot.Collections;
using System;
using System.Threading.Tasks;
using Threshold.Core;
using Threshold.Core.Agent;

public partial class MidnightEvent : Control
{
	public Array<Label> eventLabels;
	private Random random = new Random();

	// 事件变量
	private string[] possibleEvents = new string[]
	{
		"今晚无事发生。",
		"远处传来一声猫叫，但很快归于寂静。",
		"风吹过营地，带来一丝不安。",
		"有人在梦中低语，但无人应答。"
	};

	public override async void _Ready()
	{
		Fader.Instance.FadeIn(1.0f);
		eventLabels = new Array<Label>();
		foreach (var child in GetNode<HBoxContainer>("Fonts").GetChildren())
		{
			if (child is Label)
			{
				eventLabels.Add(child as Label);
			}
		}
		var communication = GameManager.Instance.CharacterManager.GetAICommunication();
		var message = new Array<ConversationMessage>
		{
			new ConversationMessage("一小句莫能两可富有哲理比较抽象，有关午夜的语言，不要超过10个字", "user", "dummy")
		};
		string result = await communication.GetResponse(message);
		await GoStart(result);

		// 随机选择一个事件描述
		string statusMessage = GetRandomEventMessage();
		await GoStartStatus(statusMessage);

		AfterMessage();
	}

	private string GetRandomEventMessage()
	{
		// 80%概率为“无事发生”，20%概率为其他描述
		int roll = random.Next(0, 100);
		if (roll < 80)
		{
			return "今晚无事发生。";
		}
		else
		{
			// 随机选取除“今晚无事发生。”以外的事件
			int idx;
			do
			{
				idx = random.Next(0, possibleEvents.Length);
			} while (possibleEvents[idx] == "今晚无事发生。");
			return possibleEvents[idx];
		}
	}

	public async System.Threading.Tasks.Task GoStart(string message)
	{
		for (int a = 0; a < message.Length; a++)
		{
			var randomSelect = random.Next(0, eventLabels.Count);
			var selectLabel = eventLabels[randomSelect].Duplicate() as Label;
			selectLabel.Text = message[a].ToString();
			GetNode<HFlowContainer>("V/Message").AddChild(selectLabel);
			await System.Threading.Tasks.Task.Delay(50);
		}
		await System.Threading.Tasks.Task.Delay(2000);
	}
	public async System.Threading.Tasks.Task GoStartStatus(string message)
	{
		for (int a = 0; a < message.Length; a++)
		{
			var randomSelect = random.Next(0, eventLabels.Count);
			var selectLabel = eventLabels[randomSelect].Duplicate() as Label;
			selectLabel.Text = message[a].ToString();
			GetNode<HFlowContainer>("V/StatusMessage").AddChild(selectLabel);
			await System.Threading.Tasks.Task.Delay(200);
		}
		await System.Threading.Tasks.Task.Delay(5000);
	}
	public void AfterMessage()
	{
		Fader.Instance.FadeOut(1.0f, Callable.From(() =>
		{
			GetTree().ChangeSceneToPacked(FastLoader.Instance.files["Rollover"]);
		}));
	}
	public override void _Process(double delta)
	{
	}
}
