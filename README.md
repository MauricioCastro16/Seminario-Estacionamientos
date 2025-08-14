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
git clone https://github.com/MauricioCastro16/Seminario-Estacionamientos
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

Ejecutar con hot reload:
dotnet watch run

El archivo .env no debe subirse a Git. Está en .gitignore.
```

# Reiniciar la base de datos
``` bash
(a) Tirar la base (usa la connection string actual)
dotnet ef database drop -f

(b) Borrar carpeta de migraciones (en el proyecto)
Remove-Item -Recurse -Force .\Migrations

(c) Crear migración inicial nueva
dotnet ef migrations add InitialCreate

(d) Aplicarla
dotnet ef database update

```

#Capas y su explicación
##Controllers
Orquestan la request → llaman servicios → devuelven View/JSON.
No deberían contener reglas de negocio ni queries complejas.

##Services
Lógica de negocio
Acá van reglas, validaciones de dominio, cálculos, casos de uso (crear turno, cerrar caja, recalcular promedio, etc.).
Se exponen como interfaces (p. ej. IPlayasService) e implementaciones inyectables.

##Data
Acceso a datos: AppDbContext (EF Core) y, si querés, repositorios finos para consultas específicas.
El service usa el DbContext (o repos), maneja transacciones y unit of work.

##Models
Entidades (EF), Value Objects, enums. Sin dependencias de UI.

##Views
Formatos para entrada/salida (lo que recibe y devuelve el controller). Usá AutoMapper si te gusta.

##Validators
Reglas de validación de entrada (FluentValidation) separadas del controller.