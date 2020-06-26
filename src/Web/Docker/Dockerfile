FROM nginx:1.19.0-alpine AS base
WORKDIR /app
EXPOSE 80
COPY ["Web/nginx.conf", "/etc/nginx/nginx.conf"]

FROM node:14.4.0-alpine as build
WORKDIR /src
COPY ["Web/Client", "Client/"]
WORKDIR /src/Client
RUN yarn
RUN yarn build

FROM base AS final
WORKDIR /app
COPY --from=build /src/Client/build /app/wwwroot
