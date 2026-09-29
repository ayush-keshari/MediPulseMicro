FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY Directory.Build.props ./
COPY Shared/ ./Shared/
COPY Gateway/ ./Gateway/

WORKDIR /src/Gateway
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
# Replace the local-dev ocelot.json with the Docker version (container hostnames)
COPY Gateway/ocelot.docker.json ./ocelot.json
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "Gateway.dll"]
