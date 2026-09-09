import { BaseComponent } from '@/shared/components/base/base-component';
import { DataTable } from '@/shared/components/data-table/data-table';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { SharedImports } from '@/shared/import.shared';
import { IColumn } from '@/shared/models/data-table.models';
import { Component, inject } from '@angular/core';
import { PaginatorState } from 'primeng/paginator';
import { Create } from './create/create';
import { TblAction, TblActionTypes } from './tbl-action/tbl-action';
import { FormControl, FormGroup } from '@angular/forms';
import { IViewRowConfigSubPlan, IFindPagingConfigSubPlan } from '@/models/traobang/sub-plan.models';
import { TraoBangSubPlanService } from '@/service/sub-plan.service';
import { Upload } from './upload/upload';
import { IViewRowConfigPlan, PlanTrangThai } from '@/models/traobang/plan.models';
import { TraoBangPlanService } from '@/service/plan.service';
import { debounceTime, distinctUntilChanged } from 'rxjs';

@Component({
    selector: 'app-sub-plan',
    imports: [SharedImports, DataTable],
    templateUrl: './sub-plan.html',
    styleUrl: './sub-plan.scss'
})
export class SubPlan extends BaseComponent {
    _subPlanService = inject(TraoBangSubPlanService);
    _traoBangPlanService = inject(TraoBangPlanService);

    listPlanActive: IViewRowConfigPlan[] = [];

    searchForm: FormGroup = new FormGroup({
        search: new FormControl(''),
        idPlan: new FormControl('')
    });

    columns: IColumn[] = [
        { header: 'STT', cellViewType: CellViewTypes.INDEX, headerContainerStyle: 'width: 6rem' },
        { header: 'Tên khoa', field: 'ten', headerContainerStyle: 'min-width: 10rem' },
        { header: 'QR tên khoa', field: 'qrTenKhoa', headerContainerStyle: 'min-width: 10rem' },
        { header: 'Trưởng Khoa', field: 'truongKhoa', headerContainerStyle: 'min-width: 10rem' },
        { header: 'Thứ tự', field: 'order', headerContainerStyle: 'width: 10rem' },
        { header: 'Hiển thị', field: 'isShow', headerContainerStyle: 'width: 10rem', cellViewType: CellViewTypes.CHECKBOX },
        { header: 'Thao tác', headerContainerStyle: 'width: 6rem', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: TblAction }
    ];

    data: IViewRowConfigSubPlan[] = [];
    query: IFindPagingConfigSubPlan = {
        pageNumber: this.START_PAGE_NUMBER,
        pageSize: 20
    };

    override ngOnInit(): void {
        this.getListPlanActive();
        this.getData();

        // Debounce search input
        this.searchForm
            .get('search')
            ?.valueChanges.pipe(debounceTime(500), distinctUntilChanged())
            .subscribe(() => {
                this.getData();
            });
    }

    getListPlanActive() {
        this._traoBangPlanService.getList().subscribe((res) => {
            this.listPlanActive = res.data;
            this.searchForm.get('idPlan')?.patchValue(this.listPlanActive.find(e => e.trangThai == PlanTrangThai.DANG_HOAT_DONG)?.id)
            this.onSearch();
        });
    }

    onSearch() {
        this.getData();
    }

    onPageChanged($event: PaginatorState) {
        this.query.pageNumber = ($event.page ?? 0) + 1;
        this.getData();
    }

    getData() {
        this.loading = true;
        let dataFilter = { idPlan: this.searchForm.get('idPlan')?.value ?? '' };
        this._subPlanService
            .findPaging({ ...this.query, keyword: this.searchForm.get('search')?.value }, dataFilter)
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res, false)) {
                        this.data = res.data.items;
                        this.totalRecords = res.data.totalItems;
                    }
                }
            })
            .add(() => {
                this.loading = false;
            });
    }

    onOpenCreate() {
        const ref = this._dialogService.open(Create, { header: 'Tạo khoa', closable: true, modal: true, styleClass: 'w-[700px]', focusOnShow: false });
        ref.onClose.subscribe((result) => {
            if (result) {
                this.getData();
            }
        });
    }

    onOpenUpdate(data: IViewRowConfigSubPlan) {
        const ref = this._dialogService.open(Create, { header: 'Cập nhật khoa', closable: true, modal: true, styleClass: 'w-[700px]', focusOnShow: false, data });
        ref.onClose.subscribe((result) => {
            if (result) {
                this.getData();
            }
        });
    }

    onDelete(data: IViewRowConfigSubPlan) {
        this.confirmDelete(
            {
                header: 'Bạn chắc chắn muốn xóa khoa?',
                message: 'Không thể khôi phục sau khi xóa'
            },
            () => {
                this._subPlanService.delete(data.id || 0, data.idPlan || 0).subscribe(
                    (res) => {
                        if (this.isResponseSucceed(res, true, 'Đã xóa')) {
                            this.getData();
                        }
                    },
                    (err) => {
                        this.messageError(err?.message);
                    }
                );
            }
        );
    }

    onCustomEmit(data: { type: string; data: IViewRowConfigSubPlan; field?: string }) {
        if (data.type === TblActionTypes.update) {
            this.onOpenUpdate(data.data);
        } else if (data.type === TblActionTypes.delete) {
            this.onDelete(data.data);
        }
    }

    downloadTemplate() {
        this.loading = true;
        this._subPlanService.downloadFileTemplate().subscribe({
            next: (res: Blob) => {
                if (res) {
                    const blob = new Blob([res], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
                    const url = window.URL.createObjectURL(blob);
                    const link = document.createElement('a');
                    link.href = url;
                    link.download = 'subplan_template.xlsx';
                    link.click();
                    window.URL.revokeObjectURL(url);
                }
            },
            error: (err) => {
                this.messageError(err?.message || 'Có lỗi xảy ra khi tải template');
            },
            complete: () => {
                this.loading = false;
            }
        });
    }
    onUpload() {
        const ref = this._dialogService.open(Upload, { header: 'Import từ file', closable: true, modal: true, styleClass: 'w-[700px]', focusOnShow: false });
        ref.onClose.subscribe((result) => {
            if (result) {
                this.getData();
            }
        });
    }
}
