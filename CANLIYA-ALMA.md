# Canlıya Alma Kontrol Listesi (NISO QMS / ISO DÖF)

Kod tarafındaki hazırlıklar tamamlandı. Aşağıdaki adımlar sunucuda, kurum içinde veya kişisel hesaplarda yapılmalıdır.

## 1. Güvenlik (canlıya almadan ÖNCE)

- [ ] **Gmail uygulama şifresini iptal edin.** `appsettings.json` dosyasının eski sürümleri, kişisel Gmail adresini herkese açık GitHub deposunda gösteriyor. Şifre repoda değildi ama bu hesap için oluşturulan uygulama şifresini iptal edip yenisini oluşturun. Canlıda kurumsal bir e-posta hesabı kullanın.
- [ ] **Git geçmişi:** Eski sabit toplu içe aktarma şifresi ve Gmail adresi git geçmişinde duruyor. Depoyu gizli (private) yapın veya geçmişi temizleyin (`git filter-repo`). Geçmiş temizliği zorla push gerektirir, ekipçe karar verin.
- [ ] **Veritabanı kullanıcısı:** Uygulama için yalnızca bu veritabanında `db_datareader`, `db_datawriter` ve migration için gerekli izinlere sahip ayrı bir SQL kullanıcısı açın. `sa` kullanmayın.
- [ ] **Migration:** `dotnet ef database update` çalıştırın. Yeni migration mevcut tüm kullanıcıları bir sonraki girişte şifre değiştirmeye zorlar.
- [ ] **Klasör izinleri:** Uygulama havuzu kullanıcısına (IIS AppPool) yalnızca `logs` ve yüklenen dosyalar klasörüne yazma izni verin. Uygulama klasörünün geri kalanı salt okunur olmalı.

## 2. Yapılandırma

`IsoDof.Web/appsettings.Production.example.json` dosyasını sunucuda `appsettings.Production.json` adıyla kopyalayıp doldurun. Bu dosya `.gitignore`'dadır. Şifreleri dosya yerine ortam değişkeni olarak da verebilirsiniz:

```
ConnectionStrings__DefaultConnection=...
Smtp__Password=...
Sentry__Dsn=...
```

- [ ] `AllowedHosts`: canlı alan adı (örn. `dof.sirketiniz.com`)
- [ ] `Smtp`: kurumsal SMTP bilgileri. Boş kalırsa e-postalar gönderilmez ve log'a uyarı düşer.
- [ ] `FileStorage:RootPath`: yüklenen DÖF eklerinin kalıcı klasörü, web kökünün dışında
- [ ] `Kvkk`: veri sorumlusu unvanı, adresi, başvuru e-postası ve varsa KEP adresi. Doldurulmazsa aydınlatma metninde uyarı görünür.
- [ ] `Sentry:Dsn`: https://sentry.io üzerinde ücretsiz bir proje açıp DSN'i girin. Boş kalırsa hata izleme kapalıdır, hatalar yalnızca `logs` klasörüne yazılır.
- [ ] `ASPNETCORE_ENVIRONMENT=Production` olduğundan emin olun. Development modu sunucuda asla açık olmamalı.

## 3. SSL / HTTPS

- [ ] Geçerli bir SSL sertifikası kurun (kurumsal sertifika veya Let's Encrypt / win-acme).
- [ ] Uygulama HTTP isteklerini HTTPS'e yönlendirir ve 1 yıllık HSTS başlığı gönderir. Sertifikayı kurmadan canlıya almayın.
- [ ] Ters vekil (IIS/Nginx) arkasındaysanız `X-Forwarded-Proto` başlığının iletildiğini doğrulayın.

## 4. Yedekleme

- [ ] Canlıya almadan hemen önce tam yedek alın:
  ```
  powershell -ExecutionPolicy Bypass -File scripts\backup.ps1 -ServerInstance "SQLSUNUCU" -BackupDir "D:\Yedekler\IsoDof" -UploadsDir "D:\IsoDofData\uploads"
  ```
- [ ] Aynı komutu Görev Zamanlayıcı ile her gece çalışacak şekilde kurun. Örnek komut betiğin başındaki açıklamada var.
- [ ] Yedekleri sunucu dışına (NAS, başka bir sunucu, bulut) kopyalayın. Aynı diskteki yedek, disk arızasında işe yaramaz.
- [ ] Ayda bir kez bir yedeği test veritabanına geri yükleyip deneyin.
- [ ] Eski sürümden kalan `wwwroot/uploads` klasörü varsa içeriğini `FileStorage:RootPath` klasörüne taşıyın. Uygulama eski konumu okumaya devam eder ama o klasör yedeklenmezse dosyalar kaybolabilir.

## 5. Yasal (KVKK)

- [ ] Aydınlatma metnini (`/Home/Privacy`) hukuk birimine veya KVKK danışmanına onaylatın. Metin uygulamanın gerçekte işlediği verilere göre hazırlandı, ancak saklama süreleri şirket politikanıza göre güncellenmelidir.
- [ ] Şirketiniz VERBİS kayıt yükümlüsüyse bu sistemdeki veri kategorilerini envantere ekleyin.
- [ ] Uygulama yalnızca zorunlu çerezleri kullanır. Analiz veya takip aracı eklenmediği sürece çerez onay çubuğu gerekmez. Google Analytics gibi bir araç eklenirse onay çubuğu zorunlu hale gelir.

## 6. Son testler (canlı adreste)

- [ ] Giriş, çıkış, şifre değiştirme ve 5 hatalı denemeden sonra hesap kilitlenmesi
- [ ] DÖF açma, dosya ekleme ve indirme, durum değiştirme, e-posta bildiriminin gelmesi
- [ ] Telefon (menü butonu) ve tablette görünüm
- [ ] Chrome, Edge, Firefox ve Safari'de açılış
- [ ] https://securityheaders.com ile güvenlik başlıkları kontrolü (A notu beklenir)
- [ ] https://www.ssllabs.com/ssltest ile sertifika kontrolü

## Geliştirici notu: CSS derlemesi

Tailwind artık CDN yerine derlenmiş bir dosyadan (`wwwroot/css/app.min.css`) yüklenir. Görünümlere yeni Tailwind sınıfı eklediğinizde CSS'i yeniden derleyin:

```
cd IsoDof.Web
npm install        # yalnızca ilk seferde
npm run build:css  # veya geliştirme sırasında: npm run watch:css
```

Derlenmiş dosya repoya eklenir. Sunucuda Node.js gerekmez.
