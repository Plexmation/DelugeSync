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

#RUN addgroup --system --gid 1000 customgroup \
#    && adduser --system --uid 1000 --ingroup customgroup --shell /bin/sh customuser
#RUN mkdir -p /app/files
#RUN chown 1000:1000 -R /app
#RUN chmod 776 -R /app/files/

#USER customuser:customgroup
#VOLUME ["/app/files"]
COPY ["entrypoint.sh", "/entrypoint.sh"]
ENTRYPOINT ["/bin/sh", "entrypoint.sh"]

CMD ["dotnet", "DelugeSync.dll"]