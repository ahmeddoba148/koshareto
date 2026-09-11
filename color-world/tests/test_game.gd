extends SceneTree
# Run: godot --headless --path . --script tests/test_game.gd
var assertions: int = 0
var failures: Array = []

func check(condition: bool, message: String) -> void:
	assertions += 1
	if not condition:
		failures.append(message)
		push_error(message)

func _init() -> void:
	call_deferred("run")

func run() -> void:
	var model: GameModel = GameModel.new()
	model.save.base = "user://qa_color_world"
	model.data = SaveSystem.fresh()
	check(model.content.areas.size()==100,"100 areas")
	check(model.content.levels.size()==1500,"1500 levels")
	for area in model.content.areas:
		check(area.groups.size()==15,"15 groups in area %s" % area.id)
	for level in model.content.levels:
		var sum: int = int(level.target[0])+int(level.target[1])+int(level.target[2])
		check(sum==1000,"target sum %s" % level.id)
		check(ColorSystem.score(level.target,level)==100.0,"reachable exact %s" % level.id)
		check(ColorSystem.exact(level.target,level.target),"exact identity")
	var ratios: Array = [334,333,333]
	seed(9301)
	for i in range(20000):
		ratios = ColorSystem.adjust(ratios,i%3,randi_range(-200,1200))
		check(int(ratios[0])+int(ratios[1])+int(ratios[2])==1000,"drag preserves sum")
		check(ratios.min()>=0 and ratios.max()<=1000,"drag range")
	check(ColorSystem.adjust([1000,0,0],0,500)==[500,250,250],"zero-channel redistribution")
	check(ColorSystem.stars(79.9)==0 and ColorSystem.stars(80)==1,"80 boundary")
	check(ColorSystem.stars(89.9)==1 and ColorSystem.stars(90)==2,"90 boundary")
	check(ColorSystem.stars(96.9)==2 and ColorSystem.stars(97)==3,"97 boundary")
	check(ColorSystem.reward(100,true)==35,"auto mix no skill bonus")
	check(not model.buy(0),"cannot buy with no coins")
	check(not model.use_helper(0,1),"cannot use empty inventory")
	# End-to-end progression through production GameModel and SaveSystem.
	for id in range(1,1501):
		check(model.available(id),"level unlocked %s" % id)
		check(model.attempt(id),"attempt saved %s" % id)
		var result: Dictionary = model.match_result(id,model.level(id).target)
		check(result.saved,"completion saved %s" % id)
		check(result.stars==3 and result.coins==50,"exact award %s" % id)
		if id%15==0:
			check(result.area_complete,"area completion %s" % id)
			check(model.area_stars(id/15)==45,"area star sum")
		if id in [1,15,150,750,1500]:
			var restored: Dictionary = model.save.load_game()
			check(restored.levels.size()==id,"resume committed color during paint")
			check(restored.pending_reveal==id,"resume pending paint")
			check(restored.coins==id*50,"resume coins")
	check(model.total_stars()==4500,"4500 stars")
	check(model.data.world_restored and model.data.perfect_world,"both endgame states")
	check(model.data.unlocked_areas==100,"100 areas unlocked")
	check(model.data.best_streak==1500,"first-try streak")
	var coins: int = int(model.data.coins)
	model.attempt(1)
	model.match_result(1,model.level(1).target)
	check(model.data.coins==coins,"replay cannot farm")
	model.attempt(1)
	model.match_result(1,[1000,0,0])
	check(model.record(1).score==100,"best score cannot regress")
	check(model.buy(0),"purchase succeeds")
	check(model.data.coins==coins-30 and model.data.helpers[0]==1,"purchase is one transaction")
	var loaded: Dictionary = model.save.load_game()
	check(loaded.helpers[0]==1 and loaded.coins==coins-30,"purchase resume")
	check(model.use_helper(0,1),"helper consumption")
	check(model.data.helpers[0]==0,"inventory consumed")
	check(not model.use_helper(0,1),"no negative inventory")
	# Truncate primary, recover previous valid save.
	var broken: FileAccess = FileAccess.open(model.save.base+".json",FileAccess.WRITE)
	broken.store_string("{truncated")
	broken.close()
	var recovered: Dictionary = model.save.load_game()
	check(SaveSystem.valid(recovered),"backup recovery")
	check(recovered.levels.size()==1500,"backup retains completed world")
	# Recovery from an interrupted atomic replacement.
	check(model.save.write(model.data),"write before interrupted replace")
	DirAccess.copy_absolute(model.save.base+".json",model.save.base+".tmp")
	DirAccess.remove_absolute(model.save.base+".json")
	check(model.save.load_game().levels.size()==1500,"temporary file recovery")
	var fresh: Dictionary = model.save.reset({"language":"ar"})
	check(fresh.levels.is_empty() and fresh.coins==0,"reset clears progress")
	check(fresh.settings.language=="ar","reset preserves preferences")
	var f: FileAccess = FileAccess.open(model.save.base+".json",FileAccess.WRITE)
	f.store_string("bad");f.close()
	check(model.save.load_game().levels.is_empty(),"backup cannot resurrect reset progress")
	for suffix in [".json",".previous",".tmp"]: DirAccess.remove_absolute(model.save.base+suffix)
	print(JSON.stringify({"suite":"production_gdscript","assertions":assertions,"failures":failures,"passed":failures.is_empty()}))
	quit(0 if failures.is_empty() else 1)
