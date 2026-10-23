import { Injectable } from '@angular/core';
import { HierarchyConnection } from './models';

export interface HierarchyConnectorPath {
  id: string;
  path: string;
}

@Injectable({ providedIn: 'root' })
export class HierarchyConnectorService {
  measure(
    canvas: HTMLElement,
    svg: SVGSVGElement,
    connections: HierarchyConnection[]
  ): HierarchyConnectorPath[] {
    const canvasBounds = canvas.getBoundingClientRect();
    const nodes = new Map<string, DOMRect>();
    canvas.querySelectorAll<HTMLElement>('[data-node-id]').forEach((node) => {
      const id = node.dataset['nodeId'];
      if (id) {
        nodes.set(id, node.getBoundingClientRect());
      }
    });

    const canvasStyle = getComputedStyle(canvas);
    let contentRight = 0;
    let contentBottom = 0;
    for (const bounds of nodes.values()) {
      contentRight = Math.max(contentRight, bounds.right - canvasBounds.left);
      contentBottom = Math.max(contentBottom, bounds.bottom - canvasBounds.top);
    }
    const width = Math.max(
      canvas.clientWidth,
      Math.ceil(contentRight + parseFloat(canvasStyle.paddingRight))
    );
    const height = Math.max(
      canvas.clientHeight,
      Math.ceil(contentBottom + parseFloat(canvasStyle.paddingBottom))
    );
    svg.setAttribute('viewBox', `0 0 ${width} ${height}`);
    svg.setAttribute('width', `${width}`);
    svg.setAttribute('height', `${height}`);

    const localBounds = (bounds: DOMRect) => ({
      left: bounds.left - canvasBounds.left,
      right: bounds.right - canvasBounds.left,
      top: bounds.top - canvasBounds.top,
      bottom: bounds.bottom - canvasBounds.top,
      centerX: (bounds.left + bounds.right) / 2 - canvasBounds.left,
      centerY: (bounds.top + bounds.bottom) / 2 - canvasBounds.top
    });

    return connections.flatMap((connection) => {
      const fromBounds = nodes.get(connection.fromId);
      const toBounds = nodes.get(connection.toId);
      if (!fromBounds || !toBounds) {
        return [];
      }

      const from = localBounds(fromBounds);
      const to = localBounds(toBounds);
      const deltaX = to.centerX - from.centerX;
      const deltaY = to.centerY - from.centerY;

      if (connection.route === 'direct') {
        const startX = deltaX < 0 ? from.left : from.right;
        const endX = deltaX < 0 ? to.right : to.left;
        return [{
          id: connection.id,
          path: `M ${startX} ${from.centerY} L ${endX} ${to.centerY}`
        }];
      }

      if (Math.abs(deltaX) > Math.abs(deltaY)) {
        const direction = Math.sign(deltaX);
        const startX = direction > 0 ? from.right : from.left;
        const endX = direction > 0 ? to.left : to.right;
        const handle = Math.max(24, Math.abs(endX - startX) * 0.42);
        return [{
          id: connection.id,
          path: `M ${startX} ${from.centerY} C ${startX + direction * handle} ${from.centerY}, ${endX - direction * handle} ${to.centerY}, ${endX} ${to.centerY}`
        }];
      }

      const direction = Math.sign(deltaY) || 1;
      const startY = direction > 0 ? from.bottom : from.top;
      const endY = direction > 0 ? to.top : to.bottom;
      const handle = Math.max(24, Math.abs(endY - startY) * 0.42);
      return [{
        id: connection.id,
        path: `M ${from.centerX} ${startY} C ${from.centerX} ${startY + direction * handle}, ${to.centerX} ${endY - direction * handle}, ${to.centerX} ${endY}`
      }];
    });
  }
}
