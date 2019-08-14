FROM mcr.microsoft.com/dotnet/core/runtime:2.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.1 AS build
WORKDIR /src
COPY ["SlateyMP.Server.Realm/SlateyMP.Server.Realm.csproj", "SlateyMP.Server.Realm/"]
COPY ["SlateyMP.Framework/SlateyMP.Framework.csproj", "SlateyMP.Framework/"]
RUN dotnet restore "SlateyMP.Server.Realm/SlateyMP.Server.Realm.csproj"
COPY . .
WORKDIR "/src/SlateyMP.Server.Realm"
RUN dotnet build "SlateyMP.Server.Realm.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "SlateyMP.Server.Realm.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SlateyMP.Server.Realm.dll"]
ENV REALM_SERVER_DB="server=host.docker.internal;database=meteordb;uid=root;password=password;SslMode=None"
ENV REALM_SERVER_ADDRESS="0.0.0.0"
ENV REALM_SERVER_PORT="11001"
EXPOSE 11001/udp