---
name: family-account-security
description: >
  Guía completa del sistema de autenticación en family-account: flujo de login en 2 pasos
  (solicitar PIN → ingresar PIN), JWT + refresh token con Redis, endpoints del API,
  AuthService Angular con Signals, interceptor, guard, y formulario de login.
  Usar SIEMPRE que se modifique, depure o extienda cualquier parte del login o autenticación.
---

# Sistema de Autenticación — family-account

Autenticación basada en **JWT Bearer** (localStorage) + **refresh token** (Redis).  
El login es un flujo de **2 pasos**: el usuario ingresa su correo → recibe PIN por email → ingresa el PIN.

---

## 1. Flujo Completo de Login

```
[Web] Paso 1: ingresa email
  → POST /api/v1/auth/request-pin  { emailUser }
  ← 200 OK { message }           ← PIN enviado al correo via Hangfire

[Web] Paso 2: ingresa PIN de 5 dígitos
  → POST /api/v1/auth/login       { emailUser, pin }
  ← 200 OK { accessToken, refreshToken, expiresAt }

[Web] Obtener perfil del usuario
  → GET  /api/v1/auth/me.json     [Authorization: Bearer <accessToken>]
  ← 200 OK { idUser, codeUser, nameUser, emailUser }

[Web] Verificar sesión activa (en app init / AuthGuard)
  → GET  /api/v1/auth/check       [Authorization: Bearer <accessToken>]
  ← 200 OK { success, isValid, message, user, expiresAt }

[Web] Renovar token (interceptor automático)
  → POST /api/v1/auth/refresh     { refreshToken }
  ← 200 OK { accessToken, refreshToken, expiresAt }

[Web] Cerrar sesión
  → POST /api/v1/auth/logout      { refreshToken? }  [Authorization: Bearer]
  ← 204 No Content
```

> El token expirado devuelve 401 → el interceptor llama a `/refresh` automáticamente → si falla → `clearSession()` + redirect a `/login`.

---

## 2. API — `Features/Auth/`

### 2.1 Estructura de archivos

```
Features/Auth/
  Dtos/
    RequestPinRequest.cs      ← { EmailUser }
    LoginRequest.cs            ← { EmailUser, Pin }
    RefreshTokenRequest.cs     ← { RefreshToken }
    LogoutRequest.cs           ← { RefreshToken? }
    AuthResponse.cs            ← AuthResponse, MeResponse, CheckAuthResponse, CheckAuthUserResponse, MessageResponse
  IAuthService.cs
  AuthService.cs
  AuthModule.cs               ← endpoints + DI registration
  ITokenService.cs
  TokenService.cs             ← genera/valida JWT + refresh token
```

### 2.2 Endpoints (`AuthModule.cs`)

| Método | Ruta                    | Auth         | Handler     |
|--------|-------------------------|--------------|-------------|
| POST   | `/auth/request-pin`     | Anonymous    | `RequestPin`|
| POST   | `/auth/login`           | Anonymous    | `Login`     |
| POST   | `/auth/refresh`         | Anonymous    | `Refresh`   |
| GET    | `/auth/me.json`         | Required     | `Me`        |
| POST   | `/auth/logout`          | Required     | `Logout`    |
| GET    | `/auth/check`           | Required     | `Check`     |

Registro en `Program.cs`:
```csharp
builder.Services.AddAuthModule();           // AddScoped<IAuthService, AuthService>
                                            // AddScoped<ITokenService, TokenService>
var v1 = app.MapGroup("/api/v1");
v1.MapAuthEndpoints();
```

### 2.3 DTOs

```csharp
// REQUEST
public sealed record RequestPinRequest   { [Required, EmailAddress] public required string EmailUser { get; init; } }
public sealed record LoginRequest        { [Required, EmailAddress] public required string EmailUser { get; init; }
                                           [Required, StringLength(5, MinimumLength = 5)] public required string Pin { get; init; } }
public sealed record RefreshTokenRequest { [Required] public required string RefreshToken { get; init; } }
public sealed record LogoutRequest       { public string? RefreshToken { get; init; } }

// RESPONSE
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public sealed record MeResponse(int IdUser, string CodeUser, string NameUser, string EmailUser);
public sealed record MessageResponse(string Message);

public sealed record CheckAuthUserResponse(
    int IdLogin, string CodeLogin, string NameLogin,
    string? PhoneLogin, string? EmailLogin, IReadOnlyList<int> Roles);

public sealed record CheckAuthResponse(
    bool Success, bool IsValid, string Message,
    CheckAuthUserResponse? User, DateTime? ExpiresAt);
```

> **Importante**: `CheckAuthUserResponse` usa los nombres que espera el frontend (`IdLogin`, `CodeLogin`, `NameLogin`, etc.), NO los de la BD (`IdUser`, `CodeUser`, etc.).

### 2.4 IAuthService

```csharp
public interface IAuthService
{
    Task<(bool success, string message)> RequestPinAsync(string emailUser, CancellationToken ct = default);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<bool> LogoutAsync(int idUser, string jti, DateTime tokenExpiresAt, string refreshToken, CancellationToken ct = default);
    Task<MeResponse?> GetMeAsync(int idUser, CancellationToken ct = default);
    Task<CheckAuthResponse> CheckAuthAsync(int idUser, DateTime tokenExpiresAt, CancellationToken ct = default);
}
```

### 2.5 AuthService — Lógica clave

**`RequestPinAsync`**:
1. Busca usuario por `emailUser` en `db.User`
2. Genera PIN de 5 dígitos: `Random.Shared.Next(0, 99999).ToString("D5")`
3. Guarda `UserPin { IdUser, Pin }` en BD
4. Encola `EmailJobs.SendPinEmailAsync` via Hangfire (asíncrono)

**`LoginAsync`**:
1. Busca usuario por `emailUser`
2. Verifica que `UserPin` exista para ese usuario con ese PIN
3. Carga roles (`db.UserRole` → `Role.NameRole`)
4. `tokenService.GenerateAccessToken(...)` → `(accessToken, jti, expiresAt)`
5. `tokenService.GenerateRefreshToken()` → bytes aleatorios en Base64
6. Guarda refresh token en Redis: `rt:{refreshToken}` → `idUser` (TTL = `RefreshTokenExpirationDays`)
7. Encola `PinJobs.DeleteAllUserPinsAsync(idUser)` via Hangfire
8. Devuelve `AuthResponse(accessToken, refreshToken, expiresAt)`

**`RefreshTokenAsync`**:
1. Lee `rt:{refreshToken}` de Redis → `idUser`
2. Busca usuario y roles
3. **Revoca el refresh token viejo**: `cache.RemoveAsync(RefreshKey(refreshToken))`
4. Genera nuevos `accessToken` + `newRefreshToken`
5. Guarda `newRefreshToken` en Redis
6. Devuelve `AuthResponse` con los tokens nuevos

**`LogoutAsync`**:
1. Agrega `jti` a blacklist Redis: `revoked:{jti}` → `"1"` (TTL = tiempo restante del token)
2. Revoca refresh token: `cache.RemoveAsync(RefreshKey(refreshToken))`

**`CheckAuthAsync`**:
1. Extrae `idUser` y `expiresAt` del claim del JWT (ya validado por middleware)
2. Carga usuario + roles (`IdRole`) de BD
3. Devuelve `CheckAuthResponse(true, true, "Token válido", user, expiresAt)`

### 2.6 TokenService — JWT

**Claims generados en `GenerateAccessToken`**:
- `sub` → `idUser`
- `email` → `emailUser`
- `jti` → `Guid.NewGuid()`
- `codeUser` → código de usuario
- `nameUser` → nombre
- `iat` → timestamp Unix
- `ClaimTypes.Role` → uno por cada rol (string)

**Algoritmo**: `HmacSha256`  
**Configuración** (`appsettings.json` → sección `"Jwt"`):
```json
{
  "Jwt": {
    "Secret": "...",
    "Issuer": "...",
    "Audience": "...",
    "TokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### 2.7 Redis — Claves

| Patrón                 | Valor     | TTL                              | Uso                         |
|------------------------|-----------|----------------------------------|-----------------------------|
| `rt:{refreshToken}`    | `idUser`  | `RefreshTokenExpirationDays`     | Validar refresh token       |
| `revoked:{jti}`        | `"1"`     | Tiempo restante del access token | Blacklist de tokens         |

> El middleware JWT valida `revoked:{jti}` en cada request para detectar tokens revocados.

---

## 3. Web — Angular

### 3.1 Modelos (`auth.models.ts`)

```typescript
// src/app/shared/models/auth.models.ts

// Requests
export interface RequestPinRequest { emailUser: string; }
export interface LoginRequest      { emailUser: string; pin: string; }
export interface RefreshTokenRequest { refreshToken: string; }
export interface LogoutRequest     { refreshToken?: string; }

// Responses
export interface UserData {
  idLogin: number;
  codeLogin: string;
  nameLogin: string;
  phoneLogin: string | null;
  emailLogin: string | null;
  roles: number[];            // IDs numéricos de roles
}

export interface LoginResponse {
  success: boolean;
  message: string;
  user: UserData;
  accessToken: string | null;
  expiresAt: string | null;
}

export interface VerifyTokenResponse {   // GET /auth/check
  success: boolean;
  isValid: boolean;
  message: string;
  user: UserData | null;
  expiresAt: string | null;
}

export interface RefreshTokenResponse {
  success: boolean;
  message: string;
  accessToken: string | null;
  expiresAt: string | null;
}
```

### 3.2 AuthService (`auth.service.ts`)

#### Signals y estado

```typescript
readonly currentUser       = signal<UserData | null>(this.getUserFromStorage());
readonly token             = signal<string | null>(this.getTokenFromStorage());
readonly refreshTokenValue = signal<string | null>(localStorage.getItem('refreshToken'));
readonly tokenExpiresAt    = signal<string | null>(localStorage.getItem('tokenExpiresAt'));

readonly isAuthenticated     = computed(() => { /* token existe y no expiró */ });
readonly isTokenExpiringSoon = computed(() => { /* expira en < 5 min */ });
readonly isTokenExpired      = computed(() => { /* ya expiró */ });
```

#### Effects (sincronización automática con localStorage)

```typescript
// En constructor:
effect(() => { /* currentUser  → localStorage.userData   */ });
effect(() => { /* token        → localStorage.token      */ });
effect(() => { /* refreshToken → localStorage.refreshToken */ });
```

#### Métodos públicos principales

```typescript
// Paso 1: solicitar PIN
requestLoginToken(emailUser: string): Observable<RequestTokenResponse>
  → POST /api/v1/auth/request-pin  { emailUser }

// Paso 2: login + obtener perfil
loginWithToken(emailUser: string, pin: string): Observable<LoginResponse>
  → POST /api/v1/auth/login  { emailUser, pin }
     ↳ guarda token, refreshToken, expiresAt en signals
  → GET  /api/v1/auth/me.json       (con Bearer)
     ↳ mapea MeResponse → UserData, guarda en currentUser signal
  → retorna LoginResponse

// Verificar sesión (guard + init)
checkAuthentication(): Observable<boolean>
  → Si !getToken() → retorna of(false) SIN llamar al backend (evita cascada 401)
  → GET  /api/v1/auth/check  (con Bearer, cache 5 segundos)

// Renovar token (llamado automáticamente por interceptor)
refreshToken(): Observable<RefreshTokenResponse>
  → POST /api/v1/auth/refresh  { refreshToken: refreshTokenValue() }
  → Actualiza token + refreshTokenValue + tokenExpiresAt

// Logout
logout(): Observable<LogoutResponse>
  → POST /api/v1/auth/logout  { ? }
  → llama clearAuth() + router.navigate(['/login'])

// Limpiar sesión local (sin llamar al backend)
clearSession(): void
  → clearAuth() → limpia todos los signals y localStorage

getToken(): string | null
  → devuelve this.token()
```

#### localStorage keys

| Key               | Contenido                          |
|-------------------|------------------------------------|
| `token`           | JWT access token                   |
| `refreshToken`    | Refresh token                      |
| `tokenExpiresAt`  | ISO 8601 datetime de expiración    |
| `userData`        | JSON de `UserData`                 |

### 3.3 Interceptor (`auth.interceptor.functional.ts`)

```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => { ... }
```

**Comportamiento**:
1. Siempre clona con `withCredentials: true` (por si se usan cookies en el futuro)
2. Agrega `Authorization: Bearer <token>` a todos los requests **excepto**:
   - Rutas públicas: `/auth/login`, `/auth/login-token`, `config.json`
   - Endpoints de auth: `/auth/check`, `/auth/refresh`, `/auth/logout`
3. En 401: llama `authService.refreshToken()` una vez (flag `isRefreshing`)
   - Si el refresh tiene éxito → reintenta el request original con el nuevo token
   - Si el refresh falla → `authService.clearSession()` + lanza el error

> **Por qué `/auth/check` no lleva Bearer**: el interceptor lo excluye para evitar que el 401 de check dispare un refresh infinito. La lógica de `checkAuthentication()` retorna `of(false)` inmediatamente si no hay token local, cortando el ciclo antes.

### 3.4 AuthGuard (`auth.guard.ts`)

```typescript
canActivate(route, state): Observable<boolean | UrlTree> {
  // 1. Siempre permite /login
  // 2. Si `authService.isAuthenticated()` (signal local) → true inmediatamente
  // 3. Sino → llama `authService.checkAuthentication()` al backend
  //    → true  → acceso permitido
  //    → false → redirect a /login?returnUrl=...
}
```

### 3.5 Inicialización (`app.config.ts`)

`initializeAppConfig` se ejecuta con `provideAppInitializer` **antes** de que arranque la app:

1. **Health check**: `GET /health/{deviceId}.json` (o `/health.json` si no hay deviceId)
   - URL correcta: con `/` inicial y `.json` al final
   - 3 reintentos con 1 segundo de espera
   - Carga `nameCompany`, `sloganCompany`, `apiVersion` en `AppSettings`
2. **Token check**: si hay token en localStorage:
   - Si expirado → `clearSession()`
   - Si próximo a expirar (< 5 min) → `refreshToken()`

### 3.6 Login Component (`login.ts` + `login.html`)

#### Estado del componente

```typescript
emailUser: string = '';    // campo email (ngModel)
token: string = '';        // campo PIN (ngModel)
loading: boolean = false;          // spinner del botón login
requestingPin: boolean = false;    // spinner del botón solicitar PIN
pinRequested: boolean = false;     // PIN ya solicitado (UX info)
pinMessage: string = '';           // mensaje de éxito del PIN
errorMessage: string = '';         // mensaje de error
```

#### Flujo del formulario

```
usuario ingresa email  →  click "Solicitar PIN"  →  requestPin()
  → authService.requestLoginToken(emailUser)
  → éxito: pinMessage = 'PIN enviado...'
  → error 404: 'No existe un usuario con ese correo'

usuario ingresa PIN  →  submit  →  formSubmit(f)
  → valida form + /^[0-9]{5}$/.test(token)
  → authService.loginWithToken(emailUser, token)
  → éxito: router.navigate([returnUrl])
  → error:  errorMessage = mensaje del servidor o genérico
```

#### Template (dos versiones en el mismo archivo)

```html
@if (isMobile()) {
  <!-- Ionic: IonInput, IonButton, IonSpinner, IonItem, IonLabel -->
}
@if (isDesktop()) {
  <!-- Color-Admin/Bootstrap: form-floating, btn-outline-theme, spinner-border -->
}
```

**Campos comunes en ambas versiones**:
- Email: `type="email"`, `name="emailUserMobile|emailUserDesktop"`, `[(ngModel)]="emailUser"`, `autocomplete="email"`
- PIN: `type="password"`, `maxlength="5"`, `minlength="5"`, `pattern="[0-9]{5}"`, `autocomplete="one-time-code"`
- Botón solicitar PIN: `type="button"`, `[disabled]="!emailUser || requestingPin || loading"`, `(click)="requestPin()"`
- Botón login: `type="submit"`, `[disabled]="loading || !form.valid"`

---

## 4. Claves de traducción del login (`i18n`)

```json
// es.json / en.json — sección "LOGIN"
{
  "LOGIN": {
    "TITLE": "Iniciar Sesión",
    "SUBTITLE": "Sistema de Gestión",
    "CODE_LABEL": "Correo electrónico",          // antes era "Código de usuario"
    "TOKEN_LABEL": "Token (5 dígitos)",
    "REQUEST_PIN": "Solicitar PIN al correo",     // nuevo
    "BUTTON": "Ingresar",
    "BUTTON_LOADING": "Ingresando...",
    "DESCRIPTION": "...",
    "ERROR_INVALID": "Código o token inválido"
  }
}
```

---

## 5. Archivos de referencia

| Archivo | Ruta |
|---------|------|
| Endpoints API | `src/familyAccountApi/Features/Auth/AuthModule.cs` |
| Lógica API | `src/familyAccountApi/Features/Auth/AuthService.cs` |
| Interface API | `src/familyAccountApi/Features/Auth/IAuthService.cs` |
| JWT | `src/familyAccountApi/Features/Auth/TokenService.cs` |
| Config JWT | `src/familyAccountApi/Infrastructure/Options/JwtOptions.cs` |
| DTOs API | `src/familyAccountApi/Features/Auth/Dtos/` |
| Modelos web | `src/familyAccountWeb/src/app/shared/models/auth.models.ts` |
| Servicio web | `src/familyAccountWeb/src/app/service/auth.service.ts` |
| Interceptor | `src/familyAccountWeb/src/app/shared/interceptors/auth.interceptor.functional.ts` |
| Guard | `src/familyAccountWeb/src/app/shared/guards/auth.guard.ts` |
| App init | `src/familyAccountWeb/src/app/app.config.ts` |
| Login TS | `src/familyAccountWeb/src/app/pages/login/login.ts` |
| Login HTML | `src/familyAccountWeb/src/app/pages/login/login.html` |
| Traducciones | `src/familyAccountWeb/src/assets/i18n/es.json` |
