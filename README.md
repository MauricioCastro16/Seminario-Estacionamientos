[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/MauricioCastro16/Seminario-Estacionamientos)

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
#En Mac/Linux
rm -rf ./Migrations

(c) Crear migración inicial nueva
#En Windows
dotnet ef migrations add "Migration_$(Get-Date -Format 'yyyy-MM-dd_HH-mm')"
#En Mac/Linux
dotnet ef migrations add "Migration_$(date '+%Y-%m-%d_%H-%M')"

(d) Aplicarla
dotnet ef database update

Podés correr reset-database.ps1 (Windows) o reset-database.sh (Mac/Linux para no copiar el comando)

(*) Para pegar en la terminal todo junto (Windows PowerShell)
dotnet ef database drop -f
Remove-Item -Recurse -Force .\Migrations
dotnet ef migrations add "Migration_$(Get-Date -Format 'yyyy-MM-dd_HH-mm')"
dotnet ef database update
dotnet run

(**) para Mac/Linux
dotnet ef database drop -f
rm -rf ./Migrations
dotnet ef migrations add "Migration_$(date '+%Y-%m-%d_%H-%M')"
dotnet ef database update
dotnet run

(***) Usando scripts automáticos (Recomendado)
# Windows PowerShell
.\reset-database.ps1

# Mac/Linux
chmod +x reset-database.sh
./reset-database.sh
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

# Autenticación y Roles en el Proyecto

## 📌 Autorización en Controladores o Acciones
## .cs
### Restringir a un rol específico:
``` bash
[Authorize(Roles = "Administrador")]
public IActionResult InterfazAdministrador()
{
    // Lógica Administrador
}
```
### Permitir varios roles:
``` bash
[Authorize(Roles = "Administrador, Playero")]
public IActionResult InterfazAdministradorYPlayero()
{
    // Lógica Administrador y Playero
}
```
### Chequear en código:
``` bash
if (User.IsInRole("Conductor"))
{
    // Lógica Conductores
}
```

### Obtener el UsuNU para realizar una consulta a la base de datos
``` bash
using System.Security.Claims;
var usuNu = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
```

## .cshtml
``` bash
@if (User.IsInRole("Administrador"))
{
    //Lógica Administrador
}
@using System.Security.Claims

@if (User.Identity?.IsAuthenticated ?? false)
{
    //Lógica Loguineado
}
else
{
    //Lógica No loguineado
}
```
