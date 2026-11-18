FROM --platform=$BUILDPLATFORM node:24-bookworm-slim AS node
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:11.0-resolute AS builder

COPY --from=node /usr/local/bin/node /usr/local/bin/node
COPY --from=node /usr/local/lib/node_modules/npm /usr/local/lib/node_modules/npm
RUN ln -s ../lib/node_modules/npm/bin/npm-cli.js /usr/local/bin/npm

WORKDIR /app

# restore tools
COPY .config .config
RUN dotnet tool restore

# install F# dependencies
COPY paket.dependencies paket.lock ./
RUN dotnet paket install && dotnet paket restore

# copy projects and build script
COPY src src
COPY package.json package-lock.json fake.sh ./
RUN chmod +x ./fake.sh

# FAKE restores frontend tools, verifies Tailwind contracts, and publishes all assets.
RUN ./fake.sh PublishDocs

FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseled-extra AS runner

WORKDIR /app

# copy compiled code from build image
COPY --from=builder /app/src/Docs/out .

ENTRYPOINT ["dotnet", "Docs.dll"]
