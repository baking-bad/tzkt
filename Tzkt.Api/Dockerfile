FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app
COPY . .
RUN cd Tzkt.Api && dotnet publish -o output

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
ENV ASPNETCORE_URLS=http://+:5000
WORKDIR /app
COPY --from=build /app/Tzkt.Api/output ./
ENTRYPOINT ["dotnet", "Tzkt.Api.dll"]
