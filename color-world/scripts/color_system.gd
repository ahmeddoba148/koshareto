class_name ColorSystem
extends RefCounted

# Integer tenths: the sum is exactly 1000, including during every drag frame.
static func adjust(ratios: Array, channel: int, requested: int) -> Array:
	var value: int = clampi(requested, 0, 1000)
	var a: int = (channel + 1) % 3
	var b: int = (channel + 2) % 3
	var remaining: int = 1000 - value
	var previous: int = int(ratios[a]) + int(ratios[b])
	var out: Array = ratios.duplicate()
	out[channel] = value
	out[a] = roundi(float(remaining) * float(ratios[a]) / previous) if previous > 0 else remaining / 2
	out[b] = remaining - int(out[a])
	return out

static func color_of(ratios: Array, level: Dictionary) -> Color:
	# A shared exposure and white admixture produce bright/dark/muted/pastel
	# families while retaining a continuous, invertible mapping of the simplex.
	var peak: float = maxf(float(ratios[0]), maxf(float(ratios[1]), float(ratios[2])))
	var brightness: float = float(level.brightness)
	var saturation: float = float(level.saturation)
	var white: float = brightness * (1.0 - saturation)
	return Color(white + brightness * saturation * ratios[0] / peak,
		white + brightness * saturation * ratios[1] / peak,
		white + brightness * saturation * ratios[2] / peak, 1.0)

static func oklab(c: Color) -> Vector3:
	var v: Color = c.srgb_to_linear()
	var l: float = pow(0.4122214708*v.r + 0.5363325363*v.g + 0.0514459929*v.b, 1.0/3.0)
	var m: float = pow(0.2119034982*v.r + 0.6806995451*v.g + 0.1073969566*v.b, 1.0/3.0)
	var s: float = pow(0.0883024619*v.r + 0.2817188376*v.g + 0.6299787005*v.b, 1.0/3.0)
	return Vector3(0.2104542553*l + 0.793617785*m - 0.0040720468*s,
		1.9779984951*l - 2.428592205*m + 0.4505937099*s,
		0.0259040371*l + 0.7827717662*m - 0.808675766*s)

static func score(ratios: Array, level: Dictionary) -> float:
	var distance: float = oklab(color_of(ratios, level)).distance_to(oklab(color_of(level.target, level)))
	# dE OK=0.01 -> 96%, .025 -> 90%, .05 -> 80%; player calibration pending.
	return snappedf(clampf(100.0 - distance * 400.0, 0.0, 100.0), 0.1)

static func stars(score_value: float) -> int:
	if score_value >= 97.0: return 3
	if score_value >= 90.0: return 2
	if score_value >= 80.0: return 1
	return 0

static func exact(ratios: Array, target: Array) -> bool:
	return int(ratios[0]) == int(target[0]) and int(ratios[1]) == int(target[1]) and int(ratios[2]) == int(target[2])

static func reward(score_value: float, assisted: bool) -> int:
	if score_value >= 99.5 and not assisted: return 50
	return [0, 10, 20, 35][stars(score_value)]
