# -------- Runtime image --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# -------- Build image --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet publish Menu.csproj -c Release -o /app/publish

# -------- Final image --------
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render sets PORT dynamically; EXPOSE is optional
EXPOSE 8080

ENTRYPOINT ["dotnet", "Menu.dll"]
