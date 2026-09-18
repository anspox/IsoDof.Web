# IsoDof - ISO DÖF Yönetim Sistemi

IsoDof, ISO kalite standartlarına uygun olarak işletmelerde Düzeltici ve Önleyici Faaliyetlerin (DÖF) takibini, atanmasını ve yönetilmesini sağlayan bir **ASP.NET Core MVC** web uygulamasıdır.

---

## 🚀 Özellikler

* **Departman Yönetimi:** Departman ekleme, düzenleme, listeleme ve silme.
* **Kullanıcı Yönetimi:** Kullanıcı kaydı, departman ataması ve veri doğrulamaları (Validation).
* **DÖF (Düzeltici Önleyici Faaliyet) Takibi:**
  * DÖF oluşturma ve kaynak belirleme (İç Denetim, Müşteri Şikayeti, vb.).
  * Sorumlu kullanıcı ve departman atama.
  * Durum takibi (Açık, İnceleniyor, Kapatıldı vb.).
  * DÖF aksiyonlarının (DofAction) planlanması ve yönetimi.
* **Veritabanı Entegrasyonu:** Entity Framework Core Code-First mimarisi ve Migration desteği.

---

## 🛠️ Kullanılan Teknolojiler

* **Framework:** .NET 8 / ASP.NET Core MVC
* **ORM:** Entity Framework Core (SQL Server)
* **Arayüz:** Bootstrap 5, HTML5, CSS3, jQuery Validation & Unobtrusive Validation
* **Veritabanı:** Microsoft SQL Server

---

## ⚙️ Kurulum ve Çalıştırma

### 1. Depoyu Klonlayın
```bash
git clone https://github.com/anspox/IsoDof.Web.git
cd IsoDof.Web
```

### 2. Veritabanı Bağlantısını Yapılandırın
`appsettings.json` dosyasındaki `ConnectionStrings` ayarını kendi yerel SQL Server bilginize göre güncelleyin:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=IsoDofDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### 3. Veritabanı Migration'larını Uygulayın
```bash
dotnet ef database update
```

### 4. Uygulamayı Başlatın
```bash
dotnet watch
```
Uygulama varsayılan olarak `https://localhost:7xxx` veya `http://localhost:5297` portunda çalışacaktır.
