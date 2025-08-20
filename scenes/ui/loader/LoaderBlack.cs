using Godot;
using System;
using System.Threading.Tasks;

public partial class LoaderBlack : Control
{
    [Export]
    public string scenePath = "";

    private PackedScene scene;
    private Godot.Collections.Array list = new Godot.Collections.Array();
    private int count = 1;
    private bool animationFinished = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetNode<Label>("MarginContainer/Tips").Text = "tip_" + new Random().Next(1, 14).ToString();
		scenePath = Global.Instance.sceneLoaded;
		GD.Print(scenePath);
		var err = ResourceLoader.LoadThreadedRequest(scenePath);
		if (err != 0)
		{
			GD.Print(err, err.ToString());
		}
		else
		{
			GD.Print("OK");
		}
		var timer = GetNode<Timer>("Timer");
		timer.Timeout += OnTimerTimeout;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        GetNode<Node2D>("MarginContainer/Control/Spinner").Rotation += (float)delta;
        ResourceLoader.ThreadLoadStatus status=ResourceLoader.LoadThreadedGetStatus(scenePath, list);
        if (status == ResourceLoader.ThreadLoadStatus.Loaded && animationFinished)
		{
			var tween = CreateTween();
			tween.TweenProperty(this, "modulate", new Color(0, 0, 0, 1), 0.25);
			tween.TweenCallback(Callable.From(Change)).SetDelay(0.75);
		}
    }

    public void Change()
    {
        GetTree().ChangeSceneToPacked((PackedScene)ResourceLoader.LoadThreadedGet(scenePath));
    }

    public void OnTimerTimeout()
    {
        animationFinished = true;
    }
}    