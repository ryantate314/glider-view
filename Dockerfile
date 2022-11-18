FROM microsoft/dotnet:2.1-sdk-nanoserver-1809 AS builder

WORKDIR /glider-view
COPY GliderView.sln .
COPY GliderView.API/GliderView.API.csproj GliderView.API/GliderView.API.csproj
COPY GliderView.Service/GliderView.Service.csproj GliderView.Service/GliderView.Service.csproj
COPY GliderView.Data/GliderView.Data.csproj GliderView.Data/GliderView.Data.csproj
RUN dotnet restore GliderView.API/GliderView.API.csproj

COPY src src
RUN dotnet publish .\src\AlbumViewerNetCore\AlbumViewerNetCore.csproj

# app image
FROM microsoft/dotnet:2.1-aspnetcore-runtime-nanoserver-1809

WORKDIR /album-viewer
COPY --from=builder /album-viewer/src/AlbumViewerNetCore/bin/Debug/netcoreapp2.0/publish/ .
CMD ["dotnet", "AlbumViewerNetCore.dll"]