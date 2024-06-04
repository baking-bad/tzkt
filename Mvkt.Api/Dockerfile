FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app
COPY . .
RUN cd Mvkt.Api && dotnet publish -o output

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
ENV ASPNETCORE_URLS=http://+:5000
RUN groupadd --gid 1000 mvkt \
    && useradd --gid mvkt --no-create-home --uid 1000 mvkt
WORKDIR /app
EXPOSE 5000
COPY --from=build /app/Mvkt.Api/output ./
USER mvkt
ENTRYPOINT ["dotnet", "Mvkt.Api.dll"]
