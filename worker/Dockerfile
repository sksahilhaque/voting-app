# --- Build stage ---
FROM --platform=${BUILDPLATFORM} mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG TARGETPLATFORM
ARG TARGETARCH
ARG BUILDPLATFORM

RUN echo "Building on $BUILDPLATFORM for $TARGETPLATFORM using arch $TARGETARCH"

WORKDIR /source
COPY *.csproj .
RUN dotnet restore -a $TARGETARCH

COPY . .
RUN dotnet publish -c Release -o /app -a $TARGETARCH --self-contained false --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /app

COPY --from=build /app .

# Runtime env vars will be set in ECS
# Don't hardcode POSTGRES_CONN_STRING or REDIS_HOST
ENTRYPOINT ["dotnet", "Worker.dll"]
