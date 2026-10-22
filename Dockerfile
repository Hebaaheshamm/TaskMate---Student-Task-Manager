FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/TaskMate.Api/TaskMate.Api.csproj backend/TaskMate.Api/
COPY Frontend/ Frontend/
RUN dotnet restore backend/TaskMate.Api/TaskMate.Api.csproj

COPY . .
RUN dotnet publish backend/TaskMate.Api/TaskMate.Api.csproj \
    --configuration Release \
    --output /out \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .
RUN mkdir -p /app/data && chown -R $APP_UID:$APP_UID /app/data
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskMate.Api.dll"]
