# React Dockerfile
FROM node:18-alpine

WORKDIR /app

# Copy package.json and install dependencies
COPY package.json .
RUN npm install

# Copy the entire project
COPY . .

# Build the application
RUN npm run build

# Set environment variables for HTTPS and host
ENV HTTPS=true
ENV VITE_HOST=0.0.0.0
ENV VITE_PORT=3001

# Expose the application port
EXPOSE 3001

# Use Vite from node_modules for the preview command
CMD [ "npm", "run", "preview", "--", "--host" ]
