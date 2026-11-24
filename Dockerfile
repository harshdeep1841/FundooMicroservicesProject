FROM mcr.microsoft.com/dotnet/aspnet:8.0

ARG APP_DLL=Fundoo.Notes.Api.dll
ENV APP_DLL=${APP_DLL}

WORKDIR /app

EXPOSE 8080

COPY ./artifacts/notes/ .

ENTRYPOINT ["sh", "-c", "dotnet \"$APP_DLL\""]
