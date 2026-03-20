import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CampaignResult } from '../../service/notification.service';

@Component({
  selector: 'app-campaign-result-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (show && result) {
      <div class="modal d-block" tabindex="-1" style="background: rgba(0, 0, 0, 0.5);" (click)="closed.emit()">
        <div class="modal-dialog modal-dialog-centered" (click)="$event.stopPropagation()">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Resultado de campana</h5>
              <button type="button" class="btn-close" (click)="closed.emit()"></button>
            </div>
            <div class="modal-body">
              <pre class="mb-0 small">{{ result | json }}</pre>
            </div>
          </div>
        </div>
      </div>
    }
  `,
})
export class CampaignResultModalComponent {
  @Input() show = false;
  @Input() result: CampaignResult | null = null;
  @Output() closed = new EventEmitter<void>();
}