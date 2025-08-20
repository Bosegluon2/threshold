using Godot;
using System;

public partial class Preface : Control
{
	[Export] public float typingSpeed = 0.02f; // 打字速度（秒/字符）
	[Export] public float pauseBetweenLines = 1.0f; // 行间暂停时间
	[Export] public float deleteSpeed = 0.01f; // 删除速度（秒/字符）
	[Export] public float pauseBeforeDelete = 1.5f; // 打完后删除前的暂停时间

	private Label contentLabel;
	private string[] prefaceWords =
	[
		"末日已经降临，真空零点能裂缝撕裂了这片大地，曾经繁华的世界只剩下废墟与荒原。",
	"在高纬度北极圈的冰雪深处，有一座孤立的科研基地——它是这片死寂中最后的灯火。",
	"一次失控的能源实验，将周围数百公里困在扭曲的空间里，与外界彻底隔绝。",
	"救援至少要三十天后才能到达，而物资和能源正在迅速消耗。",
	"你是这座基地的领导者，和另外五名幸存者一起面对未知的威胁与生存挑战。",	
	"陈维博士——冷静而执着的首席科学家，为了科研成果不惜冒险。",
    "艾莉娜·沃尔夫——坚守原则的伦理代表，誓要守护每个人的尊严与生命。",
    "玛克·安德森——退役军医，果断务实，相信活下来才是首要任务。",
    "萨拉·金——年轻的生物学家，理想主义者，渴望用科学连接未知。",
    "维克托·彼得罗夫——政府联络员，擅长平衡各方利益，但心中有自己的盘算。",
	"人类的希望，或许就系在你接下来的每一个决定之上。",
	"“接下来是晨间任务任务分配，我们都需要干些什么？”"
	];

	private int currentLineIndex = 0;
	private int currentCharIndex = 0;
	private bool isTyping = false;
	private bool isDeleting = false;
	private Timer typingTimer;
	private Timer linePauseTimer;
	private Timer deletePauseTimer;
	private string currentDisplayText = "";

	// 完成事件
	[Signal] public delegate void PrefaceFinishedEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Fader.Instance.FadeIn(0.1f);
		// 获取内容标签
		contentLabel = GetNode<Label>("ContentLabel");
		if (contentLabel == null)
		{
			GD.PrintErr("找不到ContentLabel节点！");
			return;
		}

		// 初始化标签
		contentLabel.Text = "";

		// 创建打字机定时器
		typingTimer = new Timer();
		typingTimer.WaitTime = typingSpeed;
		typingTimer.Timeout += OnTypingTimerTimeout;
		AddChild(typingTimer);

		// 创建行间暂停定时器
		linePauseTimer = new Timer();
		linePauseTimer.Timeout += OnLinePauseTimerTimeout;
		AddChild(linePauseTimer);

		// 创建删除前暂停定时器
		deletePauseTimer = new Timer();
		deletePauseTimer.Timeout += OnDeletePauseTimerTimeout;
		AddChild(deletePauseTimer);

		// 开始打字机效果
		StartTyping();
	}

	private void StartTyping()
	{
		isTyping = true;
		isDeleting = false;
		currentLineIndex = 0;
		currentCharIndex = 0;
		currentDisplayText = "";
		contentLabel.Text = "";
		typingTimer.WaitTime = typingSpeed;
		typingTimer.Start();
	}

	private void OnTypingTimerTimeout()
	{
		if (currentLineIndex >= prefaceWords.Length)
		{
			// 所有行都打完了
			FinishTyping();
			return;
		}

		string currentLine = prefaceWords[currentLineIndex];

		if (isTyping && !isDeleting)
		{
			// 打字阶段
			if (currentCharIndex < currentLine.Length)
			{
				// 继续打当前行的字符
				currentDisplayText += currentLine[currentCharIndex];
				contentLabel.Text = currentDisplayText;
				currentCharIndex++;
				typingTimer.Start();
			}
			else
			{
				// 当前行打完了，暂停一下再开始删除
				isTyping = false;
				typingTimer.Stop();
				deletePauseTimer.WaitTime = pauseBeforeDelete;
				deletePauseTimer.Start();
			}
		}
		else if (isDeleting)
		{
			// 删除阶段
			ProcessDeletion();
		}
	}

	private void OnDeletePauseTimerTimeout()
	{
		deletePauseTimer.Stop();

		// 开始删除阶段
		isDeleting = true;
		currentCharIndex = currentDisplayText.Length;
		typingTimer.WaitTime = deleteSpeed;
		typingTimer.Start();
	}

	private void OnLinePauseTimerTimeout()
	{
		linePauseTimer.Stop();

		// 开始下一行
		typingTimer.WaitTime = typingSpeed;
		typingTimer.Start();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// 按空格键跳过打字机效果
		if (Input.IsActionJustPressed("ui_accept") && (isTyping || isDeleting))
		{
			SkipTyping();
		}
	}

	private void ProcessDeletion()
	{
		if (currentCharIndex > 0)
		{
			// 继续删除字符
			currentDisplayText = currentDisplayText.Substring(0, currentDisplayText.Length - 1);
			contentLabel.Text = currentDisplayText;
			currentCharIndex--;
			typingTimer.Start();
		}
		else
		{
			// 当前行删除完了，移动到下一行
			isTyping = true;
			isDeleting = false;
			currentLineIndex++;
			currentCharIndex = 0;
			currentDisplayText = "";
			contentLabel.Text = "";

			if (currentLineIndex < prefaceWords.Length)
			{
				// 暂停一下再开始下一行
				typingTimer.Stop();
				linePauseTimer.WaitTime = pauseBetweenLines;
				linePauseTimer.Start();
			}
			else
			{
				// 所有行都打完了
				FinishTyping();
			}
		}
	}

	private void FinishTyping()
	{
		isTyping = false;
		isDeleting = false;
		typingTimer.Stop();
		linePauseTimer.Stop();
		deletePauseTimer.Stop();

		GD.Print("前言打字机效果完成！");

		// 触发完成事件
		EmitSignal(SignalName.PrefaceFinished);
		var assignMission = FastLoader.Instance.files["AssignMission"];
		Fader.Instance.FadeOut(1.0f, Callable.From(() =>
		{
			GetTree().ChangeSceneToPacked(assignMission);
		}));
	}

	// 手动跳过打字机效果
	public void SkipTyping()
	{
		if (isTyping || isDeleting)
		{
			typingTimer.Stop();
			linePauseTimer.Stop();
			deletePauseTimer.Stop();

			// 直接显示所有内容
			contentLabel.Text = string.Join("\n", prefaceWords);

			FinishTyping();
		}
	}

	// 重新开始打字机效果
	public void RestartTyping()
	{
		StartTyping();
	}

	public override void _ExitTree()
	{
		// 清理定时器
		if (typingTimer != null)
		{
			typingTimer.Timeout -= OnTypingTimerTimeout;
			typingTimer.QueueFree();
		}

		if (linePauseTimer != null)
		{
			linePauseTimer.Timeout -= OnLinePauseTimerTimeout;
			linePauseTimer.QueueFree();
		}

		if (deletePauseTimer != null)
		{
			deletePauseTimer.Timeout -= OnDeletePauseTimerTimeout;
			deletePauseTimer.QueueFree();
		}
	}
}
