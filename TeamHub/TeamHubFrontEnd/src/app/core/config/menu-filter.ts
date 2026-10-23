import { MenuItem, MenuVisibility } from './menu.config';

export interface MenuContext {
  readonly isAuthenticated: boolean;
  readonly hasPermission: (permissionCode: string) => boolean;
}

const isGranted = (code: string, context: MenuContext): boolean => context.hasPermission(code);

function matchesVisibility(visibility: MenuVisibility | undefined, context: MenuContext): boolean {
  switch (visibility) {
    case 'anonymous':
      return !context.isAuthenticated;
    case 'authenticated':
      return context.isAuthenticated;
    default:
      return true;
  }
}

function matchesPermissions(item: MenuItem, context: MenuContext): boolean {
  if (item.permission && !isGranted(item.permission, context)) {
    return false;
  }

  if (item.requiresAll && !item.requiresAll.every((code) => isGranted(code, context))) {
    return false;
  }

  if (item.requiresAny && item.requiresAny.length > 0 && !item.requiresAny.some((code) => isGranted(code, context))) {
    return false;
  }

  return !(item.hideIfPermission && isGranted(item.hideIfPermission, context));
}

export function filterMenu(items: readonly MenuItem[], context: MenuContext): MenuItem[] {
  const visible: MenuItem[] = [];

  for (const item of items) {
    const filtered = filterItem(item, context);
    if (filtered) {
      visible.push(filtered);
    }
  }

  return visible;
}

function filterItem(item: MenuItem, context: MenuContext): MenuItem | null {
  if (!matchesVisibility(item.visibility, context) || !matchesPermissions(item, context)) {
    return null;
  }

  if (!item.children) {
    return { ...item };
  }

  const children = filterMenu(item.children, context);
  return children.length > 0 ? { ...item, children } : null;
}
