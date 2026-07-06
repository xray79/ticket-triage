FROM node:20-alpine

WORKDIR /app

COPY apps/agent-console/package*.json ./
RUN npm ci

COPY apps/agent-console/ .

EXPOSE 4200
CMD ["npx", "ng", "serve", "--host", "0.0.0.0", "--disable-host-check"]
