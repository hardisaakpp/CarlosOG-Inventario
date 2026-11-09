FROM node:24-alpine AS build
WORKDIR /app

COPY package*.json ./
# npm install (y no npm ci): el lockfile no incluye dos paquetes opcionales que npm 11 reciente exige.
RUN npm install --no-audit --no-fund

COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
