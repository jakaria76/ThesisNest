# --- build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /out /p:UseAppHost=false
# --- runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000}
ENTRYPOINT ["dotnet", "ThesisNest.dll"]
