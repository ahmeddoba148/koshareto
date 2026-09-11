class_name GameAudio
extends Node
var settings: Dictionary
var voices: Array = []
var music: Array = []
var ambience: AudioStreamPlayer
var current_theme: int = -1
var current_biome: String = ""
var music_context: float = 1.0
var area_progress: float = 0.0
var last_tick: int = 0
var cache: Dictionary = {}

func _ready() -> void:
	for i in range(8):
		var player: AudioStreamPlayer = AudioStreamPlayer.new()
		add_child(player)
		voices.append(player)
	for i in range(3):
		var player: AudioStreamPlayer = AudioStreamPlayer.new()
		add_child(player)
		music.append(player)
	ambience = AudioStreamPlayer.new()
	add_child(ambience)

func sample(name: String, looped: bool = false) -> AudioStreamWAV:
	if cache.has(name): return cache[name]
	var stream: AudioStreamWAV = load("res://audio/"+name+".wav")
	if looped:
		stream.loop_mode = AudioStreamWAV.LOOP_FORWARD
		stream.loop_begin = 0
		stream.loop_end = stream.data.size()/2
	cache[name] = stream
	return stream

func effect(name: String) -> void:
	if settings.is_empty() or float(settings.sfx) <= 0: return
	if name == "tick":
		if Time.get_ticks_msec()-last_tick < 70: return
		last_tick = Time.get_ticks_msec()
	for player in voices:
		if not player.playing:
			player.stream = sample(name)
			player.volume_db = linear_to_db(float(settings.sfx)) - 5
			player.play()
			return

func haptic(duration: int = 12) -> void:
	if not settings.is_empty() and bool(settings.haptics): Input.vibrate_handheld(duration)

func area(theme: int, biome: String, progress: float) -> void:
	area_progress = progress
	if current_theme != theme:
		current_theme = theme
		for i in range(3):
			music[i].stream = sample("music_%d_%d" % [theme,i],true)
			music[i].play()
	if current_biome != biome:
		current_biome = biome
		ambience.stream = sample("ambient_"+biome,true)
		ambience.play()
	apply()

func context(gameplay: bool) -> void:
	music_context = .55 if gameplay else 1.0
	apply()

func apply() -> void:
	if settings.is_empty(): return
	for i in range(music.size()):
		var layer: float = 1.0 if i == 0 else area_progress if i == 1 else area_progress*area_progress
		music[i].volume_db = linear_to_db(maxf(.00001,float(settings.music)*layer*music_context)) - 5
	if ambience != null: ambience.volume_db = linear_to_db(maxf(.00001,float(settings.sfx)*music_context)) - 13
