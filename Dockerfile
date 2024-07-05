# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copy only the necessary files for restoring and building the application
COPY *.csproj ./
RUN dotnet restore

# Copy the entire project and build the application
COPY . .
RUN dotnet build -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Final stage: Use a lightweight image to run the published application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose the port and specify how to start the application
EXPOSE 80
ENTRYPOINT ["dotnet", "Task1.dll"]
