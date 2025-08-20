using Godot;
using Godot.Collections;
using Threshold.Core;
using Threshold.Core.Agent;
using System;
using System.Threading.Tasks;

public partial class CommitteeUi : Control
{
	public Label statusLabel;
	public float totalValue;
	public ProgressBar approveProgress;
	public ProgressBar rejectProgress;
	public Panel statusPanel;
	public Panel policyPanel;
	public int currentTurn = 0;
	public int maxTurns = 1;

	public override void _Ready()
	{
		Fader.Instance.FadeIn(1.0f);
		currentTurn = 1;
		maxTurns = GameManager.Instance.CommitteeManager.GetMaxTurns();

		SetTurns(currentTurn);

		approveProgress = GetNode<ProgressBar>("MC/Approve");
		rejectProgress = GetNode<ProgressBar>("MC/Reject");
		statusPanel = GetNode<Panel>("MC/StatusPanel");
		policyPanel = GetNode<Panel>("MC/PolicyPanel");
		statusLabel = GetNode<Label>("MC/StatusPanel/V/H/Status");

		GetNode<Button>("MC/PolicyPanel/Submit").Pressed += async () =>
		{
			string title = GetNode<LineEdit>("MC/PolicyPanel/S/V/TitleInput").Text;
			string content = GetNode<TextEdit>("MC/PolicyPanel/S/V/ContentInput").Text;
			string modifier = GetNode<TextEdit>("MC/PolicyPanel/S/V/ModifierInput").Text;
			Array<string> modifiers = new Array<string>(modifier.Split("\n"));
			if (title != "" && content != "" && modifiers.Count > 0)
			{
				// 检查标题唯一性
				if (GameManager.Instance.CommitteeManager.GetPolicy(title) != null)
				{
					GD.Print("政策标题已存在");
					return;
				}
				var policy = new Policy(title, content, modifiers, 10, 1);
				SetPolicy(policy);
				GD.Print(policy);
				await GameManager.Instance.CommitteeManager.VotePolicy(policy, true);
			}
			else
			{
				GD.Print("请输入完整信息");
			}
		};

		GetNode<Button>("MC/NextRound").Visible = false;
		GetNode<Button>("MC/NextRound").Pressed += () =>
		{
			Fader.Instance.FadeOut(1.0f, Callable.From(() =>
			{
				GetTree().ChangeSceneToPacked(FastLoader.Instance.files["Rollover"]);
			}));
		};
	}

	public override void _Process(double delta)
	{
	}

	public void Reset()
	{
		SetStatus("休庭");
		totalValue = GameManager.Instance.CommitteeManager.GetTotalVoteWeight();
		
		if (approveProgress != null && IsInstanceValid(approveProgress))
		{
			approveProgress.Value = 0;
			approveProgress.MaxValue = totalValue;
		}
		
		if (rejectProgress != null && IsInstanceValid(rejectProgress))
		{
			rejectProgress.Value = 0;
			rejectProgress.MaxValue = totalValue;
		}

		// 检查statusPanel是否仍然有效
		if (statusPanel != null && IsInstanceValid(statusPanel))
		{
			var messagesContainer = statusPanel.GetNode<VBoxContainer>("V/Messages/V");
			if (messagesContainer != null && IsInstanceValid(messagesContainer))
			{
				var children = messagesContainer.GetChildren();
				foreach (var child in children)
				{
					child.QueueFree();
				}
			}
		}

		// 检查policyPanel是否仍然有效
		if (policyPanel != null && IsInstanceValid(policyPanel))
		{
			var titleInput = policyPanel.GetNode<LineEdit>("S/V/TitleInput");
			var contentInput = policyPanel.GetNode<TextEdit>("S/V/ContentInput");
			var modifierInput = policyPanel.GetNode<TextEdit>("S/V/ModifierInput");
			var submitButton = policyPanel.GetNode<Button>("Submit");

			if (titleInput != null && IsInstanceValid(titleInput))
			{
				titleInput.Editable = true;
				titleInput.Text = "";
			}
			if (contentInput != null && IsInstanceValid(contentInput))
			{
				contentInput.Editable = true;
				contentInput.Text = "";
			}
			if (modifierInput != null && IsInstanceValid(modifierInput))
			{
				modifierInput.Editable = true;
				modifierInput.Text = "";
			}
			if (submitButton != null && IsInstanceValid(submitButton))
			{
				submitButton.Disabled = false;
				submitButton.Text = "提交政策";
			}
		}
	}
	public void SetTurns(int turns)
	{
		var turnLabel = GetNode<Label>("MC/Turn");
		if (turnLabel != null && IsInstanceValid(turnLabel))
		{
			turnLabel.Text = $"第{turns}轮";
		}
	}
	public void SetStatus(string status)
	{
		if (statusLabel != null && IsInstanceValid(statusLabel))
		{
			statusLabel.Text = status;
		}
	}

	public void SetPolicy(Policy policy)
	{
		// 检查节点是否仍然有效
		if (policyPanel == null || !IsInstanceValid(policyPanel))
		{
			GD.PrintErr("policyPanel is null or invalid in SetPolicy");
			return;
		}

		var titleInput = policyPanel.GetNode<LineEdit>("S/V/TitleInput");
		var contentInput = policyPanel.GetNode<TextEdit>("S/V/ContentInput");
		var modifierInput = policyPanel.GetNode<TextEdit>("S/V/ModifierInput");
		var submitButton = policyPanel.GetNode<Button>("Submit");

		if (titleInput != null && IsInstanceValid(titleInput)) titleInput.Editable = false;
		if (contentInput != null && IsInstanceValid(contentInput)) contentInput.Editable = false;
		if (modifierInput != null && IsInstanceValid(modifierInput)) modifierInput.Editable = false;
		if (submitButton != null && IsInstanceValid(submitButton))
		{
			submitButton.Disabled = true;
			submitButton.Text = "政策已提交";
		}
	}

	public void AddApprove(Agent agent)
	{
		if (approveProgress != null && IsInstanceValid(approveProgress))
		{
			approveProgress.Value += agent.CurrentVoteWeight;
		}
	}

	public void AddReject(Agent agent)
	{
		if (rejectProgress != null && IsInstanceValid(rejectProgress))
		{
			rejectProgress.Value += agent.CurrentVoteWeight;
		}
	}

	public void AddComment(PolicyPersonalOpinion opinion)
	{
		// 检查节点是否仍然有效
		if (statusPanel == null || !IsInstanceValid(statusPanel))
		{
			GD.PrintErr("statusPanel is null or invalid in AddComment");
			return;
		}

		var comment = statusPanel.GetNode<VBoxContainer>("V/Messages/V");
		if (comment == null || !IsInstanceValid(comment))
		{
			GD.PrintErr("comment container is null or invalid in AddComment");
			return;
		}

		string color = "white";
		switch (opinion.status)
		{
			case PolicyStatusType.Approved:
				color = "green";
				break;
			case PolicyStatusType.Rejected:
				color = "red";
				break;
			case PolicyStatusType.Abstain:
				color = "gray";
				break;
		}
		var label = new RichTextLabel()
		{
			BbcodeEnabled = true,
			FitContent = true,
			Text = $"[font size=12][color=white]{opinion.agent.Character.Name}[/color] [color={color}]{opinion.status.ToString()}[/color] {opinion.reason}[/font]",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
		};
		label.AddThemeFontSizeOverride("font_size", 16);
		comment.AddChild(label);
	}

	public async System.Threading.Tasks.Task Settlement(Policy policy, PolicyStatusType status, bool finish = true)
	{
		SetStatus("投票结束：" + status.ToString());
		GD.Print("Settlement: " + policy.content + " " + status.ToString());
		
		// 检查节点是否仍然有效
		if (statusPanel == null || !IsInstanceValid(statusPanel))
		{
			GD.PrintErr("statusPanel is null or invalid in Settlement");
			return;
		}

		var result = statusPanel.GetNode<Label>("V/H2/Result");
		if (result != null && IsInstanceValid(result))
		{
			result.Text = status.ToString();
		}

		if (status == PolicyStatusType.Approved)
		{
			GameManager.Instance.CommitteeManager.AddPolicy(policy);
		}
		GD.Print("All policy: ");

		foreach (var p in GameManager.Instance.CommitteeManager.GetPolicies())
		{
			GD.Print(p);
		}
		if (finish)
		{
			// 只有在达到最大回合数时才显示"下一轮"按钮
			if (currentTurn < maxTurns)
			{
				await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
				Reset();
				currentTurn++;
				SetTurns(currentTurn);
			}
			else
			{
				SetStatus("议会已结束");
				
				var nextRoundButton = GetNode<Button>("MC/NextRound");
				if (nextRoundButton != null && IsInstanceValid(nextRoundButton))
				{
					nextRoundButton.Visible = true;
				}
			}
		}
	}
}
