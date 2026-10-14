# UnitConversion API

A production-style ASP.NET Core Web API built with .NET 10 for converting numerical values between units of measurement. The API adheres to Clean Architecture principles, utilizes generic affine transformation modeling across all physical categories, implements RFC 7807 ProblemDetails for error handling, and provides complete OpenAPI (Swagger) documentation and test coverage.

---

## Table of Contents

- [Features](#features)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [Run Locally](#run-locally)
  - [Run with Docker](#run-with-docker)
  - [Run Tests](#run-tests)
- [API Endpoints & Examples](#api-endpoints--examples)
  - [Health Check](#1-health-check)
  - [List Categories](#2-list-all-categories)
  - [List Units for a Category](#3-list-units-for-a-category)
  - [Perform a Conversion](#4-perform-a-conversion)
  - [Error Responses (RFC 7807)](#5-error-responses-rfc-7807)
- [Project Structure](#project-structure)
- [Design Decisions & Architecture Trade-offs](#design-decisions--architecture-trade-offs)
  - [Unified Affine Transformation Model](#1-unified-affine-transformation-model)
  - [Numerical Precision: `decimal` vs `double`](#2-numerical-precision-decimal-vs-double)
  - [Extensibility: Repository Abstraction & Scaling to a Database](#3-extensibility-repository-abstraction--scaling-to-a-database)
  - [Resilient Case-Insensitive Token Matching](#4-resilient-case-insensitive-token-matching)
  - [API Versioning & OpenAPI / Swagger UI](#5-api-versioning--openapi--swagger-ui)
  - [Structured Logging & Zero-Allocation Diagnostics](#6-structured-logging--zero-allocation-diagnostics)
  - [Global Exception Handling & RFC 7807 Standards](#7-global-exception-handling--rfc-7807-standards)
- [Future Improvements](#future-improvements)

---

## Features

- **Clean Architecture Layering**: Strict boundary separation across `Domain`, `Application`, `Infrastructure`, `Api`, and `Tests`.
- **Unified Affine Model**: Converts linear units (length, weight/mass) and affine units (temperature) using a single formula: $\text{BaseValue} = (\text{Value} \times \text{Factor}) + \text{Offset}$.
- **Case-Insensitive Multi-Token Resolution**: Matches units by canonical ID, full name, plural form, symbol, or alias (e.g. `meter`, `meters`, `m`, `M`, `°C`, `Celsius`, `kg`, `lbs`).
- **Physical Boundary Validation**: Enforces category-level physical constraints (e.g., temperatures below Absolute Zero $0\text{ K} / -273.15\text{ }^\circ\text{C} / -459.67\text{ }^\circ\text{F}$ are rejected with HTTP 400).
- **RFC 7807 ProblemDetails**: Structured, standardized error contracts across model binding, missing categories (HTTP 404), and validation errors (HTTP 400).
- **Configurable Precision**: Configurable default rounding via `appsettings.json` with per-request override.
- **API Versioning**: URL-segment versioning (`/api/v1/...`) powered by `Asp.Versioning.Mvc`.
- **OpenAPI / Swagger UI**: Interactive documentation with XML summary annotations enabled in Development.
- **Health Checks**: Ready-to-probe endpoint at `/health` for orchestrator liveness and readiness probes.
- **Multi-Stage Dockerfile**: Secure, unprivileged non-root container deployment.
- **Full Test Suite**: Comprehensive xUnit unit tests and end-to-end integration tests using `WebApplicationFactory`.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned in `global.json` via version `10.0.401`)
- [Docker](https://www.docker.com/) (optional, for containerized execution)
- [curl](https://curl.se/) or the VS Code REST Client extension (for executing `requests.http`)

---

## Getting Started

### Run Locally

1. Clone or open the repository root:
   ```bash
   cd unit-convertor-api
   ```

2. Restore and run the Web API:
   ```bash
   dotnet run --project src/UnitConversion.Api/UnitConversion.Api.csproj
   ```

3. The application will start locally (default ports: `http://localhost:5000` or `https://localhost:5001`).
   - Swagger UI: `http://localhost:5000/swagger`
   - Health Check: `http://localhost:5000/health`

### Run with Docker

1. Build the multi-stage Docker image:
   ```bash
   docker build -t unitconversion-api .
   ```

2. Run the container:
   ```bash
   docker run -d -p 8080:8080 --name unitconversion-app unitconversion-api
   ```

3. Probe the health check:
   ```bash
   curl http://localhost:8080/health
   ```

### Run Tests

Execute all unit and integration tests:

```bash
dotnet test UnitConversion.sln
```

To run with code coverage:

```bash
dotnet test UnitConversion.sln --collect:"XPlat Code Coverage"
```

---

## API Endpoints & Examples

### 1. Health Check

**Request:**
```bash
curl -i -X GET http://localhost:5000/health
```

**Response (HTTP 200 OK):**
```text
Healthy
```

---

### 2. List All Categories

**Request:**
```bash
curl -i -X GET http://localhost:5000/api/v1/categories
```

**Response (HTTP 200 OK):**
```json
[
  {
    "id": "length",
    "name": "Length",
    "baseUnit": "meter",
    "unitCount": 8,
    "description": "Distance and linear measurement units."
  },
  {
    "id": "temperature",
    "name": "Temperature",
    "baseUnit": "kelvin",
    "unitCount": 3,
    "description": "Thermodynamic temperature measurement units."
  },
  {
    "id": "weight",
    "name": "Weight/Mass",
    "baseUnit": "kilogram",
    "unitCount": 7,
    "description": "Mass and weight measurement units."
  }
]
```

---

### 3. List Units for a Category

**Request:**
```bash
curl -i -X GET http://localhost:5000/api/v1/categories/length/units
```

**Response (HTTP 200 OK):**
```json
[
  {
    "id": "millimeter",
    "name": "Millimeter",
    "symbol": "mm"
  },
  {
    "id": "centimeter",
    "name": "Centimeter",
    "symbol": "cm"
  },
  {
    "id": "meter",
    "name": "Meter",
    "symbol": "m"
  },
  {
    "id": "kilometer",
    "name": "Kilometer",
    "symbol": "km"
  },
  {
    "id": "inch",
    "name": "Inch",
    "symbol": "in"
  },
  {
    "id": "foot",
    "name": "Foot",
    "symbol": "ft"
  },
  {
    "id": "yard",
    "name": "Yard",
    "symbol": "yd"
  },
  {
    "id": "mile",
    "name": "Mile",
    "symbol": "mi"
  }
]
```

---

### 4. Perform a Conversion

#### Example A: Length (10 meters to feet)
**Request:**
```bash
curl -i -X POST http://localhost:5000/api/v1/conversions \
  -H "Content-Type: application/json" \
  -d '{
    "category": "length",
    "fromUnit": "meter",
    "toUnit": "foot",
    "value": 10
  }'
```

**Response (HTTP 200 OK):**
```json
{
  "category": "length",
  "fromUnit": "meter",
  "toUnit": "foot",
  "inputValue": 10,
  "result": 32.8084
}
```

#### Example B: Temperature Using Symbols (100 °C to °F)
**Request:**
```bash
curl -i -X POST http://localhost:5000/api/v1/conversions \
  -H "Content-Type: application/json" \
  -d '{
    "category": "temperature",
    "fromUnit": "°C",
    "toUnit": "°F",
    "value": 100
  }'
```

**Response (HTTP 200 OK):**
```json
{
  "category": "temperature",
  "fromUnit": "celsius",
  "toUnit": "fahrenheit",
  "inputValue": 100,
  "result": 212.0
}
```

#### Example C: Weight with Custom Precision (10 kg to lb rounded to 2 places)
**Request:**
```bash
curl -i -X POST http://localhost:5000/api/v1/conversions \
  -H "Content-Type: application/json" \
  -d '{
    "category": "weight",
    "fromUnit": "kg",
    "toUnit": "lb",
    "value": 10,
    "precision": 2
  }'
```

**Response (HTTP 200 OK):**
```json
{
  "category": "weight",
  "fromUnit": "kilogram",
  "toUnit": "pound",
  "inputValue": 10,
  "result": 22.05
}
```

---

### 5. Error Responses (RFC 7807)

#### Unknown Category (HTTP 404 Not Found)
**Request:**
```bash
curl -i -X POST http://localhost:5000/api/v1/conversions \
  -H "Content-Type: application/json" \
  -d '{
    "category": "volume",
    "fromUnit": "liter",
    "toUnit": "gallon",
    "value": 5
  }'
```

**Response (HTTP 404 Not Found):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Category Not Found",
  "status": 404,
  "detail": "Measurement category 'volume' was not found.",
  "instance": "/api/v1/conversions",
  "traceId": "0HN7S7M3I...",
  "category": "volume"
}
```

#### Temperature Below Absolute Zero (HTTP 400 Bad Request)
**Request:**
```bash
curl -i -X POST http://localhost:5000/api/v1/conversions \
  -H "Content-Type: application/json" \
  -d '{
    "category": "temperature",
    "fromUnit": "celsius",
    "toUnit": "kelvin",
    "value": -300
  }'
```

**Response (HTTP 400 Bad Request):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "The supplied value '-300' °C evaluates to -26.85 in base units, which is below absolute zero (0 K / -273.15 °C / -459.67 °F).",
  "instance": "/api/v1/conversions",
  "traceId": "0HN7S7M3I...",
  "errors": {
    "value": [
      "The supplied value '-300' °C evaluates to -26.85 in base units, which is below absolute zero (0 K / -273.15 °C / -459.67 °F)."
    ]
  }
}
```

---

## Project Structure

```text
unit-convertor-api/
│
├── .github/
│   └── workflows/
│       └── ci.yml                          # Continuous integration workflow
│
├── src/
│   ├── UnitConversion.Domain/              # Pure domain entities, models, and exceptions
│   │   ├── Exceptions/
│   │   │   ├── CategoryNotFoundException.cs
│   │   │   ├── UnitNotFoundException.cs
│   │   │   └── UnitConversionValidationException.cs
│   │   └── Models/
│   │       ├── UnitCategory.cs
│   │       └── UnitDefinition.cs
│   │
│   ├── UnitConversion.Application/         # Application abstractions, DTOs, and services
│   │   ├── Configuration/
│   │   │   └── ConversionOptions.cs
│   │   ├── DTOs/
│   │   │   ├── CategoryDto.cs
│   │   │   ├── ConversionRequestDto.cs
│   │   │   ├── ConversionResponseDto.cs
│   │   │   └── UnitDto.cs
│   │   ├── Interfaces/
│   │   │   ├── IConversionService.cs
│   │   │   └── IUnitCatalog.cs
│   │   ├── Services/
│   │   │   └── ConversionService.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── UnitConversion.Infrastructure/      # Data store & catalog implementation
│   │   ├── Catalog/
│   │   │   └── InMemoryUnitCatalog.cs
│   │   └── DependencyInjection.cs
│   │
│   └── UnitConversion.Api/                 # ASP.NET Core API layer
│       ├── Controllers/
│       │   ├── CategoriesController.cs
│       │   └── ConversionsController.cs
│       ├── Middleware/
│       │   └── GlobalExceptionHandler.cs
│       ├── Swagger/
│       │   └── ConfigureSwaggerOptions.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Program.cs
│
├── tests/
│   ├── UnitConversion.Tests.Unit/          # Unit tests (conversion logic, catalog, edge cases)
│   │   ├── ConversionServiceTests.cs
│   │   └── InMemoryUnitCatalogTests.cs
│   │
│   └── UnitConversion.Tests.Integration/   # End-to-end integration tests (WebApplicationFactory)
│       └── ConversionApiTests.cs
│
├── .dockerignore
├── .editorconfig
├── .gitignore
├── Directory.Build.props                   # Solution-wide build rules (WarningsAsErrors, Analyzers)
├── Dockerfile                              # Multi-stage production container build
├── global.json                             # Pinned .NET SDK version
├── requests.http                           # Ready-to-execute manual test requests
├── UnitConversion.sln                      # Classic Visual Studio solution file
├── UnitConversion.slnx                     # Modern .NET XML solution file
└── README.md                               # Project documentation
```

---

## Design Decisions & Architecture Trade-offs

### 1. Unified Affine Transformation Model
Rather than branching code paths or creating isolated conversion strategies for linear categories vs. temperature, the domain models every unit using an affine transformation relative to the category's canonical base unit:

$$\text{BaseValue} = (\text{Value} \times \text{Factor}) + \text{Offset}$$

$$\text{TargetValue} = \frac{\text{BaseValue} - \text{TargetUnit.Offset}}{\text{TargetUnit.Factor}}$$

- **Linear Units (Length, Mass)**: $\text{Offset} = 0$. The formula reduces to $\text{BaseValue} = \text{Value} \times \text{Factor}$, requiring only one multiplicative factor entry.
- **Affine Units (Temperature)**: Uses Kelvin ($K$) as the SI base unit ($\text{MinAllowedBaseValue} = 0\text{ K}$).
  - Celsius: $\text{Factor} = 1.0$, $\text{Offset} = 273.15$
  - Kelvin: $\text{Factor} = 1.0$, $\text{Offset} = 0.0$
  - Fahrenheit: $\text{Factor} = \frac{5}{9}$, $\text{Offset} = 273.15 - (32 \times \frac{5}{9}) \approx 255.37222$
- **Benefit**: Adding a new unit (or an entirely new category, such as Pressure, Energy, or Speed) requires adding only one metadata definition without modifying any conversion algorithms.

### 2. Numerical Precision: `decimal` vs `double`
- **Choice**: The core calculation engine uses C#'s 128-bit `decimal` type.
- **Trade-off Analysis**:
  - `double` (IEEE 754 64-bit floating point) offers hardware-accelerated scientific arithmetic and slightly faster throughput, but introduces binary floating-point representation drift (e.g. $0.1 + 0.2 \neq 0.3$, or unexpected fractional tails in exact decimal definitions like $1\text{ in} = 0.0254\text{ m}$ or $1\text{ lb} = 0.45359237\text{ kg}$).
  - `decimal` provides 28–29 significant digits of exact base-10 representation, completely eliminating decimal rounding artifacts during conversions between international metric and imperial units.
  - Repeating fractions (such as $\frac{5}{9}$ in Fahrenheit conversion) maintain 28 digits of precision, which far exceeds physical instrumentation tolerances.
- **Rounding Strategy**: Conversions round at the boundary using `MidpointRounding.AwayFromZero` according to configurable precision options (default: 4 decimal places).

### 3. Extensibility: Repository Abstraction & Scaling to a Database
- The application layer interacts solely with the `IUnitCatalog` interface (`GetCategoriesAsync`, `GetCategoryAsync`, `GetUnitsByCategoryAsync`, `FindUnitAsync`).
- The current implementation (`InMemoryUnitCatalog`) stores categories and units in memory.
- **Migration Path**:
  To scale to thousands of units stored in PostgreSQL, SQL Server, or an external JSON catalog:
  1. Create a new class implementing `IUnitCatalog` (e.g., `EfCoreUnitCatalog`).
  2. Map relational entities to the domain `UnitCategory` and `UnitDefinition` records.
  3. Swap registration in `UnitConversion.Infrastructure/DependencyInjection.cs`:
     ```csharp
     services.AddScoped<IUnitCatalog, EfCoreUnitCatalog>();
     ```
  4. Zero changes are required in `UnitConversion.Application`, `UnitConversion.Domain`, or `UnitConversion.Api`.

### 4. Resilient Case-Insensitive Token Matching
- Users and clients often query units using different forms:
  - Canonical identifier: `meter`, `celsius`, `kilogram`
  - Display name: `Meter`, `Celsius`, `Kilogram`
  - Symbol: `m`, `°C`, `C`, `kg`
  - Common plural or alternate spelling: `meters`, `metres`, `degrees celsius`, `lbs`, `stone`
- `InMemoryUnitCatalog` pre-indexes all canonical IDs, names, symbols, and aliases into an internal composite hash map `(CategoryId, LookupKey)` providing $O(1)$ lookups regardless of query casing.

### 5. API Versioning & OpenAPI / Swagger UI
- Utilizes `Asp.Versioning.Mvc` and `Asp.Versioning.Mvc.ApiExplorer`.
- Routes are explicitly prefixed: `/api/v{version:apiVersion}/[controller]`.
- Swagger UI automatically discovers API versions via `IConfigureNamedOptions<SwaggerGenOptions>`, generating version-specific documentation endpoints.

### 6. Structured Logging & Zero-Allocation Diagnostics
- High-performance logging is implemented using .NET source-generated `[LoggerMessage]` partial methods in `ConversionService` and `GlobalExceptionHandler`.
- Avoids boxing of value types, eliminates string interpolation overhead when log levels are disabled, and complies with compiler performance rules (`CA1848`, `CA1873`).

### 7. Global Exception Handling & RFC 7807 Standards
- Employs ASP.NET Core's `IExceptionHandler` middleware (`GlobalExceptionHandler`).
- All errors (including unhandled exceptions, validation errors, and domain exceptions) return RFC 7807 compliant `ProblemDetails`:
  - `CategoryNotFoundException` $\rightarrow$ `404 Not Found`
  - `UnitNotFoundException` $\rightarrow$ `400 Bad Request`
  - `UnitConversionValidationException` $\rightarrow$ `400 Bad Request` with field-level `errors` dictionary
  - Unhandled exceptions $\rightarrow$ `500 Internal Server Error` (sanitizing stack traces in production environments).

---

## Future Improvements

1. **Persistent Relational / NoSQL Store**: Implement `EfCoreUnitCatalog` backed by PostgreSQL or SQLite with caching (e.g., `IMemoryCache` or Redis) for instant retrieval.
2. **Compound & Derived Dimensions**: Support composite units derived from powers and products (e.g. $\text{m/s}$, $\text{km/h}$, $\text{m}^2$, $\text{ft}^3$, $\text{psi}$, $\text{Pa}$).
3. **Dynamic User-Defined Units**: Allow authorized users to register custom units and conversion factors at runtime through administrative REST endpoints.
4. **Rate Limiting & Authentication**: Implement token bucket rate limiting via `Microsoft.AspNetCore.RateLimiting` and API key / OAuth2 authentication for public consumption.
5. **gRPC / Protocol Buffer Endpoints**: Expose a parallel high-performance gRPC endpoint for low-latency internal microservice communication.
