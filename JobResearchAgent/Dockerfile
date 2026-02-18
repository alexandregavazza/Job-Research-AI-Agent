FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY JobResearchAgent/JobResearchAgent.csproj JobResearchAgent/
RUN dotnet restore JobResearchAgent/JobResearchAgent.csproj

COPY . .
RUN dotnet publish JobResearchAgent/JobResearchAgent.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/playwright/dotnet:v1.58.0-jammy
WORKDIR /app
ENV PLAYWRIGHT_BROWSERS_PATH=0

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "JobResearchAgent.dll"]
