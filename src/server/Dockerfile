FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine as builder

ENV ASPNETCORE_ENVIRONMENT=Production

WORKDIR /glider-view
COPY GliderView.sln .
COPY GliderView.API/GliderView.API.csproj GliderView.API/GliderView.API.csproj
COPY GliderView.Service/GliderView.Service.csproj GliderView.Service/GliderView.Service.csproj
COPY GliderView.Data/GliderView.Data.csproj GliderView.Data/GliderView.Data.csproj
RUN dotnet restore GliderView.API/GliderView.API.csproj

COPY . .
RUN dotnet publish ./GliderView.API/GliderView.API.csproj

# app image
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine as run

WORKDIR /glider-view
COPY --from=builder /glider-view/GliderView.API/bin/Debug/net6.0/publish/ .
CMD ["dotnet", "GliderView.API.dll"]
