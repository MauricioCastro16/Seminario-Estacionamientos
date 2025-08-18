# 🚗 Seminario - Estacionamientos (MVC + PostgreSQL + .NET 9)

Proyecto ASP.NET Core MVC con conexión a PostgreSQL, usando Entity Framework Core

---

## 📋 Requisitos previos

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download)
- [PostgreSQL](https://www.postgresql.org/download/)

Instalar herramientas necesarias:
```bash
dotnet tool install --global dotnet-ef

⚙️ Configuración inicial

►Clonar el repositorio
git clone https://github.com/MauricioCastro16/Seminario-Estacionamientos

►Crear base de datos en PostgreSQL
CREATE DATABASE estacionamientosdb;

►Crear el archivo .env en la carpeta del proyecto
Seminario-Estacionamientos/estacionamientos/.env según el .env.example

►Restaurar dependencias
dotnet restore

🛠️ Base de datos y migraciones
►Aplicar migraciones iniciales:

cd estacionamientos
dotnet ef database update

🚀 Ejecutar el proyecto

cd estacionamientos #Si no lo hiciste
dotnet run

🧪 Comandos útiles
►Crear nueva migración:
dotnet ef migrations add NombreMigracion

►Aplicar migraciones:
dotnet ef database update

►Ejecutar en desarrollo:
dotnet run

►Ejecutar con hot reload:
dotnet watch run

# El archivo .env no debe subirse a Git. Está en .gitignore.
```

## Reiniciar la base de datos
``` bash
(a) Tirar la base (usa la connection string actual)
dotnet ef database drop -f

(b) Borrar carpeta de migraciones (en el proyecto)
#En Windows
Remove-Item -Recurse -Force .\Migrations
#En Mac
rm -rf ./Migrations

(c) Crear migración inicial nueva
dotnet ef migrations add InitialCreate

(d) Aplicarla
dotnet ef database update

```

# Capas y su explicación
## Controllers
Orquestan la request → llaman servicios → devuelven View/JSON.
No deberían contener reglas de negocio ni queries complejas.

## Services
Lógica de negocio
Acá van reglas, validaciones de dominio, cálculos, casos de uso (crear turno, cerrar caja, recalcular promedio, etc.).
Se exponen como interfaces (p. ej. IPlayasService) e implementaciones inyectables.

## Data
Acceso a datos: AppDbContext (EF Core) y, si querés, repositorios finos para consultas específicas.
El service usa el DbContext (o repos), maneja transacciones y unit of work.

## Models
Entidades (EF), Value Objects, enums. Sin dependencias de UI.

## Views
Formatos para entrada/salida (lo que recibe y devuelve el controller). Usá AutoMapper si te gusta.

## Validators
Reglas de validación de entrada (FluentValidation) separadas del controller.

# Estrategia de ramificación - GitFlow

## **main**
Rama principal y estable. Contiene únicamente versiones listas para producción.

## hotfix/*
Rama para arreglar rápido errores críticos en producción. Parte de **main** y luego se fusiona en **main** y **develop**.

## release/*
Rama para preparar una nueva versión (solo fixes y ajustes menores). Parte de **develop** y luego se fusiona en **main** y **develop**.

## **develop**
Rama de integración donde se juntan todas las nuevas funcionalidades antes de un release.

## feature/*
Rama temporal para desarrollar una nueva funcionalidad. Parte de **develop** y vuelve a **develop**.



