FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

COPY Microblog.Api.csproj .
RUN dotnet restore "Microblog.Api.csproj"

COPY . .
RUN dotnet publish "Microblog.Api.csproj" -c Release -o /app/publish

WORKDIR /app/publish
EXPOSE 8080
ENTRYPOINT ["dotnet", "Microblog.Api.dll"]
