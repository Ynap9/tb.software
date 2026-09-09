
import { IViewRowConfigPlan } from '@/models/traobang/plan.models';
import { ICreateConfigSubPlan, IUpdateConfigSubPlan } from '@/models/traobang/sub-plan.models';
import { TraoBangPlanService } from '@/service/plan.service';
import { TraoBangSubPlanService } from '@/service/sub-plan.service';
import { BaseComponent } from '@/shared/components/base/base-component';
import { SharedImports } from '@/shared/import.shared';
import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';

@Component({
    selector: 'app-create',
    imports: [SharedImports],
    templateUrl: './create.html',
    styleUrl: './create.scss'
})
export class Create extends BaseComponent {
    private _ref = inject(DynamicDialogRef);
    private _config = inject(DynamicDialogConfig);
    private _subPlanService = inject(TraoBangSubPlanService);
    private _planService = inject(TraoBangPlanService);

    listPlan: IViewRowConfigPlan[] = [];

    override form: FormGroup = new FormGroup({
        id: new FormControl(null),
        idPlan: new FormControl(null, [Validators.required]),
        ten: new FormControl('', [Validators.required]),
        moTa: new FormControl(''),
        truongKhoa: new FormControl(''),
        qrTenKhoa: new FormControl(''),
        note: new FormControl(''),
        moBai: new FormControl(''),
        ketBai: new FormControl(''),
        isShow: new FormControl(true),
        isShowMoBai: new FormControl(true),
        isShowKetBai: new FormControl(true),
        order: new FormControl(1, [Validators.required])
    });

    override ValidationMessages: Record<string, Record<string, string>> = {
        idPlan: {
            required: 'Không được bỏ trống'
        },
        ten: {
            required: 'Không được bỏ trống'
        },
        order: {
            required: 'Không được bỏ trống'
        }
    };

    override ngOnInit(): void {
        this.getListPlan();
        if (this.isUpdate) {
            this.form.setValue({
                ...this._config.data
            });
        }
    }

    get isUpdate() {
        return this._config.data?.id;
    }

    getListPlan() {
        this._planService.getList().subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res)) {
                    this.listPlan = res.data
                }
            }
        })
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
        const body: ICreateConfigSubPlan = {
            ...this.form.value,
        };
        this.loading = true;
        this._subPlanService.create(body).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res, true, 'Đã thêm khoa')) {
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
        const body: IUpdateConfigSubPlan = {
            idSubPlan: this._config.data.id,
            ...this.form.value,
            newOrder: this.form.value['order'],
        };
        this.loading = true;
        this._subPlanService.update(body).subscribe({
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
