FROM --platform=$BUILDPLATFORM node:24-bookworm-slim AS node
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:11.0-resolute AS builder

COPY --from=node /usr/local/bin/node /usr/local/bin/node
COPY --from=node /usr/local/lib/node_modules/npm /usr/local/lib/node_modules/npm
RUN ln -s ../lib/node_modules/npm/bin/npm-cli.js /usr/local/bin/npm

WORKDIR /app

# Resolve Tailwind and Typography from the shared build root for both consumers.
ENV PATH="/app/node_modules/.bin:${PATH}"
COPY src/Example/Example.Server/package.json src/Example/Example.Server/package-lock.json ./
RUN npm ci

# restore tools
COPY .config .config
RUN dotnet tool restore

# install F# dependencies
COPY paket.dependencies paket.lock ./
RUN dotnet paket install && dotnet paket restore

# copy projects and build script
COPY src src
COPY fake.sh .
RUN chmod +x ./fake.sh

# verify the external-library Tailwind contracts, then publish the docs app
RUN bash src/FSharp.ViewEngine.Components/verify-tailwind.sh
RUN bash src/FSharp.ViewEngine.Components/Documentation/verify-tailwind.sh
RUN ./fake.sh PublishDocs

FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseled-extra AS runner

WORKDIR /app

# copy compiled code from build image
COPY --from=builder /app/src/Docs/out .

ENTRYPOINT ["dotnet", "Docs.dll"]
