# Multi-stage build for Handily Commerce API (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution + project files first for better layer caching
COPY HandilyCommerce.slnx ./
# Product Versioning (Directory.Build.props) must be present for restore/publish
COPY Directory.Build.props ./
COPY src/HandilyCommerce.Domain/HandilyCommerce.Domain.csproj src/HandilyCommerce.Domain/
COPY src/HandilyCommerce.Application/HandilyCommerce.Application.csproj src/HandilyCommerce.Application/
COPY src/HandilyCommerce.Infrastructure/HandilyCommerce.Infrastructure.csproj src/HandilyCommerce.Infrastructure/
COPY src/HandilyCommerce.Api/HandilyCommerce.Api.csproj src/HandilyCommerce.Api/

RUN dotnet restore src/HandilyCommerce.Api/HandilyCommerce.Api.csproj

COPY src/ src/

RUN dotnet publish src/HandilyCommerce.Api/HandilyCommerce.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

USER $APP_UID

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "HandilyCommerce.Api.dll"]
