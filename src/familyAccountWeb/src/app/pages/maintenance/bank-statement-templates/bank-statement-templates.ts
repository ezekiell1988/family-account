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
  AppSettings,
  LoggerService,
  BankStatementTemplateService,
  BankMovementTypeService,
  AccountService,
  CostCenterService,
} from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import {
  CreateBankStatementTemplateRequest,
  UpdateBankStatementTemplateRequest,
} from '../../../shared/models';
import { BankStatementTemplatesWebComponent } from './components/bank-statement-templates-web/bank-statement-templates-web.component';
import { BankStatementTemplatesMobileComponent } from './components/bank-statement-templates-mobile/bank-statement-templates-mobile.component';

@Component({
  selector: 'app-bank-statement-templates',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [BankStatementTemplatesWebComponent, BankStatementTemplatesMobileComponent],
  templateUrl: './bank-statement-templates.html',
})
export class BankStatementTemplatesPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly logger = inject(LoggerService).getLogger('BankStatementTemplatesPage');
  readonly svc             = inject(BankStatementTemplateService);
  readonly movTypeSvc      = inject(BankMovementTypeService);
  readonly accountSvc      = inject(AccountService);
  readonly costCenterSvc   = inject(CostCenterService);

  loading      = signal(false);
  errorMessage = signal('');

  items         = computed(() => this.svc.items());
  movementTypes = computed(() => this.movTypeSvc.items());
  accounts      = computed(() => this.accountSvc.accounts());
  costCenters   = computed(() => this.costCenterSvc.items());

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Plantillas de Extractos Bancarios');
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set('');
    forkJoin([
      this.svc.loadList(),
      this.movTypeSvc.loadList(),
      this.accountSvc.loadList(),
      this.costCenterSvc.loadList(),
    ]).subscribe({
      next: () => this.loading.set(false),
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('Error al cargar los datos');
      },
    });
  }

  onCreate(payload: CreateBankStatementTemplateRequest): void {
    this.svc.create(payload).subscribe({
      error: () => this.errorMessage.set('Error al crear la plantilla'),
    });
  }

  onEditSave(payload: UpdateBankStatementTemplateRequest & { id: number }): void {
    const { id, ...rest } = payload;
    this.svc.update(id, rest).subscribe({
      error: () => this.errorMessage.set('Error al actualizar la plantilla'),
    });
  }

  onRemove(id: number): void {
    this.svc.delete(id).subscribe({
      error: () => this.errorMessage.set('Error al eliminar la plantilla'),
    });
  }
}
