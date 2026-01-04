FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build

WORKDIR /app

# Copy source code and compile
COPY ./ ./
RUN dotnet restore

RUN dotnet publish -o bin

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS runtime

WORKDIR /app
COPY --from=build /app/bin .

ENTRYPOINT ["dotnet", "SigmaChatServer.App.dll"]
