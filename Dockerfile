# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY EscrowApp.sln ./
COPY EscrowApp/EscrowApp.csproj EscrowApp/
COPY EscrowApp.Tests/EscrowApp.Tests.csproj EscrowApp.Tests/
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet build -c Release --no-restore
RUN dotnet test -c Release --no-build --no-restore

# Publish
RUN dotnet publish EscrowApp/EscrowApp.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Security: run as non-root user
RUN groupadd -r escrowapp && useradd -r -g escrowapp escrowapp

COPY --from=build /app/publish .

# Configure ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

USER escrowapp
ENTRYPOINT ["dotnet", "EscrowApp.dll"]
