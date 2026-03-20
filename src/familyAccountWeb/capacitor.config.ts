import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.clickeat.app',
  appName: 'ClickEat',
  webDir: 'www',
  server: {
    // Para desarrollo web, permite conexiones localhost
    hostname: 'localhost',
    androidScheme: 'http',
    iosScheme: 'http',
    cleartext: true
  }
};

export default config;
