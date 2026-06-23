# 🤖 RAG Chatbot - Hệ Thống Trợ Lý Ảo Thông Minh Trích Xuất Dữ Liệu Nội Bộ (ASP.NET Core MVC)

Dự án này là một hệ thống **RAG Chatbot** được xây dựng hoàn toàn bằng **C#** trên nền tảng **.NET 10.0**, áp dụng mô hình kiến trúc chuẩn **ASP.NET Core MVC**. Hệ thống cho phép người dùng nạp tài liệu nội bộ của doanh nghiệp và thực hiện tìm kiếm ngữ cảnh, hỏi đáp thông minh thông qua giao diện Web trực quan. Dự án tích hợp mô hình nhúng đa ngôn ngữ **multilingual-e5-base** cục bộ đảm bảo an toàn thông tin và lưu trữ dữ liệu tập trung trên **Microsoft SQL Server**.

---

## 🏗️ Kiến Trúc Hệ Thống & Mô Hình MVC

Hệ thống được tổ chức theo mô hình **MVC** kết hợp chặt chẽ với **Kiến trúc phân lớp (Layered Architecture)** để tách biệt rõ ràng mã nguồn giao diện, logic nghiệp vụ AI và truy xuất cơ sở dữ liệu:

### 1. Presentation & Routing (Mô hình MVC)
Nằm tại lớp giao diện chính, đảm nhận luồng tương tác của người dùng:
- **Models (ViewModels):** Các class C# định nghĩa cấu trúc dữ liệu truyền qua lại giữa Controller và View (ví dụ: `ChatViewModel`, `DocumentUploadViewModel`). Chúng chứa các thuộc tính cần thiết hiển thị lên giao diện chat hoặc danh sách tài liệu.
- **Views:** Sử dụng công nghệ **Razor Pages (`.cshtml`)**. View sẽ render ra giao diện hộp thoại Chat, các nút bấm tải lên tài liệu, và nhận dữ liệu phản hồi động từ Controller để hiển thị cho người dùng.
- **Controllers:** Các `ChatController` và `DocumentController` kế thừa từ `Microsoft.AspNetCore.Mvc.Controller`. Nhiệm vụ là bắt các hành động (Action) từ giao diện, gọi xuống lớp Services để xử lý RAG và trả về đúng View kèm dữ liệu tương ứng.

### 2. Business Layer (Lớp Nghiệp Vụ)
- **Services:** Chứa logic cốt lõi của RAG pipeline (Tách đoạn văn bản - Text Chunking, Quản lý lịch sử trò chuyện - Chat Memory, Xây dựng Prompt).
- **AI Model (Local):** Chạy cục bộ mô hình **multilingual-e5-base** dưới định dạng ONNX (`Microsoft.ML.OnnxRuntime`) nhằm chuyển đổi văn bản sang Vector nhúng (Embedding Vectors - 768 chiều).

### 3. Data Access Layer (Lớp Dữ Liệu)
- **DbContext & Entities:** Sử dụng **Entity Framework Core (EF Core)** để tương tác với **Microsoft SQL Server**. Lớp này chịu trách nhiệm lưu trữ thông tin văn bản gốc, lịch sử trò chuyện và thực hiện các câu lệnh T-SQL tính toán khoảng cách tương đồng (Cosine Similarity) của các Vector nhúng.

---

## 📂 Cấu Trúc Thư Mục Giải Pháp (.NET MVC Solution)

Mã nguồn được tổ chức thành các thư mục dự án (`.csproj`) tách biệt nhằm hiện thực hóa sơ đồ kiến trúc hệ thống:

```text

📁 RAGChatbotMVC.sln
│
├── 📁 src/
│   ├── 📄 RAGChatbot.Web/                # 1. ASP.NET Core MVC Project
│   │   ├── 📁 Controllers/              # ChatController.cs, DocumentController.cs
│   │   ├── 📁 Models/                   # ChatViewModel.cs, DocumentUploadViewModel.cs
│   │   ├── 📁 Views/                    # Chat/Index.cshtml, Shared/_Layout.cshtml
│   │   ├── 📁 wwwroot/                  # CSS, JS (Hỗ trợ gọi AJAX/Fetch API thời gian thực)
│   │   └── 📄 Program.cs                # Cấu hình MVC Routing, DI, và Authentication
│   │
│   ├── 📄 RAGChatbot.Business/           # 2. Class Library (Business Layer)
│   │   ├── 📁 Services/                 # ChatService.cs, RagEngine.cs
│   │   └── 📁 Interfaces/               # IChatService.cs, IEmbeddingEngine.cs
│   │
│   └── 📄 RAGChatbot.DataAccess/         # 3. Class Library (Data Access Layer)
│       ├── 📁 DbContext/                # AppDbContext.cs (EF Core cấu hình SQL Server)
│       ├── 📁 Entities/                 # DocumentEntity.cs, VectorEmbedding.cs
│       └── 📁 Repositories/             # SqlVectorRepository.cs, ChatHistoryRepository.cs
│
└── 📁 data/                             # Thư mục lưu trữ tài liệu mẫu (.pdf, .txt)


````




🛠️ Công Nghệ Sử Dụng (Tech Stack)
Framework: .NET 10.0 / ASP.NET Core MVC (Razor Views)

Embedding Model: intfloat/multilingual-e5-base.

AI Run-time (C#): Microsoft.ML.OnnxRuntime (Thực thi suy luận Vector trực tiếp trên CPU/GPU của máy chủ).

Database ORM: Entity Framework Core (Microsoft.EntityFrameworkCore.SqlServer).

Database: Microsoft SQL Server.


🚀 Hướng Dẫn Cài Đặt & Chạy Thử
Điều kiện tiên quyết
Đã cài đặt .NET 10.0 SDK.

File mô hình multilingual-e5-base.onnx đặt tại thư mục chỉ định trong cấu hình.

Đã có quyền truy cập vào một thực thể Microsoft SQL Server.


Các bước cài đặt
1. Clone dự án:
   -git clone https://github.com/huaminh0401/RAGChatbot.git
   cd RAGChatbot

2. Cấu hình ứng dụng (appsettings.json):
Mở file src/RAGChatbotMVC.Web/appsettings.json và cập nhật thông tin chuỗi kết nối SQL Server cùng đường dẫn Model:

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=MSI;Database=RAG_Research_DB;Trusted_Connection=True;TrustServerCertificate=True;"
  },


3. Khởi chạy ứng dụng Web MVC:
  - dotnet run --project src/RAGChatbot.Web
Sau khi chạy, mở trình duyệt và truy cập http://localhost:5000. Giao diện Web MVC (Razor View) sẽ xuất hiện, cho phép bạn tải lên tài liệu trực tiếp và thực hiện trò chuyện với chatbot ngay trên trình duyệt.



🤝 Đóng Góp Phát Triển
Mọi ý kiến đóng góp nhằm tối ưu hóa việc binding dữ liệu giữa Controller và View hoặc cải thiện tốc độ xử lý của lớp Business AI đều được hoan nghênh. Vui lòng gửi một Pull Request hoặc mở một Issue.

📄 Giấy Phép (License)
Dự án này được cấp phép theo Giấy phép MIT - xem file LICENSE để biết thêm chi tiết.
