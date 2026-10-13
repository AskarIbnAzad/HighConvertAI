import { Directive } from '@angular/core';
import { Table } from 'primeng/table';

/**
 * Extend this in any grid component to get a standard, consistent
 * clear() behavior without rewriting reset logic each time.
 *
 * Usage:
 *   export class UserComponent extends FilterableTableBase implements OnInit { ... }
 */
@Directive()
export abstract class FilterableTableBase {
  private boundFilterKeys: string[] = [];

  /** Call once in the child constructor/ngOnInit, listing bound filter property names. */
  protected registerFilterKeys(keys: string[]): void {
    this.boundFilterKeys = keys;
  }

  clear(table: Table): void {
    table.clear();
    for (const key of this.boundFilterKeys) {
      (this as any)[key] = typeof (this as any)[key] === 'string' ? '' : null;
    }
  }
}
