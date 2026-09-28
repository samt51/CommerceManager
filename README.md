# CommerceManager

Tek solution: modüler monolit, Onion bağımlılık yönü, CQRS/MediatR, ASP.NET Core API ve MVC, .NET MAUI, SQL Server. İlk dilim perakende satış komutu ve API uç noktasıdır. Trendyol ve mevcut site adaptörleri henüz eklenmedi.

## Bağımlılık yönü

- Domain: bağımsız.
- Application → Domain.
- Infrastructure → Application ve Domain.
- API → Application ve Infrastructure.
- MVC ve MAUI API ile HTTP üzerinden haberleşir; doğrudan DB bağlantısı veya proje referansı yoktur.

## Gereksinimler

.NET 9 SDK; MAUI için ayrıca Android/iOS workload. iOS derlemesi Mac gerektirir. SQL Server bağlantısını API için `ConnectionStrings__CommerceManager` ortam değişkeni ile verin. `src/CommerceManager.Web/appsettings.json` içindeki `Api:BaseUrl` değerini API adresinize uyarlayın. Mobilde `MauiProgram.cs` içindeki örnek API adresini geliştirme ortamının erişebileceği gerçek adrese değiştirin.

## Çalıştırma

```bash
dotnet restore CommerceManager.sln
dotnet build src/CommerceManager.Api/CommerceManager.Api.csproj
dotnet build src/CommerceManager.Web/CommerceManager.Web.csproj
dotnet workload install maui
dotnet build src/CommerceManager.Mobile/CommerceManager.Mobile.csproj -f net9.0-android
dotnet ef migrations add InitialCreate --project src/CommerceManager.Infrastructure --startup-project src/CommerceManager.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/CommerceManager.Infrastructure --startup-project src/CommerceManager.Api
```

`dotnet ef` aracı ayrıca kurulmalıdır. Veritabanını oluşturduktan sonra `POST /api/retail-sales` için örnek istek: `{ "totalAmount": 1250.00 }`. Kimlik doğrulama ve gerçek entegrasyonlar henüz bulunmadığı için API'yi internete açık yayımlamayın. Test projesi isteğiniz doğrultusunda eklenmedi.
