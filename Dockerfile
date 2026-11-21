# =========================
# Stage 1: Build
# =========================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG PROJECT_PATH

WORKDIR /src

COPY . .

RUN dotnet publish "${PROJECT_PATH}" -c Release -o /app/publish

# =========================
# Stage 2: Runtime
# =========================

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

ARG APP_DLL=Fundoo.ApiGateway.dll

ENV APP_DLL=${APP_DLL}

WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

CMD ["sh", "-c", "dotnet \"$APP_DLL\""]
