## Getting started

### 1. Setup development secrets
**⚠️ Warning:** Secret Manager doesn't encrypt the stored secrets and shouldn't be treated as a trusted store.

```shell
# DB username/password is in compose-deps.yaml 
dotnet user-secrets set "Database:ConnectionString" "User Id=fastadm;Password=KCq9vgmr6rP2TyTc8ZXrJfin6vKC8U;Host=localhost;Port=5432;Database=fastaddress;"
dotnet user-secrets set "Google:ApiKey" "<Google API key for Places API>"
```

### 2. Run docker

Set up these services:
- API at http://localhost:5000
- Web at http://localhost:3000
- Logging at http://localhost:8080
- Db at localhost:5432

```shell
docker compose -f compose-services.yaml -f compose-deps.yaml  up -d 
```
Omit compose-services.yaml if you want to manually run the Web or API project through dotnet.

### 3. Migrate database

```shell
dotnet ef database update \
  --startup-project source/FastAddress.Api/FastAddress.Api.csproj \
  --project source/FastAddress.Api.Database/FastAddress.Api.Database.csproj
```

### 4. Seed database

Run the SQL scripts in `/sql/fast-address-data.sql`
