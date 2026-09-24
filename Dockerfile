FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["EcommerceApp.csproj", "./"]

RUN dotnet restore "EcommerceApp.csproj"

COPY . .

RUN dotnet publish "EcommerceApp.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

COPY --from=build /app/publish .

EXPOSE 10000

ENTRYPOINT ["sh", "-c", "dotnet EcommerceApp.dll --urls http://0.0.0.0:${PORT:-10000}"]
