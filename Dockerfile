# Get build image
FROM mcr.microsoft.com/dotnet/sdk:6.0@sha256:43fd9b5215ce226ec22c3283c674e4625ce7caec1ffd47de40218f71a3fd1511 AS build

WORKDIR /app

# Copy source
COPY . ./

# Bring in metadata via --build-arg for build
ARG IMAGE_VERSION=unknown

# Restore packages
RUN dotnet restore

# Publish release project
RUN dotnet publish -c Release -o "/app/publish/"

# Get runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0@sha256:9096e96d53e771e7bb76b94fa9a527534eef6cae3371306148071632ab1a0d2c AS publish

WORKDIR /app

# Bring in metadata via --build-arg to publish
ARG BRANCH=unknown
ARG IMAGE_CREATED=unknown
ARG IMAGE_REVISION=unknown
ARG IMAGE_VERSION=unknown

# Configure image labels
LABEL branch=$branch \
    maintainer="Maricopa County Library District developers <development@mcldaz.org>" \
    org.opencontainers.image.authors="Maricopa County Library District developers <development@mcldaz.org>" \
    org.opencontainers.image.created=$IMAGE_CREATED \
    org.opencontainers.image.description="ImageOptimApi provides .NET access to the ImageOptim.com service" \
    org.opencontainers.image.documentation="http://github.com/MCLD/net-imageoptim-api" \
    org.opencontainers.image.source="https://github.com/MCLD/net-imageoptim-api" \
    org.opencontainers.image.licenses="MIT" \
    org.opencontainers.image.revision=$IMAGE_REVISION \
    org.opencontainers.image.source="https://github.com/MCLD/net-imageoptim-api" \
    org.opencontainers.image.title="ImageOptimApi" \
    org.opencontainers.image.url="https://github.com/MCLD/net-imageoptim-api" \
    org.opencontainers.image.vendor="Maricopa County Library District" \
    org.opencontainers.image.version=$IMAGE_VERSION

# Default image environment variable settings
ENV org.opencontainers.image.created=$IMAGE_CREATED \
    org.opencontainers.image.revision=$IMAGE_REVISION \
    org.opencontainers.image.version=$IMAGE_VERSION

# Copy source
COPY --from=build "/app/publish/" .

# Set entrypoint
ENTRYPOINT ["dotnet", "ImageOptimApi.Console.dll"]
