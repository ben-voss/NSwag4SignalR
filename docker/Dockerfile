# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compile

ARG VERSION
ARG GIT_SHA

ENV VERSION=${VERSION}
ENV GIT_SHA=${GIT_SHA}

WORKDIR /app

# Copy csproj and restore as distinct layers
RUN --mount=type=bind,target=/docker-context \
    cd /docker-context/; \
    cp NSwag4SignalR.sln /app/; \
    find . -name "*.csproj" -mindepth 0 -maxdepth 4 -exec cp --parents "{}" /app/ \;

RUN dotnet restore NSwag4SignalR.sln

# Copy everything else and build
COPY . .
RUN dotnet build
RUN dotnet publish ./src/NSwag4SignalR.Example/NSwag4SignalR.Example.csproj -c Release -o /app/publish --no-restore

FROM compile AS pack
RUN dotnet pack src/NSwag4SignalR/NSwag4SignalR.csproj -c Release -o /app/artifacts /p:PackageVersion=${VERSION} /p:VersionPrefix=${VERSION} /p:SourceRevisionId=${GIT_SHA}

# Example app image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS example
WORKDIR /app
COPY --from=compile /app/publish .

ENTRYPOINT ["dotnet", "/app/NSwag4SignalR.Example.dll"]
