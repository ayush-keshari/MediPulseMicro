FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src

# The migrator mounts the repository at runtime and uses this image for EF CLI tooling.
RUN dotnet tool install --global dotnet-ef --version 9.0.4
ENV PATH="/root/.dotnet/tools:${PATH}"

ENTRYPOINT ["/bin/sh"]
