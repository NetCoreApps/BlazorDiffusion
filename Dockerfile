FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build
WORKDIR /app

COPY ./ .
RUN dotnet restore

WORKDIR /app/BlazorDiffusion
RUN dotnet publish -c release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:6.0-focal AS runtime
EXPOSE 80
EXPOSE 443
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "BlazorDiffusion.dll"]
