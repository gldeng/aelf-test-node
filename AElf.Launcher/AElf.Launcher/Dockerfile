FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/runtime:8.0 AS base
ARG TARGETPLATFORM
USER $APP_UID
WORKDIR /app/AElf.Launcher

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETPLATFORM
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AElf.Launcher/AElf.Launcher.csproj", "AElf.Launcher/"]
COPY ../NuGet.Config .
RUN dotnet restore "AElf.Launcher/AElf.Launcher.csproj" -v d --configfile NuGet.Config
COPY . .
WORKDIR "/src/AElf.Launcher"
RUN dotnet build "AElf.Launcher.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AElf.Launcher.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app/AElf.Launcher
COPY --from=publish /app/publish .

# Create directory structure and set permissions
RUN mkdir -p /home/app/.local/share/aelf/keys && \
    chown -R $APP_UID:$APP_UID /home/app

# Switch to non-root user
USER $APP_UID

# Create symbolic link in the user's home directory
RUN ln -s /app/AElf.Launcher/W1ptWN5n5mfdVvh3khTRm9KMJCAUdge9txNyVtyvZaYRYcqc1.json /home/app/.local/share/aelf/keys/W1ptWN5n5mfdVvh3khTRm9KMJCAUdge9txNyVtyvZaYRYcqc1.json

ENV ASPNETCORE_ENVIRONMENT=LocalTestNode
ENTRYPOINT ["dotnet", "AElf.Launcher.dll"]
