# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["itoscon/itoscon.csproj", "itoscon/"]
RUN dotnet restore "itoscon/itoscon.csproj"

COPY . .
WORKDIR /src/itoscon
RUN dotnet publish "itoscon.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production

# สร้างโฟลเดอร์สำหรับ SQLite และโอนสิทธิ์ให้ user ที่ไม่ใช่ root เขียนไฟล์ได้
RUN mkdir -p /app/data && chown -R $APP_UID:$APP_UID /app/data

COPY --from=build /app/publish .

VOLUME ["/app/data"]
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "itoscon.dll"]