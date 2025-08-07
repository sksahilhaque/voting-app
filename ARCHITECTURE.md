# Architecture Documentation - Voting Application

## 🏗️ System Architecture

### High-Level Overview
```
                    ┌─────────────────┐
                    │   Application   │
                    │  Load Balancer  │
                    └─────────┬───────┘
                              │
                    ┌─────────▼───────┐
                    │   ECS Cluster   │
                    │  (my-voting-app) │
                    └─────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
   ┌────▼────┐         ┌─────▼─────┐         ┌────▼────┐
   │ Vote    │         │  Worker   │         │ Result  │
   │ Service │         │  Service  │         │ Service │
   └─────────┘         └───────────┘         └─────────┘
        │                     │                     │
        │              ┌──────▼──────┐              │
        │              │             │              │
        ▼              ▼             ▼              ▼
   ┌─────────┐    ┌─────────┐   ┌─────────┐   ┌─────────┐
   │  Redis  │    │  Redis  │   │PostgreSQL│   │PostgreSQL│
   │ Service │    │ Service │   │ Service │   │ Service │
   └─────────┘    └─────────┘   └─────────┘   └─────────┘
```

## 🔧 Component Details

### 1. Vote Service (Python Flask)
```yaml
Technology: Python 3.9 + Flask
Container: vote-app
Port: 80
CPU: 512
Memory: 1024 MB
Health Check: HTTP GET /
```

**Responsibilities:**
- Serve voting web interface
- Collect user votes (Cats vs Dogs)
- Store votes in Redis queue
- Handle HTTP requests from users

**Key Files:**
- `app.py` - Flask application
- `templates/index.html` - Voting interface
- `Dockerfile` - Container configuration

### 2. Worker Service (C# .NET)
```yaml
Technology: .NET 7.0
Container: worker-app  
Port: N/A (Background service)
CPU: 512
Memory: 1024 MB
Health Check: Process monitoring
```

**Responsibilities:**
- Poll Redis for new votes
- Process vote data
- Create database tables
- Store processed votes in PostgreSQL
- Handle vote deduplication

**Key Files:**
- `Program.cs` - Main worker logic
- `Worker.csproj` - Project configuration
- `Dockerfile` - Multi-stage build

### 3. Result Service (Node.js)
```yaml
Technology: Node.js 18 + Express + Socket.IO
Container: result-app
Port: 80  
CPU: 512
Memory: 1024 MB
Health Check: HTTP GET /health
```

**Responsibilities:**
- Serve results web interface
- Query PostgreSQL for vote counts
- Provide real-time updates via WebSocket
- Calculate vote percentages
- Handle concurrent connections

**Key Files:**
- `server.js` - Express + Socket.IO server
- `views/index.html` - Results interface
- `package.json` - Dependencies

### 4. Redis Service
```yaml
Technology: Redis Alpine
Container: redis-service
Port: 6379
CPU: 256
Memory: 512 MB
```

**Responsibilities:**
- Message queue for votes
- Temporary vote storage
- High-performance caching
- Vote deduplication support

### 5. PostgreSQL Service  
```yaml
Technology: PostgreSQL 15
Container: postgres-service
Port: 5432
CPU: 512
Memory: 1024 MB
```

**Responsibilities:**
- Persistent vote storage
- Vote counting and aggregation
- Data consistency and ACID compliance
- Historical vote data

## 🌐 Network Architecture

### Service Discovery
```yaml
Vote Service: vote.voting.local
Worker Service: worker.voting.local  
Result Service: result.voting.local
Redis Service: redis.voting.local
PostgreSQL Service: postgres.voting.local
```

### Load Balancers
```yaml
Vote ALB: vote-alb-859523202.us-east-1.elb.amazonaws.com
Result ALB: result-alb-859523202.us-east-1.elb.amazonaws.com
```

### Security Groups
```yaml
ECS Security Group: sg-02657aa10f9b02586
- Inbound: HTTP (80) from ALB
- Inbound: PostgreSQL (5432) internal
- Inbound: Redis (6379) internal
- Outbound: All traffic
```

## 📊 Data Flow

### Vote Submission Flow
```
1. User clicks vote → Vote Service (Python)
2. Vote Service → Redis Queue (LPUSH votes)
3. Worker Service → Redis Queue (LPOP votes)  
4. Worker Service → PostgreSQL (INSERT/UPDATE)
5. Result Service → PostgreSQL (SELECT COUNT)
6. Result Service → WebSocket → User Browser
```

### Database Schema
```sql
-- Votes table (created by Worker Service)
CREATE TABLE votes (
    id VARCHAR(255) NOT NULL UNIQUE,  -- Voter ID (cookie-based)
    vote VARCHAR(255) NOT NULL        -- Vote choice ('a' or 'b')
);

-- Indexes
CREATE UNIQUE INDEX idx_votes_id ON votes(id);
CREATE INDEX idx_votes_vote ON votes(vote);
```

### Redis Data Structure
```
Key: "votes"
Type: List (FIFO Queue)
Value: JSON {"vote": "a", "voter_id": "abc123"}
```

## 🔄 Deployment Architecture

### ECS Cluster Configuration
```yaml
Cluster: my-voting-app
Launch Type: Fargate
Platform Version: 1.4.0
Network Mode: awsvpc
```

### Task Definitions
```yaml
vote-app:
  Family: vote-app
  CPU: 512
  Memory: 1024
  Network: awsvpc
  
worker-app:
  Family: worker-app  
  CPU: 512
  Memory: 1024
  Network: awsvpc
  
result-app:
  Family: result-app
  CPU: 512
  Memory: 1024
  Network: awsvpc
```

### Service Configuration
```yaml
Services:
  - vote-app-service (Desired: 1)
  - worker-app-service (Desired: 1)
  - result-app-service (Desired: 1)
  - postgres-service (Desired: 1)
  - redis-service (Desired: 1)

Deployment:
  Type: Rolling Update
  Maximum: 200%
  Minimum: 100%
  Circuit Breaker: Enabled
```

## 🔍 Monitoring & Logging

### CloudWatch Log Groups
```
/ecs/vote-app
/ecs/worker-app
/ecs/result-app  
/ecs/postgres-service
/ecs/redis-service
```

### Health Checks
```yaml
Vote Service: GET / (200 OK)
Result Service: GET /health (200 OK)
Worker Service: Process health monitoring
Database Services: Connection monitoring
```

### Metrics Monitored
- CPU Utilization
- Memory Utilization  
- Task Count
- Service Events
- Application Logs
- Database Connections
- Redis Operations

## 🚀 Scalability Considerations

### Horizontal Scaling
- Vote Service: Can scale to multiple instances behind ALB
- Result Service: Can scale with sticky sessions for WebSocket
- Worker Service: Single instance recommended (vote processing)
- Database Services: Vertical scaling or RDS migration

### Performance Optimizations
- Redis: In-memory caching for high throughput
- PostgreSQL: Indexed queries for fast aggregation
- WebSocket: Real-time updates without polling
- ALB: Load distribution across AZs

## 🔐 Security Architecture

### Network Security
- Private subnets for ECS tasks
- Security groups with minimal required ports
- ALB in public subnets only

### Application Security
- No hardcoded credentials
- Environment variable configuration
- Container isolation
- Least privilege access

### Data Security
- PostgreSQL with authentication
- Redis with network isolation
- Encrypted data in transit (HTTPS)
- Container image scanning

---

**Architecture Version**: 1.0
**Last Updated**: August 7, 2025
**Status**: Production Ready ✅