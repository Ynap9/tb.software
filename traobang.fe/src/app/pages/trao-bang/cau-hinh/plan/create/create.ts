import { IViewGiaoDien } from '@/models/traobang/giao-dien.models';
import { ICreateConfigPlan, IUpdateConfigPlan, PlanTrangThai } from '@/models/traobang/plan.models';
import { GiaoDienService } from '@/service/giao-dien.service';
import { TraoBangPlanService } from '@/service/plan.service';
import { BaseComponent } from '@/shared/components/base/base-component';
import { SharedImports } from '@/shared/import.shared';
import { Utils } from '@/shared/utils';
import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { TextareaModule } from 'primeng/textarea';
@Component({
    selector: 'app-create',
    imports: [SharedImports, TextareaModule],
    templateUrl: './create.html',
    styleUrl: './create.scss'
})
export class Create extends BaseComponent {
    private _ref = inject(DynamicDialogRef);
    private _config = inject(DynamicDialogConfig);
    private _planService = inject(TraoBangPlanService);
    private _giaoDienService = inject(GiaoDienService);

    listTrangThai = PlanTrangThai.ListTrangThai;

    listGiaoDien: IViewGiaoDien[] = [];

    override form: FormGroup = new FormGroup({
        id: new FormControl(null),
        ten: new FormControl('', [Validators.required]),
        trangThai: new FormControl(this.listTrangThai[0].code),
        moTa: new FormControl(''),
        time: new FormControl(null),
        idGiaoDien: new FormControl(null)
    });

    override ValidationMessages: Record<string, Record<string, string>> = {
        ten: {
            required: 'Không được bỏ trống'
        }
        // time: {
        //     required: 'Không được bỏ trống'
        // }
    };

    override ngOnInit(): void {
        this.getListGiaoDien();
        if (this.isUpdate) {
            this.form.setValue({
                id: this._config.data.id,
                ten: this._config.data.ten,
                moTa: this._config.data.moTa,
                trangThai: this._config.data.trangThai,
                idGiaoDien: this._config.data.idGiaoDien ?? null,
                time: [new Date(this._config.data.thoiGianBatDau), new Date(this._config.data.thoiGianKetThuc)]
            });
        }
    }

    get isUpdate() {
        return this._config.data?.id;
    }

    getListGiaoDien() {
        this._giaoDienService.getList().subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res)) {
                    this.listGiaoDien = res.data;
                }
            }
        });
    }

    onSubmit() {
        if (this.isFormInvalid()) {
            return;
        }

        if (this.isUpdate) {
            this.onSubmitUpdate();
        } else {
            this.onSubmitCreate();
        }
    }

    onSubmitCreate() {
        let from = null;
        let to = null;

        if (this.form.value['time']) {
            from = Utils.formatDateCallApi(this.form.value['time'][0]);
            to = this.form.value['time'][1] ? Utils.formatDateCallApi(this.form.value['time'][1]) : from;
        }

        const body: ICreateConfigPlan = {
            ten: this.form.value['ten'],
            moTa: this.form.value['moTa'],
            trangThai: this.form.value['trangThai'],
            idGiaoDien: this.form.value['idGiaoDien'] || undefined,
            thoiGianBatDau: from,
            thoiGianKetThuc: to
        };
        this.loading = true;
        this._planService.create(body).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res, true, 'Đã thêm chương trình')) {
                    this._ref?.close(true);
                }
            },
            error: (err) => {
                this.messageError(err?.message);
            },
            complete: () => {
                this.loading = false;
            }
        });
    }

    onSubmitUpdate() {
        const from = Utils.formatDateCallApi(this.form.value['time'][0]);
        const to = this.form.value['time'][1] ? Utils.formatDateCallApi(this.form.value['time'][1]) : from;

        const body: IUpdateConfigPlan = {
            id: this.form.value['id'],
            ten: this.form.value['ten'],
            moTa: this.form.value['moTa'],
            trangThai: this.form.value['trangThai'],
            idGiaoDien: this.form.value['idGiaoDien'] || undefined,
            thoiGianBatDau: from,
            thoiGianKetThuc: to
        };
        this.loading = true;
        this._planService.update(body).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res, true, 'Đã lưu')) {
                    this._ref?.close(true);
                }
            },
            error: (err) => {
                this.messageError(err?.message);
            },
            complete: () => {
                this.loading = false;
            }
        });
    }
}
