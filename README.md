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

### 3. Panel Phải - Thông số & Điều chỉnh (Properties Sidebar)
- **Card 0 - Tỉ lệ & Kích thước mẫu**:
  - Hàng tỉ lệ: `Tự do`, `3:4`, `4:6`, `9:16`, `1:1`, `4:3`, `6:4`, `16:9` (khóa tỉ lệ khung hình trực tiếp khi kéo ngàm).
  - Hàng độ phân giải màn hình chuẩn: `Toàn bộ`, `1024x768`, `1280x720`, `1366x768`, `1440x900`, `1600x900`, `1920x1080`, `2560x1440`.
- **Card 1 - Tọa độ & Di chuyển (D-Pad)**:
  - Hộp nhập số `X`, `Y`, `W`, `H` đồng bộ 2 chiều tức thì với thao tác chuột trên canvas.
  - 4 nút mũi tên tinh chỉnh (`Left`, `Up`, `Down`, `Right`) với bước nhảy linh hoạt: `1px`, `5px`, `10px`, `50px`.
  - Nút **Căn giữa** (icon Bullseye): Đưa khung crop về chính giữa bức ảnh.
- **Card 2 - Bộ lọc hình ảnh (Image Filters)**:
  - **Nút tích "Ảnh đen trắng (Grayscale)"**: Chuyển đổi hiển thị ảnh main sang ảnh đen trắng thời gian thực mượt mà.
  - **Nút tích "Ngưỡng nhị phân (Threshold)"**: Kích hoạt thanh trượt ngưỡng nhị phân (Binarization Slider 0 - 255, mặc định 128).
  - **Thuật toán so sánh pixel**: Điểm ảnh có độ sáng (luminance) `< ngưỡng` chuyển thành Đen (0, 0, 0), $\ge$ `ngưỡng` chuyển thành Trắng (255, 255, 255).
  - **Xem & Cắt theo bộ lọc**: Hình ảnh crop lưu ra tệp hoặc copy vào Clipboard phản ánh chính xác hiệu ứng bộ lọc đang chọn mà không phá hủy ảnh gốc.
- **Thanh nút tác vụ cố định (Fixed Action Bar)**:
  - Nút **LƯU TỌA ĐỘ** (icon FloppyDisk): Lưu vùng crop vào danh sách Objects.
  - Nút **CẮT & LƯU ẢNH (`Ctrl + S`)** (icon Crop): Cắt ảnh trực tiếp và lưu ra tệp PNG/JPG/BMP.
  - Nút **COPY ẢNH (`Ctrl + C`)** (icon Copy): Sao chép ảnh đã cắt vào Clipboard.

### 4. Panel Dưới - Danh sách Objects đã lưu & Tính năng Sắp xếp
- Chiếm trọn 100% diện tích phần dưới của cửa sổ, hiển thị bảng danh sách rộng rãi, xem được nhiều dòng cùng lúc.
- **Sắp xếp linh hoạt theo tiêu đề**:
  - **Nhấn trực tiếp vào tiêu đề `Objects (N)`**: Tự động chuyển đổi chu kỳ sắp xếp theo Tên (A → Z, Z → A, Mặc định).
  - **Nhấn vào tiêu đề bất kỳ cột nào trong bảng** (`#`, `Loại`, `Tên vùng / ảnh`, `Tọa độ X`, `Tọa độ Y`, `Rộng (W)`, `Cao (H)`, `Tỉ lệ`, `Thời gian tạo`, `Ghi chú / Đường dẫn`): Tự động sắp xếp tăng dần hoặc giảm dần theo cột tương ứng kèm mũi tên chỉ hướng `▲` / `▼`.
  - **Nút "Sắp xếp" trên thanh tác vụ**: Menu ngữ cảnh hỗ trợ chọn nhanh các chế độ sắp xếp phổ biến (Tên A-Z, Tên Z-A, Mới nhất, Cũ nhất, Kích thước Lớn - Nhỏ).
- **Cột Phân loại (`Type`)**:
  - `Tọa độ`: Biểu tượng vector `LocationDot`.
  - `Hình ảnh`: Biểu tượng vector `Image`.
- **Thao tác nhanh**: Sửa tên trực tiếp trên ô bảng (F2 / double click tên), xóa từng mục hoặc xóa hết, xuất và nhập cấu hình JSON, trích xuất gói Export package chuẩn hóa.

### 5. Khung phân chia tương tác (Live Splitter) mượt mà, chống xé hình
- Khắc phục triệt để hiện tượng giật, nháy và xé hình khi kéo rê thanh ngăn cách:
  - Tích hợp kỹ thuật vùng nhớ đệm kép `DoubleBuffered` đệ quy toàn bộ các panel và bảng dữ liệu.
  - Tối ưu hóa thông điệp `WS_CLIPCHILDREN` ngăn chặn vẽ đè nền cha lên các thành phần con.
  - Loại bỏ hoàn toàn repaint đồng bộ gây nghẽn luồng UI, mang lại trải nghiệm co giãn mượt mà 60 FPS.

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
