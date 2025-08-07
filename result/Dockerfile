FROM node:18-slim

ENV DEBIAN_FRONTEND=noninteractive

WORKDIR /usr/local/app

RUN npm install -g nodemon

COPY package*.json ./
RUN npm ci && npm cache clean --force

COPY . .

ENV PORT=80

EXPOSE 80

CMD ["node", "server.js"]