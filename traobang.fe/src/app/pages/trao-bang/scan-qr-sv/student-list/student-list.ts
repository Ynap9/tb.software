
import { ELoaiSlide } from '@/models/traobang/slide.models';
import { IViewScanQrTienDoSv } from '@/models/traobang/sv-nhan-bang.models';
import { SvNhanBangStatuses } from '@/shared/constants/sv-nhan-bang.constants';
import { SharedImports } from '@/shared/import.shared';
import { Component, computed, inject, input, output } from '@angular/core';
import { TableModule } from "primeng/table";
import { TagModule } from 'primeng/tag';
import { DragDropModule, CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { BaseComponent } from '@/shared/components/base/base-component';
import { SlideService } from '@/service/slide.service';
import { SlideDragDropService } from '@/service/slide-drag-drop.service';

@Component({
    selector: 'app-student-list',
    imports: [SharedImports, TableModule, TagModule, DragDropModule],
    templateUrl: './student-list.html',
    styleUrl: './student-list.scss'
})
export class StudentList extends BaseComponent {
    _slideService = inject(SlideService);
    _slideDragDropService = inject(SlideDragDropService);

    students = input.required<IViewScanQrTienDoSv[]>();
    highlightLast = input<boolean>(false);
    constStatuses = SvNhanBangStatuses;
    loaiSlide = ELoaiSlide

    studentsList = computed(() => this.students() || []);
    studentsChange = output<any>();
    deleteSlide = output<any>();

    drop(event: CdkDragDrop<IViewScanQrTienDoSv[]>) {
        // Chặn drop vào 2 vị trí đầu tiên và chặn drag từ 2 vị trí đầu
        if (event.currentIndex < 2 || event.previousIndex < 2) {
            return;
        }

        const data = [...this.studentsList()];
        const draggedItem = data[event.previousIndex];

        moveItemInArray(data, event.previousIndex, event.currentIndex);

        data.forEach((item, index) => {
            item.orderTienDo = index + 1;
        });

        let dataChange

        if (event.currentIndex === 0) {
            dataChange = {
                idTienDo: draggedItem.id,
                idSlideTruoc: 0,
            }
        }
        else {
            dataChange = {
                idTienDo: draggedItem.id,
                IdSlideTruoc: data[event.currentIndex - 1].id,
            }
        }
        this.studentsChange.emit(dataChange);
    }


    getRowClass(student: IViewScanQrTienDoSv) {
        return {
            'bg-orange-200': student.trangThai === this.constStatuses.DANG_TRAO_BANG,
            '!bg-[#d4ffd1]': student.loaiSlide === this.loaiSlide.TEXT,
            '!bg-[#ffedd5]': student.isSlideDauCuoi
        };
    }

    changeStatus(id: number | undefined) {
        if (!id) return;
        this.confirmAction(
            {
                header: 'Xóa slide',
                message: 'Bạn chắc chắn muốn hủy checkin?'
            },
            () => {
                // Call API to delete slide
                this._slideDragDropService.changeStatus(id)
                    .subscribe({
                        next: (res) => {
                            if (this.isResponseSucceed(res, true, 'Đã hủy checkin Thành công')) {
                                this.deleteSlide.emit(true);
                            }
                        }
                    })
                    .add(() => {
                        this.loading = false;
                    });
            }
        );
    }

    onRevertSlideText(id: number | undefined) {
        if (!id) return;
        this.confirmAction(
            {
                header: 'Gỡ slide khỏi hàng đợi',
                message: 'Bạn chắc chắn muốn gỡ slide khỏi hàng đợi?'
            },
            () => {
                // Gỡ khỏi hàng đợi, slide vẫn giữ nguyên
                this._slideDragDropService.changeStatus(id)
                    .subscribe({
                        next: (res) => {
                            if (this.isResponseSucceed(res, true, 'Đã gỡ slide khỏi hàng đợi')) {
                                this.deleteSlide.emit(true);
                            }
                        }
                    })
                    .add(() => {
                        this.loading = false;
                    });
            }
        );
    }
}
