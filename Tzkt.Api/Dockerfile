FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY . .
RUN cd Tzkt.Api && dotnet publish -o output

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
ENV ASPNETCORE_URLS=http://+:5000
RUN groupadd --gid 1000 tzkt \
    && useradd --gid tzkt --no-create-home --uid 1000 tzkt
WORKDIR /app
EXPOSE 5000
COPY --from=build /app/Tzkt.Api/output ./
USER tzkt
ENTRYPOINT ["dotnet", "Tzkt.Api.dll"]
