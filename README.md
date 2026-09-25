# ScreenCropperPro - Công cụ Định vị Tọa độ & Cắt ảnh màn hình

Công cụ định vị tọa độ cắt pixel chính xác cao và trích xuất hình ảnh từ ảnh chụp màn hình, được xây dựng trên nền tảng **.NET 9.0 Windows Forms** với phong cách giao diện Dark Mode hiện đại (tương tự CapCut / Premiere / Photoshop), tích hợp toàn bộ hệ thống biểu tượng vector sắc nét từ thư viện **FontAwesome.Sharp**.

---

## 🚀 Tính năng nổi bật

### 1. Header (Thanh công cụ đỉnh) & Hệ thống Icon Thư viện
- **Hệ thống Icon Vector (FontAwesome.Sharp)**: Toàn bộ nút bấm, tiêu đề, thanh trạng thái, bảng điều khiển và danh sách đều sử dụng icon vector chuẩn hóa, sắc nét ở mọi độ phân giải DPI.
- **Nạp ảnh (`Ctrl + O`)**: Mở tệp ảnh từ ổ đĩa (PNG, JPG, JPEG, BMP, WEBP, GIF).
- **Dán Clipboard (`Ctrl + V`)**: Dán trực tiếp ảnh vừa chụp màn hình (PrintScreen) hoặc copy từ ứng dụng khác.
- **Chụp Live (`F9`)**: Phím tắt toàn cục (**Global Hotkey F9**). Có thể bấm F9 từ bất kỳ cửa sổ/game nào, ứng dụng sẽ tự động ẩn trong tích tắc và chụp lại toàn bộ màn hình thực tế.
- **Cửa sổ khác**: Chụp cửa sổ đang hoạt động kế tiếp.
- **Vừa khung / 100%**: Reset góc nhìn ảnh về vừa vặn màn hình hoặc tỉ lệ 1:1.
- **Thanh trạng thái**: Hiển thị kích thước ảnh, tọa độ pixel chuột `(X, Y)` và mã màu HEX tại điểm trỏ chuột trong thời gian thực.

### 2. Panel Trái - Canvas & Khung ảo CapCut
- **Phóng to / Thu nhỏ**: Lăn chuột (`Mouse Wheel`) zoom mượt mà từ 5% đến 4000% ngay tại vị trí con trỏ.
- **Di chuyển ảnh (Pan)**: Nhấp và giữ chuột giữa (`Middle Click Drag`) hoặc chuột phải (`Right Click Drag`) hoặc giữ `Space + Chuột trái`.
- **Khung ảo tương tác phong cách CapCut**:
  - Phủ lớp mask tối mờ xung quanh vùng chọn, làm nổi bật vùng cần crop.
  - Lưới bố cục 3x3 (Rule of thirds) mờ bên trong.
  - 4 góc có thanh ngàm chữ L dày dặn và 4 cạnh có thanh trượt giữa cạnh để kéo thay đổi kích thước.
  - Kéo bên trong khung để di chuyển toàn bộ hộp chọn.
  - Nhấp kéo bên ngoài để vẽ vùng chọn mới.
  - Thẻ thông số nổi (HUD Badge) hiển thị tọa độ X, Y và Kích thước W, H ngay trên khung crop.
  - Lưới pixel (Pixel Grid) tự động xuất hiện khi zoom $\ge 800\%$ giúp căn chỉnh chính xác đến từng pixel đơn lẻ.

### 3. Panel Phải - Hình xem trước (Live Preview)
- Tự động phóng to hoặc thu nhỏ (`Auto-fit`) vừa vặn khung xem trước trong khi vẫn bảo toàn tỉ lệ ảnh.
- Nền caro mờ trong suốt (Checkerboard) chống lóa.
- Tùy chọn **Pixel sắc nét (Nearest Neighbor)**: Rất quan trọng khi soi các ký tự số nhỏ, icon game pixel, sprite (như chỉ số tài nguyên AOE) không bị mờ nhòe.
- Thẻ thông số bên dưới: Chiều rộng, Chiều cao, Tỉ lệ khung hình (Aspect Ratio), và Độ thu phóng xem trước (`Fit %`).
- Nút nhanh: **Cắt & Lưu ảnh** (icon Crop) và **Copy ảnh** (icon Copy) vào clipboard.

### 4. Panel Dưới - Bảng điều khiển tinh chỉnh
- Hộp nhập số `X`, `Y`, `Width`, `Height` đồng bộ 2 chiều với thao tác chuột trên canvas.
- Phím điều hướng tinh chỉnh (Nudge D-Pad): `CaretLeft`, `CaretRight`, `CaretUp`, `CaretDown` với bước nhảy linh hoạt: `1px`, `5px`, `10px`, `50px` (có thể dùng 4 phím mũi tên trên bàn phím).
- Nút tăng giảm kích thước nhanh: `W-10`, `W-1`, `W+1`, `W+10`, `H-10`, `H-1`, `H+1`, `H+10`.
- Nút **Căn giữa** (icon Bullseye): Đưa khung crop về chính giữa bức ảnh.
- Nút **LƯU TỌA ĐỘ** (icon FloppyDisk): Mở hộp thoại đặt tên vùng cắt (tự động kiểm tra chống đặt tên trùng lặp).
- Nút **CẮT & LƯU ẢNH (`Ctrl + S`)** (icon Crop): Xuất vùng cắt ra tệp ảnh PNG/JPG/BMP, đồng thời tự động lưu mục này vào danh sách quản lý.
- Nút **COPY ẢNH (`Ctrl + C`)** (icon Copy): Lưu ảnh vào Clipboard để dán vào Paint, Photoshop, Zalo, Discord.

### 5. Phía Dưới Cùng - Danh sách đã lưu (Tọa độ & Hình ảnh)
- **Tiêu đề Header rõ ràng**: `DANH SÁCH ĐÃ LƯU (TỌA ĐỘ & HÌNH ẢNH)` (icon RectangleList) kèm số lượng mục.
- **Cột Phân loại (`Type`)**:
  - `Tọa độ`: Badge xanh Cyan kèm biểu tượng vector `LocationDot`.
  - `Hình ảnh`: Badge xanh ngọc Emerald kèm biểu tượng vector `Image`.
- **Nút Thao tác trên từng dòng (Bên phải dòng)**:
  - **Sửa** (icon PenToSquare): Nút nằm bên phải mỗi dòng. Khi rê chuột (hover) sẽ hiện tooltip giải thích. Nhấp vào để đổi tên, cập nhật ghi chú hoặc tọa độ mới.
  - **Xóa** (icon TrashCan): Nút xóa nằm bên phải mỗi dòng. Khi rê chuột sẽ hiện tooltip xác nhận xóa mục khỏi danh sách.
- **Tương tác dòng**:
  - **Nhấp một lần**: Nhảy khung crop trên ảnh gốc về vị trí đó và cập nhật hình xem trước.
  - **Nhấn đúp (Double-Click)**:
    - Đối với `Hình ảnh`: Mở cửa sổ **Image Viewer** xem ảnh phóng to, hỗ trợ Zoom, Pan, copy ảnh và mở thư mục chứa tệp.
    - Đối với `Tọa độ`: Nạp tọa độ lên bảng điều khiển để chỉnh sửa.
- **Chống đặt tên trùng**: Hệ thống tự động kiểm tra tên khi nhập. Nếu tên bị trùng, cảnh báo đỏ xuất hiện và vô hiệu hóa nút lưu cho tới khi chọn tên khác.
- **Xuất JSON** (icon FileExport) / **Nhập JSON** (icon FileImport) / **Xóa hết** (icon TrashCan): Lưu trữ và chia sẻ profile cấu hình tọa độ giữa các máy tính.
- Tự động lưu cấu hình vào tệp `saved_crops.json`.

---

## ⌨️ Bảng phím tắt nhanh

| Phím tắt | Thao tác |
|---|---|
| **F9** | Chụp màn hình Live toàn cục (kể cả khi app đang ở dưới nền) |
| **Ctrl + O** | Mở tệp ảnh từ máy |
| **Ctrl + V** | Dán ảnh từ Clipboard |
| **Ctrl + S** | Cắt và Lưu ảnh ra file |
| **Ctrl + C** | Copy ảnh đã cắt vào Clipboard |
| **Lăn chuột** | Phóng to / Thu nhỏ ảnh |
| **Chuột giữa / Chuột phải** | Kéo di chuyển ảnh (Pan) |
| **Phím Mũi tên (← ↑ ↓ →)** | Dịch chuyển khung crop theo bước chọn |
| **Shift + Mũi tên** | Dịch chuyển nhanh (gấp 5 lần bước chọn) |

---

## 🛠️ Hướng dẫn cài đặt & khởi chạy

### 1. Cài đặt tự động (Dành cho máy mới clone về)
Nhấp đúp chuột vào file `setup.bat`. Script sẽ tự động:
- Kiểm tra xem máy đã có **.NET 9.0 SDK** chưa (nếu chưa, sẽ tự động dùng `winget` để cài đặt).
- Khôi phục thư viện NuGet và biên dịch dự án (`dotnet build`).
- Hỏi khởi chạy ứng dụng ngay sau khi biên dịch thành công.

### 2. Khởi chạy thông thường
- Nhấp đúp file `run.bat` hoặc chạy lệnh:
  ```bash
  dotnet run
  ```

### 3. Bài kiểm tra tự chẩn đoán (Self-test)
Để chạy bài kiểm tra tự chẩn đoán hệ thống:
```bash
dotnet run -- --test
```
