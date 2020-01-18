FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app
COPY . .
RUN cd Tzkt.Sync && dotnet publish -o output

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/Tzkt.Sync/output ./
ENTRYPOINT ["dotnet", "Tzkt.Sync.dll"]
