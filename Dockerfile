#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

RUN addgroup --system --gid 1000 customgroup \
    && adduser --system --uid 1000 --ingroup customgroup --shell /bin/sh customuser
USER 1000

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY --chown=customuser:customgroup ["DelugeSync.csproj", "."]
RUN dotnet restore "./DelugeSync.csproj"
COPY --chown=customuser:customgroup . .
WORKDIR "/src/."
RUN dotnet build "DelugeSync.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DelugeSync.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --chown=customuser:customgroup --from=publish /app/publish .

ENTRYPOINT ["dotnet", "DelugeSync.dll"]