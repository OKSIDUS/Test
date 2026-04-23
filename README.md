# ProzorroAnalytics

ETL pipeline + analytics dashboard for Prozorro public procurement data.

## Run with Docker Compose

```bash
cd DataMining
docker-compose up --build -d
```

| Service | URL |
|---|---|
| Frontend dashboard | http://localhost:3000 |
| API (Swagger) | http://localhost:8080/swagger |
| PostgreSQL | `localhost:5432` — db: `prozorro`, user: `postgres`, password: `postgres` |

```bash
# Stop
docker-compose down

# Stop and delete database volume
docker-compose down -v
```

## Run tests

```bash
cd DataMining
dotnet test ProzorroAnalytics.Tests/ProzorroAnalytics.Tests.csproj
```

## Local development (without Docker)

```bash
# Start only the database
cd DataMining
docker-compose up postgres -d

# Run API (http://localhost:5140, Swagger at /swagger)
dotnet run --project ProzorroAnalytics.API/ProzorroAnalytics.API.csproj

# Run frontend (http://localhost:3000, proxies /api to :5140)
cd frontend
npm install
npm run dev
```
