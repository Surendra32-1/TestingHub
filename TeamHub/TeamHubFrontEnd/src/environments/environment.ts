export const environment = {
  production: false,
  apiUrl: 'http://localhost:5169/',
  apiUrlProduction: 'https://localhost:7178/',
  enableEntra: false,
  entra: {
    authority: 'https://login.microsoftonline.com/<TENANT_ID>',
    clientId: '<SPA_CLIENT_ID>',
    apiScope: 'api://<API_CLIENT_ID>/access_as_user',
    redirectUri: 'http://localhost:4200',
    postLogoutRedirectUri: 'http://localhost:4200'
  }
};
