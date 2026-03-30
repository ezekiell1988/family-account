import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  NgxDatatableModule,
  ColumnMode,
} from '@swimlane/ngx-datatable';
import { PanelComponent } from '../../../../../components';
import {
  PurchaseInvoiceDto,
  PurchaseInvoiceTypeDto,
  PurchaseInvoiceLineRequest,
  CreatePurchaseInvoiceRequest,
  UpdatePurchaseInvoiceRequest,
  FiscalPeriodLookup,
  CurrencyDto,
  BankAccountDto,
} from '../../../../../shared/models';

const STATUS_OPTIONS = ['Borrador', 'Confirmado', 'Anulado'] as const;

interface FormLine {
  idProductSKU:    number | null;
  descriptionLine: string;
  quantity:        number;
  unitPrice:       number;
  taxPercent:      number;
  totalLineAmount: number;
}

function emptyLine(): FormLine {
  return { idProductSKU: null, descriptionLine: '', quantity: 1, unitPrice: 0, taxPercent: 13, totalLineAmount: 0 };
}

@Component({
  selector: 'app-purchase-invoices-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, NgxDatatableModule, PanelComponent, DatePipe, DecimalPipe],
  templateUrl: './purchase-invoices-web.component.html',
})
export class PurchaseInvoicesWebComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  invoices      = input<PurchaseInvoiceDto[]>([]);
  totalCount    = input(0);
  isLoading     = input(false);
  deletingId    = input<number | null>(null);
  errorMessage  = input('');
  invoiceTypes  = input<PurchaseInvoiceTypeDto[]>([]);
  currencies    = input<CurrencyDto[]>([]);
  fiscalPeriods = input<FiscalPeriodLookup[]>([]);
  bankAccounts  = input<BankAccountDto[]>([]);

  // ── Outputs ───────────────────────────────────────────────────────
  refresh    = output<void>();
  create     = output<CreatePurchaseInvoiceRequest>();
  editSave   = output<UpdatePurchaseInvoiceRequest & { id: number }>();
  remove     = output<number>();
  confirm    = output<number>();
  cancel     = output<number>();
  clearError = output<void>();

  // ── Constantes ────────────────────────────────────────────────────
  readonly ColumnMode     = ColumnMode;
  readonly statusOptions  = STATUS_OPTIONS;

  // ── Filtros ───────────────────────────────────────────────────────
  filterStatus = signal('');
  filterSearch = signal('');

  filteredItems = computed(() => {
    let items = this.invoices();
    const status = this.filterStatus();
    const search = this.filterSearch().toLowerCase().trim();
    if (status) items = items.filter(i => i.statusInvoice === status);
    if (search) items = items.filter(i =>
      i.numberInvoice.toLowerCase().includes(search) ||
      i.providerName.toLowerCase().includes(search) ||
      i.namePurchaseInvoiceType.toLowerCase().includes(search) ||
      i.codeCurrency.toLowerCase().includes(search),
    );
    return items;
  });

  // ── Formulario ────────────────────────────────────────────────────
  showForm           = signal(false);
  editingId          = signal<number | null>(null);
  formInvoiceType    = signal(0);
  formFiscalPeriod   = signal(0);
  formCurrency       = signal(0);
  formBankAccount    = signal<number | null>(null);
  formNumber         = signal('');
  formProvider       = signal('');
  formDate           = signal('');
  formSubTotal       = signal(0);
  formTax            = signal(0);
  formTotal          = signal(0);
  formDescription    = signal('');
  formExchangeRate   = signal(1);
  formLines          = signal<FormLine[]>([emptyLine()]);

  needsBankAccount = computed(() =>
    this.invoiceTypes().find(t => t.idPurchaseInvoiceType === this.formInvoiceType())
      ?.counterpartFromBankMovement ?? false
  );

  filteredBankAccounts = computed(() =>
    this.bankAccounts().filter(b => b.idCurrency === this.formCurrency())
  );

  // ── Líneas ────────────────────────────────────────────────────────
  addLine(): void {
    this.formLines.update(ls => [...ls, emptyLine()]);
  }

  removeLine(index: number): void {
    this.formLines.update(ls => ls.filter((_, i) => i !== index));
  }

  updateLine(index: number, field: keyof FormLine, value: string | number): void {
    this.formLines.update(ls => {
      const updated = [...ls];
      (updated[index] as unknown as Record<string, unknown>)[field] = value;
      // Recalcular total de línea
      const l = updated[index];
      if (field === 'quantity' || field === 'unitPrice' || field === 'taxPercent') {
        const base = l.quantity * l.unitPrice;
        l.totalLineAmount = Math.round(base * (1 + l.taxPercent / 100) * 100) / 100;
      }
      return updated;
    });
    this.recalcTotals();
  }

  private recalcTotals(): void {
    const lines = this.formLines();
    const subTotal = lines.reduce((s, l) => s + l.quantity * l.unitPrice, 0);
    const tax      = lines.reduce((s, l) => s + l.quantity * l.unitPrice * (l.taxPercent / 100), 0);
    this.formSubTotal.set(Math.round(subTotal * 100) / 100);
    this.formTax.set(Math.round(tax * 100) / 100);
    this.formTotal.set(Math.round((subTotal + tax) * 100) / 100);
  }

  openCreate(): void {
    this.editingId.set(null);
    this.formInvoiceType.set(this.invoiceTypes()[0]?.idPurchaseInvoiceType ?? 0);
    this.formFiscalPeriod.set(this.fiscalPeriods()[0]?.idFiscalPeriod ?? 0);
    this.formCurrency.set(this.currencies()[0]?.idCurrency ?? 0);
    this.formBankAccount.set(null);
    this.formNumber.set('');
    this.formProvider.set('');
    this.formDate.set(new Date().toISOString().slice(0, 10));
    this.formSubTotal.set(0);
    this.formTax.set(0);
    this.formTotal.set(0);
    this.formDescription.set('');
    this.formExchangeRate.set(1);
    this.formLines.set([emptyLine()]);
    this.showForm.set(true);
  }

  openEdit(inv: PurchaseInvoiceDto): void {
    this.editingId.set(inv.idPurchaseInvoice);
    this.formInvoiceType.set(inv.idPurchaseInvoiceType);
    this.formFiscalPeriod.set(inv.idFiscalPeriod);
    this.formCurrency.set(inv.idCurrency);
    this.formBankAccount.set(inv.idBankAccount ?? null);
    this.formNumber.set(inv.numberInvoice);
    this.formProvider.set(inv.providerName);
    this.formDate.set(inv.dateInvoice.slice(0, 10));
    this.formSubTotal.set(inv.subTotalAmount);
    this.formTax.set(inv.taxAmount);
    this.formTotal.set(inv.totalAmount);
    this.formDescription.set(inv.descriptionInvoice ?? '');
    this.formExchangeRate.set(inv.exchangeRateValue);
    this.formLines.set(inv.lines.map(l => ({
      idProductSKU:    l.idProductSKU,
      descriptionLine: l.descriptionLine,
      quantity:        l.quantity,
      unitPrice:       l.unitPrice,
      taxPercent:      l.taxPercent,
      totalLineAmount: l.totalLineAmount,
    })));
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  saveForm(): void {
    const lines: PurchaseInvoiceLineRequest[] = this.formLines().map(l => ({
      idProductSKU:    l.idProductSKU,
      descriptionLine: l.descriptionLine,
      quantity:        l.quantity,
      unitPrice:       l.unitPrice,
      taxPercent:      l.taxPercent,
      totalLineAmount: l.totalLineAmount,
    }));

    const payload = {
      idFiscalPeriod:        this.formFiscalPeriod(),
      idCurrency:            this.formCurrency(),
      idPurchaseInvoiceType: this.formInvoiceType(),
      idBankAccount:         this.needsBankAccount() ? this.formBankAccount() : null,
      numberInvoice:         this.formNumber(),
      providerName:          this.formProvider(),
      dateInvoice:           this.formDate(),
      subTotalAmount:        this.formSubTotal(),
      taxAmount:             this.formTax(),
      totalAmount:           this.formTotal(),
      descriptionInvoice:    this.formDescription() || null,
      exchangeRateValue:     this.formExchangeRate(),
      lines,
    };

    const editId = this.editingId();
    if (editId !== null) {
      this.editSave.emit({ ...payload, id: editId });
    } else {
      this.create.emit(payload);
    }
    this.showForm.set(false);
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Confirmado': return 'badge bg-success';
      case 'Anulado':    return 'badge bg-danger';
      default:           return 'badge bg-secondary';
    }
  }

  canEdit(inv: PurchaseInvoiceDto): boolean {
    return inv.statusInvoice === 'Borrador';
  }

  canConfirm(inv: PurchaseInvoiceDto): boolean {
    return inv.statusInvoice === 'Borrador';
  }

  canCancel(inv: PurchaseInvoiceDto): boolean {
    return inv.statusInvoice === 'Confirmado';
  }

  canDelete(inv: PurchaseInvoiceDto): boolean {
    return inv.statusInvoice === 'Borrador';
  }
}
