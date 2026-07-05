# Representantes API

API .NET 10 (Minimal APIs) que serve o app Flutter `josapar_representantes` — autenticação, catálogo de produtos, clientes, pedidos e sincronização.

## Rodando localmente

1. Suba o Postgres:
   ```powershell
   copy .env.example .env
   # edite .env com senhas locais
   docker compose up -d
   docker compose ps   # aguarde "healthy"
   ```
2. Configure os segredos locais (nunca em `appsettings.json`):
   ```powershell
   cd src/Josapar.Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=josapar;Username=josapar_app;Password=<mesma senha do .env>;"
   dotnet user-secrets set "Jwt:SigningKey" "<uma string aleatória longa, só para dev>"
   ```
3. Aplique as migrations e rode:
   ```powershell
   dotnet ef database update
   dotnet run
   ```
4. UI de teste (Scalar): `http://localhost:5080/scalar/v1` (a porta exata aparece no console ao rodar).

## Credenciais de teste (seed)

- Representante já ativado: matrícula `88294` ou e-mail `ricardo.santos@josapar.com`, senha `Josapar@123`.
- Representante não ativado (para testar o fluxo de primeiro acesso): matrícula `00123456`.

## Fora de escopo desta versão

Ver `.claude/plans` do repositório do app Flutter (`peppy-beaming-flask.md`) para o plano completo e o que ficou explicitamente fora do escopo desta primeira leva (refresh token, conflitos de sync, território de representante, etc.).
