FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY . .
RUN cd Tzkt.Sync.Tests && dotnet publish -o output

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
RUN groupadd --gid 1000 tzkt \
    && useradd --gid tzkt --no-create-home --uid 1000 tzkt
WORKDIR /app
COPY --from=build /app/Tzkt.Sync.Tests/output ./
USER tzkt
ENTRYPOINT ["dotnet", "Tzkt.Sync.Tests.dll"]
