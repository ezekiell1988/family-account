import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { forkJoin } from 'rxjs';
import {
  AccountService,
  AppSettings,
  BankAccountService,
  BankMovementTypeService,
  BankStatementTemplateService,
  BankStatementImportService,
  CostCenterService,
  LoggerService,
} from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import {
  BankStatementImportDto,
  BankStatementTransactionDto,
  BulkClassifyItem,
} from '../../../shared/models';
import { BankStatementImportsWebComponent } from './components/bank-statement-imports-web/bank-statement-imports-web.component';
import { BankStatementImportsMobileComponent } from './components/bank-statement-imports-mobile/bank-statement-imports-mobile.component';

@Component({
  selector: 'app-bank-statement-imports',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [BankStatementImportsWebComponent, BankStatementImportsMobileComponent],
  templateUrl: './bank-statement-imports.html',
})
export class BankStatementImportsPage
  extends ResponsiveComponent
  implements OnInit, OnDestroy
{
  private readonly importSvc    = inject(BankStatementImportService);
  private readonly templateSvc  = inject(BankStatementTemplateService);
  private readonly accountSvc   = inject(BankAccountService);
  private readonly chartSvc     = inject(AccountService);
  private readonly movTypeSvc   = inject(BankMovementTypeService);
  private readonly costCenterSvc = inject(CostCenterService);
  private readonly logger       = inject(LoggerService).getLogger('BankStatementImportsPage');

  // ── Estado del servicio ───────────────────────────────────────────
  isLoading         = this.importSvc.isLoading;
  isUploading       = this.importSvc.isUploading;
  isBulkClassifying = this.importSvc.isBulkClassifying;
  error             = this.importSvc.error;
  imports           = this.importSvc.items;
  transactions      = this.importSvc.transactions;
  templates         = this.templateSvc.items;
  bankAccounts      = this.accountSvc.items;
  movementTypes     = this.movTypeSvc.items;
  chartAccounts     = this.chartSvc.accounts;
  costCenters       = this.costCenterSvc.items;

  // ── Estado local ───────────────────────────────────────────
  selectedImportId = signal<number | null>(null);
  showOnlyPending  = signal(false);

  // ── Derivados ──────────────────────────────────────────
  pendingCount = computed(() =>
    this.transactions().filter(t => !t.idBankMovementType && !t.idAccountingEntry).length
  );

  /** Transacciones visibles según el modo de filtrado activo */
  visibleTransactions = computed(() =>
    this.showOnlyPending()
      ? this.transactions().filter(t => !t.idBankMovementType && !t.idAccountingEntry)
      : this.transactions()
  );

  /** true mientras cuentas bancarias o plantillas siguen cargando */
  isLoadingCatalogs = computed(
    () => this.accountSvc.isLoading() || this.templateSvc.isLoading()
  );

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Cargas Bancarias');
    this.load();
  }

  load(): void {
    forkJoin([
      this.importSvc.loadList(),
      this.templateSvc.loadList(),
      this.accountSvc.loadList(),
      this.movTypeSvc.loadList(),
      this.chartSvc.loadList(),
      this.costCenterSvc.loadList(),
    ]).subscribe({
      error: err => this.logger.error('Error cargando datos iniciales', err),
    });
  }

  uploadFile(payload: { idBankAccount: number; idTemplate: number; file: File }): void {
    this.importSvc.upload(payload.idBankAccount, payload.idTemplate, payload.file).subscribe({
      next: imp => {
        this.logger.info(`Upload OK — importId=${imp.idBankStatementImport}`);
        if (imp.status !== 'Completado' && imp.status !== 'Error') {
          this.startPolling(imp.idBankStatementImport);
        }
      },
      error: err => this.logger.error('Error en upload', err),
    });
  }

  expandImport(imp: BankStatementImportDto): void {
    const id = imp.idBankStatementImport;
    if (this.selectedImportId() === id && !this.showOnlyPending()) {
      this.selectedImportId.set(null);
      this.showOnlyPending.set(false);
      this.importSvc.transactions.set([]);
      return;
    }
    this.showOnlyPending.set(false);
    this.selectedImportId.set(id);
    this.importSvc.loadTransactions(id).subscribe({
      error: err => this.logger.error('Error cargando transacciones', err),
    });
  }

  expandPendingImport(imp: BankStatementImportDto): void {
    const id = imp.idBankStatementImport;
    if (this.selectedImportId() === id && this.showOnlyPending()) {
      this.selectedImportId.set(null);
      this.showOnlyPending.set(false);
      this.importSvc.transactions.set([]);
      return;
    }
    this.showOnlyPending.set(true);
    this.selectedImportId.set(id);
    this.importSvc.loadTransactions(id).subscribe({
      error: err => this.logger.error('Error cargando transacciones (pendientes)', err),
    });
  }

  classify(_payload: { id: number; req: unknown }): void {
    // No-op: el frontend usa classify-batch para todo (individual y masivo).
    // El endpoint PATCH /classify sigue disponible para consumo directo (scripts/curl).
  }

  classifyBatch(items: BulkClassifyItem[]): void {
    const importId = this.selectedImportId();
    if (importId === null || items.length === 0) return;

    // Snapshot de las filas afectadas para revertir en caso de error
    const affected = new Set(items.map(i => i.idBankStatementTransaction));
    const snapshot = new Map(
      this.transactions()
        .filter(t => affected.has(t.idBankStatementTransaction))
        .map(t => [t.idBankStatementTransaction, { ...t }]),
    );

    this.importSvc.classifyBatch(importId, { items }).subscribe({
      next: res => {
        this.logger.info(`Bulk classify OK — clasificadas=${res.classified} keywords=${res.keywordsAdded}`);
        // Parchear el signal en memoria — sin recargar del API
        const itemMap = new Map(items.map(i => [i.idBankStatementTransaction, i]));
        this.importSvc.transactions.update(txs =>
          txs.map(t => {
            const patch = itemMap.get(t.idBankStatementTransaction);
            if (!patch) return t;
            return {
              ...t,
              idBankMovementType:   patch.idBankMovementType,
              idAccountCounterpart: patch.idAccountCounterpart ?? t.idAccountCounterpart,
              idCostCenter:         patch.idCostCenter ?? t.idCostCenter,
            };
          }),
        );
      },
      error: err => {
        this.logger.error('Error en bulk classify', err);
        // Revertir las filas afectadas al estado original
        this.importSvc.transactions.update(txs =>
          txs.map(t => snapshot.get(t.idBankStatementTransaction) ?? t),
        );
      },
    });
  }

  private startPolling(importId: number): void {
    const poll = () => {
      this.importSvc.pollById(importId).subscribe({
        next: imp => {
          if (imp.status !== 'Completado' && imp.status !== 'Error') {
            setTimeout(poll, 2000);
          }
        },
      });
    };
    setTimeout(poll, 2000);
  }
}
