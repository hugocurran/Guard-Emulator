FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY *.sln ./
COPY Guard/Guard.csproj Guard/
RUN dotnet restore
COPY . .
WORKDIR /src/Guard
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
Copy Deploy1.xml .
ENTRYPOINT ["dotnet", "Guard.dll", "Deploy1.xml"]
