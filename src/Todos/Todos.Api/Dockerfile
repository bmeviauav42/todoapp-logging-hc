FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["Todos/Todos.Api/Todos.Api.csproj", "Todos/Todos.Api/"]
RUN dotnet restore "Todos/Todos.Api/Todos.Api.csproj"
COPY . .
WORKDIR "/src/Todos/Todos.Api"
RUN dotnet build "Todos.Api.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Todos.Api.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Todos.Api.dll"]