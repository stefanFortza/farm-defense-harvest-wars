shader_type canvas_item;

uniform vec4 shine_color : source_color = vec4(1.0, 1.0, 1.0, 0.25);
uniform float shine_speed : hint_range(0.0, 10.0, 0.1) = 1.0;
uniform float shine_size : hint_range(0.0, 1.0, 0.01) = 0.01;
uniform float shine_angle : hint_range(0.0, 89.0, 0.1) = 45.0;
uniform float shine_time_offset : hint_range(0.0, 10.0, 0.1) = 0.0;

uniform vec4 pulse_color : source_color = vec4(1.0, 1.0, 1.0, 0.0);
uniform float pulse_speed : hint_range(0.0, 10.0, 0.1) = 2.0;
uniform float pulse_intensity : hint_range(0.0, 1.0, 0.01) = 0.05;
uniform float hover_intensity : hint_range(0.0, 1.0, 0.01) = 0.0;

void fragment() {
	vec4 tex_color = texture(TEXTURE, UV);
	
	// Shine effect
	float angle_rad = shine_angle * PI / 180.0;
	float shine_val = (UV.x * cos(angle_rad) + UV.y * sin(angle_rad));
	float time_val = fract((TIME + shine_time_offset) * shine_speed * 0.1);
	float shine = step(time_val - shine_size, shine_val) - step(time_val, shine_val);
	
	// Pulse effect
	float pulse = (sin(TIME * pulse_speed) + 1.0) * 0.5 * (pulse_intensity + hover_intensity * 0.2);
	
	vec4 final_color = tex_color;
	
	// Apply shine
	final_color.rgb = mix(final_color.rgb, shine_color.rgb, shine * shine_color.a);
	
	// Apply pulse
	final_color.rgb += pulse_color.rgb * pulse * tex_color.a;
	
	// Apply hover brightness
	final_color.rgb += hover_intensity * 0.1 * tex_color.a;
	
	COLOR = final_color;
}
