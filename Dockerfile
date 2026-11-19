# Stage 1: Restore dependencies and publish the .NET 8 Blazor app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["TimetableMakeup/TimetableMakeup.csproj", "TimetableMakeup/"]
RUN dotnet restore "TimetableMakeup/TimetableMakeup.csproj"

COPY . .
WORKDIR "/src/TimetableMakeup"
RUN dotnet publish "TimetableMakeup.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Run the published app on the ASP.NET Core 8 runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["sh", "-c", "dotnet TimetableMakeup.dll --urls http://0.0.0.0:${PORT:-8080}"]
