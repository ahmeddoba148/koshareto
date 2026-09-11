class_name GameModel
extends RefCounted

var content: Dictionary
var data: Dictionary
var save: SaveSystem = SaveSystem.new()
var error: String = ""

func _init() -> void:
	content = JSON.parse_string(FileAccess.get_file_as_string("res://data/world.json"))
	data = save.load_game()

func level(id: int) -> Dictionary:
	return content.levels[clampi(id,1,1500)-1]

func record(id: int) -> Dictionary:
	return data.levels.get(str(id), {})

func total_stars() -> int:
	var count: int = 0
	for r in data.levels.values(): count += int(r.stars)
	return count

func area_stars(area: int) -> int:
	var count: int = 0
	for id in range((area-1)*15+1,area*15+1): count += int(record(id).get("stars",0))
	return count

func area_count(area: int) -> int:
	var count: int = 0
	for id in range((area-1)*15+1,area*15+1):
		if not record(id).is_empty(): count += 1
	return count

func available(id: int) -> bool:
	if id < 1 or id > 1500: return false
	var a: int = (id-1)/15+1
	return a <= int(data.unlocked_areas) and (id == (a-1)*15+1 or not record(id-1).is_empty())

func commit(candidate: Dictionary) -> bool:
	if not save.write(candidate):
		error = save.last_error
		return false
	data = candidate
	error = ""
	return true

func attempt(id: int) -> bool:
	var next: Dictionary = data.duplicate(true)
	next.attempts[str(id)] = int(next.attempts.get(str(id),0))+1
	return commit(next)

func buy(helper: int) -> bool:
	error = ""
	if helper < 0 or helper > 2: return false
	var prices: Array = [30,50,80]
	if int(data.coins) < prices[helper]: return false
	var next: Dictionary = data.duplicate(true)
	next.coins = int(next.coins)-prices[helper]
	next.helpers[helper] = int(next.helpers[helper])+1
	return commit(next)

func use_helper(helper: int, id: int) -> bool:
	error = ""
	if helper < 0 or helper > 2 or int(data.helpers[helper]) < 1 or not available(id): return false
	var next: Dictionary = data.duplicate(true)
	next.helpers[helper] = int(next.helpers[helper])-1
	if helper == 2: next.assistance[str(id)] = true
	return commit(next)

func match_result(id: int, ratios: Array) -> Dictionary:
	var l: Dictionary = level(id)
	var score_value: float = ColorSystem.score(ratios,l)
	var star_count: int = ColorSystem.stars(score_value)
	var assisted: bool = bool(data.assistance.get(str(id),false))
	var result: Dictionary = {"score":score_value,"stars":star_count,"coins":0,
		"improved":false,"assisted":assisted,"perfect":score_value>=99.5 and not assisted,
		"exact":ColorSystem.exact(ratios,l.target) and not assisted,"area_complete":false,
		"world_restored":false,"perfect_world":false,"saved":true}
	var next: Dictionary = data.duplicate(true)
	if star_count == 0:
		next.streak = 0
		result.saved = commit(next)
		return result
	var old: Dictionary = record(id)
	var old_reward: int = int(old.get("reward",0))
	var reward: int = maxi(old_reward,ColorSystem.reward(score_value,assisted))
	var improved: bool = old.is_empty() or score_value > float(old.score)
	var new_record: Dictionary = old.duplicate(true)
	if improved:
		new_record = {"score":score_value,"stars":star_count,"ratios":ratios.duplicate(),"reward":reward,
			"perfect":result.perfect,"exact":result.exact,"assisted":assisted}
		var col: Color = ColorSystem.color_of(ratios,l)
		new_record.paint_color = [col.r,col.g,col.b]
		result.improved = true
	# Skill flags/rewards can improve even when an assisted 100 already exists.
	new_record.reward = reward
	new_record.perfect = bool(new_record.get("perfect",false)) or result.perfect
	new_record.exact = bool(new_record.get("exact",false)) or result.exact
	if not assisted and score_value >= float(new_record.score): new_record.assisted = false
	next.levels[str(id)] = new_record
	next.coins = int(next.coins) + reward-old_reward
	result.coins = reward-old_reward
	if old.is_empty():
		if star_count == 3 and int(next.attempts.get(str(id),0)) == 1 and not assisted: next.streak = int(next.streak)+1
		else: next.streak = 0
		next.best_streak = maxi(int(next.best_streak),int(next.streak))
	var a: int = int(l.area_id)
	var count: int = 0
	var area_sum: int = 0
	for n in range((a-1)*15+1,a*15+1):
		var item: Dictionary = next.levels.get(str(n),{})
		if not item.is_empty():
			count += 1
			area_sum += int(item.stars)
	if count == 15 and not next.areas_complete.has(a):
		next.areas_complete.append(a)
		result.area_complete = true
	if count == 15 and area_sum >= 30: next.unlocked_areas = maxi(int(next.unlocked_areas),mini(100,a+1))
	next.current_level = mini(1500,id+1) if id >= int(next.current_level) else next.current_level
	if int(next.current_level) > int(next.unlocked_areas)*15: next.current_level = int(next.unlocked_areas)*15
	if next.levels.size() == 1500 and not bool(next.world_restored):
		next.world_restored = true
		result.world_restored = true
	var all_stars: int = 0
	for item in next.levels.values(): all_stars += int(item.stars)
	if all_stars == 4500 and not bool(next.perfect_world):
		next.perfect_world = true
		result.perfect_world = true
	next.tutorial = maxi(int(next.tutorial),mini(id,6))
	if improved: next.pending_reveal = id
	result.saved = commit(next)
	return result

func finish_reveal() -> bool:
	var next: Dictionary = data.duplicate(true)
	next.pending_reveal = 0
	return commit(next)

func next_level() -> int:
	for id in range(1, mini(1500,int(data.unlocked_areas)*15)+1):
		if record(id).is_empty() and available(id): return id
	# If a gate needs stars, choose the weakest level in that area.
	var a: int = int(data.unlocked_areas)
	var weakest: int = (a-1)*15+1
	for id in range(weakest,a*15+1):
		if float(record(id).get("score",0)) < float(record(weakest).get("score",0)): weakest = id
	return weakest
