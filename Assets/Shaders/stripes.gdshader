shader_type canvas_item;

uniform vec4 stripe_color : source_color = vec4(1.0, 1.0, 1.0, 0.1);
uniform float stripe_thickness : hint_range(0.1, 10.0) = 1.0;
uniform float stripe_spacing : hint_range(0.1, 10.0) = 1.0;

void fragment() {
    float pos = FRAGCOORD.y;
    float modulo = stripe_thickness + stripe_spacing;
    if (mod(pos, modulo) < stripe_thickness) {
        COLOR = stripe_color;
    } else {
        discard;
    }
}
