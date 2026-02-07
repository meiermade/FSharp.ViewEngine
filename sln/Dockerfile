FROM mcr.microsoft.com/dotnet/sdk:10.0-noble-amd64 AS build

WORKDIR /app

# Install tailwindcss
RUN curl -sLO https://github.com/tailwindlabs/tailwindcss/releases/download/v4.1.18/tailwindcss-linux-x64 \
    && chmod +x tailwindcss-linux-x64 \
    && mv tailwindcss-linux-x64 /usr/local/bin/tailwindcss


# Restore paket dependencies
COPY .config/ .config/
RUN dotnet tool restore

# Install F# dependencies
COPY paket.dependencies paket.lock ./
RUN dotnet paket install && dotnet paket restore

COPY fake.sh ./
COPY src/ src/
RUN chmod +x fake.sh && ./fake.sh PublishDocs

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled

WORKDIR /app

COPY --from=build /app/src/Docs/out .

ENTRYPOINT ["dotnet", "Docs.dll"]
