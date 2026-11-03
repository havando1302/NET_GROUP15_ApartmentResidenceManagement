# CHƯƠNG 2. PHÂN TÍCH THIẾT KẾ HỆ THỐNG

*(Phần phụ của môn .NET – làm cơ sở cho Chương 3)*

---

## 2.1. Phân tích yêu cầu

### 2.1.1. Phân tích các yêu cầu đã xác định
Dựa trên kết quả khảo sát bối cảnh và các yêu cầu đặt ra ở Chương 1, bài toán quản lý thông tin cư trú chung cư đặt trọng tâm vào hai yếu tố cốt lõi: tính toàn vẹn của dữ liệu nhân khẩu và sự thuận tiện trong tương tác người dùng. Các yêu cầu chính được phân tích thành ba nhóm trọng điểm:

* **Nhóm yêu cầu về dữ liệu và ràng buộc pháp lý cư trú:** Quan hệ giữa cư dân và căn hộ không phải là quan hệ tĩnh mà mang tính lịch sử biến động theo thời gian. Do đó, hệ thống không chỉ lưu lại ai đang ở căn hộ nào mà bắt buộc phải phân định rõ vai trò cư trú (Chủ hộ, Thành viên, Người thuê, Tạm trú) và khoảng thời gian hiệu lực (từ ngày bắt đầu đến ngày kết thúc). Các quy tắc bất biến (Invariants) như: mỗi căn hộ chỉ có tối đa 1 chủ hộ active, mỗi cư dân chỉ active ở 1 căn hộ duy nhất tại một thời điểm cần phải được kiểm soát chặt chẽ ở cả tầng ứng dụng và tầng cơ sở dữ liệu.
* **Nhóm yêu cầu về nghiệp vụ quản trị của Ban quản lý:** Ban quản lý cần một công cụ xử lý nhanh, tự động hóa tối đa các thao tác: đăng ký cư trú mới, kết thúc cư trú, chuyển căn hộ nội bộ và tiếp nhận/duyệt phương tiện. Khi có biến động dân cư, trạng thái của căn hộ (Trống hoặc Đang ở) phải tự động đồng bộ mà không cần nhân viên can thiệp thủ công.
* **Nhóm yêu cầu về trải nghiệm Cổng thông tin Cư dân:** Cư dân có nhu cầu tra cứu hồ sơ minh bạch, tiện lợi mà không cần đến trực tiếp văn phòng. Hệ thống cần cung cấp các màn hình tra cứu trực quan về căn hộ, danh sách thành viên trong gia đình và quy trình gửi yêu cầu thêm thành viên trực tuyến để Ban quản lý xem xét phê duyệt.

### 2.1.2. Định hướng giải pháp kỹ thuật
Để hiện thực hóa các yêu cầu trên một cách hiệu quả, hệ thống được định hướng thiết kế theo kiến trúc phân tầng chuẩn mực (Multi-tier Architecture kết hợp mô hình MVVM ở tầng hiển thị):

* **Tầng Trình diễn (Presentation Layer):** Áp dụng mẫu thiết kế MVVM (Model - View - ViewModel) giúp phân tách triệt để giữa giao diện đồ họa (View) và logic xử lý giao diện (ViewModel). Sử dụng cơ chế Data Binding hai chiều và Command Pattern giúp giao diện phản hồi mượt mà, độc lập với dữ liệu bên dưới.
* **Tầng Nghiệp vụ (Application / Business Logic Layer):** Đóng gói toàn bộ các quy tắc nghiệp vụ, kiểm tra tính hợp lệ dữ liệu (validation), xử lý giao dịch biến động cư trú và điều phối luồng xử lý thông qua các Service chuyên biệt (*ApartmentService, ResidentService, ResidenceService, VehicleService, AccountService, MemberRequestService*).
* **Tầng Miền và Dữ liệu cốt lõi (Domain Layer):** Định nghĩa các thực thể dữ liệu (Entities), các kiểu liệt kê (Enums), ngoại lệ nghiệp vụ (Exceptions) và các giao diện trừu tượng (Interfaces) đảm bảo tính độc lập cao nhất, không phụ thuộc vào công nghệ cơ sở dữ liệu cụ thể.
* **Tầng Truy cập Dữ liệu (Infrastructure Layer):** Sử dụng mẫu thiết kế Repository kết hợp Unit of Work để quản lý vòng đời truy cập dữ liệu, đảm bảo tính nguyên tử (Atomicity) và an toàn trong các giao dịch đa bảng. Áp dụng kỹ thuật ánh xạ quan hệ đối tượng (ORM) hiện đại để quản lý schema và đồng bộ dữ liệu.

---

## 2.2. Thiết kế chức năng

### 2.2.1. Phân rã chức năng hệ thống
Hệ thống được chia thành 6 nhóm chức năng chính phục vụ cho 2 tác nhân (Actor): **Ban quản lý (Admin)** và **Cư dân (Resident)**:
* **F01 - Quản lý Xác thực & Phân quyền:** Đăng nhập hệ thống, kiểm tra phân quyền vai trò, đăng xuất an toàn.
* **F02 - Quản lý Danh mục Căn hộ:** Xem danh sách, lọc theo tầng/trạng thái, thêm mới, sửa thông tin căn hộ.
* **F03 - Quản lý Hồ sơ Cư dân:** Lưu trữ, tra cứu thông tin cá nhân, thêm mới cư dân, cập nhật thông tin liên lạc.
* **F04 - Quản lý Nghiệp vụ Cư trú:** Đăng ký cư trú, chuyển căn hộ, kết thúc cư trú, truy xuất lịch sử cư trú theo căn hộ/cư dân.
* **F05 - Quản lý & Phê duyệt Phương tiện:** Đăng ký xe, kiểm tra hạn mức xe căn hộ, Ban quản lý duyệt hoặc từ chối xe.
* **F06 - Cổng Cư dân & Xử lý Yêu cầu Thành viên:** Cư dân tra cứu căn hộ/thành viên, gửi yêu cầu thêm thành viên; Ban quản lý xem xét phê duyệt.

### 2.2.2. Mô tả chi tiết Đầu vào - Xử lý - Đầu ra của các chức năng chính

| Mã CN | Tên chức năng | Dữ liệu đầu vào & Quy trình xử lý | Kết quả đầu ra |
| :---: | :--- | :--- | :--- |
| **F01** | **Đăng nhập & Phân quyền** | • **Đầu vào:** Tên đăng nhập, Mật khẩu.<br>• **Xử lý:** Kiểm tra rỗng; tìm tài khoản; đối soát mật khẩu băm bảo mật; kiểm tra cờ kích hoạt `IsActive`; nạp thông tin vai trò (Admin/Resident).<br>• **Phân luồng:** Điều hướng tới giao diện Admin hoặc Cổng cư dân tương ứng. | Xác thực thành công, mở màn hình chính phân quyền đúng hoặc thông báo lỗi nếu sai. |
| **F02** | **Quản lý Căn hộ** | • **Đầu vào:** Số căn, tầng, diện tích, trạng thái.<br>• **Xử lý:** Kiểm tra trùng mã căn hộ; kiểm tra tầng và diện tích dương; tự động cập nhật trạng thái (Trống nếu 0 cư dân, Đang ở nếu có cư dân active). | Danh sách căn hộ cập nhật; thông báo kết quả thêm/sửa thành công. |
| **F03** | **Quản lý Cư dân** | • **Đầu vào:** Họ tên, ngày sinh, CCCD (12 số), SĐT, email, quê quán.<br>• **Xử lý:** Chuẩn hóa họ tên; kiểm tra CCCD 12 chữ số hợp lệ; kiểm tra ngày sinh không ở tương lai; kiểm tra trùng CCCD trong hệ thống. | Hồ sơ cư dân được ghi nhận an toàn; hiển thị danh sách tra cứu đầy đủ. |
| **F04** | **Đăng ký Cư trú** | • **Đầu vào:** Căn hộ, Cư dân, Loại quan hệ (Chủ hộ/Thành viên/Thuê/Tạm trú), Ngày bắt đầu.<br>• **Xử lý:** Kiểm tra cư dân có đang ở căn khác không (chống 2 active); nếu là Chủ hộ thì kiểm tra căn đã có chủ hộ chưa (chống 2 chủ hộ); tự động chuyển trạng thái căn sang 'Đang ở'. | Bản ghi cư trú mới có hiệu lực; đồng bộ trạng thái căn hộ thành công. |
| **F05** | **Chuyển Căn hộ** | • **Đầu vào:** Mã cư dân, Căn hộ mới, Ngày chuyển, Loại quan hệ mới.<br>• **Xử lý:** Thực hiện trong 1 giao dịch an toàn (Transaction): Kết thúc cư trú tại căn cũ (set `EndDate`, `IsActive=0`); kiểm tra trạng thái căn cũ (nếu hết người thì đổi về 'Trống'); tạo bản ghi cư trú mới tại căn mới. | Cư dân đổi sang căn hộ mới an toàn, không tạo ra 2 nơi cư trú đồng thời. |
| **F06** | **Kết thúc Cư trú** | • **Đầu vào:** Mã bản ghi cư trú, Ngày kết thúc.<br>• **Xử lý:** Kiểm tra `Ngày kết thúc >= Ngày bắt đầu`; cập nhật `IsActive = 0`; kiểm tra căn hộ còn cư dân active nào khác không, nếu không còn ai thì đổi trạng thái căn sang 'Trống'. | Chấm dứt quyền cư trú của cư dân tại căn hộ; cập nhật trạng thái căn hộ tự động. |
| **F07** | **Quản lý Phương tiện** | • **Đầu vào:** Biển số, loại xe (ô tô, xe máy, xe điện), hãng xe, màu xe, căn hộ, chủ sở hữu.<br>• **Xử lý:** Kiểm tra trùng biển số; kiểm tra chủ xe có phải cư dân đang active tại căn hộ không; kiểm tra hạn mức phương tiện quy định của căn hộ; BQL xét duyệt/từ chối. | Ghi nhận xe; cấp trạng thái duyệt (Đã duyệt / Từ chối); hiển thị danh sách xe tòa nhà. |
| **F08** | **Duyệt yêu cầu thành viên** | • **Đầu vào:** Yêu cầu đăng ký thành viên của cư dân (họ tên, ngày sinh, CCCD, quan hệ).<br>• **Xử lý:** BQL xem thông tin; duyệt yêu cầu trong Transaction: tự động tạo hồ sơ cư dân mới (nếu chưa có) và tự động tạo bản ghi cư trú mới; hoặc từ chối kèm lý do phản hồi. | Thành viên mới được bổ sung vào hộ chính thức; phản hồi trạng thái cho cư dân. |

---

## 2.3. Thiết kế cơ sở dữ liệu

### 2.3.1. Mô hình dữ liệu tổng quát
Cơ sở dữ liệu của hệ thống được thiết kế theo chuẩn cơ sở dữ liệu quan hệ (RDBMS), tập trung xoay quanh mối quan hệ giữa **Căn hộ (Apartment)** và **Cư dân (Resident)** thông qua thực thể trung gian là **Lịch sử Cư trú (ResidenceHistory)**. Đồng thời, các thực thể **Phương tiện (Vehicle)**, **Tài khoản (UserAccount)** và **Yêu cầu thành viên (MemberRegistrationRequest)** được liên kết chặt chẽ để đảm bảo toàn vẹn dữ liệu.

### 2.3.2. Chi tiết các bảng dữ liệu trong hệ thống

#### a) Bảng Apartments (Căn hộ)
Lưu trữ danh mục các căn hộ thuộc tòa nhà chung cư:

| STT | Tên trường | Kiểu dữ liệu | Khóa / Ràng buộc | Mô tả ý nghĩa |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `Id` | INT | PK, Auto Increment | Khóa chính, mã định danh căn hộ |
| 2 | `ApartmentNumber` | VARCHAR(20) | Unique, Not Null | Số hiệu căn hộ (Ví dụ: A-1203, B-0504) |
| 3 | `Floor` | INT | Not Null | Vị trí tầng của căn hộ |
| 4 | `Area` | DOUBLE | Not Null | Diện tích sử dụng của căn hộ ($m^2$) |
| 5 | `Status` | INT | Not Null | Trạng thái sử dụng (0: Trống - Empty, 1: Đang ở - Occupied) |

#### b) Bảng Residents (Cư dân)
Lưu trữ thông tin nhân thân lý lịch của cư dân:

| STT | Tên trường | Kiểu dữ liệu | Khóa / Ràng buộc | Mô tả ý nghĩa |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `Id` | INT | PK, Auto Increment | Khóa chính, mã định danh cư dân |
| 2 | `FullName` | VARCHAR(100) | Not Null | Họ và tên đầy đủ của cư dân |
| 3 | `DateOfBirth` | DATETIME(6) | Not Null | Ngày tháng năm sinh |
| 4 | `IdentityCard` | VARCHAR(20) | Nullable | Số thẻ CCCD/CMND (chuẩn 12 chữ số) |
| 5 | `PhoneNumber` | VARCHAR(15) | Nullable | Số điện thoại liên lạc |
| 6 | `Email` | VARCHAR(100) | Nullable | Địa chỉ hòm thư điện tử |
| 7 | `Gender` | INT | Nullable | Giới tính (0: Nam, 1: Nữ, 2: Khác) |
| 8 | `HomeTown` | VARCHAR(200) | Nullable | Quê quán / Địa chỉ nguyên quán |

#### c) Bảng ResidenceHistories (Lịch sử Cư trú)
Ghi nhận quan hệ và lịch sử cư trú giữa cư dân và căn hộ qua các thời kỳ:

| STT | Tên trường | Kiểu dữ liệu | Khóa / Ràng buộc | Mô tả ý nghĩa |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `Id` | INT | PK, Auto Increment | Khóa chính bản ghi cư trú |
| 2 | `ApartmentId` | INT | FK (Apartments) | Mã căn hộ cư trú |
| 3 | `ResidentId` | INT | FK (Residents) | Mã cư dân cư trú |
| 4 | `RelationshipType` | INT | Not Null | Loại cư trú (0: Chủ hộ, 1: Thành viên, 2: Thuê, 3: Tạm trú) |
| 5 | `StartDate` | DATETIME(6) | Not Null | Ngày bắt đầu cư trú tại căn hộ |
| 6 | `EndDate` | DATETIME(6) | Nullable | Ngày kết thúc cư trú (NULL nếu đang ở) |
| 7 | `IsActive` | TINYINT(1) | Default 1 | Trạng thái (1: Đang ở hiện tại, 0: Đã chuyển đi) |
| 8 | `ActiveOwnerApartmentId` | INT | Virtual, Unique | Cột tính toán VIRTUAL: Khóa ràng buộc mỗi căn chỉ 1 chủ hộ active |
| 9 | `ActiveResidentId` | INT | Virtual, Unique | Cột tính toán VIRTUAL: Khóa ràng buộc mỗi người chỉ active ở 1 căn |

#### d) Bảng Vehicles (Phương tiện)
Lưu trữ và quản lý phương tiện cá nhân của cư dân:

| STT | Tên trường | Kiểu dữ liệu | Khóa / Ràng buộc | Mô tả ý nghĩa |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `Id` | INT | PK, Auto Increment | Khóa chính phương tiện |
| 2 | `LicensePlate` | VARCHAR(20) | Unique, Not Null | Biển kiểm soát xe (Ví dụ: 29A-123.45) |
| 3 | `VehicleType` | INT | Not Null | Loại phương tiện (0: Xe máy, 1: Ô tô, 2: Xe điện) |
| 4 | `Brand` | VARCHAR(50) | Nullable | Hãng sản xuất / Nhãn hiệu xe (Honda, Toyota...) |
| 5 | `Color` | VARCHAR(30) | Nullable | Màu sơn của phương tiện |
| 6 | `ApartmentId` | INT | FK (Apartments) | Căn hộ đăng ký phương tiện |
| 7 | `ResidentId` | INT | FK (Residents) | Cư dân đứng tên chủ sở hữu xe |
| 8 | `Status` | INT | Not Null | Trạng thái duyệt (0: Chờ duyệt, 1: Đã duyệt, 2: Từ chối) |
| 9 | `RegistrationDate` | DATETIME(6) | Not Null | Thời điểm gửi hồ sơ đăng ký xe |

#### e) Bảng UserAccounts (Tài khoản người dùng)
Lưu trữ tài khoản người dùng và thông tin phân quyền bảo mật:

| STT | Tên trường | Kiểu dữ liệu | Khóa / Ràng buộc | Mô tả ý nghĩa |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `Id` | INT | PK, Auto Increment | Khóa chính tài khoản |
| 2 | `Username` | VARCHAR(50) | Unique, Not Null | Tên đăng nhập hệ thống (`admin`, `resident_a`...) |
| 3 | `PasswordHash` | LONGTEXT | Not Null | Chuỗi mật khẩu đã được băm bảo mật BCrypt |
| 4 | `Role` | INT | Not Null | Vai trò người dùng (0: Admin - Quản lý, 1: Resident - Cư dân) |
| 5 | `ResidentId` | INT | FK (Residents), Nullable | Liên kết hồ sơ cư dân (NULL nếu là Admin) |
| 6 | `IsActive` | TINYINT(1) | Default 1 | Trạng thái tài khoản (1: Hoạt động, 0: Bị khóa) |
| 7 | `CreatedAt` | DATETIME(6) | Not Null | Thời điểm tạo tài khoản |

#### f) Bảng MemberRegistrationRequests (Yêu cầu đăng ký thành viên)
Lưu trữ các yêu cầu đăng ký bổ sung thành viên do cư dân gửi lên:

| STT | Tên trường | Kiểu dữ liệu | Khóa / Ràng buộc | Mô tả ý nghĩa |
| :---: | :--- | :--- | :--- | :--- |
| 1 | `Id` | INT | PK, Auto Increment | Khóa chính yêu cầu |
| 2 | `ApartmentId` | INT | FK (Apartments) | Căn hộ xin bổ sung thêm thành viên |
| 3 | `RequesterResidentId` | INT | FK (Residents) | Cư dân (chủ hộ/người đại diện) gửi yêu cầu |
| 4 | `FullName` | VARCHAR(150) | Not Null | Họ tên của thành viên mới cần đăng ký |
| 5 | `DateOfBirth` | DATETIME(6) | Not Null | Ngày sinh của thành viên mới |
| 6 | `IdentityCard` | VARCHAR(20) | Nullable | Số CCCD của thành viên mới |
| 7 | `PhoneNumber` | VARCHAR(20) | Nullable | Số điện thoại thành viên mới |
| 8 | `RelationshipType` | INT | Not Null | Quan hệ cư trú (Thành viên gia đình, Tạm trú...) |
| 9 | `Status` | INT | Not Null | Trạng thái xử lý (0: Chờ duyệt, 1: Đã duyệt, 2: Từ chối) |
| 10 | `CreatedAt` | DATETIME(6) | Not Null | Thời điểm gửi yêu cầu |
| 11 | `ProcessedAt` | DATETIME(6) | Nullable | Thời điểm Ban quản lý xét duyệt / từ chối |
| 12 | `RejectReason` | VARCHAR(500) | Nullable | Lý do từ chối (nếu không được duyệt) |

### 2.3.3. Các quan hệ và ràng buộc toàn vẹn dữ liệu
* **Quan hệ Khóa ngoại (Foreign Keys):** Bảng `ResidenceHistories` tham chiếu đến `Apartments(Id)` và `Residents(Id)` với quy tắc `OnDelete Restrict`, ngăn chặn tuyệt đối việc xóa nhầm một căn hộ hoặc một cư dân khi đang có lịch sử cư trú ràng buộc.
* **Ràng buộc toàn vẹn Chủ hộ độc nhất (Unique Active Owner):** Cột tính toán VIRTUAL `ActiveOwnerApartmentId` kết hợp chỉ mục duy nhất `UNIQUE INDEX` đảm bảo một căn hộ không thể có 2 chủ hộ cùng active.
* **Ràng buộc toàn vẹn Nơi cư trú độc nhất (Unique Active Resident):** Cột tính toán VIRTUAL `ActiveResidentId` kết hợp chỉ mục duy nhất `UNIQUE INDEX` đảm bảo một người không thể được ghi nhận đang ở 2 căn hộ khác nhau cùng một thời điểm.
* **Ràng buộc kiểm tra hợp lệ logic (Check Constraints):** Ràng buộc `CK_ResidenceHistories_DateRange` đảm bảo $EndDate \ge StartDate$; ràng buộc `CK_ResidenceHistories_ActiveEndDate` đảm bảo nếu $IsActive = 1$ thì $EndDate$ bắt buộc là NULL, và ngược lại khi $IsActive = 0$ thì $EndDate$ bắt buộc phải có giá trị ngày cụ thể.

---

## 2.4. Thiết kế giao diện

### 2.4.1. Cấu trúc tổng thể và các màn hình chính
Giao diện hệ thống được thiết kế theo phong cách phẳng hiện đại (Modern Flat UI), sử dụng tông màu chủ đạo Xanh Navy (`#0F172A`) kết hợp Xanh Blue (`#2563EB`) tạo cảm giác chuyên nghiệp, tin cậy. Cấu trúc màn hình gồm các phân vùng làm việc chính:

1. **Màn hình Đăng nhập (Login Window):** Thiết kế dạng hộp thoại chia đôi cân đối: bên trái là hình ảnh đại diện thương hiệu và thông điệp chào mừng; bên phải là form đăng nhập gồm ô nhập Username, Password, nút 'Đăng nhập' và 2 nút hỗ trợ chọn nhanh tài khoản mẫu (Admin và Cư dân) giúp người dùng và kiểm thử viên trải nghiệm tức thì.
2. **Cửa sổ làm việc chính (Main Window):** Bao gồm Sidebar điều hướng cố định bên trái (hiển thị menu động tương ứng theo vai trò Admin hoặc Resident) và Topbar bên trên (hiển thị lời chào, thông tin căn hộ, chuông thông báo và nút Đăng xuất). Khu vực trung tâm là `ContentControl` động hiển thị các trang chức năng (Views).
3. **Màn hình Quản lý Căn hộ (Apartment View):** Bố trí thanh công cụ lọc theo Tầng và Trạng thái, ô tìm kiếm nhanh, bảng DataGrid hiển thị thông tin căn hộ với các thẻ trạng thái có màu trực quan (Xanh lá: Đang ở, Xám: Trống) cùng hộp thoại thêm mới / cập nhật căn hộ.
4. **Màn hình Quản lý Cư dân (Resident View):** Bảng dữ liệu danh sách cư dân tòa nhà, hỗ trợ tìm kiếm nhanh theo Họ tên, CCCD hoặc Số điện thoại; nút thêm mới cư dân và chức năng xem hồ sơ chi tiết.
5. **Màn hình Quản lý Cư trú (Residency View):** Màn hình tác nghiệp chính của Ban quản lý gồm bảng danh sách các quan hệ cư trú, các nút nghiệp vụ chuyên biệt: Đăng ký cư trú, Kết thúc cư trú, Chuyển căn hộ và hộp thoại tra cứu dòng thời gian (Timeline) lịch sử cư trú của từng căn hộ.
6. **Màn hình Quản lý Phương tiện (Vehicle View):** Hiển thị danh sách phương tiện kèm biển số, căn hộ, chủ sở hữu và nhãn phân loại xe (Ô tô, Xe máy). Ban quản lý có các nút Duyệt và Từ chối yêu cầu gửi xe.
7. **Cổng thông tin Cư dân (Resident Portal Views):** Dành riêng cho cư dân gồm: Trang chủ cư dân (`ResidentHomeView`) tóm tắt thông tin gia đình; Căn hộ của tôi (`MyApartmentView`) xem chi tiết căn hộ và danh sách các thành viên trong hộ kèm ảnh đại diện; Hồ sơ cá nhân (`MyProfileView`) xem và cập nhật thông tin liên hệ; cùng form gửi yêu cầu đăng ký thêm thành viên.

### 2.4.2. Cách thức tương tác và vận hành giao diện
* **Cơ chế Chuyển đổi màn hình mượt mà (Dynamic Content Template):** Thay vì mở nhiều cửa sổ con (gây rối mắt và khó quản lý bộ nhớ), hệ thống sử dụng một cửa sổ chính duy nhất (`MainWindow`). Khi người dùng nhấn nút trên Sidebar, ViewModel tương ứng được gán vào `CurrentViewModel`, WPF sẽ tự động áp dụng `DataTemplate` để nạp View tương ứng tức thì.
* **Tương tác dữ liệu thời gian thực (Data Binding & MVVM):** Tất cả các ô nhập liệu, danh sách và nút bấm đều được liên kết dữ liệu hai chiều. Khi dữ liệu trong cơ sở dữ liệu thay đổi, danh sách trên giao diện được làm mới tự động mà không làm đơ giật màn hình.
* **Hệ thống thông báo và phản hồi người dùng chuẩn mực:** Tất cả các thao tác thành công hoặc lỗi vi phạm nghiệp vụ đều được hiển thị qua thanh cảnh báo NotificationBanner gắn liền hoặc hộp thoại thông báo chuyên nghiệp (`NotificationDialog`) bằng tiếng Việt rõ ràng, giúp người dùng dễ dàng nhận biết và chỉnh sửa.

---

## 2.5. Các vấn đề khác

### 2.5.1. Bảo mật tài khoản và mật khẩu
Hệ thống áp dụng chính sách bảo mật đa tầng nhằm bảo vệ tuyệt đối dữ liệu người dùng:
* **Mã hóa mật khẩu một chiều bằng thuật toán BCrypt:** Tuyệt đối không lưu trữ mật khẩu dưới dạng văn bản thô (Plain Text) trong cơ sở dữ liệu. Mật khẩu được băm (hashing) thông qua thuật toán BCrypt kết hợp chuỗi muối ngẫu nhiên (salt). Kỹ thuật này giúp vô hiệu hóa hoàn toàn các nguy cơ bị tấn công trích xuất mật khẩu ngay cả khi cơ sở dữ liệu bị lộ.
* **Phòng chống tấn công SQL Injection:** Toàn bộ các thao tác truy vấn cơ sở dữ liệu đều được thực hiện thông qua cơ chế tham số hóa (parameterized queries) tự động của ORM, ngăn chặn triệt để các hành vi chèn mã độc vào câu lệnh SQL.

### 2.5.2. Phân quyền truy cập theo vai trò (Role-Based Access Control - RBAC)
* **Tách biệt hoàn toàn không gian làm việc:** Hệ thống chia làm 2 vai trò: Ban quản lý (Admin) và Cư dân (Resident). Sau khi đăng nhập, hệ thống kiểm tra trường Role để quyết định các tính năng được phép hiển thị.
* **Bảo vệ dữ liệu riêng tư của cư dân:** Cư dân chỉ được phép truy cập và xem dữ liệu thuộc quyền sở hữu của chính căn hộ mình đang sinh sống. Các dữ liệu của căn hộ khác, cư dân khác hoặc các công cụ quản trị hệ thống của Ban quản lý hoàn toàn bị ẩn và chặn truy cập.

### 2.5.3. An toàn dữ liệu và tính toàn vẹn giao dịch (Transaction Management)
* **Bảo đảm tính nguyên tử (Atomicity):** Các nghiệp vụ phức tạp liên quan đến nhiều bảng dữ liệu (như nghiệp vụ Chuyển căn hộ hoặc Duyệt yêu cầu thêm thành viên) bắt buộc phải được bọc trong một Giao dịch (Database Transaction). Nếu bất kỳ một bước nào gặp sự cố, toàn bộ giao dịch sẽ tự động Rollback về trạng thái an toàn ban đầu, không để lại dữ liệu rác.
* **Cơ chế tự phục hồi kết nối (Resilient Connection):** Khi làm việc với cơ sở dữ liệu Cloud, hệ thống được cấu hình cơ chế tự động thử lại kết nối (Retry on Failure) giúp ứng dụng hoạt động bền bỉ, không bị sập bất ngờ khi mạng gặp chập chờn nhất thời.

