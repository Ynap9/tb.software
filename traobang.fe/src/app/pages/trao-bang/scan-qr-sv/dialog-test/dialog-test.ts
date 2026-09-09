import { BaseComponent } from '@/shared/components/base/base-component';
import { SharedImports } from '@/shared/import.shared';
import { Component, inject } from '@angular/core';
import { DynamicDialogRef } from 'primeng/dynamicdialog';

@Component({
  selector: 'app-dialog-test',
  imports: [SharedImports],
  templateUrl: './dialog-test.html',
  styleUrl: './dialog-test.scss'
})
export class DialogTest extends BaseComponent {
  private _ref = inject(DynamicDialogRef);

  onChon(isCheckinFull: boolean) {
    this._ref?.close(isCheckinFull);
  }
}
