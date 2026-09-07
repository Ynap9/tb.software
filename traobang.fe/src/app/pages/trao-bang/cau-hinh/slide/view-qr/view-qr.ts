import { BaseComponent } from '@/shared/components/base/base-component';
import { SharedImports } from '@/shared/import.shared';
import { Component, inject } from '@angular/core';
import { DynamicDialogConfig } from 'primeng/dynamicdialog';

@Component({
    selector: 'app-view-qr',
    imports: [SharedImports],
    templateUrl: './view-qr.html'
})
export class ViewQr extends BaseComponent {
    private _config = inject(DynamicDialogConfig);

    linkQR: string = '';

    override ngOnInit(): void {
        this.linkQR = this._config.data?.linkQR ?? '';
    }
}
