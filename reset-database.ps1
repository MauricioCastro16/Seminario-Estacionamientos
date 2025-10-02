# Script para reiniciar la base de datos con migración automática
# Uso: .\reset-database.ps1

Write-Host "🗑️  Eliminando base de datos..." -ForegroundColor Red
dotnet ef database drop -f

Write-Host "📁 Eliminando migraciones existentes..." -ForegroundColor Yellow
Remove-Item -Recurse -Force .\Migrations -ErrorAction SilentlyContinue

$timestamp = Get-Date -Format 'yyyy-MM-dd_HH-mm'
$migrationName = "Migration_$timestamp"

Write-Host "🆕 Creando nueva migración: $migrationName" -ForegroundColor Green
dotnet ef migrations add $migrationName

Write-Host "📊 Aplicando migraciones..." -ForegroundColor Blue
dotnet ef database update

Write-Host "🚀 Iniciando aplicación..." -ForegroundColor Magenta
dotnet run
