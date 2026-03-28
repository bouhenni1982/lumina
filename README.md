# Lumina

Lumina هو مشروع قارئ شاشة حديث مخصص لنظام Windows 10/11، مبني بفكرة معمارية modular وقابلية التوسعة عبر Lua والإضافات.

هذا المستودع يبدأ كتصميم هندسي وblueprint عملي لمشروع Windows-only بدل Flutter، بهدف:

- الاعتماد أولًا على `Microsoft UI Automation (UIA)`
- استخدام `MSAA` و `IAccessible2` كطبقات fallback
- تشغيل محرك نطق يعتمد على `SAPI 5`
- تضمين Lua للتخصيص الديناميكي
- دعم الإضافات، المرشحات الذكية، وميزات AI مستقبلًا
- تشغيل سكربتات `Lua` فعلية عبر `NLua`
- دعم `App Profiles` عبر ملفات Lua حسب اسم العملية مثل `notepad` و`explorer`
- Inspector تشخيصي يسجل أحداث focus بصيغة JSON Lines
- Inspector حيّ يعرض آخر الأحداث في نافذة رسومية أثناء التشغيل
- fallback فعلي إلى `MSAA` عند نقص بيانات `UIA`
- probe أولي لـ `IAccessible2` لاكتشاف المتصفحات وتغذية `hint/source` في الشجرات المعقدة
- Browser Adapter أولي يطبع عناصر الويب إلى أدوار دلالية مثل الروابط والعناوين والحقول
- keyboard hook بمفتاح `Insert` كـ modifier رئيسي لأوامر قارئ الشاشة
- وضع مراجعة نصية أولي يحاكي JAWS:
  - `Insert+Tab` لقراءة معلومات موسعة عن العنصر الحالي
  - تكرار `Insert+Tab` بسرعة يقرأ تفاصيل تشخيصية أعمق عن العنصر الحالي
  - `Insert+Home` لقراءة ملخص النافذة الحالية
  - `Insert+End` لقراءة حالة العنصر الحالي
  - `Insert+Enter` لتبديل Text Review
  - `Insert+Up` للسطر الحالي
  - `Insert+Down` للقراءة المتصلة `Say All`
  - `Insert+Left/Right` للكلمة السابقة أو اللاحقة
  - `Insert+PageUp/PageDown` للجملة السابقة أو اللاحقة
  - داخل وضع المراجعة: الأسهم للحرف والسطر و`Ctrl+الأسهم` للكلمة والفقرة و`Home/End` لبداية ونهاية السطر
- أوامر Web Mode أولية لقراءة عنوان الصفحة وملخص العنصر الحالي داخل المتصفح
- تنقل ويب بالحروف داخل المتصفح:
  - `H/K/E` للعنصر التالي
  - `Shift+H/K/E` للعنصر السابق
- ملخص صفحة سريع يعدّ العناصر الدلالية الأساسية داخل الصفحة الحالية
- مخزن ظاهري أولي للويب مع تحديث وتنقل سابق/التالي وملخص حالة
- مزامنة أولية بين المخزن الظاهري والتركيز الحقيقي أثناء القراءة والتنقل

الوثيقة الأساسية بالعربية موجودة هنا:

- [docs/architecture-ar.md](/d:/flutterProjects/lumina/docs/architecture-ar.md)

أمثلة الكود الأولية:

- [samples/UiaReader.cs](/d:/flutterProjects/lumina/samples/UiaReader.cs)
- [samples/SpeechQueue.cs](/d:/flutterProjects/lumina/samples/SpeechQueue.cs)
- [samples/LuaHost.cs](/d:/flutterProjects/lumina/samples/LuaHost.cs)
- [scripts/focus_profile.lua](/d:/flutterProjects/lumina/scripts/focus_profile.lua)

مرجع الأوامر الحالي:

- [docs/commands-ar.md](/d:/flutterProjects/lumina/docs/commands-ar.md)

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
