FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["tdpTrans-app-backend/TdpTrans.Controllers/TdpTrans.Controllers.csproj", "tdpTrans-app-backend/TdpTrans.Controllers/"]
COPY ["tdpTrans-app-backend/TdpTrans.Services/TdpTrans.Services.csproj", "tdpTrans-app-backend/TdpTrans.Services/"]
COPY ["tdpTrans-app-backend/TdpTrans.Repositories/TdpTrans.Repositories.csproj", "tdpTrans-app-backend/TdpTrans.Repositories/"]
COPY ["tdpTrans-app-backend/TdpTrans.Models/TdpTrans.Models.csproj", "tdpTrans-app-backend/TdpTrans.Models/"]
COPY ["tdpTrans-app-backend/TdpTrans.DTOs/TdpTrans.DTOs.csproj", "tdpTrans-app-backend/TdpTrans.DTOs/"]

RUN dotnet restore "tdpTrans-app-backend/TdpTrans.Controllers/TdpTrans.Controllers.csproj"

COPY . .

RUN dotnet publish "tdpTrans-app-backend/TdpTrans.Controllers/TdpTrans.Controllers.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000} dotnet TdpTrans.Controllers.dll"]
