import { SlideService } from '@/service/slide.service';
import { BaseComponent } from '@/shared/components/base/base-component';
import { DataTable } from '@/shared/components/data-table/data-table';
import { CellViewTypes } from '@/shared/constants/data-table.constants';
import { SharedImports } from '@/shared/import.shared';
import { IColumn } from '@/shared/models/data-table.models';
import { Component, inject } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { TblAction, TblActionTypes } from './tbl-action/tbl-action';
import { IFindPagingSlide, IViewRowSlide, listLoaiSlide } from '@/models/traobang/slide.models';
import { PaginatorState } from 'primeng/paginator';
import { SvNhanBangStatuses } from '@/shared/constants/sv-nhan-bang.constants';
import { Create } from './create/create';
import { FileUploadModule } from 'primeng/fileupload';
import { Upload } from './upload/upload';
import { TraoBangPlanService } from '@/service/plan.service';
import { IViewRowConfigPlan, PlanTrangThai } from '@/models/traobang/plan.models';
import { TraoBangSubPlanService } from '@/service/sub-plan.service';
import { IViewRowConfigSubPlan } from '@/models/traobang/sub-plan.models';
import { GenQrCode } from './gen-qr-code/gen-qr-code';
import { ViewQr } from './view-qr/view-qr';
@Component({
    selector: 'app-slide',
    imports: [SharedImports, DataTable, FileUploadModule],
    templateUrl: './slide.html',
    styleUrl: './slide.scss'
})
export class SlideScreen extends BaseComponent {
    _slideService = inject(SlideService);
    _traoBangPlanService = inject(TraoBangPlanService);
    _subPlanService = inject(TraoBangSubPlanService);

    searchForm: FormGroup = new FormGroup({
        search: new FormControl(''),
        idPlan: new FormControl(''),
        idSubPlan: new FormControl('')
    });
    dataFilter: any = {};
    listPlanActive: IViewRowConfigPlan[] = [];
    listSubPlan: IViewRowConfigSubPlan[] = [];
    listLoaiSlide = listLoaiSlide;

    columns: IColumn[] = [
        { header: 'STT', cellViewType: CellViewTypes.INDEX, headerContainerStyle: 'width: 6rem' },
        { header: 'Nội dung', field: 'noiDung', headerContainerStyle: 'min-width: 10rem' },
        { header: 'Mã Sinh viên', field: 'sinhVien.maSoSinhVien', headerContainerStyle: 'min-width: 10rem' },
        { header: 'QR', field: 'sinhVien.linkQR', headerContainerStyle: 'min-width: 10rem', cellClass: 'break-all', clickable: true },
        { header: 'Note', field: 'note', headerContainerStyle: 'min-width: 10rem' },
        {
            header: 'Loại Slide',
            field: 'loaiSlideName',
            headerContainerStyle: 'min-width: 10rem',
            cellViewType: CellViewTypes.STATUS,
            statusSeverityFunction: (rowData: IViewRowSlide) => {
                return SvNhanBangStatuses.getName(rowData.loaiSlide ?? 0);
            }
        },
        {
            header: 'Trạng thái',
            field: 'trangThaiText',
            headerContainerStyle: 'width: 8rem',
            cellViewType: CellViewTypes.STATUS,
            statusSeverityFunction: (rowData: IViewRowSlide) => {
                return SvNhanBangStatuses.getName(rowData.trangThai ?? 0);
            }
        },
        { header: 'Thao tác', headerContainerStyle: 'width: 6rem', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: TblAction }
    ];

    data: IViewRowSlide[] = [];
    query: IFindPagingSlide = {
        pageNumber: this.START_PAGE_NUMBER,
        pageSize: this.MAX_PAGE_SIZE
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
            this.searchForm.get('idPlan')?.patchValue(this.listPlanActive.find((e) => e.trangThai == PlanTrangThai.DANG_HOAT_DONG)?.id);
            this.onChangeSelectPlan()
        });
    }

    onChangeSelectPlan() {
        this.getListSubPlan(this.searchForm.get('idPlan')?.value ?? '');
        this.dataFilter = { idPlan: this.searchForm.get('idPlan')?.value ?? '', idSubPlan: this.searchForm.get('idSubPlan')?.value ?? '' };
        this.getData();
    }

    onChangeSelectSubPlan() {
        this.dataFilter = { idPlan: this.searchForm.get('idPlan')?.value ?? '', idSubPlan: this.searchForm.get('idSubPlan')?.value ?? '' };
        this.getData();
    }

    getListSubPlan(id: number) {
        this._subPlanService.getListSubPlanByPlanActiveId(id).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res)) {
                    this.listSubPlan = res.data;
                }
            }
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

        this._slideService
            .findPaging({ ...this.query, keyword: this.searchForm.get('search')?.value }, this.dataFilter)
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res, false)) {
                        this.data = res.data.items.map((item) => {
                            let loai = this.listLoaiSlide.find((x) => x.code == item.loaiSlide);
                            let trangThaiText = SvNhanBangStatuses.List.find((x) => x.code == item.trangThai);
                            return {
                                ...item,
                                loaiSlideName: loai?.name ?? '',
                                trangThaiText: trangThaiText?.name
                            };
                        });
                        // console.log(this.data);
                        this.totalRecords = res.data.totalItems;
                    }
                },
            })
            .add(() => {
                this.loading = false;
            });
    }



    onOpenCreate() {
        const ref = this._dialogService.open(Create, { header: 'Tạo Slide', closable: true, modal: true, styleClass: 'w-[700px]', focusOnShow: false });
        ref.onClose.subscribe((result) => {
            if (result) {
                this.getData();
            }
        });
    }

    onCustomEmit(data: { type: string; data: IViewRowSlide; field?: string }) {
        if (data.type === TblActionTypes.update) {
            this.onOpenUpdate(data.data);
        } else if (data.type === TblActionTypes.delete) {
            this.onDelete(data.data);
        }
        else if (data.type === TblActionTypes.qrCode) {
            this.genQrCode(data.data)
        }
        else if (data.type === 'cellClick' && data.field === 'sinhVien.linkQR') {
            this.onViewQr(data.data);
        }
    }

    onViewQr(data: IViewRowSlide) {
        if (!data.sinhVien?.linkQR) {
            return;
        }

        this._dialogService.open(ViewQr, {
            header: 'Mã QR',
            closable: true,
            modal: true,
            styleClass: 'w-[500px]',
            focusOnShow: false,
            data: { linkQR: data.sinhVien.linkQR }
        });
    }

    onOpenUpdate(data: IViewRowSlide) {
        const ref = this._dialogService.open(Create, { header: 'Cập nhật Slide', closable: true, modal: true, styleClass: 'w-[700px]', focusOnShow: false, data });
        ref.onClose.subscribe((result) => {
            if (result) {
                this.getData();
            }
        });
    }

    onDelete(data: IViewRowSlide) {
        this.confirmDelete(
            {
                header: 'Bạn chắc chắn muốn xóa SV?',
                message: 'Không thể khôi phục sau khi xóa'
            },
            () => {
                this._slideService.delete(data.id || 0).subscribe(
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

    downloadTemplate() {
        this.loading = true;
        this._slideService.downloadFileTemplate().subscribe({
            next: (res: Blob) => {
                if (res) {
                    const blob = new Blob([res], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
                    const url = window.URL.createObjectURL(blob);
                    const link = document.createElement('a');
                    link.href = url;
                    link.download = 'slide_template.xlsx';
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

    onGenQR() {
        const ref = this._dialogService.open(GenQrCode, { header: 'Tạo mã QR', closable: true, modal: true, styleClass: 'w-[700px]', focusOnShow: false });
        ref.onClose.subscribe((result) => {
            if (result) {
                this.getData();
            }
        });
    }

    genQrCode(data: IViewRowSlide) {
        this._slideService.genQrCode(data.id).subscribe(
            (res) => {
                if (this.isResponseSucceed(res, true, 'Đã tạo mã QR')) {
                    this.getData();
                }
            },
            (err) => {
                this.messageError(err?.message);
            }
        );
    }
}
