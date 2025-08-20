using Godot;
using System;

public partial class UiLayer : CanvasLayer
{

	[Export(PropertyHint.MultilineText)]
	
	public string Message = "Hello, World!";
	[Export(PropertyHint.MultilineText)]
	public string leftButtonMessage = "Left";

	[Export(PropertyHint.MultilineText)]
	public string rightButtonMessage = "Right";

	AnimatedButton leftButton;
	AnimatedButton rightButton;
	Control Modulator;
	[Signal]
	public delegate void ButtonPressedEventHandler(string buttonMessage);
	[Signal]
	public delegate void ButtonReleasedEventHandler(string buttonMessage);
	// Called when the node enters the scene tree for the first time.
	public override void _EnterTree()
	{
		Modulator = GetNode<Control>("Modulator");

		leftButton = GetNode<AnimatedButton>("Modulator/ColorRect/MarginContainer/Back");
		rightButton = GetNode<AnimatedButton>("Modulator/ColorRect/MarginContainer/Confirm");
		leftButton.Text = leftButtonMessage;
		rightButton.Text = rightButtonMessage;
		var label = GetNode<Label>("Modulator/ColorRect/MarginContainer/Label");
		label.Text = Message;
		leftButton.Pressed += OnLeftButtonPressed;
		leftButton.ButtonUp += OnLeftButtonReleased;
		rightButton.Pressed += OnRightButtonPressed;
		rightButton.ButtonUp += OnRightButtonReleased;
	}
	public void ShowUp()
	{
		Modulator.Modulate = new Color(1f, 1f, 1f, 0f);
		this.Show();
		var tween = GetTree().CreateTween();
		tween.TweenProperty(Modulator, "modulate", new Color(1f, 1f, 1f, 1f), 0.5f).SetTrans(Tween.TransitionType.Cubic);

	}
	public void HideDown()
	{
		var tween = GetTree().CreateTween();
		tween.TweenProperty(Modulator, "modulate", new Color(1f, 1f, 1f, 0f), 0.5f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		tween.TweenCallback(
			Callable.From(() =>
			{
				this.Hide();
			})

		);
	}
	public void OnLeftButtonPressed()
	{
		EmitSignal("ButtonPressed", "left");
	}
	public void OnLeftButtonReleased()
	{
		EmitSignal("ButtonReleased", "left");
	}
	public void OnRightButtonPressed()
	{
		EmitSignal("ButtonPressed", "right");
	}
	public void OnRightButtonReleased()
	{
		EmitSignal("ButtonReleased", "right");
	}



	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

