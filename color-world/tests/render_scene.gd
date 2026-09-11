extends SceneTree
# Run under Xvfb with the compatibility renderer; captures actual native frames.
var app: Node

func _init() -> void:
	call_deferred("run")

func shot(filename: String) -> void:
	await process_frame
	await process_frame
	await RenderingServer.frame_post_draw
	var error: Error = root.get_texture().get_image().save_png("res://build/"+filename+".png")
	assert(error==OK)

func run() -> void:
	root.size = Vector2i(432,864)
	app = load("res://scenes/main.tscn").instantiate()
	root.add_child(app)
	await create_timer(.8).timeout
	await shot("01-lobby")
	app.start_level(1)
	await shot("02-gameplay")
	app.change_channel(0,500)
	assert(app.ratios[0]==500)
	assert(app.your_swatch.color==ColorSystem.color_of(app.ratios,app.model.level(1)))
	app.shop()
	await shot("03-shop")
	app.settings_screen()
	await shot("04-settings")
	app.set_setting("language","ar")
	await shot("05-arabic-settings")
	app.start_level(1)
	await shot("06-arabic-gameplay")
	app.reset_modal()
	await shot("07-reset-confirmation")
	app.close_modal()
	app.free_world()
	await shot("08-world")
	print("Native rendering snapshots completed")
	quit()
