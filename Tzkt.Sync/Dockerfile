FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app
COPY . .
RUN cd Tzkt.Sync && dotnet publish -o output

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app
COPY --from=build /app/Tzkt.Sync/output ./
ENTRYPOINT ["dotnet", "Tzkt.Sync.dll"]
