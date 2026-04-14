import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import {
  KeywordRule,
  BankMovementTypeDto,
  AccountDto,
  CostCenterDto,
} from '../../../../../shared/models';

@Component({
  selector: 'app-bank-statement-templates-keyword-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './bank-statement-templates-keyword-editor.component.html',
})
export class BankStatementTemplatesKeywordEditorComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  keywordRulesJson = input<string | null | undefined>(null);
  movementTypes    = input<BankMovementTypeDto[]>([]);
  accounts         = input<AccountDto[]>([]);
  costCenters      = input<CostCenterDto[]>([]);

  // ── Output ────────────────────────────────────────────────────────
  rulesChange = output<string>();

  // ── Estado local ─────────────────────────────────────────────────
  /** Reglas parseadas del JSON de entrada */
  private parsedRules = computed<KeywordRule[]>(() => {
    const raw = this.keywordRulesJson();
    if (!raw) return [];
    try { return JSON.parse(raw) as KeywordRule[]; } catch { return []; }
  });

  /** Copia mutable de las reglas para edición local */
  rules = signal<KeywordRule[]>([]);

  /** Nuevo keyword que se está escribiendo en cada fila (indexado por posición) */
  newKeywordInputs = signal<string[]>([]);

  /** Control de qué fila tiene el input de keyword activo */
  activeKeywordRow = signal<number | null>(null);

  ngOnInit(): void {
    this.syncFromInput();
  }

  /** Sincroniza las reglas locales desde el input JSON */
  syncFromInput(): void {
    const r = this.parsedRules().map(rule => ({ ...rule, keywords: [...rule.keywords] }));
    this.rules.set(r);
    this.newKeywordInputs.set(r.map(() => ''));
  }

  // ── Helpers de lookup ─────────────────────────────────────────────
  movTypeName(id: number): string {
    return this.movementTypes().find(t => t.idBankMovementType === id)?.nameBankMovementType ?? `(${id})`;
  }

  accountName(id: number | null | undefined): string {
    if (!id) return '—';
    const a = this.accounts().find(a => a.idAccount === id);
    return a ? `${a.codeAccount} – ${a.nameAccount}` : `(${id})`;
  }

  costCenterName(id: number | null | undefined): string {
    if (!id) return '—';
    const c = this.costCenters().find(c => c.idCostCenter === id);
    return c ? c.nameCostCenter : `(${id})`;
  }

  // ── Edición de reglas ─────────────────────────────────────────────
  addRule(): void {
    const blank: KeywordRule = {
      keywords:           [],
      idBankMovementType: 0,
      matchMode:          'Any',
    };
    this.rules.update(rs => [...rs, blank]);
    this.newKeywordInputs.update(ns => [...ns, '']);
  }

  removeRule(index: number): void {
    this.rules.update(rs => rs.filter((_, i) => i !== index));
    this.newKeywordInputs.update(ns => ns.filter((_, i) => i !== index));
    this.emit();
  }

  setMovType(index: number, value: string): void {
    this.rules.update(rs => {
      const copy = [...rs];
      copy[index] = { ...copy[index], idBankMovementType: Number(value) };
      return copy;
    });
    this.emit();
  }

  setAccount(index: number, value: string): void {
    const id = value === '' ? null : Number(value);
    this.rules.update(rs => {
      const copy = [...rs];
      copy[index] = { ...copy[index], idAccountCounterpart: id ?? undefined };
      return copy;
    });
    this.emit();
  }

  setCostCenter(index: number, value: string): void {
    const id = value === '' ? null : Number(value);
    this.rules.update(rs => {
      const copy = [...rs];
      copy[index] = { ...copy[index], idCostCenter: id ?? undefined };
      return copy;
    });
    this.emit();
  }

  // ── Gestión de keywords ───────────────────────────────────────────
  setNewKeyword(index: number, value: string): void {
    this.newKeywordInputs.update(ns => {
      const copy = [...ns];
      copy[index] = value;
      return copy;
    });
  }

  addKeyword(index: number): void {
    const kw = (this.newKeywordInputs()[index] ?? '').trim().toUpperCase();
    if (!kw) return;
    this.rules.update(rs => {
      const copy = [...rs];
      const rule = copy[index];
      if (!rule.keywords.includes(kw)) {
        copy[index] = { ...rule, keywords: [...rule.keywords, kw] };
      }
      return copy;
    });
    this.newKeywordInputs.update(ns => {
      const copy = [...ns];
      copy[index] = '';
      return copy;
    });
    this.emit();
  }

  onKeywordInputKeydown(event: KeyboardEvent, index: number): void {
    if (event.key === 'Enter' || event.key === ',') {
      event.preventDefault();
      this.addKeyword(index);
    }
  }

  removeKeyword(ruleIndex: number, kwIndex: number): void {
    this.rules.update(rs => {
      const copy = [...rs];
      const rule = copy[ruleIndex];
      copy[ruleIndex] = { ...rule, keywords: rule.keywords.filter((_, i) => i !== kwIndex) };
      return copy;
    });
    this.emit();
  }

  // ── Serialización ─────────────────────────────────────────────────
  private emit(): void {
    const clean = this.rules().map(r => {
      const out: KeywordRule = { keywords: r.keywords, idBankMovementType: r.idBankMovementType, matchMode: 'Any' };
      if (r.idAccountCounterpart) out.idAccountCounterpart = r.idAccountCounterpart;
      if (r.idCostCenter)         out.idCostCenter          = r.idCostCenter;
      if (r.regex)                out.regex                  = r.regex;
      return out;
    });
    this.rulesChange.emit(JSON.stringify(clean));
  }
}
