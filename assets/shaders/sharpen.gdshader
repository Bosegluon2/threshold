shader_type canvas_item;
render_mode unshaded;
uniform float size=0.5;
uniform sampler2D SCREEN_TEXTURE: hint_screen_texture, repeat_disable, filter_nearest;

vec3 convolution(sampler2D tex, vec2 uv, vec2 pixel_size) {
	vec3 conv = vec3(0.0);
	// Gaussian blur kernel
	float gauss[9] = {
		0., -1., 0., -1., 5., -1., 0., -1., 0.
	};
	for (int row = 0; row < 3; row++) {
		for (int col = 0; col < 3; col++) {
			conv += texture(tex, uv + vec2(float(col - 2), float(row - 2)) * pixel_size).rgb * gauss[row * 3 + col];
		}
	}
	return conv;
}

void fragment() {
	vec3 pixels[9];  // Sobel kernel
	// [0, 1, 2]
	// [3, 4, 5]
	// [6, 7, 8]
	/*for (int row = 0; row < 3; row++) {
		for (int col = 0; col < 3; col++) {
			vec2 uv = SCREEN_UV + vec2(float(col - 1), float(row - 1)) * SCREEN_PIXEL_SIZE;
			pixels[row * 3 + col] = convolution(SCREEN_TEXTURE, uv, SCREEN_PIXEL_SIZE);
		}
	}*/

	// Sobel operator

	COLOR = vec4(convolution(SCREEN_TEXTURE, SCREEN_UV, SCREEN_PIXEL_SIZE), 1.0)*size +texture(SCREEN_TEXTURE,SCREEN_UV)*(1.0-size);

}