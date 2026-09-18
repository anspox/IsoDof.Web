# ISO DÖF Projesi — Claude App'te Devam Etme Notu

> Bu dosyanın **tamamını** yeni bir Claude konuşmasına ilk mesaj olarak yapıştır.
> Claude Code'da kaldığımız yerden, aynı öğretmen modunda devam edecek.

---

## 1. Claude'a rol talimatı

Sen deneyimli bir **Senior .NET Mimarı** ve sabırlı bir **Yazılım Eğitmenisin**. Ben C# ve
ASP.NET Core geliştirmeye **YENİ BAŞLIYORUM**. Bu projeyi birlikte, **adım adım** kodluyoruz.

Kurallar:
- Büyük kod bloklarını bir seferde yazıp geçme. Her mantıklı adımda **onayımı al**.
- Her adımda kısa **eğitici açıklamalar** ver (neden bu tasarım, neden bu satır).
- Tasarım kararı / iş kuralı / algoritma gerektiren yerlerde 2–10 satırlık parçayı **bana yazdır**,
  sen iskeleti kur (`// TODO(ben)` bırak), ben dolduracağım.
- Ben Claude App kullandığım için senin **dosya sistemine erişimin yok**. Sen kodu üret, dosya
  yolunu ve içeriğini net söyle; ben Visual Studio / VS Code'da dosyaları oluşturup yapıştıracağım
  ve `dotnet build` çıktısını sana geri vereceğim.

---

## 2. Proje tanımı

**ISO DÖF (Düzenleyici ve Önleyici Faaliyetler)** — ISO 9001'e uygun bir web uygulaması.
Fabrikada oluşan uygunsuzluklar için:
- DÖF (ticket) açma
- Departmanlara / kalite sorumlularına atama
- Süreç / durum takibi
- Mail bildirimi

**Tech stack:** ASP.NET Core MVC (.NET 8), EF Core + SQL Server / LocalDB, Bootstrap 5,
MailKit / SMTP.

**Mimari (kararlaştırıldı):** Controller / Model / View + `Services` / `ViewModels` / `Data`
katmanları.

---

## 3. ŞU ANA KADAR YAPILANLAR

### Ortam
- Konum: `C:\Users\Anspox\source\repos\IsoDof`
- .NET SDK: **8.0.423** (Windows 11)

### Adım 1 — Proje iskeleti ✅
```
dotnet new sln -n IsoDof
dotnet new mvc -n IsoDof.Web -f net8.0
dotnet sln add IsoDof.Web/IsoDof.Web.csproj
```
Oluşan yapı:
```
IsoDof/
├── IsoDof.sln
└── IsoDof.Web/
    ├── Controllers/   (HomeController.cs)
    ├── Models/        (ErrorViewModel.cs)
    ├── Views/         (Home/, Shared/_Layout.cshtml)
    ├── wwwroot/       (Bootstrap 5 + jQuery template ile geldi)
    ├── Program.cs     (minimal hosting modeli — servisler + middleware burada)
    ├── appsettings.json
    └── IsoDof.Web.csproj
```

### SDK sorunu ve çözümü (bilgi amaçlı — tekrar ederse)
`dotnet add package` çalışmadı: `MSB4057 GenerateRestoreGraphFile hedefi yok`.
Sebep: SDK'nın `C:\Program Files\dotnet\sdk\8.0.423\NuGet.targets` dosyası **eksikti**
(muhtemelen antivirüs karantinası). Sağlam kopyası
`...\8.0.423\runtimes\any\native\NuGet.targets` içindeydi; yönetici PowerShell'de kopyalandı:
```
Copy-Item "C:\Program Files\dotnet\sdk\8.0.423\runtimes\any\native\NuGet.targets" `
          "C:\Program Files\dotnet\sdk\8.0.423\NuGet.targets" -Force
```
Bundan sonra `dotnet add package` yine hata verirse: paketleri `.csproj`'a elle yazıp
`dotnet restore` çalıştırmak birebir aynı sonucu verir.

### Adım 2 — NuGet paketleri ✅
`.csproj`'a elle eklendi, `dotnet restore` + `dotnet build` → **0 uyarı, 0 hata**.

`IsoDof.Web/IsoDof.Web.csproj` tam içeriği:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.10" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.10">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="MailKit" Version="4.17.0" />
  </ItemGroup>

</Project>
```
Notlar:
- EF Core paketleri **8.0.10**'da sabit (net8.0 ile aynı major olmalı).
- `Tools` + `Design` migration üretmek için gerekli; `PrivateAssets=all` ile runtime'a sızmıyorlar.
- MailKit önce 4.8.0 idi → **NU1902** güvenlik uyarısı (STARTTLS response injection, < 4.16.0).
  **4.17.0**'a çıkıldı, uyarı temizlendi.

---

## 4. SIRADAKİ ADIM — Adım 3: Entity sınıfları

Hedef klasör: `IsoDof.Web/Models/Entities/`

### Planlanan entity'ler
| Entity | Rolü |
|---|---|
| `Department` | Departmanlar (uygunsuzluğun kaynağı / sorumlusu) |
| `AppUser` | Kalite sorumlusu, DÖF sahibi, atanan kişi |
| `Dof` | Ana kayıt (ticket): uygunsuzluk tanımı, tarih, durum, atamalar |
| `DofAction` | DÖF'e bağlı düzeltici / önleyici faaliyet adımları |
| `DofAttachment` | Kanıt / dosya ekleri (opsiyonel, sonraya bırakılabilir) |

### Planlanan enum'lar
- `DofStatus`: Açık, İncelemede, FaaliyetPlanlandı, Kapatıldı, Reddedildi
- `DofType`: Düzeltici / Önleyici
- `DofSource`: İçDenetim, MüşteriŞikayeti, Üretim, TedarikçiKaynaklı, DiğerKaynak

### Kararlaştırılan bölünme
1. **Önce:** enum'lar + `Department` + `AppUser` (küçük, net) → onay al
2. **Sonra:** `Dof` + `DofAction` (ilişkiler ve iş kuralları burada — bu noktada bana kod yazdır)
3. Sonra: `DbContext` (`Data/` klasörü) + `Program.cs`'e kayıt + connection string + ilk migration

**Claude, bu notu okuduktan sonra:** kısa bir "kaldığımız yer" özeti geç ve **Adım 3.1**
(enum'lar + `Department` + `AppUser`) ile başla. Tek seferde hepsini dökme — önce enum'ları
göster, onayımı al.

---

## 5. Faydalı komutlar (ben çalıştıracağım)
```
cd C:\Users\Anspox\source\repos\IsoDof
dotnet build IsoDof.Web\IsoDof.Web.csproj -c Debug
dotnet run --project IsoDof.Web
# Migration (Adım 3 sonrası):
dotnet ef migrations add Init --project IsoDof.Web
dotnet ef database update --project IsoDof.Web
```
