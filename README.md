# 🚗 Seminario - Estacionamientos (MVC + PostgreSQL + .NET 9)

Proyecto ASP.NET Core MVC con conexión a PostgreSQL, usando Entity Framework Core

---

## 📋 Requisitos previos

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download)
- [PostgreSQL](https://www.postgresql.org/download/)
- [dotnet-ef](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

Instalar herramientas necesarias:
```bash
dotnet tool install --global dotnet-ef

⚙️ Configuración inicial
Clonar el repositorio
git clone <URL_DEL_REPO>
cd Seminario-Estacionamientos/estacionamientos

Crear base de datos en PostgreSQL
CREATE DATABASE estacionamientosdb;

Crear el archivo .env en la carpeta del proyecto
Seminario-Estacionamientos/estacionamientos/.env según el .env.example

Restaurar dependencias
dotnet restore
🛠️ Base de datos y migraciones
Aplicar migraciones iniciales:
dotnet ef database update
🚀 Ejecutar el proyecto
Desde la carpeta estacionamientos:
dotnet run
Abrir en el navegador la URL que aparezca en consola, por ejemplo:
https://localhost:7254

📦 Estructura del proyecto
Seminario-Estacionamientos/
└── estacionamientos/
    ├── Controllers/
    ├── Data/
    ├── Models/
    ├── Views/
    ├── .env
    ├── Program.cs
    ├── appsettings.json
    └── estacionamientos.csproj
🧪 Comandos útiles
Crear nueva migración:
dotnet ef migrations add NombreMigracion

Aplicar migraciones:
dotnet ef database update

Ejecutar en desarrollo:
dotnet run
El archivo .env no debe subirse a Git. Está en .gitignore.
```