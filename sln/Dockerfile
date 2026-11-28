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

FROM builder AS example-builder
RUN ./fake.sh PublishExample

FROM builder AS docs-builder
RUN ./fake.sh PublishDocs

FROM builder AS proxy-builder
RUN dotnet publish src/MockCloudflareProxy/MockCloudflareProxy.fsproj --output /proxy

FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseled-extra AS mock-cloudflare-proxy
WORKDIR /app
COPY --from=proxy-builder /proxy .
ENTRYPOINT ["dotnet", "MockCloudflareProxy.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseled-extra AS example-runtime
WORKDIR /app

FROM example-runtime AS example-application
COPY --from=example-builder /app/src/Example/Example.Application/out .
ENTRYPOINT ["dotnet", "Example.Application.dll"]

FROM example-runtime AS example-specification
COPY --from=example-builder /app/src/Example/Example.Specification/out .
ENTRYPOINT ["dotnet", "Example.Specification.dll"]

FROM example-runtime AS example-documentation
COPY --from=example-builder /app/src/Example/Example.Documentation/out .
ENTRYPOINT ["dotnet", "Example.Documentation.dll"]

FROM example-runtime AS example-presentation
COPY --from=example-builder /app/src/Example/Example.Presentation/out .
ENTRYPOINT ["dotnet", "Example.Presentation.dll"]

# Production promotion currently pins one example image for all four containers.
FROM example-runtime AS example
COPY --from=example-builder /app/src/Example/Example.Application/out ./server
COPY --from=example-builder /app/src/Example/Example.Specification/out ./spec
COPY --from=example-builder /app/src/Example/Example.Documentation/out ./docs
COPY --from=example-builder /app/src/Example/Example.Presentation/out ./presentation
ENTRYPOINT ["dotnet"]
CMD ["server/Example.Application.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseled-extra AS docs
WORKDIR /app
COPY --from=docs-builder /app/src/Docs/out .
ENTRYPOINT ["dotnet", "Docs.dll"]
