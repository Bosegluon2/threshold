shader_type spatial;

uniform vec4 line_color : source_color = vec4(1.0, 0.8, 0.2, 1.0);
uniform float line_width : hint_range(0.01, 0.2) = 0.08;
uniform float line_spacing : hint_range(0.05, 1.0) = 0.25;
uniform float speed : hint_range(0.0, 2.0) = 0.5;
uniform float alpha : hint_range(0.0, 1.0) = 0.7;

void fragment() {
    // 获取屏幕UV并换算到 -1 ~ 1 区间
    vec2 uv = (SCREEN_UV - 0.5) * 2.0;

    // 宽高比修正，保持线条角度不受拉伸
    uv.x *= VIEWPORT_SIZE.x/VIEWPORT_SIZE.y;

    // 斜线方向（45°）
    float angle = 0.785398;
    vec2 dir = vec2(cos(angle), sin(angle));

    // 投影到斜线方向
    float proj = dot(uv, dir);

    // 时间偏移
    float t = TIME * speed;

    // 多条线：proj / spacing 周期性取余
    float line_pos = fract((proj - t) / line_spacing);

    // 硬边界线条
    float mask = step(line_pos, line_width / line_spacing);
    ALBEDO = line_color.rgb;
    ALPHA = mask * line_color.a * alpha;
}

render_mode blend_mix, cull_disabled, unshaded;
