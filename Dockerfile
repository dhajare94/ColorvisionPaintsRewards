# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy the csproj and restore any dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the application and build it
COPY . ./
RUN dotnet publish -c Release -o out

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Expose the port the app will run on (Azure and Railway often use 8080)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Start the application
ENTRYPOINT ["dotnet", "QRRewardPlatform.dll"]
