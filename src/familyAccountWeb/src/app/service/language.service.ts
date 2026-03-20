import { Injectable, signal, computed } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export interface Language {
  code: string;
  name: string;
  flagCode: string; // Para usar con flag-icons: fi fi-{flagCode}
}

export const AVAILABLE_LANGUAGES: Language[] = [
  { code: 'es', name: 'Español', flagCode: 'es' },
  { code: 'en', name: 'English', flagCode: 'us' },
];

const STORAGE_KEY = 'appLanguage';
const DEFAULT_LANG = 'es';

@Injectable({
  providedIn: 'root',
})
export class LanguageService {
  readonly languages = AVAILABLE_LANGUAGES;

  private _currentLang = signal<string>(this.loadSavedLanguage());

  /** Código del idioma activo (signal) */
  readonly currentLang = this._currentLang.asReadonly();

  /** Objeto Language completo del idioma activo */
  readonly currentLanguage = computed(
    () => this.languages.find(l => l.code === this._currentLang()) ?? this.languages[0]
  );

  constructor(private translate: TranslateService) {
    this.translate.addLangs(this.languages.map(l => l.code));
    this.translate.setDefaultLang(DEFAULT_LANG);
    this.translate.use(this._currentLang());
  }

  /**
   * Cambia el idioma activo y lo persiste en localStorage.
   */
  setLanguage(code: string): void {
    const lang = this.languages.find(l => l.code === code);
    if (!lang) return;
    this.translate.use(code);
    this._currentLang.set(code);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(STORAGE_KEY, code);
    }
  }

  private loadSavedLanguage(): string {
    if (typeof localStorage !== 'undefined') {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved && AVAILABLE_LANGUAGES.some(l => l.code === saved)) {
        return saved;
      }
    }
    return DEFAULT_LANG;
  }
}
