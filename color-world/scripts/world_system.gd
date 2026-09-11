class_name WorldSystem
extends Node3D
signal object_selected(level_id: int)

var model: GameModel
var camera: Camera3D
var environment: Environment
var detail_root: Node3D
var groups: Dictionary = {}
var details: Dictionary = {}
var proxies: Dictionary = {}
var pending: Array = []
var base_meshes: Dictionary = {}
var world_shader: Shader = preload("res://scripts/paint.gdshader")
var center: Vector3 = Vector3(0,0,0)
var desired_center: Vector3 = Vector3(0,0,0)
var distance: float = 32.0
var desired_distance: float = 32.0
var yaw: float = 0.55
var desired_yaw: float = 0.55
var pitch: float = 0.78
var free_mode: bool = false
var fingers: Dictionary = {}
var drag_distance: float = 0.0
var last_stream: int = -1
var active_area: int = 1
var painting: bool = false
var clock: float = 0.0
var paint_tool: Node3D
var quality: String = "AUTO"
var citizens: Dictionary = {}

func setup(game_model: GameModel) -> void:
	model = game_model
	var world_environment: WorldEnvironment = WorldEnvironment.new()
	environment = Environment.new()
	environment.background_mode = Environment.BG_COLOR
	environment.background_color = Color("dceaf0")
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.ambient_light_color = Color("fff5e4")
	environment.ambient_light_energy = 0.25
	environment.tonemap_mode = Environment.TONE_MAPPER_LINEAR
	world_environment.environment = environment
	add_child(world_environment)
	var sun: DirectionalLight3D = DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-52,-28,0)
	sun.light_color = Color("fff3df")
	sun.light_energy = 0.45
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 75
	add_child(sun)
	camera = Camera3D.new()
	camera.fov = 48
	camera.far = 550
	camera.current = true
	add_child(camera)
	detail_root = Node3D.new()
	add_child(detail_root)
	base_meshes.box = BoxMesh.new()
	var cylinder: CylinderMesh = CylinderMesh.new()
	cylinder.top_radius = 0.5
	cylinder.bottom_radius = 0.5
	cylinder.height = 1
	cylinder.radial_segments = 12
	base_meshes.cylinder = cylinder
	var sphere_mesh: SphereMesh = SphereMesh.new()
	sphere_mesh.radius = 0.5
	sphere_mesh.height = 1
	sphere_mesh.radial_segments = 12
	sphere_mesh.rings = 6
	base_meshes.sphere = sphere_mesh
	var cone_mesh: CylinderMesh = CylinderMesh.new()
	cone_mesh.top_radius = 0
	cone_mesh.bottom_radius = 0.5
	cone_mesh.height = 1
	cone_mesh.radial_segments = 12
	base_meshes.cone = cone_mesh
	var torus_mesh: TorusMesh = TorusMesh.new()
	torus_mesh.inner_radius = 0.38
	torus_mesh.outer_radius = 0.5
	torus_mesh.rings = 16
	torus_mesh.ring_segments = 6
	base_meshes.torus = torus_mesh
	var ground: MeshInstance3D = MeshInstance3D.new()
	var gm: BoxMesh = BoxMesh.new()
	gm.size = Vector3(252,.5,252)
	ground.mesh = gm
	ground.position = Vector3(108,-.7,108)
	var mat: StandardMaterial3D = StandardMaterial3D.new()
	mat.albedo_color = Color("b7cfce")
	mat.roughness = 1
	ground.material_override = mat
	add_child(ground)
	for area in model.content.areas:
		make_proxy(area)
	focus_area(int(model.level(model.next_level()).area_id), true)

func theme_for(biome: String) -> Dictionary:
	match biome:
		"ocean": return {"sky":Color("cfe9f4"),"ambient":Color("fff0d9"),"base":Color("a8d8d7"),"road":Color("9eb7be")}
		"water": return {"sky":Color("d8ecf3"),"ambient":Color("fff4df"),"base":Color("abd7cf"),"road":Color("9fb7bd")}
		"forest": return {"sky":Color("dce9df"),"ambient":Color("fff1dd"),"base":Color("aecf9d"),"road":Color("a6b2aa")}
		"garden": return {"sky":Color("e0edf0"),"ambient":Color("fff5df"),"base":Color("b7d9a8"),"road":Color("aab7b7")}
		"market": return {"sky":Color("efe4da"),"ambient":Color("fff0dc"),"base":Color("ddc59f"),"road":Color("b6aba4")}
		"airport": return {"sky":Color("dce8f0"),"ambient":Color("fff5e7"),"base":Color("b7ced1"),"road":Color("9aa9b0")}
		"wind": return {"sky":Color("e7e6ef"),"ambient":Color("fff2e1"),"base":Color("c9c3d8"),"road":Color("aaa9b2")}
		_: return {"sky":Color("dce6ec"),"ambient":Color("fff0df"),"base":Color("b8cfce"),"road":Color("9faeb5")}

func apply_area_atmosphere(a: int) -> void:
	if environment == null: return
	var theme: Dictionary = theme_for(str(model.content.areas[a-1].biome))
	var progress: float = float(model.area_count(a))/15.0
	environment.background_color = Color("dce5ea").lerp(theme.sky,0.45+progress*.55)
	environment.ambient_light_color = Color("fff5e4").lerp(theme.ambient,0.35+progress*.65)
	environment.ambient_light_energy = lerpf(.23,.34,progress)

func make_proxy(area: Dictionary) -> void:
	var root: Node3D = Node3D.new()
	root.position = vec(area.origin)
	add_child(root)
	var plinth: MeshInstance3D = MeshInstance3D.new()
	var mesh: BoxMesh = BoxMesh.new()
	mesh.size = Vector3(22,.45,22)
	plinth.mesh = mesh
	plinth.position.y = -.25
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.albedo_color = Color("aab4bb")
	material.roughness = 1
	plinth.material_override = material
	root.add_child(plinth)
	var roads: Array = []
	# A continuous street network crosses all 100 districts.
	for axis in range(2):
		var road: MeshInstance3D = MeshInstance3D.new()
		var bm: BoxMesh = BoxMesh.new()
		bm.size = Vector3(24,.035,1.3) if axis == 0 else Vector3(1.3,.035,24)
		road.mesh = bm
		road.position.y = .01
		var rm: StandardMaterial3D = StandardMaterial3D.new()
		rm.albedo_color = Color("b5bcc3")
		rm.roughness = .95
		road.material_override = rm
		root.add_child(road)
		roads.append(road)
	var landmark: MeshInstance3D = build_group(area.groups[0])
	root.add_child(landmark)
	proxies[int(area.id)] = {"root":root,"landmark":landmark,"base":plinth,"roads":roads}
	update_area_color(int(area.id))

static func vec(a: Array) -> Vector3:
	return Vector3(float(a[0]),float(a[1]),float(a[2]))

func build_group(g: Dictionary) -> MeshInstance3D:
	# Merge each composed object into one vertex-colored surface/draw call.
	var surface: SurfaceTool = SurfaceTool.new()
	surface.begin(Mesh.PRIMITIVE_TRIANGLES)
	for part in g.primitives:
		var mesh: PrimitiveMesh = base_meshes[part.mesh]
		var arrays: Array = mesh.get_mesh_arrays()
		var vertices: PackedVector3Array = arrays[Mesh.ARRAY_VERTEX]
		var normals: PackedVector3Array = arrays[Mesh.ARRAY_NORMAL]
		var indices: PackedInt32Array = arrays[Mesh.ARRAY_INDEX]
		var basis: Basis = Basis.from_euler(Vector3(float(part.get("rx",0)),float(part.get("ry",0)),float(part.get("rz",0))))
		basis = basis.scaled(vec(part.s))
		var normal_basis: Basis = basis.inverse().transposed()
		var offset: Vector3 = vec(part.p)
		for index in indices:
			surface.set_normal((normal_basis*normals[index]).normalized())
			var tint: float = clampf(float(part.tint)/1.5,0,1)
			surface.set_color(Color(tint,tint,tint,1))
			surface.add_vertex(basis*vertices[index]+offset)
	surface.index()
	var result: MeshInstance3D = MeshInstance3D.new()
	result.mesh = surface.commit()
	result.position = vec(g.position)
	result.rotation.y = float(g.rotation)
	result.set_meta("level_id",int(g.level_id))
	result.set_meta("kind",g.kind)
	result.set_meta("base_position",result.position)
	result.set_meta("life",g.life)
	var shader_material: ShaderMaterial = ShaderMaterial.new()
	shader_material.shader = world_shader
	var bounds: AABB = result.mesh.get_aabb()
	shader_material.set_shader_parameter("min_y",bounds.position.y)
	shader_material.set_shader_parameter("max_y",bounds.end.y)
	result.material_override = shader_material
	apply_record(result,int(g.level_id))
	return result

func apply_record(node: MeshInstance3D, id: int) -> void:
	var record: Dictionary = model.record(id)
	var material: ShaderMaterial = node.material_override
	if record.is_empty():
		material.set_shader_parameter("reveal",0.0)
	else:
		material.set_shader_parameter("paint_color",ColorSystem.color_of(record.ratios,model.level(id)))
		material.set_shader_parameter("reveal",1.0)
		if node.get_meta("kind") in ["lamp","lantern","neontower"]: material.set_shader_parameter("glow",0.3)

func update_area_color(a: int) -> void:
	var t: float = float(model.area_count(a))/15.0
	var theme: Dictionary = theme_for(str(model.content.areas[a-1].biome))
	var material: StandardMaterial3D = proxies[a].base.material_override
	material.albedo_color = Color("aab4bb").lerp(theme.base,t)
	for road in proxies[a].roads:
		var rm: StandardMaterial3D = road.material_override
		rm.albedo_color = Color("b5bcc3").lerp(theme.road,t*.8)
	apply_record(proxies[a].landmark,(a-1)*15+1)
	if a == active_area: apply_area_atmosphere(a)
	if details.has(a) and model.area_count(a)==15: awaken(a)

func stream_around(a: int) -> void:
	active_area = a
	var origin: Vector3 = vec(model.content.areas[a-1].origin)
	var wanted: Array = []
	var radius: float = 25 if quality == "BATTERY SAVER" else 35
	for area in model.content.areas:
		if vec(area.origin).distance_to(origin) < radius: wanted.append(int(area.id))
	for old in details.keys():
		if not wanted.has(old):
			for g in model.content.areas[old-1].groups: groups.erase(int(g.level_id))
			citizens.erase(old)
			details[old].queue_free()
			details.erase(old)
			proxies[old].landmark.visible = true
	pending.clear()
	# Load focus first, then one surrounding area per frame.
	if not details.has(a): load_area(a)
	for id in wanted:
		if not details.has(id): pending.append(id)

func load_area(a: int) -> void:
	var area: Dictionary = model.content.areas[a-1]
	var root: Node3D = Node3D.new()
	root.position = vec(area.origin)
	detail_root.add_child(root)
	for g in area.groups:
		var node: MeshInstance3D = build_group(g)
		root.add_child(node)
		groups[int(g.level_id)] = node
	details[a] = root
	proxies[a].landmark.visible = false
	if model.area_count(a)==15: awaken(a)

func focus_area(a: int, instant: bool = false) -> void:
	free_mode = false
	desired_center = vec(model.content.areas[a-1].origin)
	desired_center.y = .8
	desired_distance = 35
	desired_yaw = .55 + (a%3)*.2
	pitch = .78
	stream_around(a)
	apply_area_atmosphere(a)
	if instant:
		center = desired_center
		distance = desired_distance
		yaw = desired_yaw

func focus_level(id: int) -> void:
	var level: Dictionary = model.level(id)
	if not details.has(int(level.area_id)): stream_around(int(level.area_id))
	active_area = int(level.area_id)
	apply_area_atmosphere(active_area)
	desired_center = vec(level.camera.focus)
	desired_distance = float(level.camera.distance)
	desired_yaw = float(level.camera.yaw)
	pitch = float(level.camera.pitch)

func _process(delta: float) -> void:
	if camera == null: return
	clock += delta
	if not pending.is_empty(): load_area(int(pending.pop_front()))
	center = center.lerp(desired_center,1.0-exp(-delta*5))
	distance = lerpf(distance,desired_distance,1.0-exp(-delta*5))
	yaw = lerp_angle(yaw,desired_yaw,1.0-exp(-delta*5))
	camera.position = center+Vector3(sin(yaw)*cos(pitch),sin(pitch),cos(yaw)*cos(pitch))*distance
	camera.look_at(center,Vector3.UP)
	if not painting and visible:
		for id in groups:
			var g: MeshInstance3D = groups[id]
			if not bool(g.get_meta("life")) or model.record(id).is_empty(): continue
			var origin: Vector3 = g.get_meta("base_position")
			var kind: String = g.get_meta("kind")
			if kind in ["car","boat","robot","drone"]: g.position.x = origin.x+sin(clock*.35+id)*.6
			elif kind in ["tree","fountain","balloon"]: g.position.y = origin.y+sin(clock*1.4+id)*.035
	for a in citizens:
		for i in range(citizens[a].size()):
			var person: Node3D = citizens[a][i]
			var pace: float = .18+float((i%3))*0.035
			var radius: float = 2.8+float(i%2)*1.1
			person.position = Vector3(sin(clock*pace+i*2.1)*radius,.02,cos(clock*pace+i*2.1)*radius)
			person.rotation.y = -clock*pace-i*2.1
	if free_mode:
		var closest: int = 1
		var d: float = INF
		for a in model.content.areas:
			var dist: float = vec(a.origin).distance_squared_to(desired_center)
			if dist < d:
				d = dist
				closest = int(a.id)
		if closest != active_area:
			stream_around(closest)
			apply_area_atmosphere(closest)

func paint_duration(tool: String) -> float:
	match tool:
		"roller": return 1.85
		"spray": return 2.25
		"wide_brush": return 1.7
		"splash": return 1.65
		"magic": return 2.35
		_: return 2.05

func paint_motion(tool: String,start: Vector3,end: Vector3,t: float) -> Vector3:
	var p: Vector3 = start.lerp(end,t)
	match tool:
		"brush":
			p.x += sin(t*TAU*2.0)*.42
		"roller":
			p.x += sin(t*TAU)*.22
		"spray":
			p.x += sin(t*TAU*4.0)*.62
			p.z += cos(t*TAU*3.0)*.24
		"wide_brush":
			p.x += sin(t*TAU*1.5)*.32
		"splash":
			p.y += sin(t*PI)*1.15
			p.x += sin(t*TAU)*.35
		"magic":
			p.x += sin(t*TAU*3.0)*.72
			p.y += sin(t*PI)*.62
			p.z += cos(t*TAU*2.0)*.38
	return p

func paint_rotation(tool: String,t: float) -> float:
	match tool:
		"roller": return -.15+sin(t*TAU)*.12
		"spray": return .25+sin(t*TAU*3.0)*.2
		"splash": return -1.0+t*2.0
		"magic": return t*TAU*1.5
		_: return -.35+sin(t*TAU*2.0)*.28

func reveal(id: int) -> void:
	painting = true
	free_mode = false
	focus_level(id)
	await get_tree().create_timer(.6).timeout
	var node: MeshInstance3D = groups[id]
	var material: ShaderMaterial = node.material_override
	var color: Color = ColorSystem.color_of(model.record(id).ratios,model.level(id))
	var tool: String = str(model.level(id).paint_tool)
	material.set_shader_parameter("paint_color",color)
	material.set_shader_parameter("reveal",0.0)
	paint_tool = make_tool(tool,color)
	add_child(paint_tool)
	var bounds: AABB = node.mesh.get_aabb()
	var start: Vector3 = node.global_position+Vector3(-1.5,bounds.position.y,-1)
	var end: Vector3 = node.global_position+Vector3(1.5,bounds.end.y,-1)
	paint_tool.position = start
	var pigment: CPUParticles3D = pigment_particles(color,tool)
	paint_tool.add_child(pigment)
	pigment.position = Vector3(.4,.8,0)
	var duration: float = paint_duration(tool)
	var tween: Tween = create_tween().set_parallel(true)
	tween.tween_method(func(v: float) -> void:
		material.set_shader_parameter("reveal",smoothstep(0.0,1.0,v))
	,0.0,1.0,duration)
	tween.tween_method(func(v: float) -> void:
		if is_instance_valid(paint_tool):
			paint_tool.position = paint_motion(tool,start,end,v)
			paint_tool.rotation.z = paint_rotation(tool,v)
	,0.0,1.0,duration).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	await tween.finished
	paint_tool.queue_free()
	paint_tool = null
	apply_record(node,id)
	var pulse: Tween = create_tween()
	pulse.tween_property(node,"scale",Vector3(1.035,1.035,1.035),.1).set_trans(Tween.TRANS_BACK)
	pulse.tween_property(node,"scale",Vector3.ONE,.16).set_trans(Tween.TRANS_SINE)
	await pulse.finished
	update_area_color(int(model.level(id).area_id))
	painting = false

func make_tool(kind: String,color: Color) -> Node3D:
	var root: Node3D = Node3D.new()
	var handle: MeshInstance3D = MeshInstance3D.new()
	var hm: CylinderMesh = CylinderMesh.new()
	hm.top_radius = .09
	hm.bottom_radius = .09
	hm.height = 1.7
	handle.mesh = hm
	handle.rotation.z = -.5
	var hmat: StandardMaterial3D = StandardMaterial3D.new()
	hmat.albedo_color = Color("e8b778")
	handle.material_override = hmat
	root.add_child(handle)
	var head: MeshInstance3D = MeshInstance3D.new()
	if kind in ["roller","spray"]:
		var cm: CylinderMesh = CylinderMesh.new()
		cm.top_radius = .22 if kind == "roller" else .16
		cm.bottom_radius = cm.top_radius
		cm.height = 1.1 if kind == "roller" else .65
		head.mesh = cm
		head.rotation.z = PI/2
	elif kind in ["splash","magic"]:
		var sm: SphereMesh = SphereMesh.new()
		sm.radius = .32
		sm.height = .64
		head.mesh = sm
	else:
		var bm: BoxMesh = BoxMesh.new()
		bm.size = Vector3(.9 if kind == "wide_brush" else .55,.6,.2)
		head.mesh = bm
	head.position = Vector3(.45,.85,0)
	var m: StandardMaterial3D = StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = .55
	m.emission_enabled = kind == "magic"
	m.emission = color
	m.emission_energy_multiplier = 1.35 if kind == "magic" else 0.0
	head.material_override = m
	root.add_child(head)
	return root

func finale() -> void:
	free_mode = false
	desired_center = Vector3(108,0,108)
	desired_distance = 340
	pitch = 1.0
	if environment != null:
		environment.background_color = Color("d7edf2")
		environment.ambient_light_color = Color("fff1cf")
		environment.ambient_light_energy = .38
	await get_tree().create_timer(3.0).timeout

func set_quality(value: String) -> void:
	quality = value
	Engine.max_fps = 30 if value == "BATTERY SAVER" else 60
	for child in get_children():
		if child is DirectionalLight3D: child.shadow_enabled = value != "BATTERY SAVER"
	if model != null: stream_around(active_area)

func _unhandled_input(event: InputEvent) -> void:
	if not free_mode or painting: return
	if event is InputEventScreenTouch:
		if event.pressed:
			fingers[event.index] = event.position
			drag_distance = 0
		else:
			fingers.erase(event.index)
			if drag_distance < 12: pick(event.position)
	elif event is InputEventScreenDrag:
		drag_distance += event.relative.length()
		if fingers.size() >= 2:
			var keys: Array = fingers.keys()
			var other: int = keys[0] if keys[0] != event.index else keys[1]
			var previous: Vector2 = fingers.get(event.index,event.position)
			var other_pos: Vector2 = fingers[other]
			var old_distance: float = previous.distance_to(other_pos)
			var new_distance: float = event.position.distance_to(other_pos)
			if new_distance > 10: desired_distance = clampf(desired_distance*old_distance/new_distance,8,320)
			var pan: Vector2 = event.relative * desired_distance *.0009
			desired_center += Vector3(-pan.x,0,-pan.y).rotated(Vector3.UP,yaw)
		else:
			desired_yaw -= event.relative.x*.008
			pitch = clampf(pitch+event.relative.y*.004,.25,1.3)
		fingers[event.index] = event.position
		desired_center.x = clampf(desired_center.x,-10,226)
		desired_center.z = clampf(desired_center.z,-10,226)
	elif event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP: desired_distance = maxf(8,desired_distance*.9)
		if event.button_index == MOUSE_BUTTON_WHEEL_DOWN: desired_distance = minf(320,desired_distance*1.1)
	elif event is InputEventMagnifyGesture:
		desired_distance = clampf(desired_distance/event.factor,8,320)
	elif event is InputEventPanGesture:
		desired_center += Vector3(event.delta.x,0,event.delta.y).rotated(Vector3.UP,yaw)

func pick(screen: Vector2) -> void:
	var best: int = 0
	var nearest: float = 48
	for id in groups:
		if model.record(id).is_empty(): continue
		var pos: Vector3 = groups[id].global_position+Vector3.UP
		if camera.is_position_behind(pos): continue
		var d: float = camera.unproject_position(pos).distance_to(screen)
		if d < nearest:
			nearest = d
			best = id
	if best > 0: object_selected.emit(best)

func pigment_particles(color: Color, tool: String) -> CPUParticles3D:
	var particles: CPUParticles3D = CPUParticles3D.new()
	var base_amount: int = 18 if quality == "BATTERY SAVER" else 36
	particles.amount = roundi(float(base_amount)*(1.35 if tool in ["spray","splash","magic"] else 1.0))
	particles.lifetime = .55 if tool != "magic" else .8
	particles.emitting = true
	particles.direction = Vector3(0,-1,0)
	particles.spread = 80 if tool in ["spray","splash"] else 45 if tool == "magic" else 25
	particles.initial_velocity_min = .4
	particles.initial_velocity_max = 2.0 if tool in ["spray","splash"] else 1.6
	particles.gravity = Vector3(0,-2,0) if tool != "magic" else Vector3(0,-.35,0)
	particles.scale_amount_min = .035
	particles.scale_amount_max = .1 if tool in ["splash","magic"] else .085
	var particle_mesh: SphereMesh = SphereMesh.new()
	particle_mesh.radius = .5
	particle_mesh.height = 1
	particle_mesh.radial_segments = 6
	particle_mesh.rings = 3
	var mat: StandardMaterial3D = StandardMaterial3D.new()
	mat.albedo_color = color
	mat.emission_enabled = tool == "magic"
	mat.emission = color
	mat.emission_energy_multiplier = 1.2 if tool == "magic" else 0.0
	particle_mesh.material = mat
	particles.mesh = particle_mesh
	return particles

func awaken(a: int) -> void:
	if citizens.has(a) or not details.has(a): return
	citizens[a] = []
	var count: int = 2 if quality == "BATTERY SAVER" else 5
	for i in range(count):
		var person: MeshInstance3D = build_group({"id":"citizen", "level_id":(a-1)*15+(i%15)+1,
			"name":"Citizen", "kind":"citizen", "position":[0,0,0], "rotation":0,
			"life":true, "primitives":[
				{"mesh":"sphere","p":[0,.82,0],"s":[.3,.32,.3],"tint":1.3},
				{"mesh":"cylinder","p":[0,.46,0],"s":[.28,.48,.25],"tint":.85},
				{"mesh":"box","p":[-.08,.12,0],"s":[.1,.24,.12],"tint":.4},
				{"mesh":"box","p":[.08,.12,0],"s":[.1,.24,.12],"tint":.4}]})
		details[a].add_child(person)
		citizens[a].append(person)
