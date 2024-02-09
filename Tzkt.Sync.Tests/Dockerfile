FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app
COPY . .
RUN cd Tzkt.Sync.Tests && dotnet publish -o output

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
RUN groupadd --gid 1000 mvkt \
    && useradd --gid mvkt --no-create-home --uid 1000 mvkt
WORKDIR /app
COPY --from=build /app/Tzkt.Sync.Tests/output ./
USER mvkt
ENTRYPOINT ["dotnet", "Tzkt.Sync.Tests.dll"]
