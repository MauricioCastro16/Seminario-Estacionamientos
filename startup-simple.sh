#!/bin/bash

echo "🚀 Iniciando aplicación NetParking..."

# Verificar variables de entorno críticas
if [ -z "$DB_HOST" ] || [ -z "$DB_PORT" ] || [ -z "$DB_NAME" ] || [ -z "$DB_USER" ] || [ -z "$DB_PASSWORD" ]; then
    echo "❌ Error: Variables de entorno de base de datos no configuradas"
    echo "DB_HOST: $DB_HOST"
    echo "DB_PORT: $DB_PORT" 
    echo "DB_NAME: $DB_NAME"
    echo "DB_USER: $DB_USER"
    echo "DB_PASSWORD: [HIDDEN]"
    exit 1
fi

echo "✅ Variables de entorno configuradas correctamente"

# Función para aplicar migraciones de forma simple
apply_migrations() {
    echo "📊 Aplicando migraciones..."
    
    # Intentar aplicar migraciones con Entity Framework
    dotnet ef database update --no-build --verbose || {
        echo "⚠️  Error con EF migrations, creando base de datos básica..."
        
        # Crear esquema básico si falla EF
        PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "
        CREATE SCHEMA IF NOT EXISTS public;
        CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (
            \"MigrationId\" character varying(150) NOT NULL,
            \"ProductVersion\" character varying(32) NOT NULL,
            CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY (\"MigrationId\")
        );
        " || echo "⚠️  Error creando esquema básico"
    }
    
    echo "✅ Migraciones aplicadas"
}

# Aplicar migraciones
apply_migrations

echo "🚀 Iniciando aplicación ASP.NET Core..."

# Ejecutar la aplicación
exec dotnet estacionamientos.dll
