FROM node:22-slim AS frontend-build
WORKDIR /src

COPY caseflow.client/package*.json ./caseflow.client/
WORKDIR /src/caseflow.client
RUN npm install --ignore-scripts=false

COPY caseflow.client/ ./
RUN npm run build


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src

COPY . .

RUN mkdir -p /src/CaseFlow.Server/wwwroot
COPY --from=frontend-build /src/caseflow.client/dist/ /src/CaseFlow.Server/wwwroot/

RUN dotnet publish /src/CaseFlow.Server/CaseFlow.Server.csproj -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

ARG BUILD_VERSION=dev
LABEL org.opencontainers.image.version=$BUILD_VERSION

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=backend-build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "CaseFlow.Server.dll"]
