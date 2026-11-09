import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'theme';

// Modo claro/oscuro. Si el usuario nunca eligió, se usa el tema del sistema.
@Injectable({ providedIn: 'root' })
export class ThemeService {
  isDark = signal(false);

  constructor() {
    let saved: string | null = null;
    try {
      saved = localStorage.getItem(STORAGE_KEY);
    } catch {
      // Sin acceso a localStorage: se usa el tema del sistema.
    }

    const dark = saved ? saved === 'dark' : matchMedia('(prefers-color-scheme: dark)').matches;
    this.apply(dark);
  }

  toggle() {
    const dark = !this.isDark();

    // La clase activa la transición de colores solo durante el cambio (ver styles.css).
    const root = document.documentElement;
    root.classList.add('theme-transition');
    setTimeout(() => root.classList.remove('theme-transition'), 350);

    this.apply(dark);

    try {
      localStorage.setItem(STORAGE_KEY, dark ? 'dark' : 'light');
    } catch {
      // No es crítico: simplemente no se recuerda la elección.
    }
  }

  private apply(dark: boolean) {
    this.isDark.set(dark);
    document.body.style.colorScheme = dark ? 'dark' : 'light';
  }
}
