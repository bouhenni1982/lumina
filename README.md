# Lumina

Lumina هو مشروع قارئ شاشة حديث مخصص لنظام Windows 10/11، مبني بفكرة معمارية modular وقابلية التوسعة عبر Lua والإضافات.

هذا المستودع يبدأ كتصميم هندسي وblueprint عملي لمشروع Windows-only بدل Flutter، بهدف:

- الاعتماد أولًا على `Microsoft UI Automation (UIA)`
- استخدام `MSAA` و `IAccessible2` كطبقات fallback
- تشغيل محرك نطق يعتمد على `SAPI 5`
- تضمين Lua للتخصيص الديناميكي
- دعم الإضافات، المرشحات الذكية، وميزات AI مستقبلًا

الوثيقة الأساسية بالعربية موجودة هنا:

- [docs/architecture-ar.md](/d:/flutterProjects/lumina/docs/architecture-ar.md)

أمثلة الكود الأولية:

- [samples/UiaReader.cs](/d:/flutterProjects/lumina/samples/UiaReader.cs)
- [samples/SpeechQueue.cs](/d:/flutterProjects/lumina/samples/SpeechQueue.cs)
- [samples/LuaHost.cs](/d:/flutterProjects/lumina/samples/LuaHost.cs)
- [scripts/focus_profile.lua](/d:/flutterProjects/lumina/scripts/focus_profile.lua)
