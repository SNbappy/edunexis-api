FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/EduNexis.API/EduNexis.API.csproj", "src/EduNexis.API/"]
COPY ["src/EduNexis.Application/EduNexis.Application.csproj", "src/EduNexis.Application/"]
COPY ["src/EduNexis.Domain/EduNexis.Domain.csproj", "src/EduNexis.Domain/"]
COPY ["src/EduNexis.Infrastructure/EduNexis.Infrastructure.csproj", "src/EduNexis.Infrastructure/"]
RUN dotnet restore "src/EduNexis.API/EduNexis.API.csproj"
COPY . .
WORKDIR "/src/src/EduNexis.API"
RUN dotnet build "EduNexis.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EduNexis.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EduNexis.API.dll"]
