# Justfile pentru Farm Defense

# Variabile
dotnet_project := "FarmDefenseHarvestWars.Backend"
godot_path := "godot" # Sau calea completă către executabilul Godot 4.5
doc_dir := "docs/diagrams"

# Default recipe
default:
    @just --list

# --- BACKEND ---

# Pornește serverul ASP.NET Core
run-api:
    cd {{dotnet_project}} && dotnet run

# Aplică migrările la baza de date (dacă folosești EF Core)
db-update:
    cd {{dotnet_project}} && dotnet ef database update

# Formatează codul C# (Code Style)
lint:
    dotnet format

# --- GODOT ---

# Deschide editorul Godot
edit:
    {{godot_path}} -e --path FarmDefenseHarvestWars.GameClient

# Pornește un Client de test (Windowed)
play:
    {{godot_path}} --path FarmDefenseHarvestWars.GameClient

# Pornește Serverul Dedicat (Headless) - Simularea unui meci
serve:
    {{godot_path}} --headless --path FarmDefenseHarvestWars.GameClient --server

# --- DOCUMENTAȚIE (Mermaid) ---

# Generează diagrame din fișiere .mmd (necesită mermaid-cli: npm install -g @mermaid-js/mermaid-cli)
build-docs:
    #!/usr/bin/env bash
    for file in {{doc_dir}}/*.mmd; do \
        echo "Generating png for $file..."; \
        mmdc -p {{doc_dir}}/puppeteer-config.json -i "$file" -o "${file%.mmd}.png"; \
    done

# Curăță proiectul (șterge bin/obj)
clean:
    rm -rf **/bin
    rm -rf **/obj
