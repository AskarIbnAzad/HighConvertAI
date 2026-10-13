import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-validation-card',
  templateUrl: './validation-card.html',
  styleUrls: ['./validation-card.css'],
  standalone: false,
})
export class ValidationCardComponent {
  @Input() title = 'Validation Failed';

  @Input() message = '';

  @Input() footer = '';

  @Input() buttonText = 'Return';

  @Output() action = new EventEmitter<void>();

  constructor(private router: Router) {}

  buttonClicked(): void {
    this.action.emit();
  }
}
