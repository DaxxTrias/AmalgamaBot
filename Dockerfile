﻿FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /src

RUN apt-get update -y && apt-get install -y python3 python3-pip
RUN python3 -m pip install EdgeGPT --upgrade

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["DougBot.csproj", "."]
RUN dotnet restore "DougBot.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "DougBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DougBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /src
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DougBot.dll"]