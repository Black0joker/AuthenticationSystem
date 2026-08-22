.PHONY: build test run docker-up docker-down docker-build migrate seed

# Build the solution
build:
	dotnet build IdentityAuthServer.slnx

# Run all tests
test:
	dotnet test IdentityAuthServer.slnx --verbosity minimal

# Run the API locally
run:
	dotnet run --project src/IdentityAuth.Api

# Build Docker images
docker-build:
	docker compose build

# Start all services with Docker Compose
docker-up:
	docker compose up -d

# Stop all services
docker-down:
	docker compose down

# Stop and remove volumes
docker-clean:
	docker compose down -v

# View logs
docker-logs:
	docker compose logs -f api

# Run database migrations
migrate:
	dotnet ef database update --project src/IdentityAuth.Infrastructure --startup-project src/IdentityAuth.Api

# Create a new migration (usage: make migration NAME=MigrationName)
migration:
	dotnet ef migrations add $(NAME) --project src/IdentityAuth.Infrastructure --startup-project src/IdentityAuth.Api
