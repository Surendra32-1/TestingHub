import { routes } from './app.routes';
import { administrationViewGuard, peopleSearchViewGuard } from './core/auth/auth.guard';

describe('application routes', () => {
  const routeFor = (path: string) => routes.find((route) => route.path === path);

  it('matches each navigation label to its page route and permission guard', () => {
    const peopleRoute = routeFor('people');
    const administrationRoute = routeFor('administration');

    expect(peopleRoute?.title).toBe('People | TeamHub');
    expect(peopleRoute?.canActivate).toContain(peopleSearchViewGuard);
    expect(administrationRoute?.title).toBe('Administration | TeamHub');
    expect(administrationRoute?.canActivate).toContain(administrationViewGuard);
  });

  it('redirects the former employee route to People', () => {
    expect(routeFor('employee')?.redirectTo).toBe('/people');
  });
});
