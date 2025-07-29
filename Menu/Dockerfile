# Use the official .NET SDK image (8.0)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet publish "Restaurant-Menu.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# ✅ Allow external traffic on port 80
EXPOSE 80

ENTRYPOINT ["dotnet", "Restaurant-Menu.dll"]
