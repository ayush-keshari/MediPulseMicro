# Contributing to MediPulseMicro

Thank you for considering contributing to MediPulseMicro! Please read this guide to get started.

## Setup Development Environment

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20.x](https://nodejs.org/) (for frontend)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for running services locally)
- [Git](https://git-scm.com/)

### Local Setup
1. Clone the repository:
   ```bash
   git clone https://github.com/ayush-keshari/MediPulseMicro.git
   cd MediPulseMicro
   ```

2. Set up environment variables (copy the example file):
   ```bash
   cp .env.example .env
   # Edit .env to set required values (e.g., SA_PASSWORD for SQL Server)
   ```

3. Restore .NET dependencies:
   ```bash
   dotnet restore
   ```

4. Install frontend dependencies:
   ```bash
   cd Frontend
   npm ci
   cd ..
   ```

## Running Tests

### Backend (Unit Tests)
You can run all backend unit tests without external dependencies:
```bash
dotnet test
```

To run tests for a specific service:
```bash
dotnet test <ServiceName>/<ServiceName>.Tests/<ServiceName>.Tests.csproj
```

### Frontend (Unit Tests)
```bash
cd Frontend
npm test
```

### Integration Tests (Requires Docker)
Integration tests require Docker and SQL Server. They are run in the CI pipeline but can be executed locally:
```bash
# Start dependencies (SQL Server)
docker compose up -d sqlserver

# Run migrations and load test data
docker compose up migrator --abort-on-container-exit --exit-code-from migrator

# Run integration tests (example: using a test project or script)
# Note: Integration tests are primarily run in CI via the integration-validation job.
```

## Coding Standards

### C#
- Follow the [.NET Coding Conventions](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- Code formatting is enforced via `.editorconfig` and `dotnet format`.
- Before submitting a PR, run:
  ```bash
  dotnet format
  ```

### TypeScript/Angular
- Follow the Angular style guide.
- Code formatting and linting are enforced via ESLint and Prettier.
- Before submitting a PR, run:
  ```bash
  cd Frontend
  npm run lint
  ng build
  ```

### General
- Write meaningful commit messages.
- Keep pull requests focused on a single concern.
- Update documentation when changing interfaces or configuration.
- Add unit tests for new features and bug fixes.

## Submitting Changes

1. Fork the repository and create your branch from `main`.
2. Make your changes, ensuring to follow the coding standards.
3. Run tests locally to ensure nothing is broken.
4. Commit your changes with a clear and descriptive message.
5. Push your branch to your fork and open a pull request against the `main` branch of this repository.
6. Ensure the PR description explains the problem and solution, and includes any relevant issue numbers.

## Code Review Process

- All PRs require at least one approval from a maintainer.
- CI checks must pass (build, tests, formatting, linting).
- Be responsive to feedback and be prepared to make adjustments.

Thank you for your contribution!