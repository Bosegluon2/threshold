using Godot;
using Godot.Collections;
using Threshold.Core;
using Threshold.Core.Enums;
using System;
using System.Threading.Tasks;
using System.Threading;

public partial class Rollover : Control
{
	// Emoji Clock

	public TimeOfDay targetTimeOfDay;
	public int[] timeOfDay = [6 * 60, 12 * 60, 18 * 60, 24 * 60];
	public double flickerDuration = 6.0;
	public double transitionDuration = 4.0;
	public bool showSeparator = true;
	public bool isFlickering = false;
	public float separatorInterval = 0.2f;
	private double separatorTimer = 0.0;
	private int finalTime = 0;
	private bool isThreadCompleted = false; // 新增：标记线程是否完成
	private int lastRandom; // 移到类级别
	private int currentRandom; // 移到类级别

	public override async void _Ready()
	{
		if (Global.Instance.globalVariables.ContainsKey("RolloverRandom"))
		{
			lastRandom = (int)Global.Instance.globalVariables["RolloverRandom"];
		}
		else
		{
			lastRandom = new Random().Next(-40, 40);
		}
		Global.Instance.globalVariables["RolloverRandom"] = new Random().Next(-40, 40);

		currentRandom = (int)Global.Instance.globalVariables["RolloverRandom"];

		Fader.Instance.FadeIn(1.0f);
		targetTimeOfDay = TimeUtils.GetTimeOfDay(GameManager.Instance.CurrentTurn + 1);

		// 使用CallDeferred确保在主线程中执行Step
		CallDeferred(nameof(ExecuteStep));
	}

	// 新增：在主线程中执行Step的方法
	private void ExecuteStep()
	{
		GameManager.Instance.Step();
		isThreadCompleted = true; // 标记线程完成

		GD.Print("targetTimeOfDay: " + targetTimeOfDay);
		var tween = CreateTween();
		int index = (int)targetTimeOfDay - 1;
		if (index < 0) index = timeOfDay.Length - 1;
		int from = timeOfDay[index] + lastRandom + new Random().Next(20, 30);
		int to = timeOfDay[(int)targetTimeOfDay] + currentRandom;
		if (to < from)
		{
			to = to + 24 * 60;
		}
		finalTime = to;
		tween.TweenMethod(Callable.From((int i) => Flicker(i)), from, to, flickerDuration).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		tween.TweenCallback(Callable.From(() => isFlickering = true));
		tween.TweenMethod(Callable.From((int i) => Flicker(i)), to, to, transitionDuration);
		tween.TweenCallback(Callable.From(CheckAndCallOnFinish)); // 改为检查条件的方法

		separatorTimer = 0.0;
	}

	// 新增：检查线程和tween是否都完成的方法
	private void CheckAndCallOnFinish()
	{
		if (isThreadCompleted)
		{
			OnFinish();
		}
	}

	public override void _Process(double delta)
	{
		separatorTimer += delta;
		double interval = isFlickering ? separatorInterval * 5.0 : separatorInterval;
		if (separatorTimer >= interval)
		{
			showSeparator = !showSeparator;
			separatorTimer = 0.0;
		}

		// OnFinish后持续闪烁最终时间
		if (isFlickering)
		{
			Flicker(finalTime);
		}
	}

	public void Flicker(int time)
	{
		time %= 24 * 60;
		int hour = time / 60;
		int minute = time % 60;
		GetNode<Label>("Sep").Visible = showSeparator;
		GetNode<Label>("Timer/Hour").Text = hour.ToString();
		GetNode<Label>("Timer/Minute").Text = minute.ToString("D2");
	}
	public void OnFinish()
	{
		GD.Print("当前时间段: " + TimeUtils.GetTimeOfDay(GameManager.Instance.CurrentTurn));
		TimeOfDay currentTimeOfDay = TimeUtils.GetTimeOfDay(GameManager.Instance.CurrentTurn);
		switch (currentTimeOfDay)
		{
			case TimeOfDay.Morning:
			case TimeOfDay.Evening:
				// 早晨和傍晚都进入AssignMission场景
				var assignMission = FastLoader.Instance.files["AssignMission"];
				Fader.Instance.FadeOut(1.0f, Callable.From(() =>
				{
					GetTree().ChangeSceneToPacked(assignMission);
				}));
				break;
			case TimeOfDay.Noon:
				GD.Print("执行中午流程");
				// 议会时间
				var committee = FastLoader.Instance.files["Committee"];
				Fader.Instance.FadeOut(1.0f, Callable.From(() =>
				{
					GetTree().ChangeSceneToPacked(committee);
				}));
				break;
			case TimeOfDay.Midnight:
				GD.Print("执行午夜流程");
				var midnightEvent = FastLoader.Instance.files["MidnightEvent"];
				Fader.Instance.FadeOut(1.0f, Callable.From(() =>
				{
					GetTree().ChangeSceneToPacked(midnightEvent);
				}));
				break;
			default:
				GD.Print("未知时间段: " + currentTimeOfDay);
				break;
		}
	}
	
	
}
