FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /
COPY . .
RUN dotnet restore "src/API/API.csproj"
WORKDIR "/src/API"
RUN dotnet publish "./API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]
