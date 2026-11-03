# MỞ ĐẦU

## 1. Lý do chọn đề tài
Trong những năm gần đây, cùng với tốc độ đô thị hóa nhanh chóng tại các trung tâm kinh tế lớn, mô hình nhà ở chung cư ngày càng phát triển mạnh mẽ và trở thành lựa chọn an cư hàng đầu của đông đảo người dân. Sự hình thành của các tòa nhà chung cư hiện đại với quy mô hàng trăm, thậm chí hàng nghìn cư dân sinh sống mang lại không gian sống tiện nghi, văn minh, song đồng thời cũng đặt ra bài toán phức tạp và áp lực rất lớn cho Ban quản lý tòa nhà trong công tác vận hành, kiểm soát nhân khẩu và quản lý cư trú.

Trên thực tế tại nhiều tòa chung cư hiện nay, việc quản lý hồ sơ cư dân, theo dõi tình trạng căn hộ và ghi nhận các biến động cư trú (như đăng ký tạm trú, chuyển căn hộ, tiếp nhận thành viên mới, hoặc kết thúc thời gian lưu trú) vẫn còn phụ thuộc nặng nề vào các phương thức thủ công như sổ sách truyền thống hoặc các file bảng tính Excel rời rạc. Phương thức quản lý này bộc lộ rất nhiều hạn chế: dữ liệu phân tán, dễ thất lạc, khó khăn trong việc tra cứu nhanh, thiếu các ràng buộc logic chặt chẽ dẫn đến nguy cơ sai sót và trùng lặp thông tin. Mặt khác, về phía cư dân, họ gặp không ít bất tiện khi muốn tra cứu thông tin căn hộ, nhân khẩu trong hộ gia đình hoặc phải mất thời gian đến trực tiếp văn phòng ban quản lý để thực hiện các thủ tục khai báo.

Xuất phát từ thực tiễn nêu trên, nhóm chúng em quyết định lựa chọn đề tài: **"Xây dựng Hệ thống Quản lý Thông tin Cư trú của một Tòa Chung cư"** làm Bài tập lớn môn học. Đề tài tập trung giải quyết bài toán cốt lõi là số hóa và chuẩn hóa toàn diện quy trình quản lý cư trú, giúp nâng cao hiệu quả quản trị của Ban quản lý, đồng thời đem lại trải nghiệm minh bạch, nhanh chóng và thuận tiện cho cư dân sinh sống trong tòa nhà.

---

## 2. Mục đích thực hiện
Mục đích chính của đề tài là xây dựng một hệ thống phần mềm ứng dụng chuyên biệt phục vụ công tác số hóa và quản lý thông tin cư trú tại một tòa chung cư đơn lẻ. Các mục tiêu cụ thể bao gồm:

* **Đối với Ban quản lý tòa nhà:** Cung cấp công cụ quản trị tập trung giúp quản lý toàn diện danh mục căn hộ, hồ sơ lý lịch cư dân; theo dõi chính xác và chặt chẽ lịch sử biến động cư trú (đăng ký mới, kết thúc cư trú, chuyển căn hộ); quản lý phương tiện cá nhân gắn liền với từng hộ gia đình; đảm bảo tính toàn vẹn, tính duy nhất và tính an toàn của cơ sở dữ liệu.
* **Đối với Cư dân tòa nhà:** Cung cấp một cổng tra cứu trực quan, thân thiện, cho phép cư dân chủ động nắm bắt thông tin căn hộ mình đang ở, danh sách các thành viên cùng sinh sống trong hộ, thông tin phương tiện đã đăng ký; hỗ trợ gửi yêu cầu đăng ký thêm thành viên hoặc phản ánh ý kiến đến Ban quản lý một cách dễ dàng.
* **Về phương diện học tập và rèn luyện:** Tạo cơ hội để các thành viên trong nhóm củng cố, vận dụng tổng hợp các kiến thức đã tiếp thu trong môn học vào việc giải quyết một bài toán thực tế mang tính ứng dụng cao; rèn luyện kỹ năng khảo sát nghiệp vụ, tư duy hệ thống, kỹ năng phối hợp làm việc nhóm và chuẩn hóa quy trình phát triển phần mềm.

---

## 3. Đối tượng và phạm vi

### a) Đối tượng nghiên cứu:
* **Nghiệp vụ quản lý:** Quy trình quản lý danh mục căn hộ, hồ sơ nhân thân cư dân và mối quan hệ pháp lý cư trú (Chủ hộ, Thành viên gia đình, Người thuê, Khách tạm trú).
* **Quy trình biến động cư trú:** Các quy định, điều kiện và nghiệp vụ liên quan đến việc tiếp nhận cư trú mới, chuyển đổi căn hộ nội bộ, kết thúc cư trú và đăng ký phương tiện của cư dân.

### b) Đối tượng sử dụng:
* **Ban quản lý tòa nhà (Admin):** Người quản trị hệ thống, có quyền xem, tạo mới, cập nhật, kết thúc hồ sơ cư trú; xét duyệt yêu cầu thành viên; quản lý danh mục căn hộ và phương tiện toàn tòa nhà.
* **Cư dân (Resident):** Người đang sinh sống hợp pháp tại các căn hộ trong tòa nhà, sử dụng tài khoản được cấp để tra cứu hồ sơ cá nhân, căn hộ, thành viên gia đình và gửi yêu cầu hành chính.

### c) Phạm vi của đề tài:
* **Phạm vi không gian:** Áp dụng trong phạm vi quản lý nội bộ của một tòa nhà chung cư độc lập.
* **Các chức năng nằm trong phạm vi thực hiện:** Quản lý danh mục căn hộ (trạng thái Trống, Đang ở); Quản lý hồ sơ cư dân; Quản lý đăng ký, kết thúc và chuyển đổi cư trú; Quản lý phương tiện cá nhân gắn theo căn hộ; Cung cấp giao diện tra cứu dành cho cư dân; Hệ thống phân quyền và xác thực người dùng an toàn.
* **Các chức năng nằm ngoài phạm vi thực hiện:** Đề tài không đi vào các phân hệ ngoài phạm vi cư trú như: quản lý điều khiển bãi giữ xe thông minh (camera nhận diện biển số, thanh chắn tự động); không quản lý tài chính kế toán (tính hóa đơn tiền điện, nước, phí dịch vụ hàng tháng); không tích hợp các thiết bị phần cứng IoT hay hệ thống kiểm soát an ninh ra vào tòa nhà.

---

## 4. Ý nghĩa của đề tài
* **Ý nghĩa thực tiễn:** Hệ thống góp phần giải quyết triệt để tình trạng phân tán và sai lệch thông tin trong công tác quản lý dân cư tại chung cư; tiết kiệm đáng kể thời gian, nhân lực và chi phí văn phòng phẩm cho Ban quản lý tòa nhà. Đồng thời, việc ứng dụng phần mềm mang lại một phương thức tương tác văn minh, hiện đại, nâng cao chất lượng cuộc sống và mức độ hài lòng của cư dân sinh sống tại tòa nhà.
* **Ý nghĩa học thuật và đào tạo:** Đề tài là sản phẩm minh chứng rõ nét cho việc ứng dụng lý thuyết vào thực tiễn phát triển phần mềm. Thông qua quá trình thực hiện Bài tập lớn, các thành viên không chỉ làm chủ kỹ năng lập trình và xây dựng cơ sở dữ liệu mà còn hình thành tác phong làm việc chuyên nghiệp, khả năng phân tích yêu cầu nghiệp vụ thực tế, kỹ năng giải quyết mâu thuẫn và phối hợp làm việc nhóm hiệu quả.

