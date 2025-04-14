## Creating an HTTPS Certificate for ASP.NET in Docker

> [!IMPORTANT]
> A certificate is required for the proper functioning of the application in Docker. Without it, the application may not work as expected when using HTTPS.

To enable HTTPS for your ASP.NET Core app running in Docker, you need to generate a development certificate and export it as a `.pfx` file:

### Linux

```bash
dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p development
dotnet dev-certs https --trust
```

### Windows

```powershell
dotnet dev-certs https -ep %USERPROFILE%\.aspnet\https\aspnetapp.pfx -p development
dotnet dev-certs https --trust
```