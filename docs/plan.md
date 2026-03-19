# Plan de Implementación: Family Account API

## Tecnologías
- **.NET 10** / **C# 14** — Minimal APIs, módulos por feature
- **Entity Framework Core 10** — SQL Server, convención camelCase
- **JWT Bearer** — access token + refresh token con Redis blacklist
- **Hangfire + SQL Server** — jobs de email y eliminación de PINs
- **Redis** — cache, refresh tokens, blacklist de JWTs revocados
- **SMTP** — envío de correos con PIN
- **Scalar** — documentación OpenAPI (reemplaza Swagger UI)

---

## Variables de Entorno

| Variable | Descripción |
|---|---|
| `DB_CONNECTION_STRING_BASE64` | Connection string SQL Server en base64 |
| `REDIS_CONNECTION_STRING_BASE64` | Connection string Redis en base64 |
| `JWT__SECRET` | Clave secreta para firmar JWT (mínimo 32 chars) |
| `JWT__ISSUER` | Issuer del JWT |
| `JWT__AUDIENCE` | Audience del JWT |
| `JWT__TOKENEXPIRATIONMINUTES` | Duración del access token en minutos |
| `JWT__REFRESHTOKENEXPIRATIONDAYS` | Duración refresh token en días |
| `SMTP__HOST` | Servidor SMTP |
| `SMTP__PORT` | Puerto SMTP |
| `SMTP__USERNAME` | Usuario SMTP |
| `SMTP__PASSWORD` | Contraseña SMTP |
| `SMTP__FROMEMAIL` | Email del remitente |
| `SMTP__FROMNAME` | Nombre del remitente |
| `SMTP__ENABLESSL` | Habilitar SSL (true/false) |

---

## Base de datos

### Tabla `user`
| Columna | Tipo | Restricciones |
|---|---|---|
| `idUser` | INT | PK, autoincremental |
| `codeUser` | NVARCHAR(50) | NOT NULL, UNIQUE |
| `nameUser` | NVARCHAR(150) | NOT NULL |
| `phoneUser` | VARCHAR(20) | NULL |
| `emailUser` | VARCHAR(200) | NOT NULL |

### Tabla `userPin`
| Columna | Tipo | Restricciones |
|---|---|---|
| `idUserPin` | INT | PK, autoincremental |
| `idUser` | INT | NOT NULL, FK → user.idUser |
| `pin` | VARCHAR(5) | NOT NULL, solo dígitos |

**Índice único:** `(idUser, pin)` — mismo PIN no puede repetirse en el mismo usuario

---

## Estructura de carpetas

```
src/familyAccountApi/
├── FamilyAccountApi.csproj
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Domain/
│   └── Entities/
│       ├── User.cs
│       └── UserPin.cs
├── Infrastructure/
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── Configuration/
│   │       ├── UserConfiguration.cs
│   │       └── UserPinConfiguration.cs
│   └── Options/
│       ├── JwtOptions.cs
│       └── SmtpOptions.cs
├── Features/
│   ├── Users/
│   │   ├── UsersModule.cs
│   │   ├── UserService.cs
│   │   ├── IUserService.cs
│   │   └── Dtos/
│   │       ├── CreateUserRequest.cs
│   │       ├── UpdateUserRequest.cs
│   │       └── UserResponse.cs
│   ├── Auth/
│   │   ├── AuthModule.cs
│   │   ├── AuthService.cs
│   │   ├── IAuthService.cs
│   │   ├── TokenService.cs
│   │   ├── ITokenService.cs
│   │   └── Dtos/
│   │       ├── RequestPinRequest.cs
│   │       ├── LoginRequest.cs
│   │       ├── RefreshTokenRequest.cs
│   │       └── AuthResponse.cs
│   └── Email/
│       ├── EmailModule.cs
│       ├── IEmailService.cs
│       └── SmtpEmailService.cs
├── BackgroundJobs/
│   ├── EmailJobs.cs
│   └── PinJobs.cs
└── OpenApi/
    └── BearerSecuritySchemeTransformer.cs
```

---

## Endpoints

### v1 — Auth Controller (`/api/v1/auth`)

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/api/v1/auth/request-pin` | ❌ | Genera PIN 5 dígitos y lo envía por correo (Hangfire) |
| POST | `/api/v1/auth/login` | ❌ | Login con email + PIN. Retorna JWT + refresh token. Borra PINs (Hangfire) |
| POST | `/api/v1/auth/refresh` | ❌ | Renueva JWT usando refresh token (almacenado en Redis) |
| GET | `/api/v1/auth/me` | ✅ | Retorna datos del usuario autenticado |
| POST | `/api/v1/auth/logout` | ✅ | Revoca JWT (blacklist Redis) y elimina refresh token |

### v1 — User Controller (`/api/v1/users`)

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/api/v1/users` | ✅ | Listar todos los usuarios |
| GET | `/api/v1/users/{id}` | ✅ | Obtener usuario por ID |
| POST | `/api/v1/users` | ✅ | Crear nuevo usuario |
| PUT | `/api/v1/users/{id}` | ✅ | Actualizar usuario |
| DELETE | `/api/v1/users/{id}` | ✅ | Eliminar usuario |

---

## Flujo de autenticación con PIN

```
1. Cliente → POST /api/v1/auth/request-pin { emailUser: "..." }
2. API → Busca usuario por email → Si no existe, retorna 404
3. API → Genera PIN 5 dígitos → Guarda en tabla userPin
4. API → Encola job Hangfire: EnviarEmailConPin(idUser, pin)
5. API → Responde 200 { message: "PIN enviado a tu correo" }

6. Cliente → POST /api/v1/auth/login { emailUser: "...", pin: "12345" }
7. API → Busca usuario por email → Valida PIN en userPin
8. API → Si inválido → 401 Unauthorized
9. API → Genera JWT (access token) + GUID (refresh token)
10. API → Guarda refresh token en Redis con TTL configurable
11. API → Encola job Hangfire: EliminarPinsDeUsuario(idUser)
12. API → Responde 200 { accessToken, refreshToken, expiresAt }
```

---

## JWT y Redis

- **Access token**: contiene claims `sub` (idUser), `email`, `jti` (UUID), `iat`, `exp`
- **Refresh token**: GUID almacenado en Redis como `rt:{guid}` → `{idUser}` con TTL configurable
- **Blacklist de JWTs revocados**: Redis `revoked:{jti}` → 1 con TTL = tiempo restante del token

---

## Hangfire Dashboard

- **URL**: `/hangfire`
- **Autenticación**: HTTP Basic Auth
  - Usuario: `admin`
  - Contraseña: `12345`
- **Storage**: SQL Server (misma BD)

---

## Scalar / OpenAPI

- **URL Scalar UI**: `/scalar/v1`
- **URL OpenAPI JSON**: `/openapi/v1.json`
- Solo disponible en entorno `Development`
- Configurado con JWT Bearer security scheme

---

## Tareas de implementación en orden

- [x] 1. Crear plan.md
- [ ] 2. Crear proyecto .NET 10 con `dotnet new webapi`
- [ ] 3. Agregar paquetes NuGet (EF Core, JWT, Hangfire, Redis, Scalar)
- [ ] 4. Crear entidades de dominio (User, UserPin)
- [ ] 5. Crear AppDbContext con convención camelCase
- [ ] 6. Crear configuraciones Fluent API para entidades
- [ ] 7. Crear clases de opciones (JwtOptions, SmtpOptions)
- [ ] 8. Crear IEmailService y SmtpEmailService
- [ ] 9. Crear ITokenService y TokenService (generación/validación JWT)
- [ ] 10. Crear IAuthService y AuthService (login, logout, refresh, me, request-pin)
- [ ] 11. Crear endpoints de Auth (AuthModule con MapGroup v1)
- [ ] 12. Crear IUserService y UserService (CRUD)
- [ ] 13. Crear endpoints de Users (UsersModule con MapGroup v1)
- [ ] 14. Crear Hangfire jobs (EmailJobs, PinJobs)
- [ ] 15. Crear HangfireAuthorizationFilter (Basic Auth admin/12345)
- [ ] 16. Crear BearerSecuritySchemeTransformer para OpenAPI
- [ ] 17. Configurar Program.cs (todo el pipeline)
- [ ] 18. Crear appsettings.json con valores por defecto
- [ ] 19. Ejecutar migración EF Core (`dotnet ef migrations add InitialCreate`)
- [ ] 20. Verificar que compila y corregir errores
