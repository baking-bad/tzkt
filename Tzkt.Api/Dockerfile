FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app
COPY . .
RUN cd Tzkt.Api && dotnet publish -o output

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
ENV ASPNETCORE_URLS=http://+:5000
WORKDIR /app
COPY --from=build /app/Tzkt.Api/output ./
ENTRYPOINT ["dotnet", "Tzkt.Api.dll"]
