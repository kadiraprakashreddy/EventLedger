FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/EventGateway/EventGateway.Api/EventGateway.Api.csproj", "src/EventGateway/EventGateway.Api/"]
COPY ["src/EventGateway/EventGateway.Application/EventGateway.Application.csproj", "src/EventGateway/EventGateway.Application/"]
COPY ["src/EventGateway/EventGateway.Domain/EventGateway.Domain.csproj", "src/EventGateway/EventGateway.Domain/"]
COPY ["src/EventGateway/EventGateway.Infrastructure/EventGateway.Infrastructure.csproj", "src/EventGateway/EventGateway.Infrastructure/"]
RUN dotnet restore "src/EventGateway/EventGateway.Api/EventGateway.Api.csproj"

COPY src/EventGateway/ src/EventGateway/
RUN dotnet publish "src/EventGateway/EventGateway.Api/EventGateway.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN mkdir -p /app/data && apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "EventGateway.Api.dll"]