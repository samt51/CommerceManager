FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/CommerceManager.Domain/CommerceManager.Domain.csproj", "src/CommerceManager.Domain/"]
COPY ["src/CommerceManager.Application/CommerceManager.Application.csproj", "src/CommerceManager.Application/"]
COPY ["src/CommerceManager.Infrastructure/CommerceManager.Infrastructure.csproj", "src/CommerceManager.Infrastructure/"]
COPY ["src/CommerceManager.Api/CommerceManager.Api.csproj", "src/CommerceManager.Api/"]

RUN dotnet restore "src/CommerceManager.Api/CommerceManager.Api.csproj"

COPY . .
RUN dotnet publish "src/CommerceManager.Api/CommerceManager.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "CommerceManager.Api.dll"]
