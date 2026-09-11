extends SceneTree
# Run the real scene, sample layout bounds, render a screenshot where supported.
func _init() -> void:
	call_deferred("run")

func run() -> void:
	var packed: PackedScene = load("res://scenes/main.tscn")
	var app: Node = packed.instantiate()
	root.add_child(app)
	await process_frame
	await process_frame
	app.start_level(1)
	await process_frame
	assert(app.sliders.size()==3)
	app.change_channel(0,500)
	assert(app.ratios[0]==500)
	assert(app.your_swatch.color==ColorSystem.color_of(app.ratios,app.model.level(1)))
	app.shop()
	await process_frame
	app.settings_screen()
	await process_frame
	app.free_world()
	await process_frame
	print("Native scene smoke completed")
	app.queue_free()
	app = null
	await process_frame
	await process_frame
	quit()
