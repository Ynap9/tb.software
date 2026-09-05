# 05 — `BaseComponent` & `DataTable`

## `BaseComponent`

`shared/components/base/base-component.ts` — là `@Directive()` abstract, không phải component.

### Có sẵn khi `extends`

| Thành viên | Kiểu | Ghi chú |
|---|---|---|
| `router` | `Router` | protected |
| `_activatedRoute` | `ActivatedRoute` | protected |
| `_messageService` | `MessageService` | dùng gián tiếp qua `messageSuccess/...` |
| `_dialogService` | `DialogService` | mở dialog |
| `_confirmationService` | `ConfirmationService` | dùng gián tiếp qua `confirmDelete/...` |
| `form` | `FormGroup` | khai báo sẵn, component con `override` |
| `ValidationMessages` | `Record<string, Record<string, string>>` | map field → mã lỗi → message |
| `loading` | `boolean` | mặc định `false` |
| `totalRecords` | `number` | **mặc định `100`** — nhớ gán lại |
| `MAX_PAGE_SIZE` | `10` | dùng làm `pageSize` mặc định |
| `START_PAGE_NUMBER` | `1` | |

### Method

```ts
ngOnInit(): void {}                    // rỗng → con phải viết `override ngOnInit()`

isFormInvalid(): boolean               // invalid thì markAllAsTouched() rồi trả true
getError(field: string): string | null // tra ValidationMessages theo lỗi đầu tiên
getErrorMessage(control, fieldName)    // bản đầy đủ của getError

isResponseSucceed(res: IBaseResponse, isShowErrorMsg = true, successMsg = ''): boolean

messageSuccess(msg: string)
messageWarning(msg: string)
messageError(msg: string)              // rỗng thì hiện "Có sự cố xảy ra. Vui lòng thử lại sau."

confirmDelete({ header, message }, acceptCallback)   // nút: Thoát / Xóa (đỏ) + icon cảnh báo
confirmAction({ header, message }, acceptCallback)   // nút: Thoát / Tiếp tục (primary)
```

### Cách dùng validation

```ts
override form: FormGroup = new FormGroup({
    ten: new FormControl('', [Validators.required])
});

override ValidationMessages: Record<string, Record<string, string>> = {
    ten: { required: 'Không được bỏ trống' }
};
```

Template:

```html
@if (form.get('ten')?.hasError('required') && form.get('ten')?.touched) {
    <p-message severity="error" variant="simple" size="small">{{ getError('ten') }}</p-message>
}
```

Field nào dùng `getError()` thì **bắt buộc** có entry trong `ValidationMessages`, nếu không `getErrorMessage` sẽ đọc `messages[errorKey]` trên `undefined` → lỗi runtime.

### Confirm

```ts
this.confirmDelete(
    { header: 'Bạn chắc chắn muốn xóa chương trình?', message: 'Không thể khôi phục sau khi xóa' },
    () => {
        this._planService.delete(id).subscribe((res) => {
            if (this.isResponseSucceed(res, true, 'Đã xóa')) this.getData();
        });
    }
);
```

Toast và confirm dialog render ở `AppLayout` → chỉ hoạt động trong vùng đã đăng nhập, **không dùng được ở route `/guest/**`**.

---

## `DataTable`

`shared/components/data-table/data-table.ts`, selector `app-data-table`. Bọc `p-table` + `p-paginator` của PrimeNG.

### Input / Output

```ts
columns   = input.required<IColumn[]>();
data      = input.required<any[]>();
pageSize  = input<number>(10);
pageNumber= input<number>(1);
total     = input<number>(100);
loading   = input<boolean>(false);

@Output() onPageChanged = new EventEmitter<any>();   // PaginatorState
@Output() customEmit    = new EventEmitter<any>();   // nội bộ, không dùng ở ngoài
@Output() onCustomComp  = new EventEmitter<any>();   // MỌI sự kiện từ cell / custom component
```

Dùng:

```html
<app-data-table
    [columns]="columns"
    [data]="data"
    [loading]="loading"
    [pageSize]="query.pageSize"
    [pageNumber]="query.pageNumber"
    [total]="totalRecords"
    (onCustomComp)="onCustomEmit($event)"
    (onPageChanged)="onPageChanged($event)" />
```

### `IColumn`

`shared/models/data-table.models.ts`:

```ts
export type IColumn = {
    header: string,
    field?: string,
    headerContainerClass?: string,
    headerContainerStyle?: string,       // dùng để set width: 'width: 6rem' / 'min-width: 10rem'
    cellClass?: string,
    cellStyle?: string,
    cellViewType?: string,               // CellViewTypes.*
    cellRender?: string,                 // truthy => render innerHTML (bypassSecurityTrustHtml)
    dateFormat?: string,                 // dùng với DATE
    customComponent?: any,               // dùng với CUSTOM_COMP
    clickable?: boolean,                 // cell bấm được -> emit type 'cellClick'
    isFrozenRight?: boolean,
    statusSeverityFunction?: (rowData: any) => string;   // dùng với STATUS
}
```

### `CellViewTypes`

`shared/constants/data-table.constants.ts`:

| Giá trị | Hiển thị |
|---|---|
| `INDEX` | Số thứ tự `rowIndex + 1` |
| `DATE` | `{{ value \| date: dateFormat }}`, mặc định `dd/MM/yyyy` |
| `CURRENCY` | Số có dấu chấm ngăn nghìn |
| `CHECKBOX` | `<input type="checkbox">`, click emit `cellClick` |
| `LINK_BLANK` | Thẻ `<a target="_blank">` |
| `STATUS` | `<p-tag>` với `severity` lấy từ `statusSeverityFunction(row)` |
| `CUSTOM_COMP` | Render `customComponent` qua `ngComponentOutlet` |
| (không set, có `field`) | Text thường |

Ví dụ đủ các loại:

```ts
columns: IColumn[] = [
    { header: 'STT', cellViewType: CellViewTypes.INDEX, headerContainerStyle: 'width: 6rem' },
    { header: 'Tên chương trình', field: 'ten', headerContainerStyle: 'min-width: 10rem' },
    {
        header: 'Trạng thái',
        field: 'trangThaiText',                    // ← field chứa TEXT, không phải code
        cellViewType: CellViewTypes.STATUS,
        statusSeverityFunction: (rowData: IViewRowConfigPlan) => PlanTrangThai.getName(rowData.trangThai ?? 0)
    },
    { header: 'Bắt đầu', field: 'thoiGianBatDau', cellViewType: CellViewTypes.DATE, dateFormat: 'dd/MM/yyyy HH:mm:ss' },
    { header: 'Thao tác', headerContainerStyle: 'width: 6rem', cellViewType: CellViewTypes.CUSTOM_COMP, customComponent: TblAction }
];
```

`field` hỗ trợ path lồng (`'subPlan.ten'`) nhờ hàm `get(obj, path)` bên trong.

### Cột STATUS — cách làm đúng

`DataTable` render `field` làm nhãn của `p-tag`, còn `statusSeverityFunction` cho màu. Nên phải **map thêm field text** khi nhận dữ liệu:

```ts
this.data = res.data.items.map((item) => {
    const trangThaiText = PlanTrangThai.ListTrangThai.find((x) => x.code == item.trangThai);
    return { ...item, trangThaiText: trangThaiText?.name ?? '' };
});
```

> Trong `plan.ts` hiện tại `statusSeverityFunction` đang trả `getName(...)` (tên) thay vì `getSeverity(...)` (màu). Đó là code đang có. Với cột STATUS mới, dùng `getSeverity(...)` cho đúng ý nghĩa.

### Cột CUSTOM_COMP và `TBL_CUSTOM_COMP_EMIT`

`DataTable` tạo một `Injector` con cấp `TBL_CUSTOM_COMP_EMIT` (`InjectionToken<EventEmitter<any>>`) và truyền 3 input cố định vào component con:

```html
<ng-container *ngComponentOutlet="col.customComponent; injector: customInjector;
    inputs: { row, rowIndex, data: get(row, col.field || '') }"></ng-container>
```

Component con vì vậy phải khai đúng ba `@Input()`: `row`, `rowIndex`, `data`. Emit ngược:

```ts
tblEmit = inject(TBL_CUSTOM_COMP_EMIT);
this.tblEmit.emit({ data: this.row, type: TblActionTypes.update });
```

Trang cha nhận ở `(onCustomComp)`:

```ts
onCustomEmit(data: { type: string; data: IViewRowConfigPlan; field?: string }) {
    if (data.type === TblActionTypes.update) this.onOpenUpdate(data.data);
    else if (data.type === TblActionTypes.delete) this.onDelete(data.data);
}
```

Cell `clickable: true` hoặc `CHECKBOX` cũng emit qua `onCustomComp` nhưng với `type: 'cellClick'` kèm `field`:

```ts
if (data.type === 'cellClick' && data.field === 'isShow') { ... }
```

### Phân trang

`DataTable` **không tự phân trang** (`[paginator]="false"`), chỉ phát `onPageChanged`. Trang cha tự tăng `pageNumber` rồi gọi lại API:

```ts
onPageChanged($event: PaginatorState) {
    this.query.pageNumber = ($event.page ?? 0) + 1;   // PrimeNG đếm từ 0, BE đếm từ 1
    this.getData();
}
```

### Gotcha

- `total` mặc định `100`; nếu quên gán `totalRecords` thì paginator hiện sai số trang.
- `cellRender` dùng `bypassSecurityTrustHtml` — chỉ dùng cho HTML do hệ thống sinh, không dùng cho dữ liệu người dùng nhập.
- `DataTable` không có sort, không có filter, không có chọn nhiều dòng. Cần thì phải tự làm ở trang, đừng sửa `DataTable` dùng chung.
