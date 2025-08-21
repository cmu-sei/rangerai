# multi-stage target: dev
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS dev
ARG TARGETARCH

COPY . /app
WORKDIR /app

ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false
RUN dotnet restore -a $TARGETARCH

RUN dotnet publish -c Release -o /app/dist -a $TARGETARCH
COPY wwwroot /app/dist/wwwroot
CMD ["dotnet", "run"]

# multi-stage target: prod
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS prod
ARG commit
ENV COMMIT=$commit

COPY --from=dev /app/dist /app
WORKDIR /app
RUN mkdir -p /app/_data && chmod -R 777 /app/_data

LABEL "product"="RANGER v0"
LABEL "maintainer"="Ranger Team at CMU SEI <info[-@-]sei.cmu.edu>"
LABEL "version"="Browse to /swagger"

EXPOSE 5000
ENV ASPNETCORE_URLS=http://*:5000 \
    ASPNETCORE_ENVIRONMENT=DEVELOPMENT

CMD ["dotnet", "RangerAi.dll"]
