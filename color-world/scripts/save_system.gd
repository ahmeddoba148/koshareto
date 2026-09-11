class_name SaveSystem
extends RefCounted

var base: String = "user://color_world"
var last_error: String = ""

static func fresh(settings: Dictionary = {}) -> Dictionary:
	var prefs: Dictionary = {"music":0.55,"sfx":0.7,"haptics":true,"language":"en","graphics":"AUTO"}
	prefs.merge(settings, true)
	return {"saveVersion":1,"revision":0,"current_level":1,"unlocked_areas":1,
		"levels":{},"coins":0,"helpers":[0,0,0],"streak":0,"best_streak":0,
		"settings":prefs,"tutorial":0,"areas_complete":[],"world_restored":false,
		"perfect_world":false,"attempts":{},"assistance":{},"pending_reveal":0}

static func valid(d: Variant) -> bool:
	if not d is Dictionary: return false
	for k in ["saveVersion","levels","coins","helpers","settings","current_level","unlocked_areas","attempts","assistance","revision","streak","best_streak","tutorial","areas_complete","world_restored","perfect_world","pending_reveal"]:
		if not d.has(k): return false
	if int(d.saveVersion) != 1 or not d.levels is Dictionary: return false
	if not d.settings is Dictionary or not d.attempts is Dictionary or not d.assistance is Dictionary: return false
	for k in ["music","sfx","haptics","language","graphics"]:
		if not d.settings.has(k): return false
	if not str(d.settings.language) in ["en","ar"]: return false
	if not str(d.settings.graphics) in ["AUTO","HIGH","BATTERY SAVER"]: return false
	if float(d.settings.music)<0 or float(d.settings.music)>1 or float(d.settings.sfx)<0 or float(d.settings.sfx)>1: return false
	if not d.areas_complete is Array or int(d.revision)<0: return false
	if int(d.coins) < 0 or int(d.unlocked_areas) < 1 or int(d.unlocked_areas) > 100: return false
	if int(d.current_level) < 1 or int(d.current_level) > 1500: return false
	if not d.helpers is Array or d.helpers.size() != 3: return false
	for n in d.helpers:
		if int(n) < 0: return false
	for id in d.levels:
		var item: Variant = d.levels[id]
		if int(id) < 1 or int(id) > 1500 or not item is Dictionary: return false
		for k in ["score","stars","ratios","reward","perfect","exact","assisted"]:
			if not item.has(k): return false
		if float(item.score) < 80 or float(item.score) > 100: return false
		if int(item.stars) != ColorSystem.stars(float(item.score)): return false
		if int(item.reward) < 0 or int(item.reward) > 50: return false
		if not item.ratios is Array or item.ratios.size() != 3: return false
		var total: int = 0
		for n in item.ratios:
			if int(n) < 0 or int(n) > 1000: return false
			total += int(n)
		if total != 1000: return false
	return true

func read_slot(path: String) -> Dictionary:
	if not FileAccess.file_exists(path): return {}
	var f: FileAccess = FileAccess.open(path, FileAccess.READ)
	if f == null: return {}
	var outer: Variant = JSON.parse_string(f.get_as_text())
	f.close()
	if not outer is Dictionary or not outer.has("payload") or not outer.has("sha256"): return {}
	if not outer.payload is String or outer.payload.sha256_text() != outer.sha256: return {}
	var parsed: Variant = JSON.parse_string(outer.payload)
	return parsed if valid(parsed) else {}

func load_game() -> Dictionary:
	# tmp is a fully flushed validated candidate. Recover the highest revision.
	var best: Dictionary = {}
	for suffix in [".json", ".previous", ".tmp"]:
		var candidate: Dictionary = read_slot(base + suffix)
		if not candidate.is_empty() and int(candidate.revision) > int(best.get("revision", -1)):
			best = candidate
	return best if not best.is_empty() else fresh()

func write(d: Dictionary) -> bool:
	last_error = ""
	if not valid(d):
		last_error = "Invalid save data"
		return false
	d.revision = int(d.revision) + 1
	var payload: String = JSON.stringify(d)
	var f: FileAccess = FileAccess.open(base + ".tmp", FileAccess.WRITE)
	if f == null:
		last_error = "Cannot write save"
		return false
	f.store_string(JSON.stringify({"payload":payload,"sha256":payload.sha256_text()}))
	f.flush()
	var write_error: Error = f.get_error()
	f.close()
	if write_error != OK or read_slot(base + ".tmp").is_empty():
		last_error = "Save validation failed"
		return false
	# Keep last primary, then atomic rename on the same filesystem.
	if not read_slot(base + ".json").is_empty():
		var backup_error: Error = DirAccess.copy_absolute(base + ".json", base + ".previous")
		if backup_error != OK:
			last_error = "Backup write failed"
			return false
	var err: Error = DirAccess.rename_absolute(base + ".tmp", base + ".json")
	if err != OK:
		last_error = "Atomic save replace failed"
		return false
	return true

func reset(settings: Dictionary) -> Dictionary:
	var d: Dictionary = fresh(settings)
	var old: Dictionary = load_game()
	d.revision = int(old.revision) + 1
	if not write(d): return {}
	# A reset must not resurrect progress from the old backup.
	DirAccess.copy_absolute(base + ".json", base + ".previous")
	return d
