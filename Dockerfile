# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project definitions for optimized caching
COPY ["UnitConversion.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/UnitConversion.Domain/UnitConversion.Domain.csproj", "src/UnitConversion.Domain/"]
COPY ["src/UnitConversion.Application/UnitConversion.Application.csproj", "src/UnitConversion.Application/"]
COPY ["src/UnitConversion.Infrastructure/UnitConversion.Infrastructure.csproj", "src/UnitConversion.Infrastructure/"]
COPY ["src/UnitConversion.Api/UnitConversion.Api.csproj", "src/UnitConversion.Api/"]
COPY ["tests/UnitConversion.Tests.Unit/UnitConversion.Tests.Unit.csproj", "tests/UnitConversion.Tests.Unit/"]
COPY ["tests/UnitConversion.Tests.Integration/UnitConversion.Tests.Integration.csproj", "tests/UnitConversion.Tests.Integration/"]

RUN dotnet restore "UnitConversion.sln"

# Copy full source tree and build
COPY . .
WORKDIR "/src/src/UnitConversion.Api"
RUN dotnet publish "UnitConversion.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "UnitConversion.Api.dll"]
