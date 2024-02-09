FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app
COPY . .
RUN cd Tzkt.Sync && dotnet publish -o output

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
RUN groupadd --gid 1000 mvkt \
    && useradd --gid mvkt --no-create-home --uid 1000 mvkt
WORKDIR /app
COPY --from=build /app/Tzkt.Sync/output ./
USER mvkt
ENTRYPOINT ["dotnet", "Tzkt.Sync.dll"]
