using Godot;
using System;
using Threshold.Core.Data;
using Threshold.Core.Utils;

public partial class CameraNavigator : Node3D
{
	private Vector3 dragStartPosition;
	private Vector3 nodeStartPosition;
	private bool isDragging = false;
	private Node3D cameraNode;
	private float targetZ;
	private Vector3 targetPosition;
	[Export]
	private float navigateRadius = 20.0f;
	[Export]
	private float nearDistance = 5.0f;
	[Export]
	private float farDistance = 30.0f;
	[Export]
	private bool dynamicPitch = false;
	private float targetPitch = MathUtils.DegToRad(-60.0f);
	private HintPanel hintPanel;
	private Selector selector;
	private SelectProxy lastHovered; // 优化hover，lastSelected改为lastHovered
	private string lastHintPlaceId = null; // 记录上次显示的hint id，避免重复刷新

	public override void _EnterTree()
	{
		if (GetTree().CurrentScene.GetNodeOrNull<HintPanel>("Hint") == null)
		{
			PackedScene hintPack = (PackedScene)GD.Load("res://prefabs/select/hint.tscn");
			var hintNode = hintPack.Instantiate<HintPanel>();

			GetTree().CurrentScene.CallDeferred("add_child", hintNode);
			hintNode.Name = "Hint";
			hintPanel = hintNode;
		}
		else
		{
			hintPanel = GetTree().CurrentScene.GetNodeOrNull<HintPanel>("Hint");
		}
		if (GetTree().CurrentScene.GetNodeOrNull<Selector>("Selector") == null)
		{
			PackedScene selectorPack = (PackedScene)GD.Load("res://prefabs/select/selector.tscn");
			var selectorNode = selectorPack.Instantiate<Selector>();

			GetTree().CurrentScene.CallDeferred("add_child", selectorNode);
			selectorNode.Name = "Selector";
			selector = selectorNode;
		}
		else
		{
			selector = GetTree().CurrentScene.GetNodeOrNull<Selector>("Selector");
		}
	}

	public override void _Ready()
	{
		GD.Print(hintPanel);
		GD.Print(selector);
		cameraNode = GetNode<Node3D>("Camera");
		targetZ = cameraNode.Transform.Origin.Z;
		targetPosition = GlobalTransform.Origin;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// 处理缩放
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.WheelUp)
		{
			targetZ = Mathf.Clamp(targetZ - 2.0f, nearDistance, farDistance);
			targetPitch = Mathf.Clamp(targetPitch + 0.03f, MathUtils.DegToRad(-90.0f), MathUtils.DegToRad(-60.0f));
		}
		else if (@event is InputEventMouseButton mouseEventDown && mouseEventDown.ButtonIndex == MouseButton.WheelDown)
		{
			targetZ = Mathf.Clamp(targetZ + 2.0f, nearDistance, farDistance);
			targetPitch = Mathf.Clamp(targetPitch - 0.03f, MathUtils.DegToRad(-90.0f), MathUtils.DegToRad(-60.0f));
		}
	}

	public override void _Process(double delta)
	{
		// 平滑移动摄像机Z轴
		var cameraTransform = cameraNode.Transform;
		cameraTransform.Origin.Z = Mathf.Lerp(cameraTransform.Origin.Z, targetZ, (float)delta * 10);
		cameraNode.Transform = cameraTransform;
		SetRotation(new Vector3(Mathf.Lerp(Rotation.X, targetPitch, (float)delta * 10), Rotation.Y, Rotation.Z));

		// 平滑移动节点位置
		GlobalTransform = new Transform3D(GlobalTransform.Basis, GlobalTransform.Origin.Lerp(targetPosition, (float)delta * 10));

		// 拖拽处理
		if (Input.IsActionJustPressed("right_click"))
		{
			isDragging = true;
			dragStartPosition = new Vector3(GetViewport().GetMousePosition().X, 0, GetViewport().GetMousePosition().Y);
			nodeStartPosition = targetPosition;
		}
		else if (Input.IsActionJustReleased("right_click"))
		{
			isDragging = false;
		}

		if (isDragging)
		{
			var currentMousePosition = GetViewport().GetMousePosition();
			var deltaMouse = (currentMousePosition - new Vector2(dragStartPosition.X, dragStartPosition.Z)) * targetZ * 0.002f;
			targetPosition = nodeStartPosition + new Vector3((float)-deltaMouse.X, 0, (float)-deltaMouse.Y);
			var clampedXZ = new Vector2(targetPosition.X, targetPosition.Z).LimitLength(navigateRadius);
			targetPosition = new Vector3(clampedXZ.X, targetPosition.Y, clampedXZ.Y);
		}

		// 优化hover检测
		if (cameraNode is Camera3D camera)
		{
			var mousePosition = GetViewport().GetMousePosition();
			var rayOrigin = camera.ProjectRayOrigin(mousePosition);
			var rayDirection = camera.ProjectRayNormal(mousePosition) * 100;

			var spaceState = GetWorld3D().DirectSpaceState;
			var param = new PhysicsRayQueryParameters3D
			{
				From = rayOrigin,
				To = rayOrigin + rayDirection,
				CollisionMask = 1 << 0 // 可根据需要调整
			};
			var result = spaceState.IntersectRay(param);

			SelectProxy currentHovered = null;
			string currentPlaceId = null;

			if (result.ContainsKey("collider"))
			{
				if (result["collider"].AsGodotObject() is SelectProxy collider)
				{
					currentHovered = collider;
					currentPlaceId = collider.Place?.Id;
				}
			}

			// 只有hover对象发生变化时才刷新hint
			if (currentHovered != lastHovered)
			{
				// 隐藏上一个hover的hint
				if (lastHovered != null)
				{
					lastHovered.FirstSelect = true;
				}
				lastHovered = currentHovered;
				lastHintPlaceId = null; // 强制刷新hint内容
			}

			if (currentHovered != null)
			{
				hintPanel.Visible = true;
				selector.TargetPosition = currentHovered.GlobalPosition;

				// 只有当hover的对象id发生变化时才刷新hint内容
				if (currentPlaceId != lastHintPlaceId)
				{
					GD.Print($"Hover: {currentPlaceId}");
					hintPanel.Size = new Vector2I(600, 0);
					hintPanel.SetHint(currentPlaceId);
					lastHintPlaceId = currentPlaceId;
					currentHovered.FirstSelect = false;
				}
				hintPanel.Position = (Vector2I)(GetViewport().GetMousePosition() + new Vector2(40, 40));

				if (Input.IsActionJustPressed("click"))
				{
					GD.Print("click");
					Global.Instance.globalVariables["selectedId"] = currentPlaceId;
				}
			}
			else
			{
				hintPanel.Visible = false;
				lastHintPlaceId = null;
			}
		}
	}
}
