# Lumina

Lumina هو مشروع قارئ شاشة حديث مخصص لنظام Windows 10/11، مبني بفكرة معمارية modular وقابلية التوسعة عبر Lua والإضافات.

هذا المستودع يبدأ كتصميم هندسي وblueprint عملي لمشروع Windows-only بدل Flutter، بهدف:

- الاعتماد أولًا على `Microsoft UI Automation (UIA)`
- استخدام `MSAA` و `IAccessible2` كطبقات fallback
- تشغيل محرك نطق يعتمد على `SAPI 5`
- تضمين Lua للتخصيص الديناميكي
- دعم الإضافات، المرشحات الذكية، وميزات AI مستقبلًا
- تشغيل سكربتات `Lua` فعلية عبر `NLua`

الوثيقة الأساسية بالعربية موجودة هنا:

- [docs/architecture-ar.md](/d:/flutterProjects/lumina/docs/architecture-ar.md)

أمثلة الكود الأولية:

- [samples/UiaReader.cs](/d:/flutterProjects/lumina/samples/UiaReader.cs)
- [samples/SpeechQueue.cs](/d:/flutterProjects/lumina/samples/SpeechQueue.cs)
- [samples/LuaHost.cs](/d:/flutterProjects/lumina/samples/LuaHost.cs)
- [scripts/focus_profile.lua](/d:/flutterProjects/lumina/scripts/focus_profile.lua)

## البناء عبر GitHub Actions فقط

لا تحتاج إلى أي متطلبات بناء محلية على جهازك.

- بناء Windows التلقائي موجود في:
  [windows-build.yml](/d:/flutterProjects/lumina/.github/workflows/windows-build.yml)
- إصدار ZIP جاهز من GitHub موجود في:
  [windows-release.yml](/d:/flutterProjects/lumina/.github/workflows/windows-release.yml)

طريقة الاستخدام:

1. ادفع التعديلات إلى `main`
2. افتح تبويب `Actions` في GitHub
3. شغّل `windows-build` للحصول على artifact
4. أو شغّل `windows-release` لإنشاء Release مع ملف ZIP
