shader_type spatial;
render_mode diffuse_burley, specular_schlick_ggx;
uniform vec4 paint_color : source_color = vec4(0.45,0.7,0.8,1.0);
uniform float reveal : hint_range(0.0,1.0) = 0.0;
uniform float min_y = 0.0;
uniform float max_y = 6.0;
uniform float glow = 0.0;
varying float height;
void vertex() {
 height = VERTEX.y;
}
void fragment() {
 float normalized_height = (height-min_y)/max(max_y-min_y,0.01);
 float amount = 1.0-smoothstep(reveal-0.025,reveal+0.025,normalized_height);
 if(reveal <= 0.0) amount=0.0;
 if(reveal >= 1.0) amount=1.0;
 vec3 neutral=vec3(0.86,0.875,0.89)*mix(0.65,1.12,COLOR.r);
 vec3 painted=clamp(paint_color.rgb*mix(0.3,1.28,COLOR.r),vec3(0.02),vec3(1.0));
 ALBEDO=mix(neutral,painted,amount);
 ROUGHNESS=0.7;
 EMISSION=painted*glow*amount;
}
