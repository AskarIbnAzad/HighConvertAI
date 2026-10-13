import { Component, EventEmitter, Input, Output } from '@angular/core';

export interface DropdownOption {
    value: string;
    label: string;
}

@Component({
    selector: 'app-dropdown',
    templateUrl: './dropdown.component.html',
    styleUrls: ['./dropdown.component.css'],
    standalone: false
})
export class DropdownComponent {
    @Input() id = '';
    @Input() label = '';
    @Input() value = '';
    @Input() options: DropdownOption[] = [];

    @Output() valueChange = new EventEmitter<string>();

    onSelect(event: Event): void {
        const target = event.target as HTMLSelectElement;
        this.valueChange.emit(target.value);
    }
}
