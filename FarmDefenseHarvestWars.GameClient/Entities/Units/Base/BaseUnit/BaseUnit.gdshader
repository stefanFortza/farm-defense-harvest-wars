shader_type canvas_item;
render_mode unshaded; // <--- Acesta este secretul. Ignoră complet CanvasModulate și luminile 2D.

// --- Setări Luciu (Inner Rim Light) ---
uniform vec4 gloss_color : source_color = vec4(1.0, 1.0, 1.0, 0.4); 
uniform float gloss_thickness : hint_range(0.0, 5.0) = 1.0;

// --- Setări Damage Flash ---
uniform vec4 flash_color : source_color = vec4(1.0, 1.0, 1.0, 1.0); // Alb pur
uniform float flash_modifier : hint_range(0.0, 1.0) = 0.0; // Controlat din scriptul C#

void fragment() {
    // 1. Citim pixelul original
    vec4 base = texture(TEXTURE, UV);
    
    // 2. Calculăm conturul interior (Luciul)
    vec2 size = TEXTURE_PIXEL_SIZE * gloss_thickness;
    float neighbors_alpha = texture(TEXTURE, UV + vec2(0.0, -size.y)).a *
                            texture(TEXTURE, UV + vec2(0.0, size.y)).a *
                            texture(TEXTURE, UV + vec2(-size.x, 0.0)).a *
                            texture(TEXTURE, UV + vec2(size.x, 0.0)).a;
                            
    float is_edge = 1.0 - step(0.1, neighbors_alpha); 
    
    // Adăugăm luciul
    vec3 color_with_gloss = mix(base.rgb, gloss_color.rgb, is_edge * gloss_color.a * base.a);
    
    // 3. Aplicăm Damage Flash-ul peste tot (dacă unitatea este lovită)
    vec3 final_rgb = mix(color_with_gloss, flash_color.rgb, flash_modifier);
    
    // 4. Randăm rezultatul final
    COLOR = vec4(final_rgb, base.a);
}