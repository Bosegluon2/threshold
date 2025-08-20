using Godot;
using System;

public partial class GuiPanel3d : Node3D
{
	// Used for checking if the mouse is inside the Area3D.
	private bool isMouseInside = false;
	// The last processed input touch/mouse event. To calculate relative movement.
	private Vector2? lastEventPos2D = null;
	// The time of the last event in seconds since engine start.
	private float lastEventTime = -1.0f;

	private SubViewport nodeViewport;
	private MeshInstance3D nodeQuad;
	private Area3D nodeArea;
	public Button startButton;
	public Button settingsButton;
	public Button quitButton;

	public override void _Ready()
	{
		nodeViewport = GetNode<SubViewport>("SubViewport");
		nodeQuad = GetNode<MeshInstance3D>("Quad");
		nodeArea = GetNode<Area3D>("Quad/Area3D");
		startButton = GetNode<Button>("SubViewport/GUI/VBoxContainer/New");
		settingsButton = GetNode<Button>("SubViewport/GUI/VBoxContainer/Settings");
		quitButton = GetNode<Button>("SubViewport/GUI/VBoxContainer/Quit");
		startButton.Pressed += () =>
		{
			Fader.Instance.FadeOut(1.0f, Callable.From(() =>
			{
				GetTree().ChangeSceneToPacked(FastLoader.Instance.files["Preface"]);
			}));
		};
		settingsButton.Pressed += () =>
		{
			GD.Print("settings");
		};
		quitButton.Pressed += () =>
		{
			ExitHandler.Instance.RequestExit();
		};
		nodeArea.MouseEntered += OnMouseEnteredArea;
		nodeArea.MouseExited += OnMouseExitedArea;
		nodeArea.InputEvent += (camera, @event, eventPosition, normal, shapeIdx) => OnMouseInputEvent(camera, @event, eventPosition, normal, (int)shapeIdx);

		// If the material is NOT set to use billboard settings, then avoid running billboard specific code
		var material = nodeQuad.GetSurfaceOverrideMaterial(0) as StandardMaterial3D;
		if (material != null && material.BillboardMode == BaseMaterial3D.BillboardModeEnum.Disabled)
		{
			SetProcess(false);
		}
	}

	public override void _Process(double delta)
	{
		// NOTE: Remove this function if you don't plan on using billboard settings.
		RotateAreaToBillboard();
	}

	private void OnMouseEnteredArea()
	{
		isMouseInside = true;
	}

	private void OnMouseExitedArea()
	{
		isMouseInside = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Check if the event is a non-mouse/non-touch event
		if (@event is InputEventMouseButton || @event is InputEventMouseMotion || 
			@event is InputEventScreenDrag || @event is InputEventScreenTouch)
		{
			// If the event is a mouse/touch event, then we can ignore it here, because it will be
			// handled via Physics Picking.
			return;
		}
		nodeViewport.PushInput(@event);
	}

	private void OnMouseInputEvent(Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, int shapeIdx)
	{
		// Get mesh size to detect edges and make conversions. This code only support PlaneMesh and QuadMesh.
		var quadMeshSize = ((QuadMesh)nodeQuad.Mesh).Size;

		// Event position in Area3D in world coordinate space.
		var eventPos3D = eventPosition;

		// Current time in seconds since engine start.
		var now = Time.GetTicksMsec() / 1000.0f;

		// Convert position to a coordinate space relative to the Area3D node.
		// NOTE: affine_inverse accounts for the Area3D node's scale, rotation, and position in the scene!
		eventPos3D = nodeQuad.GlobalTransform.AffineInverse() * eventPos3D;

		// TODO: Adapt to bilboard mode or avoid completely.

		var eventPos2D = Vector2.Zero;

		if (isMouseInside)
		{
			// Convert the relative event position from 3D to 2D.
			eventPos2D = new Vector2(eventPos3D.X, -eventPos3D.Y);

			// Right now the event position's range is the following: (-quad_size/2) -> (quad_size/2)
			// We need to convert it into the following range: -0.5 -> 0.5
			eventPos2D.X = eventPos2D.X / quadMeshSize.X;
			eventPos2D.Y = eventPos2D.Y / quadMeshSize.Y;
			// Then we need to convert it into the following range: 0 -> 1
			eventPos2D.X += 0.5f;
			eventPos2D.Y += 0.5f;

			// Finally, we convert the position to the following range: 0 -> viewport.size
			eventPos2D.X *= nodeViewport.Size.X;
			eventPos2D.Y *= nodeViewport.Size.Y;
			// We need to do these conversions so the event's position is in the viewport's coordinate system.
		}
		else if (lastEventPos2D.HasValue)
		{
			// Fall back to the last known event position.
			eventPos2D = lastEventPos2D.Value;
		}

		// Set the event's position and global position.
		if (@event is InputEventMouse mouseEvent)
		{
			mouseEvent.Position = eventPos2D;
			mouseEvent.GlobalPosition = eventPos2D;
		}

		// Calculate the relative event distance.
		if (@event is InputEventMouseMotion || @event is InputEventScreenDrag)
		{
			// If there is not a stored previous position, then we'll assume there is no relative motion.
			if (!lastEventPos2D.HasValue)
			{
				if (@event is InputEventMouseMotion motionEvent)
					motionEvent.Relative = Vector2.Zero;
			}
			// If there is a stored previous position, then we'll calculate the relative position by subtracting
			// the previous position from the new position. This will give us the distance the event traveled from prev_pos.
			else
			{
				var relative = eventPos2D - lastEventPos2D.Value;
				if (@event is InputEventMouseMotion motionEvent)
				{
					motionEvent.Relative = relative;
					motionEvent.Velocity = relative / (now - lastEventTime);
				}
			}
		}

		// Update lastEventPos2D with the position we just calculated.
		lastEventPos2D = eventPos2D;

		// Update lastEventTime to current time.
		lastEventTime = now;

		// Finally, send the processed input event to the viewport.
		nodeViewport.PushInput(@event);
	}

	private void RotateAreaToBillboard()
	{
		var material = nodeQuad.GetSurfaceOverrideMaterial(0) as StandardMaterial3D;
		if (material == null) return;

		var billboardMode = material.BillboardMode;

		// Try to match the area with the material's billboard setting, if enabled.
		if (billboardMode > BaseMaterial3D.BillboardModeEnum.Disabled)
		{
			// Get the camera.
			var camera = GetViewport().GetCamera3D();
			if (camera == null) return;

			// Look in the same direction as the camera.
			var look = camera.ToGlobal(new Vector3(0, 0, -100)) - camera.GlobalTransform.Origin;
			look = nodeArea.Position + look;

			// Y-Billboard: Lock Y rotation, but gives bad results if the camera is tilted.
			if (billboardMode == BaseMaterial3D.BillboardModeEnum.FixedY)
			{
				look = new Vector3(look.X, 0, look.Z);
			}

			nodeArea.LookAt(look, Vector3.Up);

			// Rotate in the Z axis to compensate camera tilt.
			nodeArea.RotateObjectLocal(Vector3.Back, camera.Rotation.Z);
		}
	}
}
