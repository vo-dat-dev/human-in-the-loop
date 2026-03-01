# Docker Deployment Guide

This guide explains how to run the Human-in-the-Loop application using Docker.

## Prerequisites

- Docker Desktop installed
- Docker Compose installed

## Quick Start

1. **Create environment file** (copy from example):

   ```bash
   cp .env.example .env
   ```

2. **Edit `.env` file** with your credentials:
   - Add your GitHub token
   - Add your OpenAI API key

3. **Build and run the containers**:

   ```bash
   docker-compose up --build
   ```

4. **Access the application**:
   - Frontend: http://localhost:3000
   - Agent API: http://localhost:8000

## Docker Services

### Frontend Service

- Built from Next.js application
- Exposed on port 3000
- Multi-stage build for optimized image size

### Agent Service

- Built from .NET 9.0 ASP.NET application
- Exposed on port 8000
- Communicates with frontend via internal network

## Common Commands

**Start services** (detached mode):

```bash
docker-compose up -d
```

**Stop services**:

```bash
docker-compose down
```

**View logs**:

```bash
docker-compose logs -f
```

**Rebuild after code changes**:

```bash
docker-compose up --build
```

**Remove all containers and volumes**:

```bash
docker-compose down -v
```

## Environment Variables

Configure these in your `.env` file:

- `GITHUB_TOKEN` - GitHub personal access token
- `OPENAI_API_KEY` - OpenAI API key for the agent

## Production Deployment

For production deployment, consider:

1. Using proper secrets management (not .env files)
2. Setting up reverse proxy (nginx/traefik)
3. Enabling HTTPS
4. Adjusting resource limits in docker-compose.yml
5. Setting up health checks
6. Implementing proper logging and monitoring

## Troubleshooting

**Port already in use**:
If ports 3000 or 8000 are already in use, modify the port mappings in `docker-compose.yml`:

```yaml
ports:
  - "3001:3000" # Change host port (left side)
```

**Container fails to start**:
Check logs with:

```bash
docker-compose logs [service-name]
```

**Network issues**:
Ensure both containers are on the same network and can communicate.
