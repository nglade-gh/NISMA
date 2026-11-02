#!/bin/bash
# Script d'archivage du projet NISMA
# Exclut : .git, builds (bin/obj), artefacts IDE, fichiers temporaires

set -euo pipefail

# Répertoire du projet (dernier argument ou celui par défaut)
PROJECT_DIR="${1:-$(pwd)}"
cd "$PROJECT_DIR" || exit 1

# Vérification que le projet est bien NISMA
if [[ ! -f "nbd.sln" && ! -f "NISMA_Core/NISMA_Core.csproj" ]]; then
    echo "Erreur : ce dossier ne semble pas être un projet NISMA." >&2
    exit 1
fi

# Nom de l'archive avec date
ARCHIVE_NAME="Nisma_Archive_$(date +%Y%m%d_%H%M%S).zip"

# Nettoyage des artefacts de build et .git
echo "Archivage de NISMA dans : $ARCHIVE_NAME"
echo "Exclusions appliquées : .git, bin/, obj/, .idea/, *.nupkg, *.log, temp.*"

zip -r "$ARCHIVE_NAME" \
    . \
    -x \
    '.git/*' \
    '*/bin/*' \
    '*/obj/*' \
    '.idea/*' \
    '*.nupkg' \
    '*.nuspec' \
    '*.user' \
    '*.suo' \
    '*.cache' \
    '*.tmp' \
    '*.bak' \
    '*.log' \
    '*.vs' \
    '*.swp' \
    '*.DS_Store' \
    '*.nuget' \
    '*.userosscache' \
    '*.sln.docstates' \
    'core.*'

echo "Archive créée : $ARCHIVE_NAME"
echo "Taille : $(du -h "$ARCHIVE_NAME" | cut -f1)"
