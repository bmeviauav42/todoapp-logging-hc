#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Todos/Todos.Api/Todos.Api.csproj", "Todos/Todos.Api/"]
COPY ["Todos/Todos.Domain/Todos.Domain.csproj", "Todos/Todos.Domain/"]
COPY ["Todos/Todos.Dal/Todos.Dal.csproj", "Todos/Todos.Dal/"]
RUN dotnet restore "Todos/Todos.Api/Todos.Api.csproj"
COPY . .
WORKDIR "/src/Todos/Todos.Api"
RUN dotnet build "Todos.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Todos.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Todos.Api.dll"]