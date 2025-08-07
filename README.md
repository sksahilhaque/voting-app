# Cats vs Dogs Voting Application on AWS ECS

A real-time voting application deployed on AWS ECS using Docker containers, demonstrating microservices architecture with live results.

## 🏗️ Architecture Overview

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Vote App  │    │ Worker App  │    │ Result App  │
│  (Python)   │    │    (C#)     │    │ (Node.js)   │
└─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │
       ▼                   ▼                   ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│    Redis    │◄───┤   Worker    ├───►│ PostgreSQL  │
│   (Cache)   │    │ (Processor) │    │ (Database)  │
└─────────────┘    └─────────────┘    └─────────────┘
```

## 📁 Project Structure

```
docker-pro1/
├── vote/                 # Python Flask voting interface
│   ├── app.py
│   ├── Dockerfile
│   └── requirements.txt
├── worker/               # C# .NET worker service
│   ├── Program.cs
│   ├── Worker.csproj
│   └── Dockerfile
├── result/               # Node.js result display
│   ├── server.js
│   ├── package.json
│   ├── Dockerfile
│   └── views/
└── docker-compose.yml    # Local development setup
```

## 🚀 Components

### 1. Vote Application (Python Flask)
- **Purpose**: Collects user votes for Cats vs Dogs
- **Technology**: Python Flask
- **Storage**: Stores votes in Redis queue
- **Port**: 80

### 2. Worker Application (C# .NET)
- **Purpose**: Processes votes from Redis and stores in PostgreSQL
- **Technology**: C# .NET 7.0
- **Function**: Background service that continuously polls Redis
- **Database**: Creates and manages `votes` table

### 3. Result Application (Node.js)
- **Purpose**: Displays real-time voting results
- **Technology**: Node.js with Socket.IO
- **Features**: Live updates via WebSocket
- **Port**: 80

### 4. Data Layer
- **Redis**: Message queue for votes
- **PostgreSQL**: Persistent storage for vote data

## 🔧 AWS ECS Deployment

### Prerequisites
- AWS CLI configured
- Docker installed
- ECR repositories created for each service

### Environment Variables

#### Result App (Node.js)
```bash
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_HOST=postgres.voting.local
POSTGRES_PORT=5432
POSTGRES_DB=postgres
```

#### Worker App (C#)
```bash
POSTGRES_CONN_STRING=Host=postgres.voting.local;Username=postgres;Password=postgres;Database=postgres
REDIS_HOST=redis.voting.local
```

### Task Definitions
- **result-app**: 512 CPU, 1024 Memory
- **worker-app**: 512 CPU, 1024 Memory
- **vote-app**: 512 CPU, 1024 Memory
- **postgres-service**: Database service
- **redis-service**: Cache service

## 🐛 Troubleshooting Guide

### Common Issues Fixed

#### 1. Missing Environment Variables
**Error**: `Missing required environment variables: POSTGRES_DB`
**Solution**: Added `POSTGRES_DB=postgres` to result-app task definition

#### 2. Database Connection Issues
**Error**: `TypeError: Cannot read properties of undefined (reading 'split')`
**Solution**: Fixed environment variables and database connection string format

#### 3. Table Not Found
**Error**: `relation "votes" does not exist`
**Solution**: 
- Fixed worker database connection string
- Worker now creates table automatically
- Added table creation to result app as backup

#### 4. Wrong Database Name
**Error**: Worker connecting to wrong database
**Solution**: Changed `Database=db` to `Database=postgres` in connection string

## 📋 Deployment Steps

### 1. Build and Push Images
```bash
# Build each service
docker build -t vote-app ./vote
docker build -t worker-app ./worker  
docker build -t result-app ./result

# Tag for ECR
docker tag vote-app:latest {account}.dkr.ecr.us-east-1.amazonaws.com/vote-app:latest
docker tag worker-app:latest {account}.dkr.ecr.us-east-1.amazonaws.com/worker-app:latest
docker tag result-app:latest {account}.dkr.ecr.us-east-1.amazonaws.com/result-app:latest

# Push to ECR
docker push {account}.dkr.ecr.us-east-1.amazonaws.com/vote-app:latest
docker push {account}.dkr.ecr.us-east-1.amazonaws.com/worker-app:latest
docker push {account}.dkr.ecr.us-east-1.amazonaws.com/result-app:latest
```

### 2. Create ECS Services
```bash
# Create cluster
aws ecs create-cluster --cluster-name my-voting-app

# Register task definitions and create services
aws ecs create-service --cluster my-voting-app --service-name vote-app-service --task-definition vote-app:latest
aws ecs create-service --cluster my-voting-app --service-name worker-app-service --task-definition worker-app:latest
aws ecs create-service --cluster my-voting-app --service-name result-app-service --task-definition result-app:latest
```

### 3. Update Services
```bash
# Force new deployment after image updates
aws ecs update-service --cluster my-voting-app --service {service-name} --force-new-deployment
```

## 🔍 Monitoring

### CloudWatch Logs
- `/ecs/vote-app`
- `/ecs/worker-app` 
- `/ecs/result-app`
- `/ecs/postgres-service`
- `/ecs/redis-service`

### Health Checks
- Result app: `GET /health` returns `200 OK`
- Vote app: Application load balancer health checks
- Worker app: Background service (no HTTP endpoint)

## 🌐 Access Points

- **Vote Interface**: `http://vote-alb-{id}.us-east-1.elb.amazonaws.com`
- **Result Interface**: `http://result-alb-{id}.us-east-1.elb.amazonaws.com`

## 📊 Features

- ✅ Real-time vote counting
- ✅ Live result updates via WebSocket
- ✅ Persistent vote storage
- ✅ Scalable microservices architecture
- ✅ Health monitoring
- ✅ Auto-recovery and retry logic

## 🔄 Data Flow

1. **User votes** → Vote App (Python)
2. **Vote stored** → Redis Queue
3. **Worker processes** → Reads from Redis
4. **Vote persisted** → PostgreSQL Database
5. **Result updates** → Real-time via Socket.IO
6. **User sees results** → Result App (Node.js)

## 🛠️ Local Development

```bash
# Run with Docker Compose
docker-compose up --build

# Access applications
# Vote: http://localhost:5005
# Result: http://localhost:5001
```

## 📝 Notes

- All services use Fargate launch type
- Services are deployed across multiple AZs for high availability
- Environment variables are managed through ECS task definitions
- Images are stored in Amazon ECR
- Load balancers provide external access to vote and result services

---

**Status**: ✅ Successfully deployed and operational
**Last Updated**: August 7, 2025