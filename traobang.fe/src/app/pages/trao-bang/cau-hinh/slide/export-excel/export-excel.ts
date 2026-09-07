import { IViewRowConfigPlan } from '@/models/traobang/plan.models';
import { TraoBangPlanService } from '@/service/plan.service';
import { SlideService } from '@/service/slide.service';
import { BaseComponent } from '@/shared/components/base/base-component';
import { SharedImports } from '@/shared/import.shared';
import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { DynamicDialogRef } from 'primeng/dynamicdialog';

@Component({
    selector: 'app-export-excel',
    imports: [SharedImports],
    templateUrl: './export-excel.html'
})
export class ExportExcel extends BaseComponent {
    private _ref = inject(DynamicDialogRef);
    _slideService = inject(SlideService);
    _planService = inject(TraoBangPlanService);

    listPlan: IViewRowConfigPlan[] = [];

    override form: FormGroup = new FormGroup({
        idPlan: new FormControl('', [Validators.required])
    });

    override ValidationMessages: Record<string, Record<string, string>> = {
        idPlan: {
            required: 'Vui lòng chọn chương trình'
        }
    };

    override ngOnInit(): void {
        this.getListPlan();
    }

    getListPlan() {
        this._planService.getList().subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res)) {
                    this.listPlan = res.data;
                }
            }
        });
    }

    onSubmit() {
        if (this.isFormInvalid()) {
            this.messageError('Vui lòng điền đầy đủ thông tin bắt buộc');
            return;
        }
        this.loading = true;
        this._slideService.exportSlideSinhVien({ idPlan: this.form.get('idPlan')?.value }).subscribe({
            next: (res: Blob) => {
                if (res) {
                    const blob = new Blob([res], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
                    const url = window.URL.createObjectURL(blob);
                    const link = document.createElement('a');
                    link.href = url;
                    link.download = 'slide_sinh_vien.xlsx';
                    link.click();
                    window.URL.revokeObjectURL(url);
                    this._ref?.close();
                }
            },
            error: (err) => {
                this.messageError(err?.message || 'Có lỗi xảy ra khi xuất excel');
            },
            complete: () => {
                this.loading = false;
            }
        });
    }
}
