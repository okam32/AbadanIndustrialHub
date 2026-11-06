# درگاه صنایع، شرکت‌ها و متخصصان آبادان و خرمشهر

این مخزن **نسخه اولیه کدنویسی (MVP / Starter)** است، نه محصول آماده استقرار عمومی. پنل فارسی Blazor WebAssembly + API مستقل ASP.NET Core 8 + EF Core 8 + ASP.NET Core Identity + SQL Server دارد.

## قواعد اصلی

- ثبت‌نام شرکت برای **همه شرکت‌ها آزاد** است؛ شرکت می‌تواند درخواست بررسی عضویت شهرک صنعتی آبادان بدهد. فقط مدیر شهرک/ادمین می‌تواند نشان `IsTownshipVerified` اعطا کند و شرکت‌های دارای این نشان در فهرست عمومی اولویت دارند.
- نسخه در حال ویرایش شرکت و محصول/خدمت از نسخه عمومی منتشرشده **جداست**؛ تا زمان تأیید، نسخه عمومی قبلی نمایش داده می‌شود.
- ثبت متخصص، رزومه و مهارت‌ها، انتشار آگهی‌های شرکت‌های تأییدشده، ثبت درخواست شغلی، فرایندهای کاری شرکت، مشکلات صنعتی و ارجاع پرونده به اتاق بازرگانی/پالایشگاه آبادان/بسیج مهندسین پیاده شده است.
- دسترسی پرونده مشکلات بر اساس مالکیت شرکت، نقش مدیریتی یا ارجاع به نهاد بررسی می‌شود.
- داشبورد با نمودار میله‌ای مبتنی بر داده واقعی دیتابیس پیاده شده است.
- **تمام Endpointهای تغییر‌دهنده فقط POST هستند و تمام Endpointهای خواندنی GET؛ هیچ PUT/PATCH/DELETE وجود ندارد.**
- ورود با cookie امن HttpOnly و X-CSRF-TOKEN انجام می‌شود؛ **دامنه Client و API باید یکسان باشد**.

## پیش‌نیازها

1. **.NET 8 SDK** و workload مربوط به Blazor WebAssembly (در صورت درخواست build).
2. SQL Server یا SQL Server Express/LocalDB (LocalDB فقط روی ویندوز).
3. EF CLI نسخه 8.0.33 (دستور نصب در پایین).
4. Visual Studio 2022 با پشتیبانی .NET 8 یا ترمینال dotnet.

## راه‌اندازی سریع در ویندوز

در PowerShell از ریشه پروژه:

```powershell
# مهم: ابتدا اطلاعات اتصال SQL Server را در appsettings.json یا User Secrets قرار دهید.
dotnet restore .\AbadanIndustrialHub.sln
dotnet build .\AbadanIndustrialHub.sln
dotnet tool update --global dotnet-ef --version 8.0.33
# فقط بار اول، پس از اینکه پروژه به درستی Build شد:
dotnet ef migrations add InitialCreate --project .\AbadanHub.Api --startup-project .\AbadanHub.Api
dotnet ef database update --project .\AbadanHub.Api --startup-project .\AbadanHub.Api
# ایمیل و رمز مدیر اولیه را با Secret Manager تعیین کنید؛ در Git قرار ندهید.
dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.test" --project .\AbadanHub.Api
dotnet user-secrets set "BootstrapAdmin:Password" "CHANGE-TO-A-STRONG-SECRET-123!" --project .\AbadanHub.Api
dotnet run --project .\AbadanHub.Api --launch-profile https
```

یا پس از تنظیم دیتابیس، `scripts/setup-windows.ps1` را برای restore/build/migration/update اجرا کنید. **خود فایل migration هنوز تولید نشده**؛ اولین `dotnet ef migrations add` آن را متناسب با نسخه بسته‌ها می‌سازد.

در محیط توسعه: `https://localhost:7251` صفحه عمومی، `https://localhost:7251/swagger` فهرست مسیرها را نشان می‌دهد.

**برای Linux و هاست:** `ConnectionStrings__DefaultConnection` را با کانکشن معتبر SQL Server از طریق متغیر محیطی تنظیم کنید. برای Production از HTTPS واقعی و secretهای خارج از فایل استفاده کنید. می‌توانید با دستور زیر بسته را انتشار دهید:

```bash
dotnet publish AbadanHub.Api/AbadanHub.Api.csproj -c Release -o ./publish
```

> توجه: این محیط تولید فایل، SDK .NET را در اختیار نداشت؛ بنابراین **کامپایل و اجرای واقعی پروژه در اینجا تأیید نشده است**. ابتدا در رایانه خود `dotnet build` را اجرا کنید، سپس هر خطا را با لاگ Build برای اصلاح ارسال کنید.

## اعتبارسنجی مسیرها

```bash
python scripts/check-api-methods.py
```

## ترتیب استفاده

1. ادمین اولیه با Secrets ساخته می‌شود (پس از اعمال Migration و اولین اجرای API).
2. شرکت از `/register-company` ثبت‌نام می‌کند و مدارک را از پنل شرکت بارگذاری می‌کند؛ تا زمان تأیید عمومی نیست.
3. ادمین از `/admin` و لینک بررسی کامل، اطلاعات و مدارک را می‌بیند و شرکت را تأیید یا رد می‌کند. اگر مستندات عضویت بررسی شده باشد، نشان شهرک را انتخاب می‌کند.
4. شرکت خدمت/محصول ثبت می‌کند؛ ادمین آن را جداگانه تأیید می‌کند.
5. شرکت آگهی شغلی و فرایندهای کاری ثبت می‌کند؛ متخصص رزومه را تکمیل و درخواست همکاری می‌فرستد.
6. شرکت مشکل صنعتی و نهادهای گیرنده را مشخص می‌کند؛ نهادهای مجاز می‌توانند پیگیری و تغییر وضعیت ثبت کنند.
7. مدیر و نمایندگان نهادها آمار و نمودارهای مرتبط را در `/dashboard` می‌بینند.

## نقش‌ها

`SuperAdmin` / `TownManager` / `Company` / `Specialist` / `ChamberOfCommerce` / `AbadanRefinery` / `EngineersBasij`.

ساخت حساب مدیر و نمایندگان نهادها **از طریق ثبت‌نام عمومی مجاز نیست**؛ تخصیص نقش‌ها در نسخه اولیه باید با رویه اداری امن و توسط مدیر سیستم انجام شود. به‌هیچ‌وجه یک Endpoint عمومی برای افزودن نقش مدیریتی نسازید.

## فایل‌ها و امنیت

- ارسال فایل به `/api/files/upload` تنها با POST و cookie و هدر CSRF انجام می‌شود.
- PDF, PNG, JPG با حجم حداکثر ۵ مگابایت مجازند؛ امضای اولیه فایل بررسی می‌شود.
- مسیر `private_uploads` خارج از `wwwroot` است و دانلود به مجوز نیاز دارد.
- برای محصول واقعی: اسکن ضدبدافزار، کنترل سراسری محدودیت درخواست، ذخیره ابری خصوصی و لاگ ممیزی بیشتر ضروری‌اند.
- در نسخه اولیه ایمیل کاربران **تأیید نمی‌شود**؛ پیش از انتشار عمومی تأیید ایمیل، بازیابی رمز، ضدربات، Rate limiting، حفظ حریم خصوصی و سیاست نگهداری داده اضافه شوند.
- سایت در یک Origin راه‌اندازی شود تا CORS Preflight و محدودیت‌های OPTIONS روی هاست ایجاد مشکل نکند؛ `GET` و `POST` برای کنترلرها کافی هستند.

## فهرست قابلیت‌های تکمیل‌نشده برای فاز بعد

- فرایند رسمی تأیید مدارک متخصصان، گواهینامه‌ها، احراز شرکت بر اساس مرجع حقیقی، و ثبت تاریخچه کامل تأییدها؛
- اعلان ایمیل/پیامک، مهلت‌های SLA برای ارجاع، بارگذاری ضمیمه به یک مشکل از UI (API امکان issueId را دارد)، افزودن اعضای متعدد برای یک شرکت؛
- صفحه ارزیابی درخواست‌های شغلی با تغییر مرحله، خروجی Excel/CSV، جستجوی صفحه‌بندی‌شده، و جداول طبقه‌بندی صنایع؛
- آزمون یکپارچه با دیتابیس SQL Server، بهبود Validation و مانیتورینگ عملیاتی.

## ساختار

```
AbadanIndustrialHub.sln
  AbadanHub.Shared  # قراردادها، DTO و enum
  AbadanHub.Api     # EF Identity, SQL Server, Security, Controllers
  AbadanHub.Client  # صفحات Blazor WASM، CSS فارسی، نمودارها
  scripts           # نصب اولیه و بررسی محدودیت متدهای HTTP
```
