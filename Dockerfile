#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DelugeSync.csproj", "."]
RUN dotnet restore "./DelugeSync.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "DelugeSync.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DelugeSync.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ARG USER_ID
ARG GROUP_ID

RUN addgroup --force-badname --gid $GROUP_ID user
RUN adduser --disabled-password --gecos '' --uid $USER_ID --gid $GROUP_ID user
USER user
CMD ["dotnet", "DelugeSync.dll"]