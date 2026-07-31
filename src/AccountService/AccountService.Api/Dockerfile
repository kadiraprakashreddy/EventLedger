FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/AccountService/AccountService.Api/AccountService.Api.csproj", "src/AccountService/AccountService.Api/"]
COPY ["src/AccountService/AccountService.Application/AccountService.Application.csproj", "src/AccountService/AccountService.Application/"]
COPY ["src/AccountService/AccountService.Domain/AccountService.Domain.csproj", "src/AccountService/AccountService.Domain/"]
COPY ["src/AccountService/AccountService.Infrastructure/AccountService.Infrastructure.csproj", "src/AccountService/AccountService.Infrastructure/"]
RUN dotnet restore "src/AccountService/AccountService.Api/AccountService.Api.csproj"

COPY src/AccountService/ src/AccountService/
RUN dotnet publish "src/AccountService/AccountService.Api/AccountService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN mkdir -p /app/data && apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AccountService.Api.dll"]