shader_type canvas_item;

// Dimensiunea dalei tale (ex: 64.0, 80.0)
uniform float tile_size = 64.0;

// Culoarea de bază a umbrei (Negru este ideal pentru a păstra contrastul)
uniform vec4 base_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);

// Limitele de transparență. Valorile astea fac diferența între "ieftin" și "premium"
uniform float min_alpha = 0.02; // 2% opacitate (foarte subtil)
uniform float max_alpha = 0.08; // 8% opacitate (puțin mai închis)

varying vec2 local_pos;

void vertex() {
    // Ancorăm coordonatele la mapă, imune la zoom-ul camerei
    local_pos = VERTEX;
}

// Funcție standard și ultra-rapidă de generare a numerelor pseudo-aleatoare în GLSL
float random(vec2 uv) {
    return fract(sin(dot(uv.xy, vec2(12.9898, 78.233))) * 43758.5453123);
}

void fragment() {
    // Calculăm indexul dalei (rândul și coloana curente)
    int row = int(local_pos.y / tile_size);
    int col = int(local_pos.x / tile_size);
    
    // Logica de tablă de șah: dacă suma e pară, colorăm dala
    bool is_tinted_tile = (row + col) % 2 == 0;
    
    if (is_tinted_tile) {
        // Generăm o valoare aleatoare unică pentru ACCEASTĂ dală
        // Convertim col și row la float pentru a crea un "seed" stabil
        float rand_val = random(vec2(float(col), float(row)));
        
        // Mapăm valoarea aleatoare (care e între 0 și 1) între min_alpha și max_alpha
        float final_alpha = mix(min_alpha, max_alpha, rand_val);
        
        // Aplicăm culoarea cu opacitatea randomizată
        COLOR = vec4(base_color.rgb, final_alpha);
    } else {
        // Dalele impare rămân complet transparente pentru a lăsa grafica originală să respire
        COLOR = vec4(0.0, 0.0, 0.0, 0.0);
    }
}