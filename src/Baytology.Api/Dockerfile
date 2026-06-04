# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Baytology.Api/Baytology.Api.csproj", "src/Baytology.Api/"]
COPY ["src/Baytology.Application/Baytology.Application.csproj", "src/Baytology.Application/"]
COPY ["src/Baytology.Contracts/Baytology.Contracts.csproj", "src/Baytology.Contracts/"]
COPY ["src/Baytology.Domain/Baytology.Domain.csproj", "src/Baytology.Domain/"]
COPY ["src/Baytology.Infrastructure/Baytology.Infrastructure.csproj", "src/Baytology.Infrastructure/"]
RUN dotnet restore "./src/Baytology.Api/Baytology.Api.csproj"
COPY . .
WORKDIR "/src/src/Baytology.Api"
RUN dotnet build "./Baytology.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Baytology.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Baytology.Api.dll"]
