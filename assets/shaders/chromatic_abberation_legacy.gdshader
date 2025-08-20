shader_type canvas_item;
uniform sampler2D SCREEN_TEXTURE:hint_screen_texture,filter_linear_mipmap;
uniform sampler2D offset_image;
uniform float strength = 1.0;
uniform float shake_amount_x  : hint_range(1, 500) = 250.0;
uniform float shake_amount_y  : hint_range(1, 500) = 40.0;
uniform float white_hlines : hint_range(0, 250) = 50;
uniform float white_vlines : hint_range(0,80) = 80;
vec3 convolution(sampler2D tex, vec2 uv, vec2 pixel_size) {
	vec3 conv = vec3(0.0);
	// Gaussian blur kernel
	float gauss[9] = {
		-1., -1., -1., -1., 9., -1., -1., 1., 1.
	};
	for (int row = 0; row < 3; row++) {
		for (int col = 0; col < 3; col++) {
			conv += texture(tex, uv + vec2(float(col - 2), float(row - 2)) * pixel_size).rgb * gauss[row * 3 + col]/5.;
		}
	}
	return conv;
}
float rand(vec2 co){
	return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}
void fragment() {
	vec4 output = texture(SCREEN_TEXTURE, SCREEN_UV);

	float shift = strength * texture(offset_image, SCREEN_UV).r / 100.0;
	output.r = texture(SCREEN_TEXTURE, vec2(SCREEN_UV.x + shift, SCREEN_UV.y)).r;
	output.g = texture(SCREEN_TEXTURE, SCREEN_UV).g;
	output.b = texture(SCREEN_TEXTURE, vec2(SCREEN_UV.x - shift, SCREEN_UV.y)).b;
		//COLOR.a=ceil(length(pixel.xyz)/length(vec3(1.))-0.1);
	vec4 texColor = vec4(0);
	// get position to sample
	vec2 samplePosition =  FRAGCOORD.xy / (1.0 / SCREEN_PIXEL_SIZE).xy;

	float whiteNoise = 9999.0;

	// Jitter each line left and right
	samplePosition.x = samplePosition.x+(rand(vec2(TIME,UV.y))-0.5)/shake_amount_x;
	// Jitter the whole picture up and down
	samplePosition.y = samplePosition.y+(rand(vec2(TIME))-0.5)/shake_amount_y;
	// Slightly add color noise to each line
	texColor = texColor + (vec4(-0.5)+vec4(rand(vec2(UV.y,TIME)),rand(vec2(UV.y,TIME+1.0)),rand(vec2(UV.y,TIME+2.0)),0))*0.1;

	// Either sample the texture, or just make the pixel white (to get the staticy-bit at the bottom)
	//whiteNoise = rand(vec2(floor(samplePosition.y*white_vlines),floor(samplePosition.x*white_hlines))+vec2(TIME,0));
	/*if (whiteNoise > 11.5-30.0*samplePosition.y || whiteNoise < 1.5-5.0*samplePosition.y) {
		// Sample the texture.
		//samplePosition.y = 1.0-samplePosition.y; //Fix for upside-down texture*/
	texColor = vec4(convolution(SCREEN_TEXTURE, SCREEN_UV, SCREEN_PIXEL_SIZE),1.);

	float shiftt = strength * texture(offset_image, samplePosition).r / 100.0;
	texColor.r = texture(SCREEN_TEXTURE, vec2(samplePosition.x + shiftt, samplePosition.y)).r;
	texColor.g = texture(SCREEN_TEXTURE, samplePosition).g;
	texColor.b = texture(SCREEN_TEXTURE, vec2(samplePosition.x - shiftt, samplePosition.y)).b;
	COLOR = texColor;
	//COLOR = output;
}