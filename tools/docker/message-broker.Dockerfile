FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /
COPY . .
RUN dotnet restore "src/MessageBroker/MessageBroker.csproj"
WORKDIR "/src/MessageBroker"
RUN dotnet publish "./MessageBroker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# aspnet runtime - modules depend on the Microsoft.AspNetCore.App shared framework
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MessageBroker.dll"]
