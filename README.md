<div align="center">
  
# 🎯 IsoDof - Kalite Yönetim Sistemi DÖF Modülü

[![.NET CI](https://github.com/anspox/IsoDof.Web/actions/workflows/ci.yml/badge.svg)](https://github.com/anspox/IsoDof.Web/actions/workflows/ci.yml)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET_Core-MVC-blue?logo=dotnet)
![Entity Framework Core](https://img.shields.io/badge/EF_Core-SQL_Server-green)

IsoDof, ISO kalite standartlarına uygun olarak işletmelerde Düzeltici ve Önleyici Faaliyetlerin (DÖF) takibini, atanmasını ve yönetilmesini sağlayan modern bir **ASP.NET Core MVC** web uygulamasıdır.

</div>

---

## ✨ Öne Çıkan Özellikler

- 🏢 **Departman ve Organizasyon Yönetimi:** Departmanlar oluşturma, yöneticiler atama ve organizasyon ağacı.
- 👥 **Rol Bazlı Kullanıcı Yönetimi:** Admin, Kalite Kontrol ve Çalışan rolleri ile güvenli ve sınırlandırılmış erişim (Authorization).
- 📋 **Kapsamlı DÖF Takibi:** 
  - İç Denetim, Müşteri Şikayeti vb. kaynaklara göre DÖF başlatma.
  - Kanban panosu (Board) ile DÖF durumlarını (Açık, İncelemede, Kapatıldı) görsel olarak izleme.
  - Sorumlu çalışan ataması ve adım adım aksiyon (DofAction) planlaması.
- 🖨️ **Raporlama:** DÖF raporlarını Excel ve PDF formatlarında dışa aktarabilme.
- 🔔 **Bildirim Sistemi:** Kullanıcılara atanan yeni DÖF'ler ve aksiyonlar için sistem içi bildirim modülü.
- 🧪 **Otomatik Testler (TDD/Birim Testleri):** `xUnit`, `Moq` ve `FluentAssertions` kullanılarak yazılmış, kod güvenliğini sağlayan kapsamlı test senaryoları.
- 🚀 **Sürekli Entegrasyon (CI/CD):** GitHub Actions ile her koda ekleme yapıldığında otomatik derleme (build) ve test (test) süreçlerinin çalışması.

---

## 🛠️ Kullanılan Teknolojiler

- **Backend Framework:** .NET 8 / ASP.NET Core MVC
- **Veritabanı ve ORM:** Microsoft SQL Server & Entity Framework Core (Code-First)
- **Frontend / Arayüz:** HTML5, CSS3, Bootstrap 5, jQuery (Validation & Unobtrusive)
- **Test Altyapısı:** xUnit, Moq, FluentAssertions
- **CI/CD Pipeline:** GitHub Actions

---

## 🚀 Kurulum ve Çalıştırma

Projeyi kendi bilgisayarınızda çalıştırmak için aşağıdaki adımları takip edebilirsiniz:

### 1. Depoyu Klonlayın
```bash
git clone https://github.com/anspox/IsoDof.Web.git
cd IsoDof
```

### 2. Veritabanı Bağlantısını Yapılandırın
`IsoDof.Web/appsettings.json` dosyasındaki `ConnectionStrings` ayarını kendi yerel SQL Server bilginize göre güncelleyin:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=IsoDofDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

### 3. Veritabanı Migration'larını Uygulayın
Terminal üzerinden `IsoDof.Web` klasörüne gidip EF Core migration komutunu çalıştırın:
```bash
cd IsoDof.Web
dotnet ef database update
```

### 4. Uygulamayı Başlatın
Projeyi derleyip çalıştırmak için ana klasörde (veya Web klasöründe) şu komutu kullanın:
```bash
dotnet run --project IsoDof.Web
```
Uygulama varsayılan olarak `https://localhost:7xxx` veya `http://localhost:5297` portunda çalışacaktır.

---

## 🧪 Testleri Çalıştırma

Projenin sağlığını ve doğruluğunu kontrol etmek için Unit (Birim) testlerini çalıştırabilirsiniz. Ana dizinde şu komutu girmeniz yeterlidir:

```bash
dotnet test
```

> **Not:** Projede uygulanan tüm kod değişiklikleri GitHub'a push edildiğinde **GitHub Actions** tarafından bu testler otomatik olarak çalıştırılır.

---
*Geliştirici: [anspox](https://github.com/anspox)*
