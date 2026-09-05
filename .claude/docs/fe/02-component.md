# 02 — Tạo component

## Nguyên tắc chung

- **Standalone**, không `NgModule`. `standalone: true` có thể ghi hoặc bỏ (Angular 20 mặc định standalone) — repo có cả hai kiểu.
- Ba file cùng tên, cùng thư mục: `<ten>.ts`, `<ten>.html`, `<ten>.scss`.
- `selector: 'app-<ten>'`, class **không có hậu tố `Component`**: `export class Plan`, `export class McScreen`, `export class TblAction`.
- Component trang/dialog **luôn `extends BaseComponent`**. Component hiển thị thuần (như `Breadcrumb`, `DataTable`) thì không.
- Dependency lấy bằng `inject()`, không dùng constructor injection (trừ `AppLayout` cũ của Sakai).

```ts
@Component({
    selector: 'app-plan',
    imports: [SharedImports, DataTable],
    templateUrl: './plan.html',
    styleUrl: './plan.scss'
})
export class Plan extends BaseComponent {
    _planService = inject(TraoBangPlanService);
    ...
}
```

`SharedImports` (`@/shared/import.shared`) là mảng gom sẵn `CommonModule`, `FormsModule`, `ReactiveFormsModule` + ~18 module PrimeNG hay dùng. Cần module chưa có trong đó (`PickListModule`, `FileUploadModule`, `TextareaModule`, `Menu`…) thì import thêm **bên cạnh** `SharedImports`, đừng bỏ `SharedImports` ra.

## Mẫu 1 — Trang danh sách

File mẫu: `pages/trao-bang/cau-hinh/plan/plan.ts` + `plan.html`.

### TS

```ts
export class Plan extends BaseComponent {
    _planService = inject(TraoBangPlanService);

    searchForm: FormGroup = new FormGroup({
        search: new FormControl('')
    });

    columns: IColumn[] = [
        { header: 'STT', cellViewType: CellViewTypes.INDEX, headerContainerStyle: 'width: 6rem' },
        { header: 'Tên chương trình', field: 'ten', headerContainerStyle: 'min-width: 10rem' },
        {
            header: 'Trạng thái', field: 'trangThaiText', cellViewType: CellViewTypes.STATUS,
            statusSeverityFunction: (rowData: IViewRowConfigPlan) => PlanTrangThai.getName(rowData.trangThai ?? 0)
        },
        { header: 'Bắt đầu', field: 'thoiGianBatDau', cellViewType: CellViewTypes.DATE, dateFormat: 'dd/MM/yyyy HH:mm:ss' },
        { header: 'Thao tác', headerContainerStyle: 'width: 6rem', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: TblAction }
    ];

    data: IViewRowConfigPlan[] = [];
    query: IFindPagingConfigPlan = {
        pageNumber: this.START_PAGE_NUMBER,   // = 1, từ BaseComponent
        pageSize: this.MAX_PAGE_SIZE          // = 10
    };

    override ngOnInit(): void {
        this.getData();
    }

    onSearch() { this.getData(); }

    onPageChanged($event: PaginatorState) {
        this.query.pageNumber = ($event.page ?? 0) + 1;
        this.getData();
    }

    getData() {
        this.loading = true;
        this._planService
            .findPaging({ ...this.query, keyword: this.searchForm.get('search')?.value })
            .subscribe({
                next: (res) => {
                    if (this.isResponseSucceed(res, false)) {
                        this.data = res.data.items;
                        this.totalRecords = res.data.totalItems;
                    }
                }
            })
            .add(() => { this.loading = false; });   // .add() = finally
    }

    onOpenCreate() {
        const ref = this._dialogService.open(Create, {
            header: 'Tạo chương trình', closable: true, modal: true,
            styleClass: 'w-[700px]', focusOnShow: false
        });
        ref.onClose.subscribe((result) => { if (result) this.getData(); });
    }

    onOpenUpdate(data: IViewRowConfigPlan) {
        const ref = this._dialogService.open(Create, { header: 'Cập nhật chương trình', ..., data });
        ref.onClose.subscribe((result) => { if (result) this.getData(); });
    }

    onDelete(data: IViewRowConfigPlan) {
        this.confirmDelete(
            { header: 'Bạn chắc chắn muốn xóa chương trình?', message: 'Không thể khôi phục sau khi xóa' },
            () => {
                this._planService.delete(data.id || 0).subscribe((res) => {
                    if (this.isResponseSucceed(res, true, 'Đã xóa')) this.getData();
                });
            }
        );
    }

    onCustomEmit(data: { type: string; data: IViewRowConfigPlan; field?: string }) {
        if (data.type === TblActionTypes.update) this.onOpenUpdate(data.data);
        else if (data.type === TblActionTypes.delete) this.onDelete(data.data);
    }
}
```

Điểm cần nhớ:
- Ô "Trạng thái" muốn hiện `p-tag` thì phải **thêm field text** vào data trước khi gán (`trangThaiText`), vì `DataTable` render `field` chứ không tự tra code → tên.
- Mở dialog dùng `this._dialogService.open(...)`, truyền dữ liệu sửa qua `data`, reload danh sách trong `ref.onClose` khi `result` truthy.
- Kết thúc request dùng `.add(() => this.loading = false)` hoặc `complete: () => ...`. Cả hai đều có trong repo.

### HTML

```html
<div class="card">
    <div class="flex flex-row justify-between items-center mb-4">
        <div class="font-semibold text-xl uppercase">Chương trình</div>
    </div>

    <form [formGroup]="searchForm" (ngSubmit)="onSearch()">
        <div class="flex flex-row justify-between items-center mb-4 space-x-2">
            <div class="flex flex-col justify-end flex-1">
                <p-iconfield class="w-full">
                    <p-inputicon class="pi pi-search" />
                    <input formControlName="search" pInputText type="text" placeholder="Search" class="w-150" />
                </p-iconfield>
            </div>
            <p-button label="Thêm mới" styleClass="w-32" (onClick)="onOpenCreate()" />
        </div>
    </form>

    <div class="mt-4">
        <app-data-table
            [columns]="columns"
            [data]="data"
            [loading]="loading"
            (onCustomComp)="onCustomEmit($event)"
            [pageSize]="query.pageSize"
            [pageNumber]="query.pageNumber"
            [total]="totalRecords"
            (onPageChanged)="onPageChanged($event)" />
    </div>
</div>
```

Bọc ngoài luôn là `<div class="card">`. Layout bằng class Tailwind, không viết CSS trừ khi bắt buộc (file `.scss` thường rỗng).

## Mẫu 2 — Dialog thêm/sửa

Đặt trong thư mục con `create/` của trang danh sách, class tên `Create`, dùng chung cho cả thêm lẫn sửa. File mẫu: `plan/create/create.ts`.

```ts
export class Create extends BaseComponent {
    private _ref = inject(DynamicDialogRef);
    private _config = inject(DynamicDialogConfig);
    private _planService = inject(TraoBangPlanService);

    listTrangThai = PlanTrangThai.ListTrangThai;

    override form: FormGroup = new FormGroup({
        id: new FormControl(null),
        ten: new FormControl('', [Validators.required]),
        trangThai: new FormControl(this.listTrangThai[0].code),
        moTa: new FormControl(''),
        time: new FormControl(null)
    });

    override ValidationMessages: Record<string, Record<string, string>> = {
        ten: { required: 'Không được bỏ trống' }
    };

    get isUpdate() { return this._config.data?.id; }

    override ngOnInit(): void {
        if (this.isUpdate) {
            this.form.setValue({ id: this._config.data.id, ten: this._config.data.ten, ... });
        }
    }

    onSubmit() {
        if (this.isFormInvalid()) return;
        if (this.isUpdate) this.onSubmitUpdate();
        else this.onSubmitCreate();
    }

    onSubmitCreate() {
        const body: ICreateConfigPlan = { ten: this.form.value['ten'], ... };
        this.loading = true;
        this._planService.create(body).subscribe({
            next: (res) => {
                if (this.isResponseSucceed(res, true, 'Đã thêm chương trình')) this._ref?.close(true);
            },
            error: (err) => { this.messageError(err?.message); },
            complete: () => { this.loading = false; }
        });
    }
}
```

Quy tắc:
- `get isUpdate()` = `this._config.data?.id`. Dữ liệu sửa truyền từ trang cha qua `data` của `DialogService.open`.
- `override form` và `override ValidationMessages` (đã khai ở `BaseComponent`).
- Submit: `if (this.isFormInvalid()) return;` → tách `onSubmitCreate()` / `onSubmitUpdate()`.
- Đóng dialog bằng `this._ref?.close(true)` để cha biết cần reload.
- Đọc giá trị form bằng `this.form.value['ten']` (dạng index), không phải `this.form.get('ten')?.value` — repo dùng cách đầu trong submit.

### HTML của dialog

```html
<form [formGroup]="form" (ngSubmit)="onSubmit()">
    <div class="mb-3">
        <label for="ten" class="block text-surface-900 dark:text-surface-0 font-medium mb-2">
            Tên chương trình <span class="text-red-500">*</span>
        </label>
        <input pInputText formControlName="ten" id="ten" type="text" class="w-full mb-2" />
        @if (form.get('ten')?.hasError('required') && form.get('ten')?.touched) {
            <p-message severity="error" variant="simple" size="small">{{ getError('ten') }}</p-message>
        }
    </div>

    <div class="mt-5 flex flex-row justify-end">
        <p-button label="Lưu" icon="pi pi-save" type="submit" [loading]="loading" />
    </div>
</form>
```

Khối `label` + input + `@if (...hasError...) { <p-message>{{ getError('field') }} }` là mẫu lặp lại ở mọi form. Dùng control flow mới (`@if`, `@for`) trong template mới.

## Mẫu 3 — `tbl-action` (cột thao tác)

Mỗi trang danh sách có thư mục `tbl-action/` riêng, class `TblAction`, kèm object `TblActionTypes` export cùng file:

```ts
export const TblActionTypes = {
    detail: 'detail',
    update: 'update',
    delete: 'delete',
    config: 'config',
    delete_config: 'delete_config'
};

@Component({ selector: 'app-tbl-action', standalone: true, imports: [SharedImports, Menu], templateUrl: 'tbl-action.html' })
export class TblAction extends BaseComponent {
    tblEmit = inject(TBL_CUSTOM_COMP_EMIT);      // InjectionToken từ DataTable

    @Input() row: IViewRowConfigPlan = {};
    @Input() rowIndex: number = 0;
    @Input() data: any;

    menuItems: MenuItem[] = [];

    override ngOnInit(): void {
        this.menuItems = [
            { label: 'Cập nhật', icon: 'pi pi-pencil', command: () => this.onClick(TblActionTypes.update) },
            { label: 'Xóa', icon: 'pi pi-trash', command: () => this.onClick(TblActionTypes.delete) }
        ];
    }

    onClick(customType: string) {
        this.tblEmit.emit({ data: this.row, type: customType });
    }
}
```

Ba `@Input()` `row` / `rowIndex` / `data` là **bắt buộc** — `DataTable` truyền đúng ba tên này qua `ngComponentOutlet`. Emit ngược lên bằng `TBL_CUSTOM_COMP_EMIT`, trang cha nhận ở `(onCustomComp)`.

## Mẫu 4 — Upload file

```ts
export class Upload extends BaseComponent {
    private _ref = inject(DynamicDialogRef);
    selectedFile: File | null = null;

    override form: FormGroup = new FormGroup({ IdPlan: new FormControl('', [Validators.required]) });

    onUploadChooseFile(event: any) {
        const files = event.files;
        if (files && files.length > 0) this.selectedFile = files[0];
    }

    onSubmit() {
        if (this.isFormInvalid()) { this.messageError('Vui lòng điền đầy đủ thông tin bắt buộc'); return; }
        if (!this.selectedFile) { this.messageError('Vui lòng chọn file để upload'); return; }

        const formData = new FormData();
        formData.append('file', this.selectedFile);
        formData.append('idPlan', this.form.get('IdPlan')?.value.toString());

        this.loading = true;
        this._subPlanService.uploadFile(formData).subscribe({
            next: (res) => { if (this.isResponseSucceed(res, true, 'Upload file thành công')) this._ref?.close(true); },
            error: (err) => { this.messageError(err?.message || 'Có lỗi xảy ra khi upload file'); },
            complete: () => { this.loading = false; }
        });
    }
}
```

Dùng `FileUploadModule` của PrimeNG chỉ để chọn file (`onUploadChooseFile`), gửi bằng `FormData` qua service — không dùng `[url]` tự upload của `p-fileUpload`.

## Mẫu 5 — Màn hình real-time (SignalR)

File mẫu: `pages/trao-bang/mc-screen/mc-screen.ts`.

```ts
export class McScreen extends BaseComponent implements OnDestroy {
    hubConnection: signalR.HubConnection | undefined;
    private removeListener?: () => void;
    renderer = inject(Renderer2);

    override ngOnInit(): void {
        this.initData();
        this.connectHub();
        this.removeListener = this.renderer.listen('document', 'keydown', (event: KeyboardEvent) => {
            if (event.key === 'Enter' && event.shiftKey) this.prevTraoBang();
            if (event.key === 'Enter') this.onClickNextTraoBang();
        });
    }

    connectHub() {
        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(TraoBangHubConst.HUB, {
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .build();

        this.hubConnection.on(TraoBangHubConst.ReceiveChonKhoa, (...args) => { this.initData(); });
        this.hubConnection.on(TraoBangHubConst.ReceiveCheckIn, (...args) => { this.getHangDoi(); });

        this.hubConnection.start().then();
    }

    ngOnDestroy(): void {
        this.hubConnection?.stop().then();
        if (this.removeListener) this.removeListener();
    }
}
```

Quy tắc:
- Mỗi màn hình tự tạo `HubConnection` riêng trong `connectHub()`, **không có service SignalR dùng chung**.
- Luôn `skipNegotiation: true` + `transport: WebSockets`.
- Handler nhận event thì **gọi lại API để lấy dữ liệu mới**, không dùng payload của event (BE broadcast không kèm data).
- **Bắt buộc `ngOnDestroy`**: `hubConnection?.stop()` và gỡ listener bàn phím.
- Nghe phím dùng `Renderer2.listen('document', 'keydown', ...)` và giữ hàm gỡ trả về.

## Gotcha

- `ngOnInit` của `BaseComponent` là method rỗng đã implement → component con **phải viết `override ngOnInit()`**, quên `override` là lỗi biên dịch (`noImplicitOverride: true`).
- `strictTemplates: true` — kiểu trong template được kiểm tra chặt.
- `BaseComponent` khai `totalRecords = 100` (không phải 0). Trang danh sách nào không gán lại sẽ hiển thị paginator sai.
- Có `console.log` rải rác trong code hiện tại; không cần dọn, nhưng đừng thêm mới.
