using Godot;
using System;

public partial class AnimatedButton : Button
{
	public ColorRect background;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		background = GetNode<ColorRect>("Background");
		MouseEntered += OnMouseEnter;
		MouseExited += OnMouseExit;
		background.Size= new Vector2(0, this.Size.Y);
	}
	public void OnMouseEnter()
	{
		SfxManager.Instance.PlaySfx("Hover");
		
		var tween = GetTree().CreateTween();
		tween.SetParallel();
		tween.TweenProperty(background, "color", new Color(0.1f, 0.1f, 0.1f, 0.8f), 0.2f).SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(background, "size", this.Size, 0.2f).SetTrans(Tween.TransitionType.Cubic);
	}

	public void OnMouseExit()
	{
		var tween = GetTree().CreateTween();
		tween.TweenProperty(background, "color", new Color(1, 1, 1, 1), 0.2f).SetTrans(Tween.TransitionType.Cubic);
				tween.TweenProperty(background, "size", new Vector2(0, this.Size.Y),0.2f).SetTrans(Tween.TransitionType.Cubic);
		}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
