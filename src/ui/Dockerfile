FROM node:20-alpine AS build
WORKDIR /usr/src/app
COPY package.json package-lock.json ./
RUN npm install
COPY . .
RUN npm run build

FROM nginx:1.23-alpine AS run
COPY nginx.conf /etc/nginx/nginx.conf
COPY --from=build /usr/src/app/dist/glider-view /usr/share/nginx/html
