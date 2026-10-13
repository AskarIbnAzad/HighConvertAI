import { Component, OnInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { Table } from 'primeng/table';
import { FilterService } from 'primeng/api';
import { UserService } from '../../services/user.service';
import { AuditService } from '../../services/audit.service';
import { FilterableTableBase } from '../../shared/base/filterable-table.base';
import { SelectOption } from '../../shared/utils/table-filter.util';
import { UserDto } from '../../models/dto/user.dto';

export interface UserListItem {
  id: string;
  fullName: string;
  email: string;
  isEmailVerified: boolean;
  roles: string[];
  createdAt: string;
}

@Component({
  selector: 'app-user',
  templateUrl: './user.html',
  styleUrls: ['./user.css'],
  standalone: false,
})
export class UserComponent extends FilterableTableBase implements OnInit {
  @ViewChild('dt') dt!: Table;

  users: UserDto[] = [];
  loading = true;

  roleOptions: SelectOption<string>[] = [];
  verifiedOptions: SelectOption<boolean>[] = [
    { label: 'Verified', value: true },
    { label: 'Pending', value: false },
  ];

  filterName = '';
  filterEmail = '';
  filterRole: string | null = null;
  filterVerified: boolean | null = null;
  globalFilterValue = '';

  constructor(
    private userService: UserService,
    private auditService: AuditService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private filterService: FilterService,
  ) {
    super();
    this.registerFilterKeys([
      'filterName',
      'filterEmail',
      'filterRole',
      'filterVerified',
      'globalFilterValue',
    ]);

    // Custom matchMode: checks whether the array field includes the filter value.
    this.filterService.register('arrayContains', (value: string[], filter: string): boolean => {
      if (filter === undefined || filter === null || filter === '') return true;
      if (!value) return false;
      return value.includes(filter);
    });
  }

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;

    this.userService.getUsers().subscribe({
      next: (res) => {
        this.users = [...res];
        this.roleOptions = this.buildRoleOptions(this.users);
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load users:', err);
        this.users = [];
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  private buildRoleOptions(users: UserDto[]): SelectOption<string>[] {
    const allRoles = users.flatMap((u: any) => u.roles ?? []);
    const unique = [...new Set(allRoles)];
    return unique.map((role) => ({ label: role, value: role }));
  }

  viewUser(user: UserListItem): void {
    console.log(user);
    // this.router.navigate(['/admin/users', user.id]);
  }
}
