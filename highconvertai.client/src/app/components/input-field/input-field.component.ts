import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
    selector: 'app-input-field',
    templateUrl: './input-field.component.html',
    styleUrls: ['./input-field.component.css'],
    standalone: false
})
export class InputFieldComponent {
    @Input() id = '';
    @Input() label = '';
    @Input() type = 'text';
    @Input() placeholder = '';
    @Input() value = '';
    @Input() required = false;

    @Output() valueChange = new EventEmitter<string>();

    onInput(event: Event): void {
        const target = event.target as HTMLInputElement;
        this.valueChange.emit(target.value);
    }
}
