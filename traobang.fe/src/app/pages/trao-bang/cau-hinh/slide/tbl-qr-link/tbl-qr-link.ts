import { BaseComponent } from '@/shared/components/base/base-component';
import { TBL_CUSTOM_COMP_EMIT } from '@/shared/components/data-table/data-table';
import { SharedImports } from '@/shared/import.shared';
import { Component, inject, Input } from '@angular/core';

export const TblQrLinkTypes = {
    viewQr: 'viewQr',
    viewQrOnly: 'viewQrOnly'
}

@Component({
    selector: 'app-tbl-qr-link',
    imports: [SharedImports],
    templateUrl: 'tbl-qr-link.html'
})
export class TblQrLink extends BaseComponent {
    tblEmit = inject(TBL_CUSTOM_COMP_EMIT);

    @Input() row: any = {};
    @Input() rowIndex: number = 0;
    @Input() data: any;

    actionType = TblQrLinkTypes;

    onClick(customType: string): void {
        this.tblEmit.emit({
            data: this.row,
            type: customType
        });
    }
}
