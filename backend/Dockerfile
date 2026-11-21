FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore WebApplication1.csproj
COPY . .
RUN dotnet publish WebApplication1.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN addgroup --system serena-svc && adduser --system --ingroup serena-svc serena-svc
COPY --from=build /app/publish .
RUN chown -R serena-svc:serena-svc /app

USER serena-svc
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "WebApplication1.dll"]
