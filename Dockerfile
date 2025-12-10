# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["GameReviewsAPI.csproj", "."]
RUN dotnet restore "GameReviewsAPI.csproj"

# Copy source code including clientapp
COPY . .

# Create clientapp directory if it doesn't exist
RUN mkdir -p ClientApp || true

# Build the project
RUN dotnet build "GameReviewsAPI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "GameReviewsAPI.csproj" -c Release -o /app/publish /p:SkipClientBuild=true

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "GameReviewsAPI.dll"]

