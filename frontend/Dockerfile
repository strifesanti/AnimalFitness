# syntax=docker/dockerfile:1

FROM node:22-bookworm-slim AS deps
WORKDIR /app
RUN corepack enable
COPY frontend/package.json frontend/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

FROM node:22-bookworm-slim AS build
WORKDIR /app
RUN corepack enable
ARG API_ORIGIN=http://api:8080
ENV API_ORIGIN=$API_ORIGIN
COPY --from=deps /app/node_modules ./node_modules
COPY frontend/ ./
RUN pnpm build

FROM node:22-bookworm-slim AS final
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
ARG API_ORIGIN=http://api:8080
ENV API_ORIGIN=$API_ORIGIN
RUN corepack enable
COPY --from=build /app/package.json /app/pnpm-lock.yaml ./
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/.next ./.next
COPY --from=build /app/public ./public
COPY --from=build /app/next.config.ts ./
EXPOSE 3000
CMD ["pnpm", "start"]
