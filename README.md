# itoscon — ระบบจัดการรายชื่อผู้ติดต่อ (ASP.NET Core MVC, .NET 10)

เว็บสำหรับ **เพิ่ม (Insert) / แก้ไข (Update) / ลบ (Delete)** ข้อมูลผู้ติดต่อ
และดูข้อมูลทั้งหมดในรูปแบบตาราง (ค้นหาได้)

ข้อมูลที่เก็บ: **ชื่อ, นามสกุล, เบอร์โทร, อีเมล**

## เทคโนโลยี
- ASP.NET Core MVC (.NET 10)
- Entity Framework Core + SQLite (ไฟล์ `contacts.db` สร้างอัตโนมัติตอนรันครั้งแรก ไม่ต้องติดตั้ง DB server)
- Bootstrap 5

## โครงสร้าง MVC
| ส่วน | ไฟล์ | หน้าที่ |
|---|---|---|
| **Model** | `itoscon/Models/Contact.cs` | โครงสร้างข้อมูล + กฎตรวจสอบ (validation) |
| | `itoscon/Data/AppDbContext.cs` | เชื่อมต่อฐานข้อมูล |
| **View** | `itoscon/Views/Contacts/Index.cshtml` | ตารางแสดงข้อมูล + ค้นหา |
| | `Create.cshtml` / `Edit.cshtml` / `Delete.cshtml` | ฟอร์มเพิ่ม / แก้ไข / ยืนยันลบ |
| | `_ContactFields.cshtml` | ช่องกรอกที่ใช้ร่วมกันระหว่างเพิ่มและแก้ไข |
| **Controller** | `itoscon/Controllers/ContactsController.cs` | Insert / Update / Delete / แสดงรายการ |

## วิธีรัน
ต้องมี [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```bash
git clone <URL ของ repo>
cd itoscon
dotnet run --project itoscon
```
แล้วเปิด URL ที่ขึ้นใน terminal (เช่น `https://localhost:7xxx`) หรือเปิด `itoscon.slnx` ใน Visual Studio แล้วกด F5

---

## การทำงานร่วมกับเพื่อนด้วย Git

### ครั้งแรก (คนสร้าง repo — ทำครั้งเดียว)
1. สร้าง repo เปล่าบน GitHub (ไม่ต้องติ๊ก README / .gitignore)
2. เชื่อมแล้ว push:
   ```bash
   git remote add origin https://github.com/<user>/itoscon.git
   git push -u origin main
   ```
3. GitHub → **Settings → Collaborators** → เพิ่มเพื่อน

### เพื่อน (ทำครั้งเดียว)
```bash
git clone https://github.com/<user>/itoscon.git
```

### ขั้นตอนทำงานทุกครั้ง (Feature Branch)
**อย่าแก้บน `main` ตรง ๆ** ให้แยก branch ต่องาน เพื่อไม่ให้ทับงานกัน

```bash
# 1. อัปเดต main ให้ล่าสุดก่อนเริ่มงาน
git switch main
git pull

# 2. สร้าง branch ใหม่สำหรับงานของตัวเอง
git switch -c feature/ชื่องาน        # เช่น feature/add-address

# 3. เขียนโค้ด แล้ว commit เป็นระยะ
git add .
git commit -m "เพิ่มช่องที่อยู่ในฟอร์ม"

# 4. ส่งขึ้น GitHub
git push -u origin feature/ชื่องาน
```
5. ไปที่ GitHub กด **Compare & pull request** → ให้เพื่อนรีวิว → **Merge**
6. กลับมาที่เครื่อง: `git switch main && git pull` แล้วเริ่มงานถัดไป

### ถ้าเกิด Conflict
```bash
git switch feature/ชื่องาน
git pull origin main          # ดึง main ล่าสุดมารวม
# เปิดไฟล์ที่ขึ้นว่า conflict แก้ส่วน <<<<<<< ======= >>>>>>> ให้ถูกต้อง
git add .
git commit -m "แก้ conflict กับ main"
git push
```

### ข้อตกลงของทีม
- 1 branch = 1 งาน, ตั้งชื่อ `feature/...` หรือ `fix/...`
- `git pull` ก่อนเริ่มงานทุกครั้ง
- ห้าม commit ไฟล์ `bin/`, `obj/`, `.vs/`, `*.db` (มี `.gitignore` กันไว้แล้ว)
- ตรวจว่า `dotnet build` ผ่านก่อน push
