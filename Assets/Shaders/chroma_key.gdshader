shader_type canvas_item;

uniform vec4 chroma_key_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float precision : hint_range(0.0, 1.0) = 0.1;

void fragment() {
	vec4 rgba = texture(TEXTURE, UV);
	if (distance(rgba.rgb, chroma_key_color.rgb) < precision) {
		rgba.a = 0.0;
	}
	COLOR = rgba;
}
