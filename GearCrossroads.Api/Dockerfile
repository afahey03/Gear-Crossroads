# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy and restore
COPY *.sln .
COPY GearCrossroads.Api/*.csproj ./GearCrossroads.Api/
RUN dotnet restore GearCrossroads.Api/GearCrossroads.Api.csproj

# Copy everything and build
COPY . .
RUN dotnet publish GearCrossroads.Api/GearCrossroads.Api.csproj -c Release -o /app/out

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Expose port 8080 for Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GearCrossroads.Api.dll"]
