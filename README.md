# InternetLog

A simple Windows Forms application that monitors internet connectivity by sending periodic ping requests.

## Features
- Every 15 seconds, pings Google DNS (`8.8.8.8`), Cloudflare DNS (`1.1.1.1`) and your modem (default gateway, detected automatically)
- Internet counts as available if either DNS server responds
- Pings run asynchronously in parallel, so the window never freezes
- Logs connection loss and recovery times, including how long each outage lasted
- Window title shows the number of drops in the last 24 hours
- Saves logs to `internet_log.txt` next to the executable
- No background service
- No data is sent anywhere

## Build
Requires the .NET 8 SDK on Windows (or Visual Studio 2022). Open `InternetLog.sln`, or run:

```
dotnet run --project InternetLog
```

## Usage
- Run the application
- Leave it open to monitor your connection
- Close the app to stop all activity

This project is for personal connection monitoring.

---

## Türkçe

İnternet bağlantınızı düzenli ping atarak izleyen basit bir Windows Forms uygulaması.

- 15 saniyede bir `8.8.8.8`, `1.1.1.1` ve modeme (varsayılan ağ geçidi, otomatik bulunur) ping atar
- Kopma ve geri gelme anlarını, kesintinin ne kadar sürdüğüyle birlikte kaydeder
- Pencere başlığında son 24 saatteki kopma sayısını gösterir
- Loglar exe'nin yanındaki `internet_log.txt` dosyasına yazılır
- Hiçbir veri dışarı gönderilmez
