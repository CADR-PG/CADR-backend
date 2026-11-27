FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /
COPY . .
RUN dotnet restore "src/FunctionsWorker/FunctionsWorker.csproj"
WORKDIR "/src/FunctionsWorker"
RUN dotnet publish "./FunctionsWorker.csproj" -c Release -o /home/site/wwwroot

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS final
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
COPY --from=build /home/site/wwwroot /home/site/wwwroot
