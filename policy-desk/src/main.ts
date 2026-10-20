import { bootstrapApplication } from '@angular/platform-browser';  
import { appConfig } from './app/app.config';
import { App } from './app/app';

// We launch the app components using the settings as imported above
bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
