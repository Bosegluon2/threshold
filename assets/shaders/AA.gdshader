shader_type canvas_item;

// 公式类型 (1-8)
uniform int formula_type = 1;
// 颜色模式 (0-2)
uniform int color_mode = 0;
// 缩放参数
uniform float formula_zoom = 1.0;
uniform float target_scale = 1.0;
uniform float color_size = 1.0;
// 光照方向
uniform vec3 light_dir = vec3(-0.304367, -0.910224, -0.280959);
// 相机参数
uniform float cam_f = 0.005;
uniform float cam_dist = 0.0;
// 在 uniform 区域添加
uniform float camera_distance = 5.0;
uniform float perspective_strength = 0.5;
uniform float base_size = 0.01;        // 基础点大小
uniform float depth_falloff = 0.3;     // 深度衰减系数

// 公式参数
uniform float a = 1.0;
uniform float b = 1.0;
uniform float c = 1.0;
// 旋转角度
uniform float rotation_angle = 0.0;
// 点数量和分布范围
uniform int point_count = 2000;
uniform float point_range = 3.0;

// 随机数生成器
float rand(vec2 co) {
    return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453);
}

// 3D旋转矩阵 - X轴
mat4 rotateX(float angle) {
    return mat4(
        vec4(1.0, 0.0, 0.0, 0.0),
        vec4(0.0, cos(angle), -sin(angle), 0.0),
        vec4(0.0, sin(angle), cos(angle), 0.0),
        vec4(0.0, 0.0, 0.0, 1.0)
    );
}

// 3D旋转矩阵 - Y轴
mat4 rotateY(float angle) {
    return mat4(
        vec4(cos(angle), 0.0, sin(angle), 0.0),
        vec4(0.0, 1.0, 0.0, 0.0),
        vec4(-sin(angle), 0.0, cos(angle), 0.0),
        vec4(0.0, 0.0, 0.0, 1.0)
    );
}

// 3D旋转矩阵 - Z轴
mat4 rotateZ(float angle) {
    return mat4(
        vec4(cos(angle), sin(angle), 0.0, 0.0),
        vec4(-sin(angle), cos(angle), 0.0, 0.0),
       vec4(0.0, 0.0, 1.0, 0.0),
        vec4(0.0, 0.0, 0.0, 1.0)
    );
}

// 公式函数
float formula(float x, float y, float z) {
    if (formula_type == 1) {
        return (x*x)/a+(z*z)/c-(y*y)/b+1.0;
    }
    if (formula_type == 2) {
        return x*sin(y)+y*sin(z)+z*sin(x)-1.0;
    }
    if (formula_type == 3) {
        return (x*x)/a+(z*z)/c+(y*y)/b-point_range;
    }
    if (formula_type == 4) {
        return sin(2.0*x*x+2.0*y*y)-z;
    }
    if (formula_type == 5) {
        return sin(x*2.0)+sin(z*2.0)-y*2.0;
    }
    if (formula_type == 6) {
        return x*y*z;
    }
    if (formula_type == 7) {
        return sin(x)+sin(y)+sin(z);
    }
    if (formula_type == 8) {
        return sin(x)*sin(x)+sin(y)*sin(y)+sin(z)*sin(z);
    }
    return 0.0;
}

// 计算公式在x方向的导数
float dx(float x, float y, float z) {
    if (formula_type == 1) {
        return 2.0/a*x;
    }
    if (formula_type == 2) {
        return sin(y)+z*cos(x);
    }
    if (formula_type == 3) {
        return 2.0/a*x;
    }
    if (formula_type == 4) {
        return cos(2.0*x*x+2.0*y*y)*2.0*x*2.0;
    }
    if (formula_type == 5) {
        return cos(x*2.0)*2.0;
    }
    if (formula_type == 6) {
        return y*z;
    }
    if (formula_type == 7) {
        return cos(x);
    }
    if (formula_type == 8) {
        return 2.0*sin(x)*cos(x);
    }
    return 0.0;
}

// 计算公式在y方向的导数
float dy(float x, float y, float z) {
    if (formula_type == 1) {
        return -2.0/b*y;
    }
    if (formula_type == 2) {
        return sin(z)+x*cos(y);
    }
    if (formula_type == 3) {
        return 2.0/b*y;
    }
    if (formula_type == 4) {
        return cos(2.0*x*x+2.0*y*y)*2.0*y*2.0;
    }
    if (formula_type == 5) {
        return -2.0;
    }
    if (formula_type == 6) {
        return x*z;
    }
    if (formula_type == 7) {
        return cos(y);
    }
    if (formula_type == 8) {
        return 2.0*sin(y)*cos(y);
    }
    return 0.0;
}

// 计算公式在z方向的导数
float dz(float x, float y, float z) {
    if (formula_type == 1) {
        return 2.0/c*z;
    }
    if (formula_type == 2) {
        return sin(x)+y*cos(z);
    }
    if (formula_type == 3) {
        return 2.0/c*z;
    }
    if (formula_type == 4) {
        return -1.0;
    }
    if (formula_type == 5) {
        return cos(z*2.0)*2.0;
    }
    if (formula_type == 6) {
        return x*y;
    }
    if (formula_type == 7) {
        return cos(z);
    }
    if (formula_type == 8) {
        return 2.0*sin(z)*cos(z);
    }
    return 0.0;
}

void fragment() {
    vec2 screen_pixel = SCREEN_PIXEL_SIZE;
    vec2 texture_pixel = TEXTURE_PIXEL_SIZE;
    
    // 计算宽高比 (屏幕宽高比)
    float aspect_ratio = screen_pixel.y / screen_pixel.x;
    
    // 归一化坐标，中心为原点，并应用宽高比修正
    vec2 uv = (UV - 0.5) * 2.0 * target_scale;
    uv.x *= aspect_ratio;
    // 计算点的大小
    float point_size = 0.0;
    vec4 point_color = vec4(0.0);
    
    // 模拟点的分布 (在着色器中无法使用循环创建大量点，这里使用伪随机分布)
    for (int i = 0; i < point_count; i++) {
        // 使用i和一个随机种子生成点的位置
        float seed = float(i) * 123.456;
        vec3 point_pos = vec3(
            rand(vec2(seed, 0.0)) * 2.0 - 1.0,
            rand(vec2(seed, 44.0)) * 2.0 - 1.0,
            rand(vec2(seed, 444.0)) * 2.0 - 1.0
        ) * point_range;
        
        // 应用公式迭代 (简化版)
        vec3 p2 = point_pos * formula_zoom;
        float f = formula(p2.x, p2.y, p2.z);
        point_pos.x -= clamp(0.001 * 2.0 * f * dx(p2.x, p2.y, p2.z), -0.5, 0.5);
        point_pos.y -= clamp(0.001 * 2.0 * f * dy(p2.x, p2.y, p2.z), -0.5, 0.5);
        point_pos.z -= clamp(0.001 * 2.0 * f * dz(p2.x, p2.y, p2.z), -0.5, 0.5);
        
        // 应用旋转
        mat4 rotation = rotateY(rotation_angle);
        vec4 rotated_pos = rotation * vec4(point_pos, 1.0);
        
        // 计算屏幕位置
        vec2 screen_pos = vec2(rotated_pos.x, rotated_pos.y);
        
        // 计算点大小和透明度
        float depth_factor = 1.0 / (1.0 + abs(rotated_pos.z)); // 随深度增加而减小
		float size = max(0.01, base_size * depth_factor);      // base_size 是基础大小
        
        // 计算与当前像素的距离
        float dist = distance(uv, screen_pos);
        
        // 如果在点的范围内
        if (dist < size) {
            // 计算颜色
            vec3 color;
            if (color_mode == 0) {
                // 基于位置的颜色
                vec3 p_color = point_pos * color_size / point_range / 2.0 + vec3(0.5, 0.5, 0.5);
                color = p_color;
            } else if (color_mode == 1) {
                // 基于公式值的颜色
                float f_color = pow(formula(point_pos.x * formula_zoom, point_pos.y * formula_zoom, point_pos.z * formula_zoom), 2.0);
                f_color /= f_color + 1.0;
                f_color = -f_color + 1.0;
                color = vec3(f_color, f_color, f_color);
            } else if (color_mode == 2) {
                // 基于法线和光照的颜色
                vec3 normal = normalize(cross(
                    normalize(vec3(1.0, dx(rotated_pos.x, rotated_pos.y, rotated_pos.z), 0.0)),
                    normalize(vec3(0.0, dz(rotated_pos.x, rotated_pos.y, rotated_pos.z), 1.0))
                ));
                float lighting = dot(normal, light_dir);
                color = mix(vec3(0.0, 0.0, 0.01), vec3(0.99, 0.97, 0.91), lighting);
            }
            
			float depth = abs(rotated_pos.z);
			float alpha = clamp(1.0 - depth * depth_falloff, 0.0, 1.0); // depth_falloff 是衰减系数
            
            // 合并颜色 (使用加法混合，模拟点的叠加效果)
            point_color += vec4(color, alpha) * (1.0 - dist/size);
        }
    }
    
    // 输出最终颜色
    COLOR = point_color;
}