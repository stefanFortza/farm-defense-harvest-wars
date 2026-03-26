shader_type canvas_item;

// --- PARAMETRI DE LUMINĂ ---
// O culoare caldă (portocaliu/chihlimbar) care simulează o lumânare sau o lampă
uniform vec4 warm_light_color : source_color = vec4(1.0, 0.8, 0.5, 1.0);
uniform float light_intensity : hint_range(0.0, 2.0) = 0.7;
uniform float light_radius : hint_range(0.1, 1.5) = 1.0;

// --- PARAMETRI DE VIGNETĂ (UMBRE PE MARGINI) ---
// O culoare foarte închisă (maro profund sau negru) pentru marginile mesei
uniform vec4 vignette_color : source_color = vec4(0.05, 0.02, 0.0, 1.0);
uniform float vignette_radius : hint_range(0.0, 2.0) = 0.9;
uniform float vignette_intensity : hint_range(0.0, 1.0) = 0.85;

// --- PARAMETRI DE TEXTURĂ ---
uniform float contrast : hint_range(0.5, 2.0) = 1.2; 

void fragment() {
    // 1. Preluăm pixelul original din textura de lemn pe care ai pus-o pe UI
    vec4 wood_color = texture(TEXTURE, UV);
    
    // 2. Calculăm distanța de la centrul ecranului (UV 0.5, 0.5)
    // Coordonatele UV sunt normalizate [0, 1].
    vec2 center_uv = UV - vec2(0.5);
    
    // Corectăm aspect ratio-ul (opțional, dar util dacă ecranul e 16:9, ca lumina să fie ovală/adaptată)
    // center_uv.x *= 16.0 / 9.0; // Decomentează dacă vrei ca lumina să fie perfect rotundă pe un ecran wide
    
    float dist = length(center_uv);
    
    // 3. Aplicăm Lumina Centrală (Glow)
    // smoothstep creează o tranziție fină de la intensitate maximă în centru la zero spre margini
    float light_factor = 1.0 - smoothstep(0.0, light_radius, dist);
    vec3 final_color = wood_color.rgb + (warm_light_color.rgb * light_factor * light_intensity);
    
    // 4. Aplicăm Vigneta (Umbrele de pe margini)
    float vignette_factor = smoothstep(vignette_radius * 0.3, vignette_radius, dist);
    final_color = mix(final_color, vignette_color.rgb, vignette_factor * vignette_intensity);
    
    // 5. Ajustăm contrastul pentru a face fibra lemnului să iasă în evidență
    final_color = mix(vec3(0.5), final_color, contrast);
    
    // Returnăm culoarea finală păstrând canalul Alpha original
    COLOR = vec4(final_color, wood_color.a);
}