shader_type canvas_item;
uniform sampler2D SCREEN_TEXTURE:hint_screen_texture,filter_linear_mipmap;
uniform sampler2D offset_image;
uniform float strength = 1.0;
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
	vec4 output = texture(SCREEN_TEXTURE, SCREEN_UV);
	vec4 texColor = vec4(0);
	vec2 samplePosition =  FRAGCOORD.xy / (1.0 / SCREEN_PIXEL_SIZE).xy;
	texColor = vec4(convolution(SCREEN_TEXTURE, UV, SCREEN_PIXEL_SIZE),1.);

	float shiftt = strength * texture(offset_image, samplePosition).r / 100.0;
	texColor.r = texture(SCREEN_TEXTURE, vec2(samplePosition.x + shiftt, samplePosition.y)).r;
	texColor.g = texture(SCREEN_TEXTURE, samplePosition).g;
	texColor.b = texture(SCREEN_TEXTURE, vec2(samplePosition.x - shiftt, samplePosition.y)).b;
	COLOR = texColor;

}