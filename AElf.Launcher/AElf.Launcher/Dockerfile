FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ARG TARGETPLATFORM
WORKDIR /app

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETPLATFORM
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AElf.Launcher/AElf.Launcher.csproj", "AElf.Launcher/"]
COPY NuGet.Config .
RUN dotnet restore "AElf.Launcher/AElf.Launcher.csproj" --configfile NuGet.Config
COPY . .
WORKDIR "/src/AElf.Launcher"
RUN dotnet build "AElf.Launcher.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AElf.Launcher.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directory structure
RUN mkdir -p /root/.local/share/aelf/keys

# Copy configuration files
COPY ["AElf.Launcher/log4net.config", "AElf.Launcher/appsettings.json", "AElf.Launcher/appsettings.LocalTestNode.json", "./"]
COPY ./*.xml ./
# Create symbolic link
RUN ln -s /app/W1ptWN5n5mfdVvh3khTRm9KMJCAUdge9txNyVtyvZaYRYcqc1.json /root/.local/share/aelf/keys/W1ptWN5n5mfdVvh3khTRm9KMJCAUdge9txNyVtyvZaYRYcqc1.json

ENV ASPNETCORE_ENVIRONMENT=LocalTestNode
ENTRYPOINT ["dotnet", "AElf.Launcher.dll"]
