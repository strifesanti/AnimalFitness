# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/Directory.Build.props ./backend/
COPY backend/AnimalFitness.sln ./backend/
COPY backend/src/AnimalFitness.Domain/AnimalFitness.Domain.csproj ./backend/src/AnimalFitness.Domain/
COPY backend/src/AnimalFitness.Application/AnimalFitness.Application.csproj ./backend/src/AnimalFitness.Application/
COPY backend/src/AnimalFitness.Infrastructure/AnimalFitness.Infrastructure.csproj ./backend/src/AnimalFitness.Infrastructure/
COPY backend/src/AnimalFitness.API/AnimalFitness.API.csproj ./backend/src/AnimalFitness.API/

RUN dotnet restore ./backend/src/AnimalFitness.API/AnimalFitness.API.csproj

COPY backend/src/ ./backend/src/
RUN dotnet publish ./backend/src/AnimalFitness.API/AnimalFitness.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
HEALTHCHECK --interval=10s --timeout=5s --start-period=25s --retries=8 \
    CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "AnimalFitness.API.dll"]
