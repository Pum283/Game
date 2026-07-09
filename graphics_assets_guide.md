# Hướng Dẫn Chuẩn Bị & Tích Hợp Đồ Họa Đẹp Cho Game

Hiện tại game của chúng ta đang dùng các hình vuông màu đơn sắc (vẽ trực tiếp từ code) để chạy thử nghiệm cơ chế. Để game có đồ họa và giao diện pixel art chuyên nghiệp, cuốn hút, chúng ta cần thay thế chúng bằng các file hình ảnh **`.png` có nền trong suốt (transparent background)**.

---

## 🎨 1. Danh Sách Tài Nguyên Đồ Họa Cần Thiết (Sprites)

Bạn có thể tự vẽ, tải từ các trang miễn phí (như [itch.io](https://itch.io) mục 2D Game Assets), hoặc nhờ tôi tạo hộ. Dưới đây là danh sách file ảnh và kích thước chuẩn:

### 👤 Nhân Vật & Quái Vật (Characters & Monsters)
| Tên File Gợi Ý | Kích Thước | Đối Tượng Trong Game | Mô tả hình ảnh |
| :--- | :--- | :--- | :--- |
| `player.png` | 32x32 px | Nhân vật chính | Chiến binh, pháp sư hoặc nhà thám hiểm. |
| `zombie.png` | 24x24 px | Quái vật mặc định | Zombie di chuyển chậm, màu xanh lá/đỏ. |
| `bat.png` | 16x16 px | Quái vật bay nhanh | Dơi màu tím đang vỗ cánh. |
| `skeleton.png` | 24x24 px | Quái vật bắn tên | Bộ xương cầm cung tên. |
| `golem.png` | 48x48 px | Boss Golem khổng lồ | Người đá lớn màu cam sẫm/xám. |

### 💥 Vũ Khí & Đạn (Weapons & Projectiles)
| Tên File Gợi Ý | Kích Thước | Đối Tượng | Mô tả hình ảnh |
| :--- | :--- | :--- | :--- |
| `bullet_wand.png`| 8x8 px | Đạn Magic Wand | Tia sét phép thuật màu xanh/vàng nhỏ. |
| `bullet_fire.png`| 16x16 px | Đạn quả cầu lửa | Quả cầu lửa đang bốc cháy dữ dội. |
| `bullet_axe.png` | 14x14 px | Đạn rìu bay | Chiếc rìu chiến màu bạc. |
| `garlic_aura.png`| 128x128 px | Vòng tỏi hộ vệ | Vòng tròn năng lượng tỏa sáng màu trắng mờ. |
| `holy_bottle.png`| 12x12 px | Lọ nước thánh | Lọ thủy tinh chứa dung dịch màu xanh nước biển. |
| `holy_pool.png`  | 64x64 px | Vũng nước thánh | Vũng nước thánh phát sáng trên nền đất. |
| `arrow.png`      | 8x8 px | Tên của quái Skeleton | Mũi tên gỗ của bộ xương. |

### 💎 Vật Phẩm & Môi Trường (Collectibles & Environment)
| Tên File Gợi Ý | Kích Thước | Đối Tượng | Mô tả hình ảnh |
| :--- | :--- | :--- | :--- |
| `gem.png` | 10x10 px | Ngọc kinh nghiệm (XP) | Viên pha lê sáng lấp lánh (màu xanh cyan). |
| `chest.png` | 32x24 px | Rương báu | Chiếc rương gỗ viền vàng chứa châu báu. |
| `gold_coin.png` | 10x10 px | Đồng tiền vàng | Đồng tiền vàng xu tròn. |
| `tile_grass.png` | 32x32 hoặc 64x64 px | Nền bản đồ | Gạch lát cỏ/đất để xếp lặp lại vô tận (Tiled Map). |

### 🔤 Font Chữ Đẹp (Retro Pixel Font)
*   Mặc định game đang dùng Arial từ Windows. Để game đậm chất retro, bạn có thể tải file font dạng `.ttf` (Ví dụ font: **Press Start 2P**, **Monocraft**, hoặc **Pixeloid Sans**).

---

## 📥 2. Quy Trình Tích Hợp Đồ Họa Vào Dự Án

Khi bạn đã có các file ảnh trên, quy trình đưa vào game cực kỳ đơn giản nhờ hệ thống biên dịch tài nguyên của MonoGame:

1.  **Copy các file ảnh** của bạn vào thư mục:
    `d:\HungNDM\Game\Content\`
2.  **Khai báo hình ảnh với MonoGame**:
    Mở cửa sổ Terminal của bạn và gõ lệnh sau để mở trình quản lý Assets trực quan:
    ```powershell
    dotnet mgcb-editor
    ```
    *Giao diện MGCB Editor sẽ hiện ra. Bạn click chuột phải vào thư mục `Content` trong app -> Chọn `Add` -> `Existing Item...` -> Chọn các file ảnh `.png` bạn vừa copy vào.*
3.  **Lưu lại và Đóng app**:
    Nhấn `Ctrl + S` trên giao diện MGCB Editor để lưu lại cấu hình, sau đó đóng nó lại.
4.  **Sửa code nạp ảnh**:
    Tôi sẽ cập nhật mã nguồn C# để thay đổi lệnh nạp:
    ```csharp
    // Thay vì vẽ hình vuông màu đơn sắc, ta sẽ vẽ texture ảnh:
    _playerTexture = Content.Load<Texture2D>("player");
    ```

---

> [!TIP]
> **Hãy bắt đầu từ bước đơn giản nhất:**
> - Bạn có muốn tôi sử dụng công cụ AI tự tạo ra một số hình vẽ mẫu Pixel Art cơ bản trước (như ảnh nhân vật, quái vật, rương báu) rồi gửi bạn xem không?
> - Hoặc nếu bạn đã có sẵn gói đồ họa (Asset Pack) tải từ trên mạng về, bạn chỉ cần copy các file ảnh đó vào thư mục `Content/` và báo tôi tên file, tôi sẽ cập nhật code vẽ hình ảnh cho bạn ngay lập tức!
