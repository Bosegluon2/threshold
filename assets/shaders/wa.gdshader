shader_type spatial;

render_mode unshaded, blend_add;

uniform vec3 uv1_scale = vec3(1.0, 1.0, 1.0);
uniform vec3 uv1_offset = vec3(0.0, 0.0, 0.0);
uniform float variation = 0.0;
varying float elapsed_time;
void vertex() {
	elapsed_time = TIME;
	UV = UV*uv1_scale.xy+uv1_offset.xy;
}
float rand(vec2 x) {
    return fract(cos(mod(dot(x, vec2(13.9898, 8.141)), 3.14)) * 43758.5453);
}
vec2 rand2(vec2 x) {
    return fract(cos(mod(vec2(dot(x, vec2(13.9898, 8.141)),
						      dot(x, vec2(3.4562, 17.398))), vec2(3.14))) * 43758.5453);
}
vec3 rand3(vec2 x) {
    return fract(cos(mod(vec3(dot(x, vec2(13.9898, 8.141)),
							  dot(x, vec2(3.4562, 17.398)),
                              dot(x, vec2(13.254, 5.867))), vec3(3.14))) * 43758.5453);
}
float param_rnd(float minimum, float maximum, float seed) {
	return minimum+(maximum-minimum)*rand(vec2(seed));
}
vec3 rgb2hsv(vec3 c) {
	vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	vec4 p = c.g < c.b ? vec4(c.bg, K.wz) : vec4(c.gb, K.xy);
	vec4 q = c.r < p.x ? vec4(p.xyw, c.r) : vec4(c.r, p.yzx);
	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}
vec3 hsv2rgb(vec3 c) {
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
	return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}
uniform sampler2D texture_1;
const float texture_1_size = 128.0;
vec2 transform2_clamp(vec2 uv) {
	return clamp(uv, vec2(0.0), vec2(1.0));
}
vec2 transform2(vec2 uv, vec2 translate, float rotate, vec2 scale) {
 	vec2 rv;
	uv -= translate;
	uv -= vec2(0.5);
	rv.x = cos(rotate)*uv.x + sin(rotate)*uv.y;
	rv.y = -sin(rotate)*uv.x + cos(rotate)*uv.y;
	rv /= scale;
	rv += vec2(0.5);
	return rv;	
}
uniform sampler2D texture_2;
const float texture_2_size = 256.0;
float pingpong(float a, float b)
{
  return (b != 0.0) ? abs(fract((a - b) / (b * 2.0)) * b * 2.0 - b) : 0.0;
}
vec3 blend_normal(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	return opacity*c1 + (1.0-opacity)*c2;
}
vec3 blend_dissolve(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	if (rand(uv) < opacity) {
		return c1;
	} else {
		return c2;
	}
}
vec3 blend_multiply(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	return opacity*c1*c2 + (1.0-opacity)*c2;
}
vec3 blend_screen(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	return opacity*(1.0-(1.0-c1)*(1.0-c2)) + (1.0-opacity)*c2;
}
float blend_overlay_f(float c1, float c2) {
	return (c1 < 0.5) ? (2.0*c1*c2) : (1.0-2.0*(1.0-c1)*(1.0-c2));
}
vec3 blend_overlay(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	return opacity*vec3(blend_overlay_f(c1.x, c2.x), blend_overlay_f(c1.y, c2.y), blend_overlay_f(c1.z, c2.z)) + (1.0-opacity)*c2;
}
vec3 blend_hard_light(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	return opacity*0.5*(c1*c2+blend_overlay(uv, c1, c2, 1.0)) + (1.0-opacity)*c2;
}
float blend_soft_light_f(float c1, float c2) {
	return (c2 < 0.5) ? (2.0*c1*c2+c1*c1*(1.0-2.0*c2)) : 2.0*c1*(1.0-c2)+sqrt(c1)*(2.0*c2-1.0);
}
vec3 blend_soft_light(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	return opacity*vec3(blend_soft_light_f(c1.x, c2.x), blend_soft_light_f(c1.y, c2.y), blend_soft_light_f(c1.z, c2.z)) + (1.0-opacity)*c2;
}
float blend_burn_f(float c1, float c2) {
	return (c1==0.0)?c1:max((1.0-((1.0-c2)/c1)),0.0);
}
vec3 blend_burn(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	return opacity*vec3(blend_burn_f(c1.x, c2.x), blend_burn_f(c1.y, c2.y), blend_burn_f(c1.z, c2.z)) + (1.0-opacity)*c2;
}
float blend_dodge_f(float c1, float c2) {
	return (c1==1.0)?c1:min(c2/(1.0-c1),1.0);
}
vec3 blend_dodge(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	return opacity*vec3(blend_dodge_f(c1.x, c2.x), blend_dodge_f(c1.y, c2.y), blend_dodge_f(c1.z, c2.z)) + (1.0-opacity)*c2;
}
vec3 blend_lighten(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	return opacity*max(c1, c2) + (1.0-opacity)*c2;
}
vec3 blend_darken(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	return opacity*min(c1, c2) + (1.0-opacity)*c2;
}
vec3 blend_difference(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	return opacity*clamp(c2-c1, vec3(0.0), vec3(1.0)) + (1.0-opacity)*c2;
}
vec3 blend_additive(vec2 uv, vec3 c1, vec3 c2, float oppacity) {
	return c2 + c1 * oppacity;
}
vec3 blend_addsub(vec2 uv, vec3 c1, vec3 c2, float oppacity) {
	return c2 + (c1 - .5) * 2.0 * oppacity;
}
float blend_linear_light_f(float c1, float c2) {
	return (c1 + 2.0 * c2) - 1.0;
}
vec3 blend_linear_light(vec2 uv, vec3 c1, vec3 c2, float opacity) {
return opacity*vec3(blend_linear_light_f(c1.x, c2.x), blend_linear_light_f(c1.y, c2.y), blend_linear_light_f(c1.z, c2.z)) + (1.0-opacity)*c2;
}
float blend_vivid_light_f(float c1, float c2) {
	return (c1 < 0.5) ? 1.0 - (1.0 - c2) / (2.0 * c1) : c2 / (2.0 * (1.0 - c1));
}
vec3 blend_vivid_light(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	return opacity*vec3(blend_vivid_light_f(c1.x, c2.x), blend_vivid_light_f(c1.y, c2.y), blend_vivid_light_f(c1.z, c2.z)) + (1.0-opacity)*c2;
}
float blend_pin_light_f( float c1, float c2) {
	return (2.0 * c1 - 1.0 > c2) ? 2.0 * c1 - 1.0 : ((c1 < 0.5 * c2) ? 2.0 * c1 : c2);
}
vec3 blend_pin_light(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	return opacity*vec3(blend_pin_light_f(c1.x, c2.x), blend_pin_light_f(c1.y, c2.y), blend_pin_light_f(c1.z, c2.z)) + (1.0-opacity)*c2;
}
float blend_hard_mix_f(float c1, float c2) {
	return floor(c1 + c2);
}
vec3 blend_hard_mix(vec2 uv, vec3 c1, vec3 c2, float opacity) {
		return opacity*vec3(blend_hard_mix_f(c1.x, c2.x), blend_hard_mix_f(c1.y, c2.y), blend_hard_mix_f(c1.z, c2.z)) + (1.0-opacity)*c2;
}
float blend_exclusion_f(float c1, float c2) {
	return c1 + c2 - 2.0 * c1 * c2;
}
vec3 blend_exclusion(vec2 uv, vec3 c1, vec3 c2, float opacity) {
	return opacity*vec3(blend_exclusion_f(c1.x, c2.x), blend_exclusion_f(c1.y, c2.y), blend_exclusion_f(c1.z, c2.z)) + (1.0-opacity)*c2;
}
uniform sampler2D texture_3;
const float texture_3_size = 1024.0;
const float p_o892204_fresnel_amount = 1.000000000;
vec3 o892204_input_offset(vec2 uv, float _seed_variation_) {
return vec3(0.0);
}
vec3 o892204_input_scale(vec2 uv, float _seed_variation_) {
return vec3(1.0);
}
const float p_o892245_amount1 = 1.000000000;
const float p_o892243_amount1 = 1.000000000;
const float p_o892232_amount1 = 0.250000000;
const float p_o892227_default_in1 = 0.000000000;
const float p_o892227_default_in2 = 0.000000000;
const float p_o892212_default_in1 = 0.000000000;
const float p_o892212_default_in2 = 50.000000000;
const float p_o892223_default_in1 = 0.000000000;
const float p_o892223_default_in2 = 0.000000000;
const float p_o892213_amount = 0.255000000;
const float p_o892213_eps = 0.100000000;
const float p_o892218_amount = 0.190000000;
const float p_o892218_eps = 0.100000000;
const float p_o892222_translate_x = 0.000000000;
const float p_o892222_rotate = 90.000000000;
const float p_o892222_scale_x = 1.000000000;
const float p_o892222_scale_y = -1.000000000;
float o892218_input_d(vec2 uv, float _seed_variation_) {
vec4 o892224_0 = textureLod(texture_1, fract(transform2((uv), vec2(p_o892222_translate_x*(2.0*1.0-1.0), (elapsed_time*0.2)*(2.0*1.0-1.0)), p_o892222_rotate*0.01745329251*(2.0*1.0-1.0), vec2(p_o892222_scale_x*(2.0*1.0-1.0), p_o892222_scale_y*(2.0*1.0-1.0)))), 0.0);
vec4 o892222_0_1_rgba = o892224_0;
return (dot((o892222_0_1_rgba).rgb, vec3(1.0))/3.0);
}
vec2 o892218_slope(vec2 uv, float epsilon, float _seed_variation_) {
	return vec2(o892218_input_d(fract(uv+vec2(epsilon, 0.0)), _seed_variation_)-o892218_input_d(fract(uv-vec2(epsilon, 0.0)), _seed_variation_), o892218_input_d(fract(uv+vec2(0.0, epsilon)), _seed_variation_)-o892218_input_d(fract(uv-vec2(0.0, epsilon)), _seed_variation_));
}float o892213_input_d(vec2 uv, float _seed_variation_) {
vec2 o892218_0_slope = o892218_slope((uv), p_o892218_eps, _seed_variation_);
vec2 o892218_0_warp = o892218_0_slope;vec4 o892234_0 = textureLod(texture_2, (uv)+p_o892218_amount*o892218_0_warp, 0.0);
vec4 o892218_0_1_rgba = o892234_0;
return (dot((o892218_0_1_rgba).rgb, vec3(1.0))/3.0);
}
vec2 o892213_slope(vec2 uv, float epsilon, float _seed_variation_) {
	return vec2(o892213_input_d(fract(uv+vec2(epsilon, 0.0)), _seed_variation_)-o892213_input_d(fract(uv-vec2(epsilon, 0.0)), _seed_variation_), o892213_input_d(fract(uv+vec2(0.0, epsilon)), _seed_variation_)-o892213_input_d(fract(uv-vec2(0.0, epsilon)), _seed_variation_));
}const float p_o892210_translate_x = 0.000000000;
const float p_o892210_rotate = 0.000000000;
const float p_o892210_scale_x = 1.000000000;
const float p_o892210_scale_y = -1.000000000;
const float p_o892226_default_in1 = 0.000000000;
const float p_o892226_default_in2 = 5.000000000;
const float p_o892233_translate_x = 0.016250000;
const float p_o892233_translate_y = -0.002200000;
const float p_o892228_count = 5.000000000;
const float p_o892228_width = 10.000000000;
vec4 o892228_input_in(vec2 uv, float _seed_variation_) {
vec2 o892213_0_slope = o892213_slope((uv), p_o892213_eps, _seed_variation_);
vec2 o892213_0_warp = o892213_0_slope;vec4 o892210_0_1_rgba = vec4((fract(transform2(((uv)+p_o892213_amount*o892213_0_warp), vec2(p_o892210_translate_x*(2.0*1.0-1.0), (-elapsed_time*0.5)*(2.0*1.0-1.0)), p_o892210_rotate*0.01745329251*(2.0*1.0-1.0), vec2(p_o892210_scale_x*(2.0*1.0-1.0), p_o892210_scale_y*(2.0*1.0-1.0))))), 0.0, 1.0);
vec4 o892213_0_1_rgba = o892210_0_1_rgba;
float o892211_1_1_f = o892213_0_1_rgba.g;
float o892223_0_clamp_false = fract(o892211_1_1_f);
float o892223_0_clamp_true = clamp(o892223_0_clamp_false, 0.0, 1.0);
float o892223_0_1_f = o892223_0_clamp_false;
float o892212_0_clamp_false = pow(o892223_0_1_f,p_o892212_default_in2);
float o892212_0_clamp_true = clamp(o892212_0_clamp_false, 0.0, 1.0);
float o892212_0_2_f = o892212_0_clamp_false;
float o892226_0_clamp_false = pow(o892223_0_1_f,p_o892226_default_in2);
float o892226_0_clamp_true = clamp(o892226_0_clamp_false, 0.0, 1.0);
float o892226_0_2_f = o892226_0_clamp_false;
float o892227_0_clamp_false = o892212_0_2_f+o892226_0_2_f;
float o892227_0_clamp_true = clamp(o892227_0_clamp_false, 0.0, 1.0);
float o892227_0_1_f = o892227_0_clamp_false;
return vec4(vec3(o892227_0_1_f), 1.0);
}
vec4 supersample_o892228(vec2 uv, float size, int count, float width, float _seed_variation_) {
	vec4 rv = vec4(0.0);
	vec2 step_size = vec2(width)/size/float(count);
	uv -= vec2(0.5)/size;
	for (int x = 0; x < count; ++x) {
		for (int y = 0; y < count; ++y) {
			rv += o892228_input_in(uv+(vec2(float(x), float(y))+vec2(0.5))*step_size, _seed_variation_);
		}
	}
	return rv/float(count*count);
}const float p_o892244_value = 0.013700000;
const float p_o892244_width = 0.022400000;
const vec4 p_o892246_color = vec4(1.000000000, 0.237948000, 0.743390977, 1.000000000);
void fragment() {
	float _seed_variation_ = variation;
	vec2 uv = fract(UV);
vec2 o892213_0_slope = o892213_slope((uv), p_o892213_eps, _seed_variation_);
vec2 o892213_0_warp = o892213_0_slope;vec4 o892210_0_1_rgba = vec4((fract(transform2(((uv)+p_o892213_amount*o892213_0_warp), vec2(p_o892210_translate_x*(2.0*1.0-1.0), (-elapsed_time*0.5)*(2.0*1.0-1.0)), p_o892210_rotate*0.01745329251*(2.0*1.0-1.0), vec2(p_o892210_scale_x*(2.0*1.0-1.0), p_o892210_scale_y*(2.0*1.0-1.0))))), 0.0, 1.0);
vec4 o892213_0_1_rgba = o892210_0_1_rgba;
float o892211_1_1_f = o892213_0_1_rgba.g;
float o892223_0_clamp_false = fract(o892211_1_1_f);
float o892223_0_clamp_true = clamp(o892223_0_clamp_false, 0.0, 1.0);
float o892223_0_1_f = o892223_0_clamp_false;
float o892212_0_clamp_false = pow(o892223_0_1_f,p_o892212_default_in2);
float o892212_0_clamp_true = clamp(o892212_0_clamp_false, 0.0, 1.0);
float o892212_0_2_f = o892212_0_clamp_false;
float o892226_0_clamp_false = pow(o892223_0_1_f,p_o892226_default_in2);
float o892226_0_clamp_true = clamp(o892226_0_clamp_false, 0.0, 1.0);
float o892226_0_2_f = o892226_0_clamp_false;
float o892227_0_clamp_false = o892212_0_2_f+o892226_0_2_f;
float o892227_0_clamp_true = clamp(o892227_0_clamp_false, 0.0, 1.0);
float o892227_0_1_f = o892227_0_clamp_false;
vec4 o892228_0_1_rgba = supersample_o892228(((uv)-vec2(p_o892233_translate_x, p_o892233_translate_y)), 256.000000000, int(p_o892228_count), p_o892228_width, _seed_variation_);
vec4 o892233_0_1_rgba = o892228_0_1_rgba;
vec4 o892232_0_b = vec4(vec3(o892227_0_1_f), 1.0);
vec4 o892232_0_l;
float o892232_0_a;

o892232_0_l = o892233_0_1_rgba;
o892232_0_a = p_o892232_amount1*1.0;
o892232_0_b = vec4(blend_lighten((uv), o892232_0_l.rgb, o892232_0_b.rgb, o892232_0_a*o892232_0_l.a), min(1.0, o892232_0_b.a+o892232_0_a*o892232_0_l.a));

vec4 o892232_0_2_rgba = o892232_0_b;
vec4 o892241_0 = textureLod(texture_3, (uv), 0.0);
vec3 o892244_0_false = clamp((o892241_0.rgb-vec3(p_o892244_value))/max(0.0001, p_o892244_width)+vec3(0.5), vec3(0.0), vec3(1.0));
vec3 o892244_0_true = vec3(1.0)-o892244_0_false;vec4 o892244_0_1_rgba = vec4(o892244_0_true, o892241_0.a);
vec4 o892243_0_b = o892232_0_2_rgba;
vec4 o892243_0_l;
float o892243_0_a;

o892243_0_l = o892232_0_2_rgba;
o892243_0_a = p_o892243_amount1*(dot((o892244_0_1_rgba).rgb, vec3(1.0))/3.0);
o892243_0_b = vec4(blend_additive((uv), o892243_0_l.rgb, o892243_0_b.rgb, o892243_0_a*o892243_0_l.a), min(1.0, o892243_0_b.a+o892243_0_a*o892243_0_l.a));

vec4 o892243_0_1_rgba = o892243_0_b;
vec4 o892246_0_1_rgba = p_o892246_color;
vec4 o892245_0_b = o892243_0_1_rgba;
vec4 o892245_0_l;
float o892245_0_a;

o892245_0_l = o892246_0_1_rgba;
o892245_0_a = p_o892245_amount1*1.0;
o892245_0_b = vec4(blend_multiply((uv), o892245_0_l.rgb, o892245_0_b.rgb, o892245_0_a*o892245_0_l.a), min(1.0, o892245_0_b.a+o892245_0_a*o892245_0_l.a));

vec4 o892245_0_2_rgba = o892245_0_b;

	vec4 color_tex = o892245_0_2_rgba;
	color_tex = mix(pow((color_tex + vec4(0.055)) * (1.0 / (1.0 + 0.055)),vec4(2.4)),color_tex * (1.0 / 12.92),lessThan(color_tex,vec4(0.04045)));
	ALBEDO = color_tex.rgb;
	ALPHA = color_tex.a;

}



