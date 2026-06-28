import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'currencyFormat', standalone: true })
export class CurrencyFormatPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value == null) return '—';
    const formatted = value.toLocaleString('zh-TW');
    return `NT$ ${formatted}`;
  }
}
