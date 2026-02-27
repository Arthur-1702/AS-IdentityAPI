# IdentityService — AgroMonitor

Microsserviço responsável por autenticação de produtores rurais via JWT.

---

## Endpoints

| Método | Rota             | Descrição               | Auth |
| ------ | ---------------- | ----------------------- | ---- |
| POST   | `/auth/register` | Cadastra novo produtor  | —    |
| POST   | `/auth/login`    | Autentica e retorna JWT | —    |
| GET    | `/auth/health`   | Health check            | —    |
| GET    | `/metrics`       | Métricas Prometheus     | —    |
| GET    | `/swagger`       | Documentação interativa | —    |

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [dotnet-ef CLI](#1-instalar-dotnet-ef-cli)

---

## Passo a Passo — Ambiente Local

### 1. Instalar dotnet-ef CLI

```bash
dotnet tool install --global dotnet-ef
```

Verifique:

```bash
dotnet ef --version
```

---

### 2. Subir SQL Server via Docker

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=123@login" \
  -p 1433:1433 --name agromonitor-sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Aguarde ~20 segundos para o SQL Server inicializar.

---

### 3. Criar o banco de dados

Conecte com qualquer cliente SQL (Azure Data Studio, DBeaver, SSMS) em:

- **Server:** `localhost,1433`
- **Login:** `fiapas`
- **Password:** `123@login`

Execute:

```sql
CREATE DATABASE [agromonitor-identity];
```

---

### 4. Gerar e aplicar a Migration

Na raiz do projeto:

```bash
# Gera a migration inicial
dotnet ef migrations add InitialCreate \
  --project IdentityService.csproj \
  --output-dir src/Data/Migrations

# Aplica ao banco
dotnet ef database update
```

> O `Program.cs` também chama `db.Database.Migrate()` no startup,
> então em produção/Docker a migration é aplicada automaticamente.

---

### 5. Rodar o serviço

```bash
dotnet run
```

Acesse:

- Swagger UI: http://localhost:5001/swagger
- Metrics: http://localhost:5001/metrics

---

## Passo a Passo — Docker Compose (tudo junto)

```bash
# Build + sobe SQL Server e IdentityService
docker compose up --build

# Derrubar tudo
docker compose down -v
```

O serviço estará disponível em `http://localhost:5001`.

---

## Passo a Passo — Azure (Produção)

### 1. Criar Azure SQL Server e banco

```bash
# Grupo de recursos
az group create --name rg-agromonitor --location brazilsouth

# SQL Server
az sql server create \
  --name agromonitor-sqlsrv \
  --resource-group rg-agromonitor \
  --location brazilsouth \
  --admin-user fiapas \
  --admin-password "123@login"

# Banco de dados
az sql db create \
  --resource-group rg-agromonitor \
  --server agromonitor-sqlsrv \
  --name agromonitor-identity \
  --service-objective Basic

# Liberar IP para Migrations (opcional, dev)
az sql server firewall-rule create \
  --resource-group rg-agromonitor \
  --server agromonitor-sqlsrv \
  --name AllowMyIP \
  --start-ip-address <SEU_IP> \
  --end-ip-address <SEU_IP>
```

### 2. Atualizar connection string no appsettings.json

```json
"IdentityDb": "Server=agromonitor-sqlsrv.database.windows.net;Database=agromonitor-identity;User Id=fiapas;Password=123@login;Encrypt=True;"
```

### 3. Aplicar migration apontando para Azure

```bash
dotnet ef database update
```

### 4. Configurar Jwt:Secret seguro

Use **Azure Key Vault** ou **variáveis de ambiente**:

```bash
# Exemplo via variável de ambiente no container/App Service
Jwt__Secret=sua-chave-super-secreta-producao-32chars
```

---

## Variáveis de Ambiente (referência)

| Variável                        | Descrição                             |
| ------------------------------- | ------------------------------------- |
| `ConnectionStrings__IdentityDb` | Connection string do Azure SQL        |
| `Jwt__Secret`                   | Chave HMAC (mín. 32 chars)            |
| `Jwt__Issuer`                   | Issuer do token (ex: IdentityService) |
| `Jwt__Audience`                 | Audience (ex: AgroMonitor)            |
| `Jwt__ExpiresInMinutes`         | Validade do token em minutos          |

---

## Observabilidade

O endpoint `GET /metrics` expõe métricas no formato Prometheus. Métricas customizadas:

| Métrica                                  | Tipo      | Descrição                      |
| ---------------------------------------- | --------- | ------------------------------ |
| `identity_login_attempts_total`          | Counter   | Total de logins (result label) |
| `identity_register_attempts_total`       | Counter   | Total de registros             |
| `identity_http_request_duration_seconds` | Histogram | Latência das requisições       |

Configure o `prometheus.yml` para fazer scrape em `http://identity-service:8080/metrics`.
