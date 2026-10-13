import { Component, OnInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { Table } from 'primeng/table';
import { FilterService } from 'primeng/api';
import { AuditService } from '../../services/audit.service';
import { FilterableTableBase } from '../../shared/base/filterable-table.base';
import { buildSelectOptions, SelectOption } from '../../shared/utils/table-filter.util';
import { AuditDto } from '../../models/dto/audit.dto';
import { Router } from '@angular/router';

export interface AuditListItem {
  id: number;
  userName: string;
  role: string;
  url: string;
  pageType: string;
  score: number;
  createdAtUtc: string;
}

@Component({
  selector: 'app-audit',
  standalone: false,
  templateUrl: './audit.html',
  styleUrls: ['./audit.css'],
})
export class AuditComponent extends FilterableTableBase implements OnInit {
  @ViewChild('dt') dt!: Table;

  audits: AuditDto[] = [];
  loading = true;

  roleOptions: SelectOption<string>[] = [];
  pageTypeOptions: SelectOption<string>[] = [];

  filterUserName = '';
  filterUrl = '';
  filterRole: string | null = null;
  filterPageType: string | null = null;
  filterDateRange: Date[] | null = null;
  globalFilterValue = '';

  constructor(
    private auditService: AuditService,
    private cdr: ChangeDetectorRef,
    private router: Router,
    private filterService: FilterService,
  ) {
    super();
    this.registerFilterKeys([
      'filterUserName',
      'filterUrl',
      'filterRole',
      'filterPageType',
      'filterDateRange',
      'globalFilterValue',
    ]);

    // Custom matchMode: checks whether a date value falls within a [start, end] range.
    this.filterService.register('dateRange', (value: string, filter: Date[] | null): boolean => {
      if (!filter || !filter[0]) return true;
      if (!value) return false;

      const cellDate = new Date(value);
      const start = new Date(filter[0]);
      start.setHours(0, 0, 0, 0);

      // If only the start date is picked so far, don't filter yet.
      if (!filter[1]) return true;

      const end = new Date(filter[1]);
      end.setHours(23, 59, 59, 999);

      return cellDate >= start && cellDate <= end;
    });
  }

  ngOnInit(): void {
    this.loadAudits();
  }

  loadAudits(): void {
    this.loading = true;

    this.auditService.getAllAudits().subscribe({
      next: (res) => {
        this.audits = [...res];
        this.roleOptions = buildSelectOptions(this.audits, 'role');
        this.pageTypeOptions = buildSelectOptions(this.audits, 'pageType');
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load audits:', err);
        this.audits = [];
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  onDateRangeChange(): void {
    // Only apply the filter once both start and end dates are selected.
    if (this.filterDateRange && this.filterDateRange[0] && this.filterDateRange[1]) {
      this.dt.filter(this.filterDateRange, 'createdAtUtc', 'dateRange');
    }
  }

  onDateRangeClear(): void {
    this.filterDateRange = null;
    this.dt.filter(null, 'createdAtUtc', 'dateRange');
  }

  getScoreSeverity(score: number): 'success' | 'warn' | 'danger' {
    if (score >= 80) return 'success';
    if (score >= 50) return 'warn';
    return 'danger';
  }

  openAudit(id: number): void {
    this.router.navigate(['/result', id]);
  }
}
