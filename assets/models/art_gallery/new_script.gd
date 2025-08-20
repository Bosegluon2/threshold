extends Node

var dict={"aa":"bb"}
# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	print(dict.get("aa")) # Replace with function body.
func test() -> int: return 42
	
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass
