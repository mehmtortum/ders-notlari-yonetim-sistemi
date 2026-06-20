# Ders Notlari Yonetim Sistemi

Bu proje, kullanicilarin kendi hesaplariyla giris yaparak ders notlarini yonetebildigi full stack bir uygulamadir. Kullanici sadece kendisine ait notlari gorebilir; not ekleyebilir, dosya yukleyebilir, mevcut notlari guncelleyebilir, notlari arsive alabilir ve arsivden kalici olarak silebilir.

## Kullanilan Teknolojiler

- Frontend: React.js, Vite, modern CSS, lucide-react ikonlari
- Backend: ASP.NET Core 8 Web API
- Veritabani: Microsoft SQL Server
- ORM: Entity Framework Core
- Kimlik dogrulama: JWT
- Dosya yukleme: PDF, Word, metin ve gorsel dosyalari

## Ozellikler

- Kullanici kaydi ve girisi
- JWT ile korunan API endpointleri
- Kullaniciya ozel not listeleme
- Ders adi, aciklama ve dosya ile not ekleme
- Not bilgilerini ve dosyasini guncelleme
- Soft delete: notu arsive alma
- Arsivden geri alma
- Hard delete: arsivdeki notu sistemden tamamen silme
- Migration dosyalari ve ornek seeder verisi

## Proje Yapisi

```text
ders-notlari-yonetim-sistemi/
  backend/
    Data/
      Migrations/
      AppDbContext.cs
      SeedData.cs
    Dtos/
    Models/
    Services/
    Program.cs
  frontend/
    src/
      App.jsx
      api.js
      styles.css
  docker-compose.yml
```

## Kurulum

### 1. SQL Server'i baslatma

Docker kullaniyorsaniz proje kok dizininde:

```bash
docker compose up -d
```

Varsayilan connection string:

```text
Server=localhost,1433;Database=DersNotlariDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True
```

Gerekirse `backend/appsettings.json` icinden guncelleyebilirsiniz.

### 2. Backend'i calistirma

```bash
cd backend
dotnet restore
dotnet run --urls http://localhost:5000
```

Uygulama acilirken `SeedData` migrationlari uygular ve ornek kullaniciyi ekler.

Demo kullanici:

```text
E-posta: demo@tetacode.com
Sifre: Demo123!
```

### 3. Frontend'i calistirma

Yeni bir terminalde:

```bash
cd frontend
npm install
npm run dev
```

React uygulamasi varsayilan olarak `http://localhost:5173` adresinde calisir.

Farkli bir API adresi kullanmak icin:

```bash
VITE_API_URL=http://localhost:5000 npm run dev
```

## API Uc Noktalari

| Metot | Endpoint | Aciklama |
| --- | --- | --- |
| POST | `/api/auth/register` | Yeni kullanici kaydi |
| POST | `/api/auth/login` | Kullanici girisi |
| GET | `/api/notes` | Aktif notlari listeler |
| GET | `/api/notes/archive` | Arsivlenmis notlari listeler |
| POST | `/api/notes` | Yeni not ekler |
| PUT | `/api/notes/{id}` | Notu gunceller |
| DELETE | `/api/notes/{id}` | Notu arsive alir |
| POST | `/api/notes/{id}/restore` | Arsivden geri alir |
| DELETE | `/api/notes/{id}/hard` | Arsivdeki notu kalici siler |

Not ekleme ve guncelleme endpointleri `multipart/form-data` bekler:

- `courseName`
- `description`
- `file` opsiyonel

## Teslim Notlari

- Migration dosyalari `backend/Data/Migrations` altindadir.
- Seeder verisi `backend/Data/SeedData.cs` icindedir.
- Kod yapisi backend ve frontend olarak ayrilmistir.
- Frontend modern, responsive ve kullanici dostu olacak sekilde hazirlanmistir.
