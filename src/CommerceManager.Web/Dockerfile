FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/CommerceManager.Web/CommerceManager.Web.csproj", "src/CommerceManager.Web/"]

RUN dotnet restore "src/CommerceManager.Web/CommerceManager.Web.csproj"

COPY . .
RUN dotnet publish "src/CommerceManager.Web/CommerceManager.Web.csproj" \
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

ENTRYPOINT ["dotnet", "CommerceManager.Web.dll"]
