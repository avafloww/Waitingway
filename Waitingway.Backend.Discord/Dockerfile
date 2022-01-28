FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5100

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Waitingway.Backend.Discord/Waitingway.Backend.Discord.csproj", "Waitingway.Backend.Discord/"]
COPY ["Waitingway.Backend.Database/Waitingway.Backend.Database.csproj", "Waitingway.Backend.Database/"]
RUN dotnet restore "Waitingway.Backend.Discord/Waitingway.Backend.Discord.csproj"
COPY . .
WORKDIR "/src/Waitingway.Backend.Discord"
RUN dotnet build "Waitingway.Backend.Discord.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Waitingway.Backend.Discord.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Waitingway.Backend.Discord.dll"]
