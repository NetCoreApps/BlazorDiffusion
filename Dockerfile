FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build
WORKDIR /app
COPY ./ .

RUN --mount=type=secret,id=github_actor \
    --mount=type=secret,id=github_token \
    export github_actor=$(cat /run/secrets/github_actor) && \
    export github_token=$(cat /run/secrets/github_token) && \
    dotnet nuget add source "https://nuget.pkg.github.com/ServiceStack/index.json" --username $github_actor --password $github_token --store-password-in-clear-text --name github && \
    dotnet restore

WORKDIR /app/BlazorDiffusion
RUN dotnet publish -c release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:6.0-focal AS runtime
EXPOSE 80
EXPOSE 443
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "BlazorDiffusion.dll"]
