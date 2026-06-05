#!/bin/bash

# --- CONFIGURARE ---
# Poți modifica aceste valori sau le poți trimite ca argumente
VPS_USER="root"
VPS_IP="1.2.3.4" # Modifică cu IP-ul tău real
DOCKER_USER="contul_tau_dockerhub"
IMAGE_NAME="farmdefense-backend"
TAG="latest"

FULL_IMAGE="${DOCKER_USER}/${IMAGE_NAME}:${TAG}"

echo "🚀 Începere proces de deployment pentru ${FULL_IMAGE}..."

# 1. Build & Push (Opțional - dacă vrei să faci totul dintr-un pas)
echo "📦 Building and pushing image..."
docker build -t ${FULL_IMAGE} .
docker push ${FULL_IMAGE}

# 2. Copiere fișier docker-compose.prod.yml pe VPS
echo "📤 Copiere configurație pe VPS (${VPS_IP})..."
scp docker-compose.prod.yml ${VPS_USER}@${VPS_IP}:~/docker-compose.yml

# 3. Executare comenzi pe VPS prin SSH
echo "🛠️ Repornire servicii pe VPS..."
ssh ${VPS_USER}@${VPS_IP} << EOF
    # Setăm variabilele de mediu pentru docker-compose
    export DOCKER_IMAGE=${FULL_IMAGE}
    export VPS_IP=${VPS_IP}
    
    # Oprim containerele vechi
    docker compose down || true
    
    # Tragem imaginea nouă
    docker compose pull
    
    # Pornim totul în background
    docker compose up -d
    
    echo "✅ Deployment finalizat cu succes pe VPS!"
EOF
