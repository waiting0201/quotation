import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CountryFacade } from '../../facades/country.facade';
import { CountryListItem, CountryCreateUpdate } from '../../models/country.model';
import { CountryFormDialogComponent } from '../../components/country-form-dialog/country-form-dialog.component';

/** 刪除對話框的狀態 */
interface DeleteDialogState {
  open: boolean;
  countryId: number;
  countryTitle: string;
  hasCustomers: boolean;
  customerCount: number;
}

/** 分頁設定 */
const PAGE_SIZE = 20;

@Component({
  selector: 'app-countries-page',
  standalone: true,
  imports: [CountryFormDialogComponent],
  templateUrl: './countries-page.component.html',
  styleUrl: './countries-page.component.scss',
})
export class CountriesPageComponent implements OnInit {
  private readonly facade = inject(CountryFacade);

  // ─── Facade signals ───────────────────────────────────────────────────────
  readonly countries = this.facade.countries;
  readonly loading = this.facade.loading;
  readonly saving = this.facade.saving;

  // ─── Dialog state ─────────────────────────────────────────────────────────
  readonly formDialogOpen = signal(false);
  readonly editingCountry = signal<CountryListItem | null>(null);

  readonly deleteDialog = signal<DeleteDialogState>({
    open: false,
    countryId: 0,
    countryTitle: '',
    hasCustomers: false,
    customerCount: 0,
  });

  // ─── Pagination ───────────────────────────────────────────────────────────
  readonly currentPage = signal(1);
  readonly pageSize = PAGE_SIZE;

  readonly totalCount = this.facade.totalCount;
  readonly totalPages = this.facade.totalPages;
  readonly pageNumbers = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const maxVisible = 5;
    let start = Math.max(1, current - Math.floor(maxVisible / 2));
    let end = start + maxVisible - 1;
    if (end > total) {
      end = total;
      start = Math.max(1, end - maxVisible + 1);
    }
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  });

  // ─── Lifecycle ───────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.facade.loadCountries(1, this.pageSize);
  }

  // ─── Create / Edit ────────────────────────────────────────────────────────
  openCreateDialog(): void {
    this.editingCountry.set(null);
    this.formDialogOpen.set(true);
  }

  openEditDialog(country: CountryListItem): void {
    this.editingCountry.set(country);
    this.formDialogOpen.set(true);
  }

  closeFormDialog(): void {
    this.formDialogOpen.set(false);
    this.editingCountry.set(null);
  }

  onFormSaved(dto: CountryCreateUpdate): void {
    const editing = this.editingCountry();
    const obs = editing
      ? this.facade.updateCountry(editing.countryId, dto, this.currentPage(), this.pageSize)
      : this.facade.createCountry(dto, this.currentPage(), this.pageSize);

    obs.subscribe((ok) => {
      if (ok) {
        this.closeFormDialog();
      }
    });
  }

  // ─── Delete ───────────────────────────────────────────────────────────────
  openDeleteDialog(country: CountryListItem): void {
    this.deleteDialog.set({
      open: true,
      countryId: country.countryId,
      countryTitle: country.title,
      hasCustomers: country.customerCount > 0,
      customerCount: country.customerCount,
    });
  }

  closeDeleteDialog(): void {
    this.deleteDialog.update((d) => ({ ...d, open: false }));
  }

  confirmDelete(): void {
    const { countryId } = this.deleteDialog();
    this.facade.deleteCountry(countryId, this.currentPage(), this.pageSize).subscribe((ok) => {
      if (ok) {
        this.closeDeleteDialog();
      }
    });
  }

  // ─── Pagination ───────────────────────────────────────────────────────────
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.facade.loadCountries(page, this.pageSize);
  }
}
