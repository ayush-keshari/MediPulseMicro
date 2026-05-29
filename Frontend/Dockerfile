# Multi-stage: build Angular with Node, serve static files with nginx.
# Build context = Frontend/

# ---------- Build stage ----------
FROM node:20-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npx ng build --configuration production

# ---------- Runtime stage ----------
FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
# Angular 16+ outputs to dist/<project>/browser
COPY --from=build /src/dist/Frontend/browser /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
