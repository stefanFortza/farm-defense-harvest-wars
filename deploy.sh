#!/bin/bash

# --- CONFIGURARE ---
VPS_USER="root"
VPS_IP="51.15.131.79" # Modifică cu IP-ul tău real
DOCKER_USER="stefantacu"
IMAGE_NAME="farmdefense-backend"
TAG="latest"

FULL_IMAGE="${DOCKER_USER}/${IMAGE_NAME}:${TAG}"

echo "🚀 Începere proces de deployment pentru ${FULL_IMAGE}..."

# 1. Copiere fișier docker-compose.prod.yml pe VPS (ca docker-compose.yml)
echo "📤 Copiere configurație pe VPS (${VPS_IP})..."
scp docker-compose.prod.yml ${VPS_USER}@${VPS_IP}:~/docker-compose.yml

# 2. Executare comenzi de instalare oficială și pornire pe VPS prin SSH
echo "🛠️ Conectare la VPS pentru configurare depozit oficial și deployment..."
ssh ${VPS_USER}@${VPS_IP} << EOF
    
    # Verificăm dacă Docker CE este deja instalat
    if ! command -v docker &> /dev/null; then
        echo "📦 Docker nu este detectat. Se începe instalarea oficială prin APT repository..."
        
        # 1. Sincronizare pachete și instalare dependente inițiale
        sudo apt update -y
        sudo apt install -y ca-certificates curl
        
        # 2. Configurare cheie oficială GPG Docker
        sudo install -m 0755 -d /etc/apt/keyrings
        sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
        sudo chmod a+r /etc/apt/keyrings/docker.asc

        # 3. Adăugare depozit oficial Docker în sursele APT
        sudo tee /etc/apt/sources.list.d/docker.sources <<EOT
Types: deb
URIs: https://download.docker.com/linux/ubuntu
Suites: \$(. /etc/os-release && echo "\${UBUNTU_CODENAME:-\$VERSION_CODENAME}")
Components: stable
Architectures: \$(dpkg --print-architecture)
Signed-By: /etc/apt/keyrings/docker.asc
EOT

        # 4. Actualizare index pachete cu noul depozit
        sudo apt update -y

        # 5. Instalare Docker Engine și plugin-uri oficiale
        sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
        
        echo "✅ Docker Engine a fost instalat cu succes din sursele oficiale!"
    else
        echo "✅ Docker este deja instalat pe sistem."
    fi

    # Asigurarea că serviciul Docker este pornit și activat la boot
    sudo systemctl start docker
    sudo systemctl enable docker

    # Verificare Docker Compose v2
    if ! docker compose version &> /dev/null; then
        echo "🔄 Docker Compose v2 lipsește sau este configurat greșit. Se reîncearcă instalarea plugin-ului..."
        sudo apt-get update -y
        sudo apt-get install -y docker-compose-plugin
    fi
    echo "✅ Versiune Docker Compose: \$(docker compose version)"

    # Configurare Firewall (UFW) pentru a deschide porturile de joc
    echo "🛡️ Configurare Firewall (UFW)..."
    if command -v ufw &> /dev/null; then
        sudo ufw allow 22/tcp comment 'SSH'
        sudo ufw allow 80/tcp comment 'HTTP Backend'
        sudo ufw allow 5177/tcp comment 'API Port Alternativ'
        sudo ufw allow 7777:7800/udp comment 'Godot Game Servers UDP'
        sudo ufw allow 7777:7800/tcp comment 'Godot Game Servers TCP'
        
        # Activăm firewall-ul fără confirmare manuală
        sudo ufw --force enable
        echo "✅ Porturile UDP (7777-7800) și HTTP (80) sunt deschise!"
    else
        echo "⚠️ UFW nu este instalat pe acest sistem, verifică manual porturile."
    fi

    # Procedura de Deployment propriu-zisă
    echo "🏗️ Pornire pipeline de producție în Docker..."
    export DOCKER_IMAGE="${FULL_IMAGE}"
    export VPS_IP="${VPS_IP}"
    
    # Oprim containerele vechi (dacă există)
    docker compose down || true
    
    # Tragem imaginea nouă de pe Docker Hub
    echo "📥 Se descarcă imaginea stabilă de pe Docker Hub..."
    docker compose pull
    
    # Pornim containerele în background
    echo "⚡ Lansare containere active în mod Production..."
    docker compose up -d
    
    echo "📊 Status containere active:"
    docker ps
    
    echo "✅ Scriptul de deployment s-a executat complet!"
EOF