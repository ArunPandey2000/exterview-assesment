# System Architecture

## Table of Contents

1. [Local Implementation](#local-implementation)
2. [Production Cloud Design](#production-cloud-design)
3. [Scalability Strategy](#scalability-strategy)
4. [Processing Pipeline](#processing-pipeline)
5. [Storage Architecture](#storage-architecture)

---

## Local Implementation

### Component Diagram

```mermaid
graph TB
    subgraph "Client"
        USER[User/Postman]
        SWAGGER[Swagger UI]
    end

    subgraph "API Layer"
        CTRL[MeetingSimulationController]
        API[ASP.NET Core Web API]
    end

    subgraph "Queue Layer"
        QUEUE[In-Memory Queue<br/>System.Threading.Channels]
    end

    subgraph "Processing Layer"
        WORKER[TranscriptProcessorWorker<br/>BackgroundService]
        RETRY[Retry Policy<br/>Polly - Exponential Backoff]
        IDEM[Idempotency Service]
    end

    subgraph "Services"
        MOCK[MockGraphService<br/>Returns Sample Transcript]
        STORAGE[LocalFileStorageService<br/>Saves to ./transcripts/]
        REPO[TranscriptRepository<br/>EF Core]
    end

    subgraph "Data"
        PG[(PostgreSQL<br/>Docker Container)]
        FS[File System<br/>./transcripts/]
    end

    USER --> SWAGGER
    SWAGGER --> API
    API --> CTRL
    CTRL --> QUEUE
    QUEUE --> WORKER
    WORKER --> IDEM
    WORKER --> RETRY
    RETRY --> MOCK
    WORKER --> STORAGE
    WORKER --> REPO
    STORAGE --> FS
    REPO --> PG
```

### Data Flow

```mermaid
sequenceDiagram
    participant User
    participant API
    participant Queue
    participant Worker
    participant Services
    participant Storage

    User->>API: POST /api/meetings/simulate
    API->>Queue: Enqueue TranscriptProcessingMessage
    API-->>User: 202 Accepted

    loop Background Processing
        Worker->>Queue: Dequeue message
        Worker->>Services: Check idempotency
        alt Not Processed
            Worker->>Services: Fetch transcript (with retry)
            Services-->>Worker: Mock transcript content
            Worker->>Storage: Save to file system
            Worker->>Storage: Save metadata to DB
            Worker->>Services: Mark as processed
        else Already Processed
            Worker->>Worker: Skip (idempotency)
        end
    end
```

---

## Production Cloud Design

### Azure Architecture for 10,000 Meetings/Day

```mermaid
graph TB
    subgraph "Microsoft Teams"
        TEAMS[Teams Meeting]
        BOT_SVC[Azure Bot Service<br/>Teams Channel]
    end

    subgraph "Ingress"
        APIM[API Management<br/>Rate Limiting<br/>Authentication]
        LB[Load Balancer]
    end

    subgraph "Compute - Auto-Scale"
        APP1[App Service P2v3<br/>Instance 1]
        APP2[App Service P2v3<br/>Instance 2]
        APP3[App Service P2v3<br/>Instance N]
    end

    subgraph "Message Queue"
        SB[Azure Service Bus<br/>Standard Tier<br/>80GB Storage<br/>1M ops/day]
    end

    subgraph "Processing - Auto-Scale"
        WORK1[Worker Instance 1]
        WORK2[Worker Instance 2]
        WORK3[Worker Instance N]
    end

    subgraph "External APIs"
        GRAPH[Microsoft Graph API<br/>Throttle Limit:<br/>10K req/10min]
    end

    subgraph "Storage"
        SP[SharePoint Online<br/>Document Library<br/>/Interview-Transcripts/]
        SQLDB[(Azure SQL Database<br/>Standard S2<br/>50 DTU)]
        BLOB[(Azure Blob Storage<br/>Hot Tier)]
    end

    subgraph "Security & Monitoring"
        KV[Key Vault<br/>Secrets Management]
        INSIGHTS[Application Insights<br/>Distributed Tracing]
        ALERTS[Azure Monitor<br/>Alerts & Dashboards]
    end

    TEAMS -->|Meeting Events| BOT_SVC
    BOT_SVC --> APIM
    APIM --> LB
    LB --> APP1 & APP2 & APP3

    APP1 & APP2 & APP3 -->|Queue Message| SB
    SB -->|Dequeue| WORK1 & WORK2 & WORK3

    WORK1 & WORK2 & WORK3 -->|Fetch Transcript| GRAPH
    WORK1 & WORK2 & WORK3 -->|Upload| SP
    WORK1 & WORK2 & WORK3 -->|Metadata| SQLDB
    WORK1 & WORK2 & WORK3 -.->|Temp Storage| BLOB

    APP1 & APP2 & APP3 -.->|Get Secrets| KV
    WORK1 & WORK2 & WORK3 -.->|Telemetry| INSIGHTS
    INSIGHTS --> ALERTS
```
---

## Scalability Strategy

### Capacity Planning for 10,000 Meetings/Day

**Throughput Analysis:**
- **Daily**: 10,000 meetings
- **Peak hour**: ~600 meetings (8-hour workday, 20% peak factor)
- **Per minute (peak)**: ~10 meetings
- **Processing time**: 5-15 minutes per transcript (including delay)

### Auto-Scaling Configuration

```mermaid
graph LR
    subgraph "Scaling Triggers"
        CPU[CPU > 70%]
        QUEUE[Queue Depth > 100]
        MEMORY[Memory > 80%]
    end

    subgraph "Actions"
        SCALE_OUT[Scale Out<br/>Add Instance]
        SCALE_IN[Scale In<br/>Remove Instance]
    end

    subgraph "Limits"
        MIN[Min: 3 instances]
        MAX[Max: 10 instances]
    end

    CPU --> SCALE_OUT
    QUEUE --> SCALE_OUT
    MEMORY --> SCALE_OUT
    SCALE_OUT --> MAX
    MIN --> SCALE_IN
```

### Resource Sizing

| Component | Size | Justification |
|-----------|------|---------------|
| **App Service** | P2v3 (2 cores, 8GB) | Handles 200 req/hour per instance |
| **Service Bus** | Standard Tier | 80GB storage, 1M operations/day |
| **SQL Database** | Standard S2 (50 DTU) | ~1000 transactions/min |
| **Workers** | 5 instances | 120 meetings/hour each = 600/hour total |

### Retry Strategy

```mermaid
graph TB
    START[API Call] --> ATTEMPT{Attempt}
    ATTEMPT -->|1| DELAY1[Wait 1s]
    DELAY1 --> RETRY1{Success?}
    RETRY1 -->|No| ATTEMPT2[Attempt 2]
    ATTEMPT2 --> DELAY2[Wait 2s]
    DELAY2 --> RETRY2{Success?}
    RETRY2 -->|No| ATTEMPT3[Attempt 3]
    ATTEMPT3 --> DELAY3[Wait 4s]
    DELAY3 --> RETRY3{Success?}
    RETRY3 -->|No| DLQ[Dead Letter Queue]
    RETRY1 -->|Yes| SUCCESS[Success]
    RETRY2 -->|Yes| SUCCESS
    RETRY3 -->|Yes| SUCCESS
    DLQ --> ALERT[Send Alert]
```

**Configuration:**
- **Max Retries**: 3
- **Backoff**: Exponential (1s, 2s, 4s)
- **Timeout**: 30 seconds per attempt
- **Circuit Breaker**: Opens after 5 consecutive failures

---

## Processing Pipeline

### Transcript Processing Timeline

```mermaid
gantt
    title Transcript Processing Timeline (Per Meeting)
    dateFormat mm:ss
    axisFormat %M:%S

    section Meeting
    Meeting Ends           :milestone, 00:00, 0s

    section Queue
    Enqueue Message        :00:00, 00:02

    section Processing
    Dequeue & Check Idem   :00:02, 00:05

    section Graph API
    Wait for Transcript    :00:05, 05:00
    Poll Attempt 1         :05:00, 05:05
    Poll Attempt 2         :06:00, 06:05
    Transcript Ready       :milestone, 07:00, 0s

    section Storage
    Save to File System    :07:00, 07:10
    Save to Database       :07:10, 07:15
    Mark Idempotent        :07:15, 07:18

    section Complete
    Processing Complete    :milestone, 07:18, 0s
```

### Error Handling Flow

```mermaid
graph TD
    START[Start Processing] --> CHECK_IDEM{Idempotency<br/>Check}
    CHECK_IDEM -->|Processed| SKIP[Skip Processing]
    CHECK_IDEM -->|New| PROCESS[Process Transcript]

    PROCESS --> FETCH{Fetch<br/>Transcript}
    FETCH -->|404| RETRY{Retry<br/>Count < 3?}
    RETRY -->|Yes| WAIT[Exponential<br/>Backoff]
    WAIT --> FETCH
    RETRY -->|No| FAIL[Mark Failed]

    FETCH -->|200| SAVE{Save<br/>Transcript}
    SAVE -->|Error| LOG_ERROR[Log Error]
    LOG_ERROR --> FAIL

    SAVE -->|Success| UPDATE_DB[Update<br/>Database]
    UPDATE_DB --> MARK_IDEM[Mark<br/>Idempotent]
    MARK_IDEM --> SUCCESS[Success]

    FAIL --> ALERT[Send Alert]
    SKIP --> END[End]
    SUCCESS --> END
    ALERT --> END
```

---

## Storage Architecture

### Database Schema

```mermaid
erDiagram
    TRANSCRIPTS {
        uuid id PK
        string tenant_id
        string meeting_id UK
        string transcript_id UK
        string file_path
        string status
        timestamp processed_at
        int retry_count
        timestamp created_at
        timestamp updated_at
    }

    IDEMPOTENCY_RECORDS {
        uuid id PK
        string idempotency_key UK
        string tenant_id UK
        string request_hash
        text response_body
        int status_code
        string operation_type
        timestamp created_at
        timestamp expires_at
    }

    AUDIT_EVENTS {
        uuid id PK
        string tenant_id
        timestamp timestamp
        string event_type
        string actor_id
        string resource_id
        string action
        string result
        jsonb metadata_json
    }

    TRANSCRIPTS ||--o{ AUDIT_EVENTS : "generates"
    IDEMPOTENCY_RECORDS ||--o{ AUDIT_EVENTS : "tracks"
```

### File Storage Structure

```
SharePoint: /Interview-Transcripts/
├── {meeting-id-1}/
│   └── transcript_20260401120000.txt
├── {meeting-id-2}/
│   └── transcript_20260401120100.txt
└── {meeting-id-n}/
    └── transcript_YYYYMMDDHHMMSS.txt
```

**Local equivalent:**
```
./transcripts/
├── test-meeting-001/
│   └── transcript_20260401120000.txt
└── test-meeting-002/
    └── transcript_20260401120100.txt
```

---

## Monitoring & Observability

### Key Metrics

| Metric | Threshold | Alert |
|--------|-----------|-------|
| **Processing Success Rate** | < 95% | Critical |
| **Average Processing Time** | > 10 min | Warning |
| **Queue Depth** | > 100 messages | Warning |
| **Database Connection Pool** | > 80% | Warning |
| **API Response Time** | > 2s | Warning |
| **Failed Retries** | > 5/min | Critical |

### Distributed Tracing

```mermaid
graph LR
    REQUEST[HTTP Request] -->|Correlation ID| API[API Layer]
    API -->|Correlation ID| QUEUE[Queue]
    QUEUE -->|Correlation ID| WORKER[Worker]
    WORKER -->|Correlation ID| GRAPH[Graph API]
    WORKER -->|Correlation ID| STORAGE[Storage]

    API -.->|Telemetry| INSIGHTS[Application<br/>Insights]
    WORKER -.->|Telemetry| INSIGHTS
    INSIGHTS --> DASHBOARD[Dashboard]
```

---

## Deployment Strategy

### Infrastructure as Code (Bicep)

```bicep
// Example resource deployment
resource botService 'Microsoft.BotService/botServices@2022-09-15' = {
  name: 'exterview-bot'
  location: 'global'
  sku: {
    name: 'S1'
  }
  properties: {
    displayName: 'ExterView Assessment Bot'
    endpoint: 'https://exterview-api.azurewebsites.net/api/messages'
    msaAppId: botAppId
  }
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: 'exterview-sb'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}

resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: 'exterview-api'
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      numberOfWorkers: 3
      minTlsVersion: '1.2'
      alwaysOn: true
    }
  }
}
```

### CI/CD Pipeline

```mermaid
graph LR
    COMMIT[Git Commit] --> BUILD[Build]
    BUILD --> TEST[Unit Tests]
    TEST --> DOCKER[Build Container]
    DOCKER --> STAGING[Deploy Staging]
    STAGING --> SMOKE[Smoke Tests]
    SMOKE --> APPROVE{Manual<br/>Approval}
    APPROVE -->|Yes| PROD[Deploy Production]
    APPROVE -->|No| ROLLBACK[Rollback]
```

---

## Migration Path: Local to Production

### Phase 1: Infrastructure Setup
1. Provision Azure resources (Bicep/Terraform)
2. Configure networking and security
3. Set up Key Vault with secrets
4. Deploy databases with migrations

### Phase 2: Application Deployment
1. Build and push Docker containers
2. Deploy to App Service
3. Configure auto-scaling rules
4. Set up Application Insights

### Phase 3: Bot Registration
1. Register bot with Azure Bot Service
2. Configure Teams channel
3. Test bot in Teams environment
4. Enable production endpoint

### Phase 4: Validation
1. Run integration tests
2. Load test with 600 meetings/hour
3. Verify monitoring and alerts
4. Document runbooks

---

## Cost Estimation (Azure)

| Service | SKU | Monthly Cost (USD) |
|---------|-----|-------------------|
| App Service (P2v3 x3) | P2v3 | $510 |
| Service Bus | Standard | $10 |
| Azure SQL Database | S2 | $150 |
| SharePoint Online | Included in M365 | - |
| Application Insights | Pay-as-you-go | $50 |
| **Total** | | **~$720/month** |

---

This architecture demonstrates understanding of:
- ✅ Distributed systems design
- ✅ Async processing patterns
- ✅ Scalability and auto-scaling
- ✅ Reliability and retry logic
- ✅ Monitoring and observability
- ✅ Cloud-native architecture
