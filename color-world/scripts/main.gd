extends Node

enum State { BOOT, LOAD_SAVE, LOBBY, FREE_WORLD, LEVEL_LOAD, PLAYING,
	MATCH_REVEAL, FAIL, SUCCESS, REWARD, WORLD_TRANSITION, PAINT,
	AREA_COMPLETE, NEXT_LEVEL, SHOP, SETTINGS, FINAL_WORLD_RESTORE, PERFECT_WORLD }

var state: State = State.BOOT
var model: GameModel
var world: WorldSystem
var audio: GameAudio
var layer: CanvasLayer
var ui: Control
var safe: Control
var overlay: Control
var strings: Dictionary
var font: Font = preload("res://fonts/Interface.ttf")
var level_id: int = 1
var ratios: Array = [334,333,333]
var sliders: Array = []
var value_labels: Array = []
var your_swatch: ColorRect
var target_swatch: ColorRect
var help_labels: Array = []
var revealed: Dictionary = {}
var guidance: Array = []
var syncing: bool = false
var busy: bool = false
var page_area: int = 1
var last_purchase: int = -1000
var last_button: int = -1000
var reset_held: bool = false
var reset_elapsed: float = 0.0
var reset_progress: ProgressBar
var coin_label: Label
var pending_resize: bool = false
var ui_epoch: int = 0
var input_epoch: int = 0
var ink: Color = Color("233b4f")
var muted: Color = Color("718595")
var accent: Color = Color("0c9c94")
var screen_bg: Color = Color("f4f7f9")

func _ready() -> void:
	state = State.LOAD_SAVE
	strings = JSON.parse_string(FileAccess.get_file_as_string("res://data/strings.json"))
	model = GameModel.new()
	audio = GameAudio.new()
	add_child(audio)
	audio.settings = model.data.settings
	world = WorldSystem.new()
	add_child(world)
	world.setup(model)
	world.object_selected.connect(show_object)
	world.set_quality(str(model.data.settings.graphics))
	layer = CanvasLayer.new()
	add_child(layer)
	ui = Control.new()
	ui.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	ui.mouse_filter = Control.MOUSE_FILTER_IGNORE
	layer.add_child(ui)
	get_viewport().size_changed.connect(func() -> void:
		pending_resize = true
	)
	lobby()
	if int(model.data.pending_reveal) > 0:
		level_id = int(model.data.pending_reveal)
		resume_reveal()

func tr_key(key: String) -> String:
	var language: String = str(model.data.settings.language)
	return str(strings.get(language,strings.en).get(key,strings.en.get(key,key)))

func _process(delta: float) -> void:
	if pending_resize and not busy and state in [State.LOBBY,State.FREE_WORLD,State.PLAYING,State.SHOP,State.SETTINGS]:
		pending_resize = false
		match state:
			State.PLAYING: gameplay()
			State.SHOP: shop()
			State.SETTINGS: settings_screen()
			State.FREE_WORLD: free_world()
			_: lobby()
	if reset_held:
		reset_elapsed += delta
		if is_instance_valid(reset_progress): reset_progress.value = reset_elapsed/1.5*100
		if reset_elapsed >= 1.5:
			reset_held = false
			perform_reset()

func _notification(what: int) -> void:
	if what == NOTIFICATION_APPLICATION_PAUSED:
		# Confirmed mutations were already committed synchronously.
		reset_held = false
	if what == NOTIFICATION_WM_GO_BACK_REQUEST and model != null:
		if busy: return
		if overlay != null and is_instance_valid(overlay): close_modal()
		elif state == State.PLAYING or state == State.SHOP or state == State.SETTINGS: lobby()

func safe_rect() -> Rect2:
	var size: Vector2 = get_viewport().get_visible_rect().size
	var r: Rect2 = Rect2(Vector2(14,14),size-Vector2(28,28))
	if OS.has_feature("mobile"):
		var physical: Vector2i = DisplayServer.window_get_size()
		var area: Rect2i = DisplayServer.get_display_safe_area()
		if physical.x > 0 and physical.y > 0:
			var scale: Vector2 = size/Vector2(physical)
			var pos: Vector2 = Vector2(area.position)*scale
			var ss: Vector2 = Vector2(area.size)*scale
			r = Rect2(pos+Vector2(12,8),ss-Vector2(24,16))
	return r

func clear_screen(background: bool = false) -> void:
	ui.size = get_viewport().get_visible_rect().size
	ui_epoch += 1
	close_modal()
	for child in ui.get_children():
		ui.remove_child(child)
		child.queue_free()
	if background:
		var bg: ColorRect = ColorRect.new()
		bg.color = screen_bg
		bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
		bg.mouse_filter = Control.MOUSE_FILTER_STOP
		ui.add_child(bg)
	safe = Control.new()
	safe.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var r: Rect2 = safe_rect()
	safe.position = r.position
	safe.size = r.size
	ui.add_child(safe)
	safe.layout_direction = Control.LAYOUT_DIRECTION_RTL if model.data.settings.language == "ar" else Control.LAYOUT_DIRECTION_LTR
	coin_label = null

func place(control: Control, x: float, y: float, w: float, h: float, parent: Control = null) -> void:
	var host: Control = safe if parent == null else parent
	host.add_child(control)
	control.position = Vector2(x,y)*host.size
	control.size = Vector2(w,h)*host.size

func box_style(color: Color, radius: int = 18, border: Color = Color.TRANSPARENT) -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = color
	style.set_corner_radius_all(radius)
	if border.a > 0:
		style.border_color = border
		style.set_border_width_all(1)
	return style

func panel(x: float,y: float,w: float,h: float,color: Color = Color.WHITE,parent: Control = null) -> Panel:
	var p: Panel = Panel.new()
	p.add_theme_stylebox_override("panel",box_style(color))
	p.mouse_filter = Control.MOUSE_FILTER_IGNORE
	place(p,x,y,w,h,parent)
	return p

func label(text: String,x: float,y: float,w: float,h: float,size: int = 18,color: Color = Color("233b4f"),centered: bool = false,parent: Control = null) -> Label:
	var l: Label = Label.new()
	l.text = text
	l.add_theme_font_override("font",font)
	var factor: float = clampf(safe.size.y/820.0,.76,1.13)
	l.add_theme_font_size_override("font_size",maxi(10,roundi(size*factor)))
	l.add_theme_color_override("font_color",color)
	l.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	l.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER if centered else HORIZONTAL_ALIGNMENT_LEFT
	if not centered and model.data.settings.language == "ar": l.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	place(l,x,y,w,h,parent)
	return l

func button(text: String,x: float,y: float,w: float,h: float,action: Callable,primary: bool = false,parent: Control = null) -> Button:
	var b: Button = Button.new()
	b.text = text
	b.add_theme_font_override("font",font)
	b.add_theme_font_size_override("font_size",maxi(11,roundi(17*clampf(safe.size.y/820,.78,1.12))))
	b.add_theme_color_override("font_color",Color.WHITE if primary else ink)
	b.add_theme_stylebox_override("normal",box_style(accent if primary else Color.WHITE,16))
	b.add_theme_stylebox_override("hover",box_style(Color("138e88") if primary else Color("e9f0f3"),16))
	b.add_theme_stylebox_override("pressed",box_style(Color("117973") if primary else Color("d7e4e9"),16))
	b.add_theme_stylebox_override("disabled",box_style(Color("dce4e8"),16))
	b.add_theme_color_override("font_disabled_color",muted)
	b.focus_mode = Control.FOCUS_NONE
	b.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS
	place(b,x,y,w,h,parent)
	b.pressed.connect(func() -> void:
		if busy or Time.get_ticks_msec()-last_button < 160: return
		last_button = Time.get_ticks_msec()
		audio.effect("tap")
		action.call()
	)
	return b

func title_bar(title: String,back_action: Callable) -> void:
	button("‹",0,.008,.13,.057,back_action)
	label(title,.16,.008,.54,.06,23)
	coin_label = label("● %d" % int(model.data.coins),.7,.008,.3,.06,19,Color("ab7827"),true)

func sync_audio(a: int,playing: bool) -> void:
	var area: Dictionary = model.content.areas[a-1]
	audio.settings = model.data.settings
	audio.area(int(area.music),str(area.biome),float(model.area_count(a))/15)
	audio.context(playing)

func lobby() -> void:
	state = State.LOBBY
	busy = false
	world.visible = true
	world.set_process(true)
	level_id = model.next_level()
	var a: int = int(model.level(level_id).area_id)
	world.focus_area(a)
	sync_audio(a,false)
	clear_screen()
	panel(0,0,1,.108)
	label("COLOR WORLD",.04,.008,.58,.047,24)
	label(tr_key("tagline"),.04,.053,.75,.04,12,muted)
	coin_label = label("● %d" % int(model.data.coins),.69,.012,.27,.043,18,Color("ab7827"),true)
	panel(.05,.135,.9,.09,Color(1,1,1,.92))
	label("%s %02d · %s" % [tr_key("area"),a,model.content.areas[a-1].name],.075,.141,.85,.04,18,ink,true)
	label("★ %d / 4500    ·    %d / 15" % [model.total_stars(),model.area_count(a)],.075,.179,.85,.035,14,muted,true)
	if int(model.data.streak)>1:
		label("%s  × %d" % [tr_key("streak"),int(model.data.streak)],.12,.246,.76,.043,14,accent,true)
	panel(.06,.684,.88,.08,Color(1,1,1,.93))
	label("%s   %d / 45 ★" % [tr_key("area"),model.area_stars(a)],.1,.69,.8,.032,16,ink,true)
	label(tr_key("saved"),.1,.727,.8,.027,11,muted,true)
	button("%s  ·  %s %d" % [tr_key("continue"),tr_key("level"),level_id],.04,.785,.92,.077,func() -> void: start_level(level_id),true)
	var keys: Array = ["world","levels","shop","settings"]
	var actions: Array = [free_world,open_levels,shop,settings_screen]
	for i in range(4): button(tr_key(keys[i]),i*.253,.902,.241,.068,actions[i])
	if model.area_count(a)==15 and model.area_stars(a)<30:
		label(tr_key("gate"),.08,.59,.84,.082,15,ink,true)

func start_level(id: int) -> void:
	if not model.available(id):
		toast(tr_key("area_locked"))
		return
	state = State.LEVEL_LOAD
	level_id = id
	revealed.clear()
	guidance.clear()
	var old: Dictionary = model.record(id)
	ratios = old.ratios.duplicate() if not old.is_empty() and not bool(old.assisted) else [334,333,333]
	# Assisted colors are never restored as an unassisted attempt.
	var next: Dictionary = model.data.duplicate(true)
	next.assistance[str(id)] = false
	if not model.commit(next):
		toast(tr_key("save_error"))
		return
	gameplay()

func gameplay() -> void:
	state = State.PLAYING
	world.free_mode = false
	world.visible = false
	world.set_process(false)
	var level: Dictionary = model.level(level_id)
	sync_audio(int(level.area_id),true)
	clear_screen(true)
	title_bar("%s %04d" % [tr_key("level"),level_id],lobby)
	label("%s %02d  /  100" % [tr_key("area"),int(level.area_id)],0,.077,.5,.035,12,muted)
	label("★ %d / 4500" % model.total_stars(),.5,.077,.5,.035,12,muted,true)
	label(tr_key("mix_hint"),.02,.13,.96,.048,21,ink,true)
	label(tr_key("target"),.02,.194,.46,.03,12,muted,true)
	label(tr_key("your_color"),.52,.194,.46,.03,12,muted,true)
	panel(0,.235,1,.225,Color("d4dce1"))
	target_swatch = ColorRect.new()
	target_swatch.color = ColorSystem.color_of(level.target,level)
	place(target_swatch,.009,.239,.491,.217)
	your_swatch = ColorRect.new()
	your_swatch.color = ColorSystem.color_of(ratios,level)
	place(your_swatch,.5,.239,.491,.217)
	label(tr_key("total"),.15,.467,.7,.034,12,muted,true)
	sliders.clear()
	value_labels.clear()
	help_labels.clear()
	for channel in range(3):
		var y: float = .51+channel*.092
		var color: Color = [Color("d96070"),Color("389c80"),Color("5f85d1")][channel]
		label(["R","G","B"][channel],.01,y,.08,.03,17,color,true)
		value_labels.append(label("%.1f%%" % (float(ratios[channel])/10),.12,y,.32,.03,17,ink))
		var tip: String = ""
		if revealed.has(channel): tip = "= %.1f%%" % (float(revealed[channel])/10)
		if guidance.size()==3: tip = guidance[channel]
		help_labels.append(label(tip,.53,y,.43,.03,14,color,true))
		button("−",.0,y+.034,.115,.049,func() -> void: change_channel(channel,int(ratios[channel])-1))
		var slider: HSlider = HSlider.new()
		slider.min_value = 0
		slider.max_value = 1000
		slider.step = 1
		slider.value = ratios[channel]
		slider.focus_mode = Control.FOCUS_NONE
		slider.layout_direction = Control.LAYOUT_DIRECTION_LTR
		var track: StyleBoxFlat = box_style(Color("dfe6eb"),5)
		track.content_margin_top = 4
		track.content_margin_bottom = 4
		slider.add_theme_stylebox_override("slider",track)
		var filled: StyleBoxFlat = box_style(color,5)
		filled.content_margin_top = 4
		filled.content_margin_bottom = 4
		slider.add_theme_stylebox_override("grabber_area",filled)
		slider.add_theme_stylebox_override("grabber_area_highlight",filled)
		place(slider,.14,y+.034,.72,.049)
		slider.value_changed.connect(func(v: float) -> void:
			if not syncing: change_channel(channel,roundi(v))
		)
		sliders.append(slider)
		button("+",.885,y+.034,.115,.049,func() -> void: change_channel(channel,int(ratios[channel])+1))
	var names: Array = ["peek","vision","auto"]
	for i in range(3):
		button("%s · %d" % [tr_key(names[i]),int(model.data.helpers[i])],i*.34,.815,.32,.051,func() -> void: use_helper(i))
	button(tr_key("match"),0,.894,1,.08,do_match,true)
	if level_id==1 and model.record(1).is_empty():
		label(tr_key("tutorial"),.02,.758,.96,.047,11,muted,true)
	else:
		var previous: Dictionary = model.record(level_id)
		if not previous.is_empty(): label("%s %.1f%%  %s" % [tr_key("best"),float(previous.score),"★".repeat(int(previous.stars))],0,.769,1,.034,12,muted,true)

func change_channel(channel: int,value: int) -> void:
	if busy or state != State.PLAYING: return
	ratios = ColorSystem.adjust(ratios,channel,value)
	# Assistance remains attached to the current attempt even after manual edits.
	sync_colors()
	audio.effect("tick")
	audio.haptic(5)
	# Vision guidance describes only the snapshot when the helper was consumed.
	if not guidance.is_empty():
		guidance.clear()
		for i in range(3):
			help_labels[i].text = "= %.1f%%" % (float(revealed[i])/10) if revealed.has(i) else ""

func sync_colors() -> void:
	syncing = true
	for i in range(3):
		sliders[i].set_value_no_signal(float(ratios[i]))
		value_labels[i].text = "%.1f%%" % (float(ratios[i])/10)
	syncing = false
	your_swatch.color = ColorSystem.color_of(ratios,model.level(level_id))

func use_helper(kind: int) -> void:
	if int(model.data.tutorial)<3:
		toast(tr_key("helper_not_yet"))
		return
	if int(model.data.helpers[kind])==0:
		toast(tr_key("empty_helper"))
		return
	if kind==0:
		var card: Panel = modal(tr_key("peek"),tr_key("select_channel"))
		for i in range(3):
			button(["R","G","B"][i],.08+i*.29,.63,.26,.18,func() -> void: peek(i),false,card)
		return
	if not model.use_helper(kind,level_id):
		toast(tr_key("save_error"))
		return
	audio.effect("helper")
	audio.haptic(25)
	if kind==1:
		guidance.clear()
		for i in range(3):
			var delta: int = int(model.level(level_id).target[i])-int(ratios[i])
			guidance.append("✓" if delta==0 else "↑" if delta>0 else "↓")
		gameplay()
	else:
		busy = true
		var initial: Array = ratios.duplicate()
		var target: Array = model.level(level_id).target
		var tween: Tween = create_tween()
		tween.tween_method(func(t: float) -> void:
			ratios[0] = roundi(lerpf(float(initial[0]),float(target[0]),t))
			ratios[1] = clampi(roundi(lerpf(float(initial[1]),float(target[1]),t)),0,1000-int(ratios[0]))
			ratios[2] = 1000-int(ratios[0])-int(ratios[1])
			sync_colors()
		,0.0,1.0,.65)
		await tween.finished
		busy = false
		gameplay()

func peek(channel: int) -> void:
	if revealed.has(channel):
		close_modal()
		return
	if not model.use_helper(0,level_id):
		close_modal()
		toast(tr_key("save_error"))
		return
	revealed[channel] = model.level(level_id).target[channel]
	close_modal()
	audio.effect("helper")
	gameplay()

func do_match() -> void:
	if busy: return
	busy = true
	state = State.MATCH_REVEAL
	# Persist attempt before awarding any outcome, even if the process closes.
	if not model.attempt(level_id):
		busy = false
		state = State.PLAYING
		toast(tr_key("save_error"))
		return
	var result: Dictionary = model.match_result(level_id,ratios)
	if not result.saved:
		busy = false
		state = State.PLAYING
		toast(tr_key("save_error"))
		return
	audio.effect("match")
	audio.haptic(24)
	var card: Panel = modal(tr_key("matching"),"")
	var counter: Label = label("0.0%",.05,.29,.9,.23,55,ink,true,card)
	var tween: Tween = create_tween()
	tween.tween_method(func(v: float) -> void:
		counter.text = "%.1f%%" % v
		audio.effect("score")
	,0.0,float(result.score),.8).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	await tween.finished
	close_modal()
	if int(result.stars)==0:
		state = State.FAIL
		audio.effect("fail")
		busy = false
		gameplay()
		toast("%s  %.1f%%" % [tr_key("fail"),float(result.score)])
		return
	state = State.SUCCESS
	var heading: String = tr_key("exact") if result.exact else tr_key("perfect") if result.perfect else tr_key("success")
	card = modal(heading,"")
	label("%.1f%%" % float(result.score),.05,.24,.9,.2,52,ink,true,card)
	label("★".repeat(int(result.stars)),.05,.45,.9,.15,37,Color("e5b64c"),true,card)
	label("+%d %s%s" % [int(result.coins),tr_key("coins")," · "+tr_key("assisted") if result.assisted else ""],.05,.61,.9,.1,16,muted,true,card)
	audio.effect("exact" if result.exact else "perfect" if result.perfect else "star%d" % int(result.stars))
	audio.haptic(55 if result.perfect else 30)
	state = State.REWARD
	busy = false
	button(tr_key("continue"),.1,.78,.8,.15,func() -> void: show_paint(result),true,card)

func show_paint(result: Dictionary) -> void:
	busy = true
	close_modal()
	world.visible = true
	world.set_process(true)
	state = State.WORLD_TRANSITION
	clear_screen()
	label(tr_key("paint"),.05,.065,.9,.08,22,ink,true)
	coin_label = label("● %d" % int(model.data.coins),.68,.005,.3,.05,20,Color("ab7827"),true)
	state = State.PAINT
	if result.improved:
		audio.effect(model.level(level_id).paint_tool)
		await world.reveal(level_id)
		audio.effect("paint_complete")
		audio.haptic(30)
		if not model.finish_reveal():
			busy = false
			toast(tr_key("save_error"))
			return
	else:
		world.focus_level(level_id)
		await get_tree().create_timer(.5).timeout
	if int(result.coins)>0:
		audio.effect("coins")
		coin_flight(int(result.coins))
	if result.world_restored or result.perfect_world:
		await celebration_final(result.perfect_world)
		return
	if result.area_complete:
		state = State.AREA_COMPLETE
		world.focus_area(int(model.level(level_id).area_id))
		audio.effect("area_complete")
		audio.haptic(70)
		await get_tree().create_timer(.65).timeout
		var card: Panel = modal(tr_key("area_complete"),str(model.content.areas[int(model.level(level_id).area_id)-1].name))
		label("★ %d / 45" % model.area_stars(int(model.level(level_id).area_id)),.1,.48,.8,.15,34,Color("ab7827"),true,card)
		busy = false
		button(tr_key("continue"),.1,.77,.8,.16,advance,true,card)
	else:
		label(tr_key("improved") if int(model.data.attempts.get(str(level_id),1))>1 else tr_key("success"),.05,.69,.9,.06,24,ink,true)
		busy = false
		button(tr_key("next"),.04,.83,.92,.077,advance,true)
		button(tr_key("world"),.2,.925,.6,.052,free_world)
		if level_id==1: toast(tr_key("coin_tip"))
		elif level_id==3: toast(tr_key("helper_tip"))

func advance() -> void:
	state = State.NEXT_LEVEL
	var next: int = model.next_level()
	var current_area: int = int(model.level(level_id).area_id)
	var next_area: int = int(model.level(next).area_id)
	if current_area != next_area:
		busy = true
		clear_screen()
		world.focus_area(next_area)
		label("%s %02d" % [tr_key("area"),next_area],.05,.35,.9,.14,45,ink,true)
		label(str(model.content.areas[next_area-1].name),.05,.48,.9,.09,22,ink,true)
		await get_tree().create_timer(1.15).timeout
		busy = false
	start_level(next)

func resume_reveal() -> void:
	busy = true
	clear_screen()
	label(tr_key("resume_paint"),.05,.05,.9,.08,22,ink,true)
	await world.reveal(level_id)
	model.finish_reveal()
	busy = false
	lobby()

func celebration_final(perfect: bool) -> void:
	state = State.PERFECT_WORLD if perfect else State.FINAL_WORLD_RESTORE
	audio.effect("perfect_world" if perfect else "world_restored")
	audio.haptic(100)
	await world.finale()
	var card: Panel = modal(tr_key("perfect_world" if perfect else "world_restored"),tr_key("world_done_copy"))
	label("★ %d / 4500" % model.total_stars(),.05,.49,.9,.14,30,Color("ab7827"),true,card)
	busy = false
	button(tr_key("finish"),.06,.78,.88,.16,free_world,true,card)

func free_world() -> void:
	state = State.FREE_WORLD
	busy = false
	world.visible = true
	world.set_process(true)
	world.free_mode = true
	clear_screen()
	panel(0,0,1,.1,Color(1,1,1,.95))
	title_bar(tr_key("world"),lobby)
	label(tr_key("explore_hint"),.03,.12,.94,.065,13,ink,true)
	button(tr_key("continue"),.15,.91,.7,.065,lobby,true)

func show_object(id: int) -> void:
	if state != State.FREE_WORLD or busy: return
	world.free_mode = false
	var r: Dictionary = model.record(id)
	var l: Dictionary = model.level(id)
	var g: Dictionary = model.content.areas[int(l.area_id)-1].groups[(id-1)%15]
	var card: Panel = modal(str(g.name),"%s %d · %.1f%%" % [tr_key("level"),id,float(r.score)])
	label("★".repeat(int(r.stars)),.1,.48,.8,.12,32,Color("ab7827"),true,card)
	button(tr_key("improve"),.08,.69,.84,.15,func() -> void: start_level(id),true,card)
	button(tr_key("close"),.2,.86,.6,.11,free_world,false,card)

func open_levels() -> void:
	page_area = int(model.level(model.next_level()).area_id)
	levels_screen()

func levels_screen() -> void:
	state = State.LOBBY
	world.free_mode = false
	clear_screen(true)
	title_bar(tr_key("levels"),lobby)
	label("%s %02d" % [tr_key("area"),page_area],.2,.105,.6,.047,24,ink,true)
	label(str(model.content.areas[page_area-1].name),.12,.155,.76,.055,20,ink,true)
	button("‹",0,.128,.12,.065,func() -> void:
		page_area = maxi(1,page_area-1)
		levels_screen()
	)
	button("›",.88,.128,.12,.065,func() -> void:
		page_area = mini(100,page_area+1)
		levels_screen()
	)
	label("★ %d / 45" % model.area_stars(page_area),.1,.217,.8,.04,18,Color("ab7827"),true)
	for i in range(15):
		var id: int = (page_area-1)*15+i+1
		var r: Dictionary = model.record(id)
		var text: String = "%d\n%s" % [id,"★".repeat(int(r.get("stars",0))) if model.available(id) else tr_key("locked")]
		var b: Button = button(text,(i%3)*.34,.288+(i/3)*.105,.32,.089,func() -> void: start_level(id),false)
		b.disabled = not model.available(id)
	label(tr_key("gate"),.03,.833,.94,.09,13,muted,true)
	button(tr_key("world"),.15,.931,.7,.053,free_world)

func shop() -> void:
	state = State.SHOP
	world.free_mode = false
	clear_screen(true)
	title_bar(tr_key("shop"),lobby)
	var names: Array = ["peek","vision","auto"]
	var symbols: Array = ["1", "↕", "✦"]
	var prices: Array = [30,50,80]
	for i in range(3):
		var y: float = .155+i*.245
		panel(0,y,1,.223)
		label(symbols[i],.045,y+.025,.14,.055,29,accent,true)
		label(tr_key(names[i]),.23,y+.018,.72,.053,23)
		label(tr_key(names[i]+"_desc"),.23,y+.073,.7,.068,14,muted)
		label("%s: %d" % [tr_key("owned"),int(model.data.helpers[i])],.055,y+.15,.4,.048,15,muted)
		button("%s · %d ●" % [tr_key("buy"),prices[i]],.5,y+.15,.45,.056,func() -> void: purchase(i),true)
	label(tr_key("coin_tip"),.04,.902,.92,.062,13,muted,true)

func purchase(kind: int) -> void:
	if Time.get_ticks_msec()-last_purchase<500: return
	last_purchase = Time.get_ticks_msec()
	if int(model.data.tutorial)<3:
		toast(tr_key("helper_not_yet"))
		return
	busy = true
	var ok: bool = model.buy(kind)
	busy = false
	if not ok:
		toast(tr_key("save_error") if not model.error.is_empty() else tr_key("insufficient"))
		return
	audio.effect("spend")
	audio.haptic(20)
	shop()
	toast(tr_key("purchase_done"))

func settings_screen() -> void:
	state = State.SETTINGS
	world.free_mode = false
	clear_screen(true)
	title_bar(tr_key("settings"),lobby)
	for i in range(2):
		var key: String = ["music","sfx"][i]
		var y: float = .145+i*.113
		panel(0,y,1,.099)
		label(tr_key(key),.04,y+.014,.36,.067,18)
		var slider: HSlider = HSlider.new()
		slider.min_value = 0
		slider.max_value = 1
		slider.step = .05
		slider.value = float(model.data.settings[key])
		place(slider,.43,y+.02,.5,.056)
		slider.value_changed.connect(func(v: float) -> void:
			var next: Dictionary = model.data.duplicate(true)
			next.settings[key] = v
			if model.commit(next):
				audio.settings = model.data.settings
				audio.apply()
			else: toast(tr_key("save_error"))
		)
	panel(0,.378,1,.085)
	label(tr_key("haptics"),.04,.39,.5,.06,18)
	button(tr_key("on" if model.data.settings.haptics else "off"),.66,.392,.29,.058,func() -> void:
		set_setting("haptics",not bool(model.data.settings.haptics))
	)
	panel(0,.488,1,.085)
	label(tr_key("language"),.04,.499,.4,.06,18)
	button("العربية" if model.data.settings.language=="en" else "English",.55,.502,.4,.056,func() -> void:
		set_setting("language","ar" if model.data.settings.language=="en" else "en")
	)
	label(tr_key("graphics"),.03,.594,.94,.038,18)
	var qualities: Array = ["AUTO","HIGH","BATTERY SAVER"]
	for i in range(3):
		button(qualities[i],i*.34,.645,.32,.064,func() -> void: set_setting("graphics",qualities[i]),model.data.settings.graphics==qualities[i])
	button(tr_key("about"),0,.765,1,.065,about_screen)
	var reset_button: Button = button(tr_key("reset"),0,.868,1,.071,reset_modal)
	reset_button.add_theme_color_override("font_color",Color("c55761"))

func set_setting(key: String,value: Variant) -> void:
	var next: Dictionary = model.data.duplicate(true)
	next.settings[key] = value
	if not model.commit(next):
		toast(tr_key("save_error"))
		return
	audio.settings = model.data.settings
	audio.apply()
	if key=="graphics": world.set_quality(str(value))
	settings_screen()

func about_screen() -> void:
	var card: Panel = modal(tr_key("about"),tr_key("about_body"))
	button(tr_key("close"),.15,.8,.7,.14,close_modal,true,card)

func reset_modal() -> void:
	reset_elapsed = 0
	var card: Panel = modal(tr_key("reset_title"),tr_key("reset_warning"))
	button(tr_key("cancel"),.1,.61,.8,.13,close_modal,false,card)
	var hold: Button = button(tr_key("hold_reset"),.06,.79,.88,.14,func() -> void: pass,true,card)
	hold.add_theme_stylebox_override("normal",box_style(Color("c65860")))
	hold.button_down.connect(func() -> void:
		reset_elapsed = 0
		reset_held = true
	)
	hold.button_up.connect(cancel_hold)
	hold.mouse_exited.connect(cancel_hold)
	reset_progress = ProgressBar.new()
	reset_progress.show_percentage = false
	reset_progress.mouse_filter = Control.MOUSE_FILTER_IGNORE
	place(reset_progress,.06,.945,.88,.025,card)

func cancel_hold() -> void:
	reset_held = false
	reset_elapsed = 0
	if is_instance_valid(reset_progress): reset_progress.value = 0

func perform_reset() -> void:
	busy = true
	var fresh: Dictionary = model.save.reset(model.data.settings)
	if fresh.is_empty():
		busy = false
		close_modal()
		toast(tr_key("save_error"))
		return
	model.data = fresh
	for id in world.groups: world.apply_record(world.groups[id],id)
	for a in world.citizens:
		for person in world.citizens[a]: person.queue_free()
	world.citizens.clear()
	for a in range(1,101): world.update_area_color(a)
	busy = false
	lobby()
	toast(tr_key("reset_done"))

func modal(title: String,body: String) -> Panel:
	close_modal()
	overlay = Control.new()
	overlay.size = ui.size
	overlay.mouse_filter = Control.MOUSE_FILTER_STOP
	ui.add_child(overlay)
	var dim: ColorRect = ColorRect.new()
	dim.color = Color(.08,.15,.2,.5)
	dim.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	overlay.add_child(dim)
	var card: Panel = Panel.new()
	card.size = Vector2(safe.size.x*.96,minf(safe.size.y*.53,460))
	card.position = safe.position+(safe.size-card.size)*.5
	card.add_theme_stylebox_override("panel",box_style(Color("f8fafb"),25))
	overlay.add_child(card)
	label(title,.06,.055,.88,.15,25,ink,true,card)
	label(body,.08,.22,.84,.34,17,muted,true,card)
	return card

func close_modal() -> void:
	cancel_hold()
	if overlay != null and is_instance_valid(overlay):
		ui.remove_child(overlay)
		overlay.queue_free()
	overlay = null

func toast(message: String) -> void:
	if safe == null: return
	var p: Panel = panel(.035,.39,.93,.15,Color("233b4f"))
	label(message,.06,.06,.88,.88,16,Color.WHITE,true,p)
	var tween: Tween = create_tween()
	tween.tween_interval(2.1)
	tween.tween_property(p,"modulate:a",0.0,.25)
	tween.tween_callback(p.queue_free)

func coin_flight(amount: int) -> void:
	for i in range(7):
		var coin: Label = label("●",.42+i*.022,.54,.07,.06,25,Color("edbc54"),true)
		var tween: Tween = create_tween().set_parallel(true)
		tween.tween_property(coin,"position",Vector2(safe.size.x*.87,safe.size.y*.05),.6+i*.035).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
		tween.tween_property(coin,"modulate:a",0.0,.8+i*.035)
		tween.chain().tween_callback(coin.queue_free)
	label("+%d ●" % amount,.3,.6,.4,.06,24,Color("ab7827"),true)
