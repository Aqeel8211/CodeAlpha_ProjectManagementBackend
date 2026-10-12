# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["ProjectManagementBackend.csproj", "./"]
RUN dotnet restore "ProjectManagementBackend.csproj"

# Copy the rest of the code and build the release
COPY . .
RUN dotnet publish "ProjectManagementBackend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the ASP.NET runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render provides the PORT environment variable, defaulting to 10000
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

# Start the application
ENTRYPOINT ["dotnet", "ProjectManagementBackend.dll"]
