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
- تعرض نافذة Inspector حيّة فيها آخر أحداث focus
- ترصد التطبيقات المرشحة لـ `IAccessible2` مثل Firefox وChromium وElectron
- تطبّع عناصر الويب إلى `semantic roles` أوضح داخل المتصفح
- تدعم hotkeys:
  - `Ctrl+Alt+F` لقراءة العنصر الحالي
  - `Ctrl+Alt+L` لإعادة آخر نطق
  - `Ctrl+Alt+I` لإظهار أو إخفاء Inspector وإيقافه مؤقتًا

## ما الذي لم يكتمل بعد

- تعميق قراءة `IAccessible2` إلى واجهات النص والعلاقات وليس الاكتشاف فقط
- اعتراض لوحة المفاتيح
- طبقة AI وOCR
