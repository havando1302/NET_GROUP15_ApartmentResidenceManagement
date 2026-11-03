# CHƯƠNG 1. XÁC ĐỊNH YÊU CẦU

## 1.1. Bối cảnh và đối tượng sử dụng

### 1.1.1. Bối cảnh thực tế
Trong bối cảnh đô thị hóa ngày càng diễn ra mạnh mẽ, các khu nhà ở chung cư mọc lên ngày càng nhiều và trở thành xu hướng sinh sống tất yếu tại các thành phố lớn. Một tòa nhà chung cư tiêu chuẩn thường bao gồm từ hàng trăm đến hàng nghìn căn hộ với quy mô dân số tương đương một khu dân cư thu nhỏ. Điểm đặc thù của môi trường chung cư là tính biến động cư trú diễn ra liên tục và rất thường xuyên: người mua căn hộ dọn đến ở, chủ căn hộ cho thuê lại, cư dân chuyển đổi căn hộ trong cùng tòa nhà, khách đến tạm trú hoặc các trường hợp kết thúc hợp đồng thuê chuyển đi nơi khác. Điều này đòi hỏi công tác quản lý thông tin nhân khẩu và tình trạng cư trú phải luôn được cập nhật chính xác, kịp thời và có tính hệ thống cao.

### 1.1.2. Đối tượng sử dụng hệ thống
Hệ thống hướng đến phục vụ hai nhóm đối tượng người dùng chính trong phạm vi tòa nhà chung cư:
* **Ban quản lý tòa nhà (Admin / Ban quản trị):** Bao gồm các cán bộ, nhân viên văn phòng quản lý tòa nhà – những người trực tiếp chịu trách nhiệm vận hành, tiếp nhận hồ sơ cư dân, theo dõi tình trạng sử dụng căn hộ, cấp phát tài khoản, phê duyệt các yêu cầu đăng ký cư trú và quản lý phương tiện của cư dân.
* **Cư dân sinh sống tại tòa nhà (Resident):** Bao gồm tất cả các cá nhân đang sinh sống hợp pháp trong các căn hộ của tòa nhà (chủ sở hữu căn hộ, các thành viên trong gia đình, hoặc người thuê căn hộ). Cư dân có nhu cầu tra cứu thông tin căn hộ của mình, kiểm tra danh sách thành viên cùng ở, theo dõi phương tiện và gửi các yêu cầu thay đổi nhân khẩu đến Ban quản lý.

### 1.1.3. Cách thức quản lý hiện tại
Tại nhiều tòa chung cư quy mô vừa và nhỏ hiện nay, công tác quản lý cư trú phần lớn vẫn được thực hiện theo phương thức bán thủ công: hồ sơ nhân khẩu được lưu trữ trên sổ sách giấy tờ kết hợp với các bảng tính Excel phân tán trên máy tính cá nhân của từng nhân viên quản lý. Khi cư dân chuyển đến hoặc chuyển đi, nhân viên ghi chép vào sổ theo dõi hoặc sửa đổi trực tiếp trên file Excel. Thủ tục khai báo thành viên mới hoặc đăng ký gửi xe đòi hỏi cư dân phải đến trực tiếp văn phòng ban quản lý trong giờ hành chính để điền các biểu mẫu giấy tờ.

---

## 1.2. Vấn đề cần giải quyết

### 1.2.1. Những khó khăn, bất cập trong công tác quản lý hiện tại
* **Dữ liệu phân tán và thiếu tính đồng bộ:** Việc lưu trữ thông tin trên các file Excel rời rạc hoặc sổ giấy khiến dữ liệu dễ bị sai lệch khi có nhiều người cùng tham gia cập nhật. Không có một nguồn dữ liệu tập trung, chính xác để tất cả các bộ phận cùng tra cứu.
* **Nguy cơ sai sót và vi phạm tính toàn vẹn dữ liệu:** Quản lý thủ công hoàn toàn thiếu cơ chế kiểm soát tự động, dẫn đến các sai sót nghiêm trọng như: trùng lặp hồ sơ nhân khẩu, một căn hộ có hai chủ hộ cùng tồn tại, hoặc một cư dân được ghi nhận đang cư trú đồng thời ở hai căn hộ khác nhau.
* **Tra cứu thông tin và lịch sử khó khăn, tốn thời gian:** Khi cần xác minh quá trình sinh sống của một cư dân hay tra cứu lịch sử thay đổi chủ hộ của một căn hộ, nhân viên quản lý phải lật tìm lại từng trang sổ lưu trữ cũ hoặc nhiều file bảng tính khác nhau, gây mất nhiều công sức và dễ bỏ sót thông tin.
* **Bất tiện cho cư dân trong giao dịch hành chính:** Cư dân không có kênh tra cứu thông tin trực tuyến, phải trực tiếp đến văn phòng quản lý để khai báo hoặc kiểm tra thông tin, gây lãng phí thời gian và phiền hà cho cả hai bên.
* **Khó khăn trong việc kiểm soát phương tiện gắn với căn hộ:** Việc kiểm tra số lượng phương tiện thực tế của từng căn hộ có vượt định mức quy định hay không gặp nhiều trở ngại do thiếu sự liên kết chặt chẽ giữa dữ liệu xe với dữ liệu cư dân đang hoạt động.

### 1.2.2. Vấn đề hệ thống cần giải quyết
Nhằm khắc phục triệt để các tồn tại trên, hệ thống cần giải quyết các bài toán trọng tâm sau:
* **Xây dựng cơ sở dữ liệu tập trung và chuẩn hóa:** Lưu trữ thống nhất toàn bộ thông tin căn hộ, nhân khẩu cư dân, lịch sử cư trú và danh sách phương tiện trên một nền tảng an toàn, tin cậy.
* **Thiết lập các quy tắc ràng buộc nghiệp vụ tự động:** Đảm bảo các quy tắc quản lý luôn được thực thi nghiêm ngặt: mỗi căn hộ chỉ có duy nhất một chủ hộ đang hiệu lực, mỗi cư dân chỉ có một nơi ở chính thức tại một thời điểm, và trạng thái căn hộ tự động cập nhật đồng bộ với biến động cư trú thực tế.
* **Cung cấp công cụ tác nghiệp trực quan cho Ban quản lý:** Tự động hóa các thao tác đăng ký cư trú, chuyển căn hộ, kết thúc cư trú và phê duyệt phương tiện, giúp tra cứu thông tin chỉ trong vài giây.
* **Cung cấp kênh tương tác số dành cho Cư dân:** Cho phép cư dân chủ động theo dõi thông tin hồ sơ của gia đình và gửi các yêu cầu đăng ký thành viên mới trực tuyến.

---

## 1.3. Yêu cầu chung

### 1.3.1. Mục tiêu hệ thống
Mục tiêu tổng quát là xây dựng một hệ thống phần mềm chuyên trách, tin cậy và tiện dụng, phục vụ công tác số hóa toàn diện quy trình quản lý thông tin cư trú và phương tiện tại một tòa nhà chung cư; mang lại sự minh bạch, chính xác trong quản trị của Ban quản lý và sự tiện lợi tối đa cho cư dân.

### 1.3.2. Phạm vi xử lý
Hệ thống tập trung xử lý dữ liệu nội bộ trong khuôn viên của một tòa nhà chung cư đơn lẻ, bao gồm:
* **Danh mục căn hộ:** Số căn, tầng, diện tích, trạng thái trống hoặc đang có người sinh sống.
* **Hồ sơ cư dân:** Thông tin cá nhân, định danh, số điện thoại, ngày sinh và lý lịch cư trú.
* **Biến động cư trú:** Đăng ký mới, kết thúc cư trú, chuyển đổi căn hộ và lưu vết toàn bộ lịch sử qua các thời kỳ.
* **Phương tiện cá nhân:** Đăng ký, kiểm tra hạn mức và phê duyệt phương tiện gắn liền với căn hộ.
* **Yêu cầu thành viên:** Tiếp nhận, xử lý và phê duyệt yêu cầu đăng ký bổ sung thành viên của cư dân.

### 1.3.3. Đối tượng sử dụng và quyền hạn
* **Nhóm Quản trị viên (Admin):** Toàn quyền quản lý danh mục căn hộ, cư dân, cư trú, phương tiện, xét duyệt yêu cầu và phân quyền tài khoản.
* **Nhóm Cư dân (Resident):** Quyền xem thông tin cá nhân, căn hộ đang sinh sống, danh sách thành viên cùng hộ, phương tiện đã đăng ký và gửi yêu cầu đăng ký thành viên mới.

### 1.3.4. Các nhóm chức năng chính
* **1. Phân hệ Quản lý Căn hộ:** Quản lý danh sách, thông tin chi tiết và trạng thái sử dụng của từng căn hộ trong tòa nhà.
* **2. Phân hệ Quản lý Cư dân:** Lưu trữ, tra cứu, thêm mới và cập nhật hồ sơ thông tin cá nhân của cư dân.
* **3. Phân hệ Quản lý Cư trú:** Thực hiện nghiệp vụ đăng ký cư trú, chuyển căn hộ, kết thúc cư trú và tra cứu lịch sử cư trú.
* **4. Phân hệ Quản lý Phương tiện:** Tiếp nhận, xét duyệt và quản lý danh sách phương tiện cá nhân của cư dân theo căn hộ.
* **5. Phân hệ Cổng Cư dân (Resident Portal):** Giao diện tra cứu thông tin hộ gia đình và gửi yêu cầu hành chính trực tuyến cho cư dân.
* **6. Phân hệ Quản lý Tài khoản & Xác thực:** Đăng nhập phân quyền bảo mật, cấp phát tài khoản cư dân và quản lý trạng thái tài khoản.

### 1.3.5. Định hướng tổng thể của hệ thống
Hệ thống được định hướng xây dựng theo các tiêu chuẩn:
* **Tính đơn giản và tiện dụng:** Giao diện thân thiện, các luồng thao tác ngắn gọn, dễ hiểu, phù hợp với mọi đối tượng người dùng.
* **Tính toàn vẹn và nhất quán:** Đặt tính đúng đắn của dữ liệu lên hàng đầu thông qua hệ thống kiểm soát quy tắc nghiệp vụ tự động.
* **Kiến trúc phân lớp chuẩn mực:** Tách biệt rõ ràng giữa giao diện hiển thị, xử lý nghiệp vụ và lưu trữ dữ liệu, tạo thuận lợi cho việc bảo trì và nâng cấp.

---

## 1.4. Yêu cầu cụ thể

### 1.4.1. Yêu cầu chức năng chi tiết
* **Đăng nhập và phân quyền:** Xác thực danh tính người dùng bằng tên đăng nhập và mật khẩu; điều hướng chính xác vào giao diện làm việc tương ứng với vai trò (Admin hoặc Resident).
* **Quản lý danh mục căn hộ:** Hiển thị danh sách căn hộ kèm trạng thái (Trống / Đang ở); cho phép thêm mới, chỉnh sửa thông tin căn hộ (tầng, diện tích); tìm kiếm và lọc căn hộ theo tầng và trạng thái.
* **Quản lý hồ sơ cư dân:** Thêm mới hồ sơ cư dân với đầy đủ thông tin cá nhân; cập nhật chỉnh sửa thông tin liên hệ; tìm kiếm cư dân theo họ tên, số CCCD hoặc số điện thoại; kiểm tra ràng buộc trước khi xóa hồ sơ cư dân.
* **Đăng ký và biến động cư trú:** Đăng ký cư trú mới với vai trò xác định (Chủ hộ, Thành viên, Người thuê, Tạm trú); chuyển căn hộ cho cư dân từ căn cũ sang căn mới đảm bảo tính liên tục; kết thúc cư trú khi cư dân rời đi.
* **Theo dõi lịch sử cư trú:** Truy xuất toàn bộ danh sách những người đã và đang sinh sống tại một căn hộ bất kỳ, cũng như quá trình thay đổi chỗ ở của một cư dân theo trục thời gian.
* **Quản lý phương tiện cá nhân:** Tiếp nhận thông tin xe (biển số, loại xe, màu xe, hãng xe); kiểm tra hạn mức xe của căn hộ; Ban quản lý thực hiện phê duyệt hoặc từ chối kèm lý do.
* **Cổng thông tin cư dân:** Cư dân xem tổng quan căn hộ, danh sách thành viên cùng sống trong căn hộ; xem hồ sơ cá nhân và cập nhật số điện thoại, email liên hệ; gửi yêu cầu đăng ký thêm thành viên mới.
* **Xử lý yêu cầu thành viên:** Ban quản lý xem danh sách các yêu cầu đang chờ xử lý; duyệt yêu cầu (hệ thống tự động tạo hồ sơ cư dân và đăng ký cư trú) hoặc từ chối yêu cầu.

### 1.4.2. Dữ liệu cần quản lý
* **Dữ liệu căn hộ:** Mã căn hộ (duy nhất), số tầng, diện tích (m²), trạng thái căn hộ (Trống, Đang ở).
* **Dữ liệu cư dân:** Mã cư dân, họ và tên, ngày sinh, giới tính, số định danh cá nhân (CCCD/CMND), số điện thoại, email, quê quán.
* **Dữ liệu quan hệ cư trú:** Căn hộ liên kết, cư dân liên kết, loại quan hệ cư trú (Chủ hộ, Thành viên, Người thuê, Tạm trú), ngày bắt đầu cư trú, ngày kết thúc cư trú, trạng thái đang hoạt động.
* **Dữ liệu phương tiện:** Biển số xe (duy nhất), loại phương tiện (Ô tô, Xe máy, Xe điện), nhãn hiệu, màu sơn, chủ sở hữu, căn hộ đăng ký, trạng thái duyệt (Chờ duyệt, Đã duyệt, Từ chối).
* **Dữ liệu yêu cầu thành viên:** Mã yêu cầu, căn hộ, người gửi yêu cầu, thông tin người được đề nghị (họ tên, ngày sinh, CCCD, SĐT, quan hệ), trạng thái yêu cầu, thời gian gửi, thời gian xử lý, lý do từ chối.
* **Dữ liệu tài khoản:** Tên đăng nhập (duy nhất), mật khẩu bảo mật, vai trò sử dụng, liên kết hồ sơ cư dân, trạng thái hoạt động (kích hoạt/khóa).

### 1.4.3. Yêu cầu xử lý chính (Quy tắc nghiệp vụ)
* **Ràng buộc tính hợp lệ đầu vào:** Số CCCD bắt buộc phải đủ 12 chữ số hợp lệ; ngày sinh không được ở tương lai và tuổi hợp lệ; số điện thoại đúng định dạng; họ tên không được để trống.
* **Ràng buộc duy nhất Chủ hộ (Single Active Owner):** Tại mỗi thời điểm, một căn hộ chỉ có thể có duy nhất một cư dân giữ vai trò Chủ hộ đang hoạt động.
* **Ràng buộc duy nhất nơi cư trú (Single Active Residence):** Một cư dân tại một thời điểm chỉ có thể cư trú đang hoạt động tại duy nhất một căn hộ trong tòa nhà.
* **Ràng buộc tính hợp lý thời gian:** Ngày kết thúc cư trú phải lớn hơn hoặc bằng ngày bắt đầu cư trú; ngày bắt đầu không được để trống.
* **Đồng bộ trạng thái căn hộ tự động:** Khi một căn hộ có ít nhất một cư dân đang hoạt động, trạng thái căn hộ tự động chuyển sang 'Đang ở'. Khi tất cả cư dân đều đã kết thúc cư trú, trạng thái căn hộ tự động trở về 'Trống'.
* **Tính an toàn giao dịch:** Nghiệp vụ chuyển căn hộ và duyệt yêu cầu thành viên phải được thực hiện theo cơ chế giao dịch an toàn (toàn bộ các bước cùng thành công hoặc khôi phục trạng thái ban đầu nếu có lỗi phát sinh).

### 1.4.4. Yêu cầu phi chức năng
* **Tính tin cậy và chính xác:** Dữ liệu được lưu trữ an toàn, nhất quán, không xảy ra sai lệch hay mất mát trong quá trình vận hành.
* **Tính hiệu năng:** Thời gian phản hồi các thao tác tra cứu, tìm kiếm danh sách và ghi nhận dữ liệu nhanh chóng, mượt mà.
* **Tính bảo mật:** Phân định quyền hạn rõ ràng giữa Quản trị viên và Cư dân; mật khẩu người dùng được mã hóa bảo vệ, ngăn ngừa truy cập trái phép.
* **Giao diện và trải nghiệm người dùng:** Thiết kế bố cục trực quan, màu sắc hài hòa, hỗ trợ tiếng Việt đầy đủ và có thông báo lỗi cụ thể, rõ ràng khi người dùng thao tác sai.

---

## 1.5. Phạm vi và giới hạn

### 1.5.1. Những nội dung thực hiện
* Xây dựng hoàn chỉnh các phân hệ quản lý: Căn hộ, cư dân, biến động cư trú và phương tiện dành cho Ban quản lý tòa nhà.
* Xây dựng cổng thông tin dành cho Cư dân: Cung cấp giao diện tra cứu hồ sơ cá nhân, căn hộ, thành viên gia đình và gửi yêu cầu đăng ký thành viên mới trực tuyến.
* Cài đặt đầy đủ các cơ chế ràng buộc nghiệp vụ: Đảm bảo tính toàn vẹn dữ liệu cư trú theo đúng các bất biến quản lý chung cư.
* Hệ thống phân quyền hai lớp: Xác thực danh tính an toàn cho Admin và Resident.

### 1.5.2. Những nội dung không thực hiện (Giới hạn của đề tài)
* **Không quản lý bãi giữ xe thông minh:** Không tích hợp camera nhận diện biển số tự động tại cổng, không điều khiển phần cứng barie ra vào.
* **Không quản lý tài chính kế toán:** Không tính toán biểu phí dịch vụ hàng tháng, không tính hóa đơn tiền điện, tiền nước và không tích hợp cổng thanh toán trực tuyến.
* **Không kết nối cơ sở dữ liệu quốc gia:** Không tích hợp trực tiếp với hệ thống định danh VNeID hay cơ sở dữ liệu của cơ quan Công an quản lý cư trú.
* **Không tích hợp phần cứng giám sát:** Không kết nối với hệ thống camera an ninh tòa nhà, chuông hình hay thiết bị khóa thông minh.

