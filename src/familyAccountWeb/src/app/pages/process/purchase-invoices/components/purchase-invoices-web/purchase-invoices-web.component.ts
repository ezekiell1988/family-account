import {
  Component,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  ViewChild,
  inject,
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
  DatatableRowDetailDirective,
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
  AccountingEntryDto,
  AccountingEntryLineRequest,
  UpdateAccountingEntryRequest,
  AccountDto,
  ProductSKUDto,
  ProductDto,
  ProductAccountDto,
  CostCenterDto,
  CreateProductAccountRequest,
  UpdateProductAccountRequest,
  CreateProductWithAccountsRequest,
} from '../../../../../shared/models';

const STATUS_OPTIONS = ['Borrador', 'Confirmado', 'Anulado'] as const;
const ENTRY_STATUS_OPTIONS = ['Borrador', 'Publicado', 'Anulado'] as const;

interface FormLine {
  skuCode:         string;
  descriptionLine: string;
  quantity:        number;
  unitPrice:       number;
  taxPercent:      number;
  totalLineAmount: number;
}

function emptyLine(): FormLine {
  return { skuCode: '', descriptionLine: '', quantity: 1, unitPrice: 0, taxPercent: 13, totalLineAmount: 0 };
}

interface FormEntryLine {
  idAccount:       number;
  debitAmount:     number;
  creditAmount:    number;
  descriptionLine: string;
}

interface FormLineAccount {
  idProductAccount: number | null; // null = nuevo
  idAccount:        number;
  idCostCenter:     number | null;
  percentageAccount: number;
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
  entries       = input<AccountingEntryDto[]>([]);
  accounts      = input<AccountDto[]>([]);
  productSKUs   = input<ProductSKUDto[]>([]);
  products      = input<ProductDto[]>([]);
  productAccounts = input<ProductAccountDto[]>([]);
  costCenters      = input<CostCenterDto[]>([]);
  lineAccountError  = input<string | null>(null);

  // ── Outputs ───────────────────────────────────────────────────────
  refresh       = output<void>();
  create        = output<CreatePurchaseInvoiceRequest>();
  editSave      = output<UpdatePurchaseInvoiceRequest & { id: number }>();
  remove        = output<number>();
  confirm       = output<number>();
  cancel        = output<number>();
  clearError    = output<void>();
  editEntrySave = output<UpdateAccountingEntryRequest & { id: number }>();
  createProductAccount      = output<CreateProductAccountRequest>();
  updateProductAccount      = output<UpdateProductAccountRequest & { id: number }>();
  deleteProductAccount      = output<number>();
  createProductWithAccounts = output<CreateProductWithAccountsRequest>();

  // ── Row detail ────────────────────────────────────────────────────
  @ViewChild(DatatableRowDetailDirective) rowDetail!: DatatableRowDetailDirective;
  private cdr = inject(ChangeDetectorRef);
  expandedId  = signal<number | null>(null);

  // ── Constantes ────────────────────────────────────────────────────
  readonly ColumnMode         = ColumnMode;
  readonly statusOptions      = STATUS_OPTIONS;
  readonly entryStatusOptions = ENTRY_STATUS_OPTIONS;

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

  filteredBankAccounts = computed(() => {
    const currency    = this.formCurrency();
    const invType     = this.invoiceTypes().find(t => t.idPurchaseInvoiceType === this.formInvoiceType());
    const isTc        = invType?.codePurchaseInvoiceType === 'TC';
    const expectedType = isTc ? 'Pasivo' : 'Activo';
    const validIds    = new Set(
      this.accounts().filter(a => a.typeAccount === expectedType).map(a => a.idAccount)
    );
    return this.bankAccounts().filter(b => b.idCurrency === currency && validIds.has(b.idAccount));
  });

  // ── Líneas ────────────────────────────────────────────────────────
  addLine(): void {
    this.formLines.update(ls => [...ls, emptyLine()]);
  }

  removeLine(index: number): void {
    this.formLines.update(ls => ls.filter((_, i) => i !== index));
  }

  updateLine(index: number, field: keyof FormLine, value: string | number | null): void {
    this.formLines.update(ls => {
      const updated = [...ls];
      (updated[index] as unknown as Record<string, unknown>)[field] = value;
      const l = updated[index];
      // Auto-rellenar descripción cuando se ingresa un código SKU conocido
      if (field === 'skuCode' && typeof value === 'string') {
        const code = value.trim();
        const match = this.productSKUs().find(
          s => s.codeProductSKU.toLowerCase() === code.toLowerCase()
        );
        if (match) {
          updated[index].descriptionLine = match.nameProductSKU;
        }
      }
      // Recalcular total de línea
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
      skuCode:         l.codeProductSKU ?? '',
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
      skuCode:         l.skuCode.trim() || null,
      skuName:         l.skuCode.trim() ? l.descriptionLine : null,
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

  // ── Row detail ────────────────────────────────────────────────────
  toggleExpand(row: PurchaseInvoiceDto): void {
    this.rowDetail.toggleExpandRow(row);
    const id = row.idPurchaseInvoice;
    this.expandedId.update(k => (k === id ? null : id));
    this.cdr.markForCheck();
  }

  // ── Helper: asiento vinculado ─────────────────────────────────────
  getLinkedEntry(inv: PurchaseInvoiceDto): AccountingEntryDto | undefined {
    if (!inv.idAccountingEntry) return undefined;
    return this.entries().find(e => e.idAccountingEntry === inv.idAccountingEntry);
  }

  getEntryTotalDebitInRow(entry: AccountingEntryDto): number {
    return entry.lines.reduce((s, l) => s + l.debitAmount, 0);
  }

  getEntryTotalCreditInRow(entry: AccountingEntryDto): number {
    return entry.lines.reduce((s, l) => s + l.creditAmount, 0);
  }

  // ── Editor de Asiento Contable ────────────────────────────────────
  showEntryForm            = signal(false);
  entryEditingId           = signal<number | null>(null);
  entryFormFiscalPeriod    = signal(0);
  entryFormCurrency        = signal(0);
  entryFormCurrencyDisplay = signal('');
  entryFormNumber          = signal('');
  entryFormDate            = signal('');
  entryFormDescription     = signal('');
  entryFormStatus          = signal<string>('Borrador');
  entryFormReference       = signal('');
  entryFormExchangeRate    = signal(1);
  entryFormLines           = signal<FormEntryLine[]>([]);

  entryTotalDebit  = computed(() => this.entryFormLines().reduce((s, l) => s + (l.debitAmount || 0), 0));
  entryTotalCredit = computed(() => this.entryFormLines().reduce((s, l) => s + (l.creditAmount || 0), 0));
  entryIsBalanced  = computed(() => this.entryTotalDebit() > 0 && this.entryTotalDebit() === this.entryTotalCredit());

  entryIsFormValid = computed(() =>
    this.entryFormFiscalPeriod() > 0 &&
    this.entryFormCurrency() > 0 &&
    this.entryFormNumber().trim().length > 0 &&
    this.entryFormDate().length > 0 &&
    this.entryFormDescription().trim().length > 0 &&
    this.entryFormExchangeRate() > 0 &&
    this.entryFormLines().length >= 2 &&
    this.entryIsBalanced(),
  );

  movementAccounts = computed(() => this.accounts().filter(a => a.allowsMovements && a.isActive));

  openEditEntry(entry: AccountingEntryDto): void {
    this.entryEditingId.set(entry.idAccountingEntry);
    this.entryFormFiscalPeriod.set(entry.idFiscalPeriod);
    this.entryFormCurrency.set(entry.idCurrency);
    this.entryFormCurrencyDisplay.set(`${entry.codeCurrency} – ${entry.nameCurrency}`);
    this.entryFormNumber.set(entry.numberEntry);
    this.entryFormDate.set(entry.dateEntry);
    this.entryFormDescription.set(entry.descriptionEntry);
    this.entryFormStatus.set(entry.statusEntry);
    this.entryFormReference.set(entry.referenceEntry ?? '');
    this.entryFormExchangeRate.set(entry.exchangeRateValue);
    this.entryFormLines.set(entry.lines.map(l => ({
      idAccount:       l.idAccount,
      debitAmount:     l.debitAmount,
      creditAmount:    l.creditAmount,
      descriptionLine: l.descriptionLine ?? '',
    })));
    this.showEntryForm.set(true);
  }

  cancelEntryForm(): void {
    this.showEntryForm.set(false);
    this.entryEditingId.set(null);
  }

  addEntryLine(): void {
    this.entryFormLines.update(ls => [
      ...ls,
      { idAccount: 0, debitAmount: 0, creditAmount: 0, descriptionLine: '' },
    ]);
  }

  removeEntryLine(index: number): void {
    this.entryFormLines.update(ls => ls.filter((_, i) => i !== index));
  }

  updateEntryLine<K extends keyof FormEntryLine>(index: number, key: K, value: FormEntryLine[K]): void {
    this.entryFormLines.update(ls => ls.map((l, i) => i === index ? { ...l, [key]: value } : l));
  }

  submitEntryForm(): void {
    if (!this.entryIsFormValid()) return;
    const id = this.entryEditingId();
    if (id === null) return;
    const lines: AccountingEntryLineRequest[] = this.entryFormLines().map(l => ({
      idAccount:       l.idAccount,
      debitAmount:     l.debitAmount,
      creditAmount:    l.creditAmount,
      descriptionLine: l.descriptionLine.trim() || null,
    }));
    this.editEntrySave.emit({
      id,
      idFiscalPeriod:   this.entryFormFiscalPeriod(),
      idCurrency:       this.entryFormCurrency(),
      numberEntry:      this.entryFormNumber().trim(),
      dateEntry:        this.entryFormDate(),
      descriptionEntry: this.entryFormDescription().trim(),
      statusEntry:      this.entryFormStatus(),
      referenceEntry:   this.entryFormReference().trim() || null,
      exchangeRateValue: this.entryFormExchangeRate(),
      lines,
    });
    this.cancelEntryForm();
  }

  // ── Panel de distribución contable por línea ─────────────────────
  expandedLineAccountIndex = signal<number | null>(null);
  lineAccountRows          = signal<FormLineAccount[]>([]);

  // Suma de porcentajes para validación
  lineAccountTotal = computed(() =>
    this.lineAccountRows().reduce((s, r) => s + (r.percentageAccount || 0), 0)
  );
  lineAccountValid = computed(() =>
    this.lineAccountRows().length > 0 &&
    this.lineAccountRows().every(r => r.idAccount > 0) &&
    Math.abs(this.lineAccountTotal() - 100) < 0.01
  );

  lineAccountMissingAccount = computed(() =>
    this.lineAccountRows().some(r => r.idAccount === 0)
  );

  // Sólo cuentas que permiten movimientos
  movementAccountsForLine = computed(() =>
    this.accounts().filter(a => a.allowsMovements && a.isActive)
  );

  /** Devuelve el idProduct que tiene el skuCode dado, o null si no se encuentra. */
  getProductIdForSkuCode(skuCode: string): number | null {
    if (!skuCode?.trim()) return null;
    const code = skuCode.trim().toLowerCase();
    // Lookup primario: productSKUs → products (relación por idProductSKU)
    const sku = this.productSKUs().find(s => s.codeProductSKU.toLowerCase() === code);
    if (sku) {
      const prod = this.products().find(p => p.skus?.some(s => s.idProductSKU === sku.idProductSKU));
      if (prod) return prod.idProduct;
    }
    // Fallback: buscar en productAccounts por codeProduct === skuCode
    // (al crear un producto nuevo se usa el skuCode como codeProduct)
    const fromAccount = this.productAccounts().find(pa => pa.codeProduct.toLowerCase() === code);
    return fromAccount?.idProduct ?? null;
  }

  /** Abre/cierra el panel de distribución contable para la línea indicada. */
  toggleLineAccounts(lineIndex: number): void {
    if (this.expandedLineAccountIndex() === lineIndex) {
      this.expandedLineAccountIndex.set(null);
      this.lineAccountRows.set([]);
      return;
    }
    const line      = this.formLines()[lineIndex];
    const idProduct = this.getProductIdForSkuCode(line.skuCode);
    // Si no hay producto asociado, abrir de todas formas para mostrar el aviso
    if (idProduct === null) {
      this.lineAccountRows.set([]);
      this.expandedLineAccountIndex.set(lineIndex);
      return;
    }
    // Carga distribuciones existentes para ese producto
    const existing = this.productAccounts()
      .filter(pa => pa.idProduct === idProduct)
      .map(pa => ({
        idProductAccount:  pa.idProductAccount,
        idAccount:         pa.idAccount,
        idCostCenter:      pa.idCostCenter,
        percentageAccount: pa.percentageAccount,
      }));
    this.lineAccountRows.set(existing.length ? existing : [this.emptyLineAccount()]);
    this.expandedLineAccountIndex.set(lineIndex);
  }

  private emptyLineAccount(): FormLineAccount {
    return { idProductAccount: null, idAccount: 0, idCostCenter: null, percentageAccount: 100 };
  }

  addLineAccount(): void {
    this.lineAccountRows.update(rows => [...rows, this.emptyLineAccount()]);
  }

  removeLineAccount(i: number): void {
    this.lineAccountRows.update(rows => rows.filter((_, idx) => idx !== i));
  }

  updateLineAccount<K extends keyof FormLineAccount>(i: number, key: K, value: FormLineAccount[K]): void {
    this.lineAccountRows.update(rows => rows.map((r, idx) => idx === i ? { ...r, [key]: value } : r));
  }

  /** Guarda la distribución contable de la línea expandida. */
  saveLineAccounts(lineIndex: number): void {
    const line      = this.formLines()[lineIndex];
    const idProduct = this.getProductIdForSkuCode(line.skuCode);

    // ── Producto NUEVO: no existe en catálogo → emitir creación completa ───────
    if (idProduct === null) {
      const rows = this.lineAccountRows();
      this.createProductWithAccounts.emit({
        skuCode: line.skuCode.trim(),
        skuName: line.descriptionLine.trim() || line.skuCode.trim(),
        accounts: rows.map(r => ({
          idAccount:         r.idAccount,
          idCostCenter:      r.idCostCenter,
          percentageAccount: r.percentageAccount,
        })),
      });
      this.expandedLineAccountIndex.set(null);
      this.lineAccountRows.set([]);
      return;
    }

    // ── Producto EXISTENTE: crear / actualizar / borrar distribuciones ─────────
    const rows     = this.lineAccountRows();
    const existing = this.productAccounts().filter(pa => pa.idProduct === idProduct);

    // Determinar registros a borrar (existentes que ya no están en los rows)
    const rowIds = new Set(rows.filter(r => r.idProductAccount !== null).map(r => r.idProductAccount!));
    existing
      .filter(pa => !rowIds.has(pa.idProductAccount))
      .forEach(pa => this.deleteProductAccount.emit(pa.idProductAccount));

    // Crear o actualizar
    rows.forEach(row => {
      if (row.idProductAccount === null) {
        this.createProductAccount.emit({
          idProduct,
          idAccount:         row.idAccount,
          idCostCenter:      row.idCostCenter,
          percentageAccount: row.percentageAccount,
        });
      } else {
        this.updateProductAccount.emit({
          id:                row.idProductAccount,
          idAccount:         row.idAccount,
          idCostCenter:      row.idCostCenter,
          percentageAccount: row.percentageAccount,
        });
      }
    });

    this.expandedLineAccountIndex.set(null);
    this.lineAccountRows.set([]);
  }
}
