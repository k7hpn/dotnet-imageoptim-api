# Get build image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /app

# Copy source
COPY . ./

# Bring in metadata via --build-arg for build
ARG IMAGE_VERSION=unknown

# Restore packages
RUN dotnet restore

# Publish release project
RUN dotnet build -c Release

# Generate NuGet package
RUN dotnet pack -c Release ImageOptimApi -o "/app/publish/"

# Copy release-publish.bash script
RUN cp /app/release-publish.bash "/app/publish/"

# Get runtime image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS publish

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
    org.opencontainers.image.documentation="http://github.com/MCLD/dotnet-imageoptim-api" \
    org.opencontainers.image.licenses="MIT" \
    org.opencontainers.image.revision=$IMAGE_REVISION \
    org.opencontainers.image.source="https://github.com/MCLD/dotnet-imageoptim-api" \
    org.opencontainers.image.title="ImageOptimApi" \
    org.opencontainers.image.url="https://github.com/MCLD/dotnet-imageoptim-api" \
    org.opencontainers.image.vendor="Maricopa County Library District" \
    org.opencontainers.image.version=$IMAGE_VERSION

# Default image environment variable settings
ENV org.opencontainers.image.created=$IMAGE_CREATED \
    org.opencontainers.image.revision=$IMAGE_REVISION \
    org.opencontainers.image.version=$IMAGE_VERSION

# Copy source
COPY --from=build "/app/publish/" .

# Set entrypoint
ENTRYPOINT ["/app/release-publish.bash"]
