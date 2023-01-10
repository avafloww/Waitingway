FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Waitingway.Backend.Server/Waitingway.Backend.Server.csproj", "Waitingway.Backend.Server/"]
COPY ["Waitingway.Backend.Database/Waitingway.Backend.Database.csproj", "Waitingway.Backend.Database/"]
COPY ["Waitingway.Protocol/Waitingway.Protocol.csproj", "Waitingway.Protocol/"]
RUN dotnet restore "Waitingway.Backend.Server/Waitingway.Backend.Server.csproj"
COPY . .
WORKDIR "/src/Waitingway.Backend.Server"
RUN dotnet build "Waitingway.Backend.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Waitingway.Backend.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Waitingway.Backend.Server.dll"]
