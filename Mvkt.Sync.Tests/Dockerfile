FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app
COPY . .
RUN cd Mvkt.Sync.Tests && dotnet publish -o output

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
RUN groupadd --gid 1000 mvkt \
    && useradd --gid mvkt --no-create-home --uid 1000 mvkt
WORKDIR /app
COPY --from=build /app/Mvkt.Sync.Tests/output ./
USER mvkt
ENTRYPOINT ["dotnet", "Mvkt.Sync.Tests.dll"]
