# تشغيل النسخة الأولية

## ما الذي تم بناؤه

هذه النسخة الأولية تتكون من:

- `Lumina.Accessibility`
  يلتقط تغيّر الـ focus عبر `UI Automation`
- `Lumina.Scripting`
  يقرر كيف تُحوَّل الأحداث إلى نص منطوق
- `Lumina.Speech`
  ينطق النص عبر `SAPI`
- `Lumina.Host`
  يشغل كل الطبقات معًا

## أوامر التشغيل المتوقعة على Windows

```powershell
dotnet build .\Lumina.sln
dotnet run --project .\src\Lumina.Host\Lumina.Host.csproj
```

## ما الذي تفعله النسخة الحالية

- تراقب تغيّر العنصر الذي يملك focus
- تقرأ اسم العنصر ودوره وقيمته إن وجدت
- تمرر الحدث إلى طبقة السكربت
- تنطق الناتج
- تسجل Inspector في `inspector/focus-events.jsonl`

## ما الذي لم يكتمل بعد

- دمج Lua الحقيقي بدل المحرك المؤقت
- دعم `MSAA` و `IAccessible2`
- اعتراض لوحة المفاتيح
- أداة inspection
- طبقة AI وOCR
