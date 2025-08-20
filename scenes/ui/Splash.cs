using Godot;
using System;

public partial class Splash : Control
{
	VideoStreamPlayer videoPlayer;
	bool[] loaded = new bool[2];
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var err = ResourceLoader.LoadThreadedRequest("res://scenes/main.tscn");
		if (err != 0)
		{
			GD.Print(err, err.ToString());
		}
		else
		{
			GD.Print("OK");
		}
		Fader.Instance.FadeIn(3.0f);
		videoPlayer = GetNode<VideoStreamPlayer>("VideoStreamPlayer");
		videoPlayer.Finished += OnVideoFinished;
		videoPlayer.Play();
	}
	public void OnVideoFinished()
	{
		AnimationPlayer animationPlayer = GetNode<AnimationPlayer>("Player");
		animationPlayer.Play("intro");
		animationPlayer.AnimationFinished += (name) => OnAnimationFinished();
	}
	public void OnAnimationFinished()
	{
		Fader.Instance.FadeOut(0.1f, Callable.From(() =>
		{
			loaded[0] = true;
		})
		);
		
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (loaded[0] && loaded[1])
		{
			GetTree().ChangeSceneToPacked((PackedScene)ResourceLoader.LoadThreadedGet("res://scenes/main.tscn"));
		}



		// check if the scene is loaded
		if (ResourceLoader.LoadThreadedGetStatus("res://scenes/main.tscn") == ResourceLoader.ThreadLoadStatus.Loaded)
		{
			loaded[1] = true;
		}
	}
}
