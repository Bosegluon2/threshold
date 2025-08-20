using Godot;
using System;

public partial class MapControl : Node3D
{
	Button backButton;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Fader.Instance.FadeIn(1.0f);
		backButton=GetNode<Button>("Control/Back");
		backButton.Pressed+=()=>{
			Fader.Instance.FadeOut(1.0f,Callable.From(()=>{
				GetTree().ChangeSceneToPacked(Global.Instance.backScene);
			}));
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
