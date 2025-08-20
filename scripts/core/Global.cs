using Godot;
using System;
using System.Collections.Generic;
using YamlDotNet.Core.Events;

public partial class Global : Node
{
    private static Global _instance;
    public static Global Instance => _instance;
    public Dictionary<string,Variant> globalVariables = new Dictionary<string, Variant>();

    public int Score { get; set; } = 0;
    public HashSet<string> visitedCameraLockers = new HashSet<string>();
    public String sceneLoaded = "test.tscn";
    public PackedScene backScene;


    public static Global GetInstance()
    {
        return _instance;
    }
    public override void _EnterTree()
    {
        if (_instance != null)
        {
            QueueFree();

        }
        else
        {
            _instance = this;
            GD.Print("Global instance created");
        }
    }

        
    }
