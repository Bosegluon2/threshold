using Godot;
using System;

public partial class SfxManager : Node
{
	//Singleton
	public static SfxManager Instance { get; private set; }
	public override void _EnterTree()
	{
		Instance = this;
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	public void PlaySfx(string sfxName, float fromPosition = 0.0f)
	{
		var sfx = GetNode<AudioStreamPlayer>(sfxName);
		sfx.Play(fromPosition);
	}
	public AudioStreamPlayer GetSfx(string sfxName)
	{
		return GetNode<AudioStreamPlayer>(sfxName);
	}
}
