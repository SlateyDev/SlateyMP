FROM mcr.microsoft.com/dotnet/core/runtime:2.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.1 AS build
WORKDIR /src
COPY ["SlateyMP.Server.Login/SlateyMP.Server.Login.csproj", "SlateyMP.Server.Login/"]
COPY ["SlateyMP.Framework/SlateyMP.Framework.csproj", "SlateyMP.Framework/"]
RUN dotnet restore "SlateyMP.Server.Login/SlateyMP.Server.Login.csproj"
COPY . .
WORKDIR "/src/SlateyMP.Server.Login"
RUN dotnet build "SlateyMP.Server.Login.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "SlateyMP.Server.Login.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SlateyMP.Server.Login.dll"]
ENV LOGIN_SERVER_DB="server=host.docker.internal;database=meteordb;uid=root;password=password;SslMode=None"
ENV LOGIN_SERVER_ADDRESS="0.0.0.0"
ENV LOGIN_SERVER_PORT="11000"
EXPOSE 11000/udp
