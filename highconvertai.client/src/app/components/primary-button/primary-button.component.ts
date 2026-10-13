import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
    selector: 'app-primary-button',
    templateUrl: './primary-button.component.html',
    styleUrls: ['./primary-button.component.css'],
    standalone: false
})
export class PrimaryButtonComponent {
    @Input() text = 'Submit';
    @Input() disabled = false;
    @Input() type: 'button' | 'submit' = 'button';

    @Output() clicked = new EventEmitter<void>();

    onClick(): void {
        this.clicked.emit();
    }
}
