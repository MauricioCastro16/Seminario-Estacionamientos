#!/bin/bash
# Script para reiniciar la base de datos con migración automática
# Uso: ./reset-database.sh

echo "🗑️  Eliminando base de datos..."
dotnet ef database drop -f

echo "📁 Eliminando migraciones existentes..."
rm -rf ./Migrations

timestamp=$(date '+%Y-%m-%d_%H-%M')
migrationName="Migration_$timestamp"

echo "🆕 Creando nueva migración: $migrationName"
dotnet ef migrations add $migrationName

echo "📊 Aplicando migraciones..."
dotnet ef database update

echo "🚀 Iniciando aplicación..."
dotnet run
