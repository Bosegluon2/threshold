using Godot;
using Godot.Collections;
using Threshold.Core;
using Threshold.Core.Data;
using System;

public partial class Map : Node3D
{
	PackedScene SelectProxyScene = ResourceLoader.Load<PackedScene>("res://prefabs/select/select_proxy.tscn");
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
		GD.Print("所有可用位置");
		Array<Place> places = GameManager.Instance.Library.GetAllPlaces();
		GD.Print(places);
		foreach (var place in GameManager.Instance.Library.GetAllPlaces())
		{
			var selectProxy = SelectProxyScene.Instantiate<SelectProxy>();
			selectProxy.Place = place;

			AddChild(selectProxy);
			GD.Print("位置", place.Position);
			selectProxy.GlobalPosition = new Vector3(place.Position.X, place.Position.Z/5.0f, place.Position.Y)/2;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
