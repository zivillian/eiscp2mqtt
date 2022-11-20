FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

COPY src/eiscp ./src/eiscp
COPY src/eiscp2mqtt ./src/eiscp2mqtt
COPY src/eiscp-command-generator ./src/eiscp-command-generator
COPY lib ./lib

ARG TARGETARCH
RUN if [ "$TARGETARCH" = "amd64" ]; then \
    RID=linux-musl-x64 ; \
    elif [ "$TARGETARCH" = "arm64" ]; then \
    RID=linux-musl-arm64 ; \
    elif [ "$TARGETARCH" = "arm" ]; then \
    RID=linux-musl-arm ; \
    fi \
    && dotnet publish -c Release -o out -r $RID --sc -nowarn:IL2026,IL2104 src/eiscp2mqtt/eiscp2mqtt.csproj

FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine
WORKDIR /app
COPY --from=build-env /app/out .

ENV DEBUG=false
ENV HOST=
ENV MQTTHOST=
ENV MQTTUSERNAME=
ENV MQTTPASSWORD=
ENV MQTTPREFIX=eiscp

ENTRYPOINT ["/app/eiscp2mqtt"]