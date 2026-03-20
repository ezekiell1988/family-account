/**
 * Modelos de autenticación - 100% compatibles con OpenAPI backend
 * Base URL: /api/v1/auth/
 * Actualizado: 11 de marzo de 2026
 */

// ========================================
// REQUEST MODELS
// ========================================

/**
 * Paso 1: Solicitar token temporal (PIN)
 * POST /api/v1/auth/request-token
 */
export interface RequestTokenRequest {
  codeLogin: string;
}

/**
 * Paso 2: Login con token temporal
 * POST /api/v1/auth/login
 */
export interface LoginRequest {
  codeLogin: string;
  token: string;
}

/**
 * Registro de dispositivo anónimo
 * POST /health/device
 */
export interface RegisterDeviceRequest {
  uuidApp: string;
  platform: string;
  model: string;
  osVersion: string;
  manufacturer: string;
  isVirtual: boolean;
  webUserAgent?: string;
}

/**
 * Response del registro de dispositivo
 */
export interface RegisterDeviceResponse {
  success: boolean;
  deviceToken: string;
  expiresAt: string;
  message: string;
}

// ========================================
// RESPONSE MODELS
// ========================================

/**
 * Response al solicitar token (Paso 1)
 */
export interface RequestTokenResponse {
  success: boolean;
  message: string;
  emailMasked?: string | null;
  expiresInMinutes?: number;
}

/**
 * Datos del usuario autenticado
 */
export interface UserData {
  idLogin: number;
  codeLogin: string;
  nameLogin: string;
  phoneLogin: string | null;
  emailLogin: string | null;
  /** Roles del usuario en el customer actual, consultados en tiempo real (IDs numéricos) */
  roles: number[];
}

/**
 * Response de login exitoso (Paso 2)
 */
export interface LoginResponse {
  success: boolean;
  message: string;
  user: UserData;
  accessToken: string | null;
  expiresAt: string | null; // ISO 8601 format
}

/**
 * Response de verify-token (GET /api/v1/auth/check)
 * Requiere autenticación: Cookie httpOnly o Bearer token
 */
export interface VerifyTokenResponse {
  success: boolean;
  isValid: boolean;
  message: string;
  user: UserData | null;
  expiresAt: string | null;
}

/**
 * Response de refresh-token (POST /api/v1/auth/refresh-token)
 * Requiere autenticación: Bearer token
 */
export interface RefreshTokenResponse {
  success: boolean;
  message: string;
  accessToken: string | null;
  expiresAt: string | null;
  expiresIn?: number;
}

/**
 * Response de logout (POST /api/v1/auth/logout)
 * Requiere autenticación: Bearer token
 */
export interface LogoutResponse {
  success: boolean;
  message: string;
  microsoft_logout_url?: string | null;
  redirect_required?: boolean;
}

/**
 * Error response genérico de autenticación
 */
export interface AuthErrorResponse {
  detail: string;
}

// ========================================
// DEPRECATED (mantener para compatibilidad)
// ========================================

/** @deprecated Usar RequestTokenRequest */
export interface LoginTokenRequest {
  codeLogin: string;
}

/** @deprecated Usar RequestTokenResponse */
export interface LoginTokenResponse {
  success: boolean;
  message: string;
  idLogin?: number;
  tokenGenerated: boolean;
}

/** @deprecated Usar VerifyTokenResponse */
export interface UserInfoResponse {
  success: boolean;
  user: UserData;
  expiresAt: string;
  issuedAt?: string;
}
