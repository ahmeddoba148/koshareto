extends Node3D

# كشريتو v0.1 — fully procedural mobile prototype.
# No external game assets are required for this first build.

const BASE_INGREDIENTS := PackedStringArray(["رز", "مكرونة", "عدس", "حمص", "صلصة"])
const OPTIONAL_INGREDIENTS := PackedStringArray(["دقة", "شطة", "بصل"])

var ingredient_colors: Dictionary = {
	"رز": Color("#F4E5B8"),
	"مكرونة": Color("#E8B55D"),
	"عدس": Color("#A86B3F"),
	"حمص": Color("#D7B66C"),
	"صلصة": Color("#B73A2D"),
	"دقة": Color("#D6C58A"),
	"شطة": Color("#8E1D18"),
	"بصل": Color("#9B5B25")
}

var rng := RandomNumberGenerator.new()
var camera: Camera3D
var bowl_root: Node3D
var bowl_layers: Node3D
var customer_root: Node3D

var order_label: Label
var bowl_label: Label
var stats_label: Label
var timer_label: Label
var message_label: Label
var patience_bar: ProgressBar
var upgrade_button: Button
var end_panel: PanelContainer
var end_label: Label

var current_order := PackedStringArray()
var bowl_items := PackedStringArray()
var portion_name := "وسط"
var portion_bonus := 10
var cash := 50
var rating := 4.60
var served := 0
var missed := 0
var price_level := 1
var price_bonus := 0
var time_left := 120.0
var patience := 1.0
var game_over := false
var camera_yaw := 0.0
var camera_radius := 10.8
var camera_height := 6.4


func _ready() -> void:
	rng.randomize()
	_build_environment()
	_build_shop()
	_build_ui()
	_update_camera()
	_new_order()
	_show_message("اضغط مكونات الطلب ثم قدّم الطبق", Color("#FFE08A"))


func _process(delta: float) -> void:
	if game_over:
		return

	time_left = maxf(0.0, time_left - delta)
	patience = maxf(0.0, patience - delta / 28.0)
	timer_label.text = "⏱ %02d:%02d" % [int(time_left) / 60, int(time_left) % 60]
	patience_bar.value = patience * 100.0

	if patience <= 0.0:
		_customer_left()
		return

	if time_left <= 0.0:
		_end_day()


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventScreenDrag:
		camera_yaw = clampf(camera_yaw - event.relative.x * 0.0022, -0.38, 0.38)
		_update_camera()
	elif event is InputEventMouseMotion and Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT):
		camera_yaw = clampf(camera_yaw - event.relative.x * 0.0022, -0.38, 0.38)
		_update_camera()


func _build_environment() -> void:
	var world := WorldEnvironment.new()
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color("#17130F")
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("#FFD9A2")
	env.ambient_light_energy = 0.68
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	world.environment = env
	add_child(world)

	var sun := DirectionalLight3D.new()
	sun.light_color = Color("#FFE2B4")
	sun.light_energy = 1.55
	sun.shadow_enabled = true
	sun.rotation_degrees = Vector3(-52.0, -28.0, 0.0)
	add_child(sun)

	var warm_light := OmniLight3D.new()
	warm_light.position = Vector3(0.0, 3.6, 0.0)
	warm_light.light_color = Color("#FFB85C")
	warm_light.light_energy = 5.2
	warm_light.omni_range = 7.5
	warm_light.shadow_enabled = false
	add_child(warm_light)

	camera = Camera3D.new()
	camera.current = true
	camera.fov = 54.0
	add_child(camera)


func _build_shop() -> void:
	# Floor and walls.
	_make_box(Vector3(0.0, -0.12, 0.0), Vector3(11.0, 0.24, 8.0), Color("#3A2B20"), "Floor")
	_make_box(Vector3(0.0, 2.15, -3.95), Vector3(11.0, 4.3, 0.25), Color("#7D3328"), "BackWall")
	_make_box(Vector3(-5.38, 2.15, 0.0), Vector3(0.25, 4.3, 8.0), Color("#D6B58E"), "LeftWall")
	_make_box(Vector3(5.38, 2.15, 0.0), Vector3(0.25, 4.3, 8.0), Color("#D6B58E"), "RightWall")

	# Decorative cream stripe.
	_make_box(Vector3(0.0, 2.82, -3.80), Vector3(10.65, 0.24, 0.12), Color("#F0C77C"), "Stripe")

	# Back sign.
	var sign_back := _make_box(Vector3(0.0, 3.35, -3.72), Vector3(4.8, 0.78, 0.16), Color("#201712"), "Sign")
	var sign_text := Label3D.new()
	sign_text.text = "كشريتو"
	sign_text.font_size = 78
	sign_text.outline_size = 10
	sign_text.modulate = Color("#FFD05E")
	sign_text.outline_modulate = Color("#4A2415")
	sign_text.position = Vector3(0.0, 3.34, -3.59)
	add_child(sign_text)

	# Service counter.
	_make_box(Vector3(0.0, 0.62, 0.72), Vector3(8.5, 1.24, 1.02), Color("#7B3D24"), "Counter")
	_make_box(Vector3(0.0, 1.27, 0.72), Vector3(8.75, 0.12, 1.12), Color("#D6A35E"), "CounterTop")
	_make_box(Vector3(-3.55, 0.55, 0.18), Vector3(0.08, 0.78, 0.08), Color("#E8CFA8"), "TrimL")
	_make_box(Vector3(3.55, 0.55, 0.18), Vector3(0.08, 0.78, 0.08), Color("#E8CFA8"), "TrimR")

	# Ingredient pots in two rows.
	var pot_positions := [
		Vector3(-2.7, 1.52, -0.38), Vector3(-0.9, 1.52, -0.38), Vector3(0.9, 1.52, -0.38), Vector3(2.7, 1.52, -0.38),
		Vector3(-2.7, 1.52, -1.48), Vector3(-0.9, 1.52, -1.48), Vector3(0.9, 1.52, -1.48), Vector3(2.7, 1.52, -1.48)
	]
	for i in range(BASE_INGREDIENTS.size() + OPTIONAL_INGREDIENTS.size()):
		var item := BASE_INGREDIENTS[i] if i < BASE_INGREDIENTS.size() else OPTIONAL_INGREDIENTS[i - BASE_INGREDIENTS.size()]
		_make_pot(pot_positions[i], item, ingredient_colors[item])

	# Serving bowl in front-center.
	bowl_root = Node3D.new()
	bowl_root.name = "ServingBowl"
	bowl_root.position = Vector3(0.0, 1.37, 1.05)
	add_child(bowl_root)

	var bowl_base := CylinderMesh.new()
	bowl_base.top_radius = 0.64
	bowl_base.bottom_radius = 0.48
	bowl_base.height = 0.18
	bowl_base.radial_segments = 32
	var bowl_mesh := MeshInstance3D.new()
	bowl_mesh.mesh = bowl_base
	bowl_mesh.material_override = _material(Color("#F4E6CF"), 0.34)
	bowl_root.add_child(bowl_mesh)

	var inner := CylinderMesh.new()
	inner.top_radius = 0.53
	inner.bottom_radius = 0.53
	inner.height = 0.025
	inner.radial_segments = 32
	var inner_mesh := MeshInstance3D.new()
	inner_mesh.mesh = inner
	inner_mesh.position.y = 0.10
	inner_mesh.material_override = _material(Color("#2D211A"), 0.75)
	bowl_root.add_child(inner_mesh)

	bowl_layers = Node3D.new()
	bowl_layers.name = "Layers"
	bowl_root.add_child(bowl_layers)

	# Tables and stools as background dressing.
	for x in [-3.8, 3.8]:
		_make_box(Vector3(x, 0.72, 2.95), Vector3(1.7, 0.12, 1.25), Color("#8A4A28"), "Table")
		_make_box(Vector3(x, 0.36, 2.95), Vector3(0.18, 0.72, 0.18), Color("#33251D"), "TableLeg")
		for dx in [-0.62, 0.62]:
			_make_cylinder(Vector3(x + dx, 0.34, 2.25), 0.22, 0.68, Color("#5B3424"), "Stool")

	# Static queue customers for atmosphere.
	_make_customer(Vector3(-3.5, 0.0, 3.35), Color("#316A8A"), 0.86)
	_make_customer(Vector3(-4.25, 0.0, 3.65), Color("#6E3C76"), 0.80)


func _build_ui() -> void:
	var layer := CanvasLayer.new()
	add_child(layer)

	var root := Control.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.mouse_filter = Control.MOUSE_FILTER_PASS
	layer.add_child(root)

	# Top status bar.
	var top := PanelContainer.new()
	_set_rect(top, 0.02, 0.025, 0.98, 0.135)
	top.add_theme_stylebox_override("panel", _panel_style(Color(0.08, 0.055, 0.038, 0.94), Color("#D79C45"), 18))
	root.add_child(top)

	var top_row := HBoxContainer.new()
	top_row.add_theme_constant_override("separation", 22)
	top.add_child(top_row)

	var title := Label.new()
	title.text = "كشريتو  •  PROTOTYPE v0.1"
	title.add_theme_font_size_override("font_size", 28)
	title.add_theme_color_override("font_color", Color("#FFD15D"))
	title.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	top_row.add_child(title)

	var spacer := Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	top_row.add_child(spacer)

	stats_label = Label.new()
	stats_label.add_theme_font_size_override("font_size", 24)
	stats_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	stats_label.add_theme_color_override("font_color", Color("#F6E6C8"))
	top_row.add_child(stats_label)

	timer_label = Label.new()
	timer_label.text = "⏱ 02:00"
	timer_label.add_theme_font_size_override("font_size", 29)
	timer_label.add_theme_color_override("font_color", Color("#FFF0A6"))
	timer_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	top_row.add_child(timer_label)

	# Current order card.
	var order_panel := PanelContainer.new()
	_set_rect(order_panel, 0.025, 0.155, 0.47, 0.315)
	order_panel.add_theme_stylebox_override("panel", _panel_style(Color(0.055, 0.045, 0.038, 0.92), Color("#8B6A48"), 16))
	root.add_child(order_panel)

	var order_box := VBoxContainer.new()
	order_box.add_theme_constant_override("separation", 6)
	order_panel.add_child(order_box)

	order_label = Label.new()
	order_label.add_theme_font_size_override("font_size", 26)
	order_label.add_theme_color_override("font_color", Color("#FFF4DF"))
	order_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	order_box.add_child(order_label)

	patience_bar = ProgressBar.new()
	patience_bar.min_value = 0.0
	patience_bar.max_value = 100.0
	patience_bar.value = 100.0
	patience_bar.show_percentage = false
	patience_bar.custom_minimum_size = Vector2(0.0, 14.0)
	order_box.add_child(patience_bar)

	bowl_label = Label.new()
	bowl_label.add_theme_font_size_override("font_size", 18)
	bowl_label.add_theme_color_override("font_color", Color("#D9C7A9"))
	order_box.add_child(bowl_label)

	# Floating feedback.
	message_label = Label.new()
	_set_rect(message_label, 0.27, 0.38, 0.73, 0.46)
	message_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	message_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	message_label.add_theme_font_size_override("font_size", 30)
	message_label.add_theme_color_override("font_color", Color("#FFE08A"))
	message_label.add_theme_color_override("font_outline_color", Color(0.06, 0.04, 0.03, 0.95))
	message_label.add_theme_constant_override("outline_size", 9)
	root.add_child(message_label)

	# Bottom action deck.
	var deck := PanelContainer.new()
	_set_rect(deck, 0.035, 0.665, 0.965, 0.975)
	deck.add_theme_stylebox_override("panel", _panel_style(Color(0.055, 0.042, 0.032, 0.96), Color("#9F6D35"), 20))
	root.add_child(deck)

	var deck_box := VBoxContainer.new()
	deck_box.add_theme_constant_override("separation", 10)
	deck.add_child(deck_box)

	var grid := GridContainer.new()
	grid.columns = 4
	grid.size_flags_vertical = Control.SIZE_EXPAND_FILL
	grid.add_theme_constant_override("h_separation", 9)
	grid.add_theme_constant_override("v_separation", 9)
	deck_box.add_child(grid)

	var all_items := PackedStringArray()
	for item in BASE_INGREDIENTS:
		all_items.append(item)
	for item in OPTIONAL_INGREDIENTS:
		all_items.append(item)

	for item in all_items:
		var b := Button.new()
		b.text = item
		b.custom_minimum_size = Vector2(210.0, 58.0)
		b.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		b.add_theme_font_size_override("font_size", 24)
		_style_button(b, ingredient_colors[item].darkened(0.42), ingredient_colors[item].darkened(0.22))
		b.pressed.connect(_add_ingredient.bind(item))
		grid.add_child(b)

	var action_row := HBoxContainer.new()
	action_row.add_theme_constant_override("separation", 10)
	deck_box.add_child(action_row)

	var clear_button := Button.new()
	clear_button.text = "إفرغ الطبق"
	clear_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	clear_button.custom_minimum_size = Vector2(0.0, 58.0)
	clear_button.add_theme_font_size_override("font_size", 23)
	_style_button(clear_button, Color("#4A3A31"), Color("#6A5142"))
	clear_button.pressed.connect(_clear_bowl)
	action_row.add_child(clear_button)

	var serve_button := Button.new()
	serve_button.text = "قدّم للزبون ✓"
	serve_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	serve_button.custom_minimum_size = Vector2(0.0, 58.0)
	serve_button.add_theme_font_size_override("font_size", 25)
	_style_button(serve_button, Color("#2F6E43"), Color("#3E9057"))
	serve_button.pressed.connect(_serve)
	action_row.add_child(serve_button)

	upgrade_button = Button.new()
	upgrade_button.text = "ترقية السعر 100ج"
	upgrade_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	upgrade_button.custom_minimum_size = Vector2(0.0, 58.0)
	upgrade_button.add_theme_font_size_override("font_size", 22)
	_style_button(upgrade_button, Color("#8B5A20"), Color("#B27426"))
	upgrade_button.pressed.connect(_buy_upgrade)
	action_row.add_child(upgrade_button)

	# End-of-day card.
	end_panel = PanelContainer.new()
	_set_rect(end_panel, 0.24, 0.20, 0.76, 0.66)
	end_panel.add_theme_stylebox_override("panel", _panel_style(Color(0.055, 0.037, 0.025, 0.985), Color("#E1A847"), 24))
	end_panel.visible = false
	root.add_child(end_panel)

	var end_box := VBoxContainer.new()
	end_box.alignment = BoxContainer.ALIGNMENT_CENTER
	end_box.add_theme_constant_override("separation", 16)
	end_panel.add_child(end_box)

	var end_title := Label.new()
	end_title.text = "انتهى يوم الشغل 🍲"
	end_title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	end_title.add_theme_font_size_override("font_size", 36)
	end_title.add_theme_color_override("font_color", Color("#FFD15D"))
	end_box.add_child(end_title)

	end_label = Label.new()
	end_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	end_label.add_theme_font_size_override("font_size", 27)
	end_label.add_theme_color_override("font_color", Color("#F4E6CE"))
	end_box.add_child(end_label)

	var restart := Button.new()
	restart.text = "يوم جديد"
	restart.custom_minimum_size = Vector2(260.0, 64.0)
	restart.add_theme_font_size_override("font_size", 26)
	_style_button(restart, Color("#8E5C1F"), Color("#B87927"))
	restart.pressed.connect(func() -> void: get_tree().reload_current_scene())
	end_box.add_child(restart)

	_update_stats()
	_update_bowl_label()


func _new_order() -> void:
	if game_over:
		return

	current_order.clear()
	for item in BASE_INGREDIENTS:
		current_order.append(item)

	# Modifiers make every customer slightly different.
	for item in OPTIONAL_INGREDIENTS:
		if rng.randf() < 0.58:
			current_order.append(item)

	var portion_roll := rng.randi_range(0, 2)
	if portion_roll == 0:
		portion_name = "صغير"
		portion_bonus = 0
	elif portion_roll == 1:
		portion_name = "وسط"
		portion_bonus = 10
	else:
		portion_name = "كبير"
		portion_bonus = 20

	patience = 1.0
	_clear_bowl(false)
	_spawn_active_customer()
	order_label.text = "طلب الزبون — %s\n%s" % [portion_name, " + ".join(current_order)]
	_show_message("طلب جديد!", Color("#FFE08A"))


func _add_ingredient(item: String) -> void:
	if game_over:
		return
	if bowl_items.has(item):
		_show_message("%s موجود بالفعل" % item, Color("#E7C99A"))
		return

	bowl_items.append(item)
	_add_bowl_layer(item)
	_update_bowl_label()

	if current_order.has(item):
		_show_message("+ %s" % item, Color("#9EE6A9"))
	else:
		_show_message("الزبون ماطلبش %s!" % item, Color("#FF9B82"))


func _serve() -> void:
	if game_over:
		return
	if bowl_items.is_empty():
		_show_message("الطبق فاضي يا معلم 😂", Color("#FFB071"))
		return

	if _order_is_correct():
		var earned := 25 + portion_bonus + (current_order.size() - BASE_INGREDIENTS.size()) * 4 + price_bonus
		cash += earned
		served += 1
		rating = minf(5.0, rating + 0.055)
		_show_message("مظبوط! +%d جنيه 🔥" % earned, Color("#8FF0A4"))
		_success_pop()
		_update_stats()
		await get_tree().create_timer(0.55).timeout
		_new_order()
	else:
		rating = maxf(1.0, rating - 0.16)
		_show_message("الطلب غلط! راجع المكونات", Color("#FF806C"))
		_update_stats()


func _customer_left() -> void:
	if game_over:
		return
	missed += 1
	rating = maxf(1.0, rating - 0.22)
	_show_message("الزبون زهق ومشي 😤", Color("#FF806C"))
	_update_stats()
	_new_order()


func _buy_upgrade() -> void:
	if game_over:
		return
	if price_level >= 3:
		_show_message("وصلت لأعلى ترقية في البروتوتايب", Color("#FFE08A"))
		return
	var cost := 100 if price_level == 1 else 180
	if cash < cost:
		_show_message("محتاج %d جنيه للترقية" % cost, Color("#FFB071"))
		return

	cash -= cost
	price_level += 1
	price_bonus += 8
	var next_cost := 180 if price_level == 2 else 0
	upgrade_button.text = "ترقية السعر %dج" % next_cost if next_cost > 0 else "السعر MAX ✓"
	_update_stats()
	_show_message("طورت المحل! قيمة كل طبق زادت", Color("#FFD15D"))


func _end_day() -> void:
	if game_over:
		return
	game_over = true
	time_left = 0.0
	timer_label.text = "⏱ 00:00"
	end_label.text = "بعت: %d طبق\nمشي منك: %d زبون\nالكاش: %d جنيه\nالتقييم: %.2f ★" % [served, missed, cash, rating]
	end_panel.visible = true
	_show_message("", Color.WHITE)


func _order_is_correct() -> bool:
	if bowl_items.size() != current_order.size():
		return false
	var a := Array(current_order)
	var b := Array(bowl_items)
	a.sort()
	b.sort()
	return a == b


func _clear_bowl(show_feedback: bool = true) -> void:
	bowl_items.clear()
	if is_instance_valid(bowl_layers):
		for child in bowl_layers.get_children():
			child.queue_free()
	_update_bowl_label()
	if show_feedback:
		_show_message("فضّيت الطبق", Color("#D6C3A4"))


func _add_bowl_layer(item: String) -> void:
	var index := bowl_items.size() - 1
	var layer_mesh := CylinderMesh.new()
	layer_mesh.top_radius = 0.50 - minf(index * 0.012, 0.06)
	layer_mesh.bottom_radius = layer_mesh.top_radius
	layer_mesh.height = 0.042
	layer_mesh.radial_segments = 28

	var layer := MeshInstance3D.new()
	layer.mesh = layer_mesh
	layer.position.y = 0.125 + index * 0.043
	layer.material_override = _material(ingredient_colors[item], 0.62)
	bowl_layers.add_child(layer)

	# Onion gets a few crunchy blocks on top for a more readable 3D bowl.
	if item == "بصل":
		for i in range(8):
			var angle := TAU * float(i) / 8.0 + rng.randf_range(-0.18, 0.18)
			var r := rng.randf_range(0.16, 0.40)
			var piece := _new_box_mesh(Vector3(0.095, 0.045, 0.16), ingredient_colors[item].lightened(0.18))
			piece.position = Vector3(cos(angle) * r, layer.position.y + 0.055, sin(angle) * r)
			piece.rotation_degrees.y = rng.randf_range(0.0, 180.0)
			bowl_layers.add_child(piece)


func _update_bowl_label() -> void:
	if not is_instance_valid(bowl_label):
		return
	bowl_label.text = "طبقك: فاضي" if bowl_items.is_empty() else "طبقك: %s" % " + ".join(bowl_items)


func _update_stats() -> void:
	if is_instance_valid(stats_label):
		stats_label.text = "💰 %dج   ★ %.2f   🍽 %d" % [cash, rating, served]


func _show_message(text: String, color: Color) -> void:
	if not is_instance_valid(message_label):
		return
	message_label.text = text
	message_label.add_theme_color_override("font_color", color)


func _spawn_active_customer() -> void:
	if is_instance_valid(customer_root):
		customer_root.queue_free()

	var colors := [Color("#B64B3B"), Color("#2F718C"), Color("#54834C"), Color("#8A4D82"), Color("#C47B2F")]
	customer_root = _make_customer(Vector3(2.45, 0.0, 4.45), colors[rng.randi_range(0, colors.size() - 1)], 1.0)
	customer_root.name = "ActiveCustomer"
	customer_root.scale = Vector3(0.92, 0.92, 0.92)
	var tween := create_tween()
	tween.set_trans(Tween.TRANS_QUAD)
	tween.set_ease(Tween.EASE_OUT)
	tween.tween_property(customer_root, "position", Vector3(2.45, 0.0, 2.48), 0.55)


func _make_customer(pos: Vector3, shirt: Color, alpha_scale: float = 1.0) -> Node3D:
	var root := Node3D.new()
	root.position = pos
	root.scale = Vector3.ONE * alpha_scale
	add_child(root)

	# Body.
	var body_mesh := CapsuleMesh.new()
	body_mesh.radius = 0.34
	body_mesh.height = 1.18
	var body := MeshInstance3D.new()
	body.mesh = body_mesh
	body.position.y = 1.12
	body.material_override = _material(shirt, 0.82)
	root.add_child(body)

	# Head.
	var head_mesh := SphereMesh.new()
	head_mesh.radius = 0.29
	head_mesh.height = 0.58
	var head := MeshInstance3D.new()
	head.mesh = head_mesh
	head.position.y = 1.93
	head.material_override = _material(Color("#C98C68"), 0.88)
	root.add_child(head)

	# Hair cap.
	var hair_mesh := SphereMesh.new()
	hair_mesh.radius = 0.295
	hair_mesh.height = 0.30
	var hair := MeshInstance3D.new()
	hair.mesh = hair_mesh
	hair.position = Vector3(0.0, 2.07, 0.0)
	hair.material_override = _material(Color("#261B17"), 0.96)
	root.add_child(hair)

	# Legs.
	for x in [-0.17, 0.17]:
		var leg := _new_box_mesh(Vector3(0.20, 0.62, 0.24), Color("#2F3340"))
		leg.position = Vector3(x, 0.35, 0.0)
		root.add_child(leg)

	# Eyes facing roughly toward the counter/camera.
	for x in [-0.10, 0.10]:
		var eye_mesh := SphereMesh.new()
		eye_mesh.radius = 0.036
		eye_mesh.height = 0.072
		var eye := MeshInstance3D.new()
		eye.mesh = eye_mesh
		eye.position = Vector3(x, 1.99, 0.27)
		eye.material_override = _material(Color("#17100D"), 1.0)
		root.add_child(eye)

	return root


func _make_pot(pos: Vector3, item: String, food_color: Color) -> void:
	var pot := _make_cylinder(pos, 0.58, 0.46, Color("#878A8C"), "Pot_%s" % item)
	pot.material_override = _metal_material(Color("#929596"))

	var food_mesh := CylinderMesh.new()
	food_mesh.top_radius = 0.50
	food_mesh.bottom_radius = 0.50
	food_mesh.height = 0.035
	food_mesh.radial_segments = 24
	var food := MeshInstance3D.new()
	food.mesh = food_mesh
	food.position = pos + Vector3(0.0, 0.245, 0.0)
	food.material_override = _material(food_color, 0.65)
	add_child(food)

	var tag := Label3D.new()
	tag.text = item
	tag.font_size = 38
	tag.outline_size = 7
	tag.modulate = Color("#FFF0D2")
	tag.outline_modulate = Color("#241810")
	tag.position = pos + Vector3(0.0, 0.60, 0.0)
	add_child(tag)


func _success_pop() -> void:
	for i in range(12):
		var spark_mesh := SphereMesh.new()
		spark_mesh.radius = 0.055
		spark_mesh.height = 0.11
		var spark := MeshInstance3D.new()
		spark.mesh = spark_mesh
		spark.position = bowl_root.global_position + Vector3(rng.randf_range(-0.3, 0.3), 0.40, rng.randf_range(-0.3, 0.3))
		spark.material_override = _material(Color("#FFD15D") if i % 2 == 0 else Color("#8FE3A0"), 0.35)
		add_child(spark)
		var target := spark.position + Vector3(rng.randf_range(-0.85, 0.85), rng.randf_range(0.55, 1.25), rng.randf_range(-0.45, 0.45))
		var tw := create_tween()
		tw.set_trans(Tween.TRANS_QUAD)
		tw.set_ease(Tween.EASE_OUT)
		tw.tween_property(spark, "position", target, 0.52)
		tw.parallel().tween_property(spark, "scale", Vector3.ZERO, 0.52)
		tw.tween_callback(spark.queue_free)


func _update_camera() -> void:
	if not is_instance_valid(camera):
		return
	camera.position = Vector3(sin(camera_yaw) * camera_radius, camera_height, cos(camera_yaw) * camera_radius)
	camera.look_at(Vector3(0.0, 1.20, 0.10), Vector3.UP)


func _make_box(pos: Vector3, size: Vector3, color: Color, node_name: String = "Box") -> MeshInstance3D:
	var box := _new_box_mesh(size, color)
	box.name = node_name
	box.position = pos
	add_child(box)
	return box


func _new_box_mesh(size: Vector3, color: Color) -> MeshInstance3D:
	var mesh := BoxMesh.new()
	mesh.size = size
	var instance := MeshInstance3D.new()
	instance.mesh = mesh
	instance.material_override = _material(color, 0.78)
	return instance


func _make_cylinder(pos: Vector3, radius: float, height: float, color: Color, node_name: String = "Cylinder") -> MeshInstance3D:
	var mesh := CylinderMesh.new()
	mesh.top_radius = radius
	mesh.bottom_radius = radius
	mesh.height = height
	mesh.radial_segments = 24
	var instance := MeshInstance3D.new()
	instance.name = node_name
	instance.mesh = mesh
	instance.position = pos
	instance.material_override = _material(color, 0.72)
	add_child(instance)
	return instance


func _material(color: Color, roughness: float = 0.7) -> StandardMaterial3D:
	var mat := StandardMaterial3D.new()
	mat.albedo_color = color
	mat.roughness = roughness
	mat.metallic = 0.0
	return mat


func _metal_material(color: Color) -> StandardMaterial3D:
	var mat := StandardMaterial3D.new()
	mat.albedo_color = color
	mat.metallic = 0.72
	mat.roughness = 0.32
	return mat


func _set_rect(control: Control, left: float, top: float, right: float, bottom: float) -> void:
	control.anchor_left = left
	control.anchor_top = top
	control.anchor_right = right
	control.anchor_bottom = bottom
	control.offset_left = 0.0
	control.offset_top = 0.0
	control.offset_right = 0.0
	control.offset_bottom = 0.0


func _panel_style(bg: Color, border: Color, radius: int) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = bg
	style.border_color = border
	style.set_border_width_all(2)
	style.corner_radius_top_left = radius
	style.corner_radius_top_right = radius
	style.corner_radius_bottom_left = radius
	style.corner_radius_bottom_right = radius
	style.content_margin_left = 18.0
	style.content_margin_right = 18.0
	style.content_margin_top = 12.0
	style.content_margin_bottom = 12.0
	return style


func _style_button(button: Button, normal: Color, hover: Color) -> void:
	var normal_style := StyleBoxFlat.new()
	normal_style.bg_color = normal
	normal_style.corner_radius_top_left = 12
	normal_style.corner_radius_top_right = 12
	normal_style.corner_radius_bottom_left = 12
	normal_style.corner_radius_bottom_right = 12
	normal_style.border_color = normal.lightened(0.22)
	normal_style.set_border_width_all(1)

	var hover_style := normal_style.duplicate()
	hover_style.bg_color = hover
	var pressed_style := normal_style.duplicate()
	pressed_style.bg_color = hover.lightened(0.08)

	button.add_theme_stylebox_override("normal", normal_style)
	button.add_theme_stylebox_override("hover", hover_style)
	button.add_theme_stylebox_override("pressed", pressed_style)
	button.add_theme_stylebox_override("focus", hover_style)
	button.add_theme_color_override("font_color", Color("#FFF4DF"))
	button.add_theme_color_override("font_hover_color", Color.WHITE)
	button.add_theme_color_override("font_pressed_color", Color.WHITE)
