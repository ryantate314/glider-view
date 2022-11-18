FROM microsoft/dotnet:2.1-sdk-nanoserver-1809 AS builder

WORKDIR /glider-view
COPY GliderView.sln .
COPY GliderView.API/GliderView.API.csproj GliderView.API/GliderView.API.csproj
COPY GliderView.Service/GliderView.Service.csproj GliderView.Service/GliderView.Service.csproj
COPY GliderView.Data/GliderView.Data.csproj GliderView.Data/GliderView.Data.csproj
RUN dotnet restore GliderView.API/GliderView.API.csproj

COPY . .
RUN dotnet publish ./GliderView.API/GliderView.API.csproj

# app image
FROM microsoft/dotnet:2.1-aspnetcore-runtime-nanoserver-1809

WORKDIR /glider-view
COPY --from=builder /glider-view/GliderView.API/bin/Debug/netcoreapp2.0/publish/ .
CMD ["dotnet", "GliderView.API.dll"]