# syntax=docker/dockerfile:1

ARG CONFIGURATION=Release

# ============ Build ============
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiamos primero el csproj para cachear el restore
COPY ./estacionamientos.csproj ./estacionamientos.csproj
RUN dotnet restore ./estacionamientos.csproj

# Copiamos el resto del código y publicamos
COPY . .
RUN dotnet publish ./estacionamientos.csproj -c $CONFIGURATION -o /app/publish --no-restore

# ============ Runtime ============
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Puerto usual para contenedores (Render usa 8080)
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

COPY --from=build /app/publish ./

# Si el assembly no se llama "estacionamientos.dll", cambialo aquí
ENTRYPOINT ["dotnet", "estacionamientos.dll"]
