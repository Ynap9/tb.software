import { BaseComponent } from '@/shared/components/base/base-component';
import { SharedImports } from '@/shared/import.shared';
import { Component, inject, OnDestroy } from '@angular/core';
import { TableModule } from 'primeng/table';
import { LeftSidebar } from './left-sidebar/left-sidebar';
import { Header } from './header/header';
import { StudentList } from './student-list/student-list';
import { Footer } from './footer/footer';

import { DialogMssv } from './dialog-mssv/dialog-mssv';
import { DialogTest } from './dialog-test/dialog-test';

import { SubPlanStatuses, SvNhanBangStatuses, TraoBangHubConst } from '@/shared/constants/sv-nhan-bang.constants';
import * as signalR from '@microsoft/signalr';

import { NgIcon } from '@ng-icons/core';
import { TagModule } from 'primeng/tag';
import { IViewScanQrCurrentSubPlan, IViewScanQrTienDoSv, IViewScanQrSubPlan, ISlideItem } from '@/models/traobang/sv-nhan-bang.models';
import { ScanQrService } from '@/service/scan-qr.service';
import { TraoBangSvService } from '@/service/sv-nhan-bang.service';
import { SystemTraoBangService } from '@/service/system-trao-bang';
import { SlideList } from './slide-list/slide-list';
import { SlideDragDropService } from '@/service/slide-drag-drop.service';

@Component({
    selector: 'app-scan-qr-sv',
    imports: [SharedImports, TableModule, LeftSidebar, Header, StudentList, Footer, NgIcon, TagModule, SlideList],
    templateUrl: './scan-qr-sv.html',
    styleUrl: './scan-qr-sv.scss'
})
export class ScanQrSv extends BaseComponent implements OnDestroy {
    hubConnection: signalR.HubConnection | undefined;
    _svTraoBangService = inject(TraoBangSvService);
    _scanQrService = inject(ScanQrService);
    _slideDragService = inject(SlideDragDropService);
    _systemService = inject(SystemTraoBangService);

    idSubPlan: number = 0;
    currentSubPlanInfo: IViewScanQrCurrentSubPlan | undefined = undefined;
    students: IViewScanQrTienDoSv[] = [];
    listSubPlan: IViewScanQrSubPlan[] = [];
    pushedSuccessSv: IViewScanQrTienDoSv = {};
    listSlide: ISlideItem[] = [];
    removingFirstSlide = false;
    highlightLastStudent = false;
    loadingTest = false;

    override ngOnInit(): void {
        this.initData();
        this.connectHub();
        this._scanQrService.addListener((mssv) => {
            this.pushHangDoi(mssv);
        });
    }

    initData() {
        this._svTraoBangService.getQrListSubPlan(1).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res)) {
                    this.listSubPlan = res.data;
                    const dangTrao = this.listSubPlan.find((x) => x.trangThai === SubPlanStatuses.DANG_TRAO_BANG);
                    if (typeof dangTrao !== 'undefined') {
                        this.idSubPlan = dangTrao.id || 0;
                    } else {
                        this.idSubPlan = 0;
                    }
                    this.getCurrentSubPlan();
                    this.getHangDoi();
                }
            }
        });
    }

    getListSubPlan() {
        this._svTraoBangService.getQrListSubPlan(1).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res)) {
                    this.listSubPlan = res.data;
                    const dangTrao = this.listSubPlan.find((x) => x.trangThai === SubPlanStatuses.DANG_TRAO_BANG);
                    if (typeof dangTrao !== 'undefined') {
                        this.idSubPlan = dangTrao.id || 0;
                    } else {
                        this.idSubPlan = 1;
                    }
                }
            }
        });
    }

    cutSlide() {
        this.removingFirstSlide = true;
        setTimeout(() => {
            this._svTraoBangService.cutSlideThuong(this.idSubPlan).subscribe({
                next: (res) => {
                    this.removingFirstSlide = false;
                    if (this.isResponseSucceed(res, true, 'Chuyển loại slide thành công ')) {
                        this.currentSubPlanInfo = res.data;
                        this.getCurrentSubPlan();
                        this._svTraoBangService.getHangDoi({ SoLuong: 7 }).subscribe({
                            next: (hangDoiRes) => {
                                if (this.isResponseSucceed(hangDoiRes)) {
                                    this.students = hangDoiRes.data;
                                    this.highlightLastStudent = true;
                                    setTimeout(() => {
                                        this.highlightLastStudent = false;
                                    }, 420);
                                } else {
                                    this.students = [];
                                }
                            }
                        });
                    }
                },
                error: () => {
                    this.removingFirstSlide = false;
                }
            });
        }, 260);
    }

    getCurrentSubPlan() {
        this._svTraoBangService.getCurrentSubPlanById(this.idSubPlan).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res)) {
                    this.currentSubPlanInfo = res.data;
                }
            }
        });
    }

    getHangDoi() {
        this._svTraoBangService.getHangDoi({ SoLuong: 7 }).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res)) {
                    this.students = res.data;
                } else {
                    this.students = [];
                }
            }
        });
    }

    // gỡ khỏi hàng đợi: slide text được gỡ sẽ quay lại danh sách slide chờ nên phải load lại cả thông tin subplan
    onRevertTienDo() {
        this.getHangDoi();
        this.getCurrentSubPlan();
    }

    // check sv đã trong hàng đợi chưa, có rồi thì hỏi lại trước khi đẩy vào tiếp
    checkHangDoi(mssv: string, callback: () => void) {
        this.loading = true;
        this._svTraoBangService
            .checkHangDoi(mssv)
            .subscribe({
                next: (res) => {
                    if (!this.isResponseSucceed(res)) {
                        return;
                    }

                    const trangThai = res.data;
                    if (!trangThai) {
                        callback();
                        return;
                    }

                    this._confirmationService.confirm({
                        header: 'Sinh viên đã trong hàng đợi',
                        message: `SV này đang trong hàng đợi. Trạng thái: ${SvNhanBangStatuses.getName(trangThai)}`,
                        closable: true,
                        closeOnEscape: true,
                        rejectButtonProps: {
                            label: 'Hủy',
                            severity: 'seconday',
                            outlined: true
                        },
                        acceptButtonProps: {
                            label: 'Ok',
                            severity: 'primary'
                        },
                        accept: () => {
                            callback();
                        }
                    });
                }
            })
            .add(() => {
                this.loading = false;
            });
    }

    pushHangDoi(mssv: string) {
        this.checkHangDoi(mssv, () => this.pushHangDoiXacNhan(mssv));
    }

    pushHangDoiXacNhan(mssv: string) {
        this.loading = true;
        this._svTraoBangService
            .pushHangDoi(mssv)
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res)) {
                        this.pushedSuccessSv = res.data;
                        this.getHangDoi();
                        this.getCurrentSubPlan();
                    }
                }
            })
            .add(() => {
                this.loading = false;
            });
    }

    pushHangDoiDacBiet(mssv: string) {
        this.checkHangDoi(mssv, () => this.pushHangDoiDacBietXacNhan(mssv));
    }

    pushHangDoiDacBietXacNhan(mssv: string) {
        this.loading = true;
        this._svTraoBangService
            .pushHangDoiTruongHopDacBiet(mssv)
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res)) {
                        this.pushedSuccessSv = res.data;
                        this.getHangDoi();
                        this.getCurrentSubPlan();
                    }
                }
            })
            .add(() => {
                this.loading = false;
            });
    }

    onResetData() {
        this.confirmAction(
            {
                header: 'Xóa hết dữ liệu',
                message: `Xóa hết dữ liệu?`
            },
            () => {
                // this.callApiImport();
                this._systemService.restart().subscribe({
                    next: (res) => {
                        if (this.isResponseSucceed(res, true, 'Đã clear data')) {
                            this.initData();
                        }
                    }
                });
            }
        );
    }

    onOpenDialogMssv() {
        const ref = this._dialogService.open(DialogMssv, { header: 'Chuyển SV vào hàng đợi', closable: true, modal: true, styleClass: 'w-[500px]', focusOnShow: false });
        ref.onClose.subscribe((mssv) => {
            if (mssv) {
                this.pushHangDoiDacBiet(mssv);
            }
        });
    }

    onChangeSubPlan(idSubPlan: number | null | undefined) {
        if (idSubPlan) {
            this.loading = true;
            this._svTraoBangService
                .backToDaTraoBangSubPlan(idSubPlan)
                .subscribe({
                    next: (res) => {
                        if (this.isResponseSucceed(res)) {
                            this.idSubPlan = idSubPlan;
                            this.getListSubPlan();
                            this.getHangDoi();
                            this.getCurrentSubPlan();
                        }
                    }
                })
                .add(() => {
                    this.loading = false;
                });
        }
    }

    onNextSubPlan() {
        this.loading = true;
        this._svTraoBangService
            .nextSubPlan()
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res)) {
                        this.initData();
                    }
                }
            })
            .add(() => {
                this.loading = false;
            });
    }

    connectHub() {
        const hubUrl = TraoBangHubConst.HUB;
        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .build();

        this.hubConnection.on(TraoBangHubConst.ReceiveSinhVienDangTrao, (...args) => {
            const idSubPlan = args[0];

            // if (!idSubPlan) return;

            this.initData();
        });

        this.hubConnection.start().then();
    }

    ngOnDestroy(): void {
        this.hubConnection?.stop().then();
        this._scanQrService.clearListener();
    }

    onDragDropStudent(data: any) {
        this.loading = true
        this._slideDragService.onChangeOrderSlideStudent(data)
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res)) {
                        this.getHangDoi()
                    }
                }
            })
            .add(() => {
                this.loading = false;
            });
    }

    createFastSlide() {
        this.getCurrentSubPlan()
    }

    onOpenDialogTest() {
        const ref = this._dialogService.open(DialogTest, { header: 'Chuẩn bị dữ liệu test', closable: true, modal: true, styleClass: 'w-[500px]', focusOnShow: false });
        ref.onClose.subscribe((isCheckinFull) => {
            if (typeof isCheckinFull === 'boolean') {
                this.onModeTest(isCheckinFull);
            }
        });
    }

    onModeTest(isCheckinFull: boolean) {
        this.loadingTest = true;
        this._slideDragService.onModeTest(this.currentSubPlanInfo?.idPlan, isCheckinFull)
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res)) {
                        this.ngOnInit()
                    }
                }
            })
            .add(() => {
                this.loadingTest = false;
            });
    }
}
