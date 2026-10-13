import { Component, Input } from '@angular/core';

interface AuditStep {
  key: string;
  title: string;
  description: string;
  status: 'done' | 'running' | 'pending';
  value?: string;
}

@Component({
  selector: 'app-scan-loading-modal',
  templateUrl: './scan-loading-modal.html',
  styleUrls: ['./scan-loading-modal.css'],
  standalone: false
})
export class ScanLoadingModalComponent {
  @Input() visible = false;
  @Input() url = '';
  @Input() progress = 0;
  @Input() siteType = 'Detecting...';
  @Input() steps: AuditStep[] = [];
  @Input() tipText = '';
}
