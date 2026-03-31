# ExterView Assessment: Microsoft Teams Meeting Bot - SOLUTION

## Quick Start

### Prerequisites

- [.NET 8/9 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Step 1: Start PostgreSQL

```bash
docker-compose up -d
```

Wait ~10 seconds for PostgreSQL to be ready.

### Step 2: Run the Application

```bash
dotnet restore
dotnet run --project src/ExterView.Api
```

### Step 3: Test the Endpoint

Open [http://localhost:5000](http://localhost:5000) in your browser to see Swagger UI.

**Using Swagger UI:**
1. Navigate to POST `/api/meetings/simulate`
2. Click "Try it out"
3. Use this JSON:
```json
{
  "meetingId": "test-meeting-001",
  "tenantId": "tenant-001"
}
```
4. Click "Execute"

**Using curl:**
```bash
curl -X POST http://localhost:5000/api/meetings/simulate \
  -H "Content-Type: application/json" \
  -d '{"meetingId":"test-meeting-001","tenantId":"tenant-001"}'
```

### Step 4: Verify Results

1. **API Response**: 202 Accepted with processing details
2. **Database**: Query PostgreSQL `transcripts` table
3. **File System**: Check `./transcripts/test-meeting-001/` folder
4. **Logs**: Console shows entire processing pipeline

## Architecture

### Local Implementation

```
REST API (Swagger) → Controller → In-Memory Queue
                                       ↓
                              Background Worker
                                 ↓    ↓    ↓
                            Graph  File  Database
                            Mock   Sys   PostgreSQL
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed diagrams.

## API Endpoints

### POST /api/meetings/simulate
Triggers transcript processing

### GET /api/meetings/{meetingId}/status
Check processing status

## Documentation

- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System design for 10K meetings/day
- **[EXPLANATION.md](EXPLANATION.md)** - Security, Copilot, scalability
