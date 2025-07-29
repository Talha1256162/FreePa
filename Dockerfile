# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /src

# Copy project files for dependency restoration
COPY ["src/NextGenArchitecture.API/NextGenArchitecture.API.csproj", "src/NextGenArchitecture.API/"]
COPY ["src/NextGenArchitecture.Application/NextGenArchitecture.Application.csproj", "src/NextGenArchitecture.Application/"]
COPY ["src/NextGenArchitecture.Domain/NextGenArchitecture.Domain.csproj", "src/NextGenArchitecture.Domain/"]
COPY ["src/NextGenArchitecture.Infrastructure/NextGenArchitecture.Infrastructure.csproj", "src/NextGenArchitecture.Infrastructure/"]
COPY ["src/NextGenArchitecture.Persistence/NextGenArchitecture.Persistence.csproj", "src/NextGenArchitecture.Persistence/"]
COPY ["src/NextGenArchitecture.SharedKernel/NextGenArchitecture.SharedKernel.csproj", "src/NextGenArchitecture.SharedKernel/"]

# Restore dependencies
RUN dotnet restore "src/NextGenArchitecture.API/NextGenArchitecture.API.csproj"

# Copy the entire source code
COPY . .

# Set the working directory to the API project
WORKDIR "/src/src/NextGenArchitecture.API"

# Build the application in Release mode
RUN dotnet build "NextGenArchitecture.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "NextGenArchitecture.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Set the working directory
WORKDIR /app

# Create logs directory and set permissions
RUN mkdir -p /app/logs && chown -R appuser:appuser /app

# Copy the published application from the publish stage
COPY --from=publish /app/publish .

# Change ownership of the app directory to the non-root user
RUN chown -R appuser:appuser /app

# Switch to the non-root user
USER appuser

# Expose the port the app runs on
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Configure health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "NextGenArchitecture.API.dll"]

# Labels for better container management
LABEL maintainer="NextGen Architecture Team <support@nextgenarchitecture.com>"
LABEL version="1.0.0"
LABEL description="NextGen Architecture API - A production-ready .NET Core architecture"
LABEL org.opencontainers.image.source="https://github.com/nextgen-architecture/nextgen-architecture"
LABEL org.opencontainers.image.documentation="https://github.com/nextgen-architecture/nextgen-architecture/README.md"
LABEL org.opencontainers.image.licenses="MIT"