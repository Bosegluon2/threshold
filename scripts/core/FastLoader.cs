using Godot;
using Godot.Collections;
using System;

public partial class FastLoader : Node
{
	public Dictionary<string, PackedScene> files = new Dictionary<string, PackedScene>();
	
	//Singleton
	public static FastLoader Instance { get; private set; }
	public override void _EnterTree()
	{
		Instance = this;
		files["UiLayer"] = GD.Load<PackedScene>("res://scenes/ui/ui_layer/ui_layer.tscn");
		files["LoaderBlack"] = GD.Load<PackedScene>("res://scenes/ui/loader/loader_black.tscn");
		files["Committee"] = GD.Load<PackedScene>("res://scenes/levels/committee_scene.tscn");
		files["Rollover"] = GD.Load<PackedScene>("res://scenes/ui/rollover/rollover.tscn");
		files["AssignMission"] = GD.Load<PackedScene>("res://scenes/levels/assign_misson/assign_mission.tscn");
		files["MidnightEvent"] = GD.Load<PackedScene>("res://scenes/levels/midnight_event/midnight_event.tscn");
		files["Preface"] = GD.Load<PackedScene>("res://scenes/levels/preface/preface.tscn");
		files["Map"] = GD.Load<PackedScene>("res://scenes/levels/map/map.tscn");
	}
	public void LoadScene(string scenePath)
	{
		if (files.ContainsKey(scenePath))
		{
			var scene = files[scenePath];
			if (scene != null)
			{
				GetTree().ChangeSceneToPacked(scene);
			}
			else
			{
				GD.PrintErr($"Scene {scenePath} not found in FastLoader.");
			}
		}
		else
		{
			GD.PrintErr($"Scene {scenePath} not registered in FastLoader.");
		}
	}
	public void Goto(string scenePath)
	{
		Global.Instance.sceneLoaded = scenePath;
		if (files.ContainsKey("LoaderBlack"))
		{
			var loaderScene = files["LoaderBlack"];
			GetTree().ChangeSceneToPacked(loaderScene);
		}
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
