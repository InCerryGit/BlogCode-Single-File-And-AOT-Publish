# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /source

# install clang
RUN apk add clang libexecinfo binutils musl-dev build-base zlib-static krb5-dev

# copy everything else and build app
COPY . ./
RUN dotnet publish SingleFilePublish-AOT.csproj -c release --self-contained true -r linux-musl-x64  -o /app -p:InvariantGlobalization=true
RUN strip /app/SingleFilePublish-AOT

# final stage/image
FROM alpine:latest
RUN apk add --no-cache openssl ncurses-libs libstdc++
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS="http://*:80"
EXPOSE 80
ENTRYPOINT ["/app/SingleFilePublish-AOT"]