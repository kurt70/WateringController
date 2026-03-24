FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/WateringController.Backend.csproj backend/
COPY frontend/WateringController.Frontend.csproj frontend/

RUN dotnet restore backend/WateringController.Backend.csproj
RUN dotnet restore frontend/WateringController.Frontend.csproj

COPY backend/. backend/
COPY frontend/. frontend/

RUN dotnet publish frontend/WateringController.Frontend.csproj -c Release -o /tmp/frontend-publish
RUN rm -rf backend/wwwroot && mkdir -p backend/wwwroot && cp -a /tmp/frontend-publish/wwwroot/. backend/wwwroot/
RUN dotnet publish backend/WateringController.Backend.csproj -c Release -o /app/publish /p:UseAppHost=false /p:SkipFrontendPublish=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WateringController.Backend.dll"]
