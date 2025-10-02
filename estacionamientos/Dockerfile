# syntax=docker/dockerfile:1

# Valor por defecto del build-arg (para cuando no lo pasás desde el build)
ARG CONFIGURATION=Release

# ============ Build ============
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# REDECLARAR el ARG dentro del stage para que tenga valor aquí
ARG CONFIGURATION=Release

# Copiamos primero el csproj para cachear el restore
COPY ./estacionamientos.csproj ./estacionamientos.csproj
RUN dotnet restore ./estacionamientos.csproj

# Copiamos el resto del código y publicamos
COPY . .
RUN dotnet publish ./estacionamientos.csproj \
    --configuration "$CONFIGURATION" \
    --output /app/publish \
    --no-restore

# ============ Runtime ============
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "estacionamientos.dll"]
