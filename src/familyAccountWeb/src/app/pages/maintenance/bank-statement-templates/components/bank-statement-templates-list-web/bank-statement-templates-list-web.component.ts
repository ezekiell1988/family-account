import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgxDatatableModule } from '@swimlane/ngx-datatable';
import { PanelComponent } from '../../../../../components';
import { BankStatementTemplateDto } from '../../../../../shared/models';

@Component({
  selector: 'app-bank-statement-templates-list-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, NgxDatatableModule, PanelComponent],
  templateUrl: './bank-statement-templates-list-web.component.html',
})
export class BankStatementTemplatesListWebComponent {
  items = input<BankStatementTemplateDto[]>([]);
  loading = input(false);
  errorMessage = input('');
  select = output<number>();
  create = output<void>();
  edit = output<number>();
  remove = output<number>();
}
