# تصميم مشروع Lumina بالعربية

## 1. الفكرة العامة

`Lumina` هو قارئ شاشة حديث لويندوز فقط، مصمم ليكون أسرع من الحلول التقليدية وأكثر مرونة في التخصيص.  
الاعتماد الرئيسي يكون على `UI Automation` لأن هذا هو API الأكثر تطورًا في تطبيقات Windows الحديثة، مع وجود طبقات fallback للتعامل مع التطبيقات القديمة أو غير المكتملة وصوليًا.

الاختيار المعماري المقترح هنا هو:

- اللغة الأساسية: `C# / .NET 8` لسرعة التطوير وسهولة التعامل مع Windows APIs
- أجزاء الأداء الحرجة يمكن نقلها لاحقًا إلى `C++`
- التخصيص السلوكي: `Lua` ويفضل `LuaJIT` إن كان الدمج مناسبًا
- منصة مستهدفة: `Windows 10/11`

## 2. المعمارية الكاملة

```text
Lumina.Host
|
+-- Core Engine
|   +-- Event Loop
|   +-- Focus Tracker
|   +-- Navigation Controller
|   +-- Event Dispatcher
|
+-- Accessibility Layer
|   +-- UIA Client
|   +-- MSAA Adapter
|   +-- IA2 Adapter
|   +-- OCR / Vision Adapter
|
+-- Speech Layer
|   +-- SAPI 5 Engine
|   +-- Optional SAPI 4 Adapter
|   +-- Speech Queue
|   +-- Priority / Interrupt Controller
|
+-- Script Layer
|   +-- Lua Host
|   +-- Script Registry
|   +-- App Profiles
|   +-- Event Hooks
|
+-- Input Layer
|   +-- Keyboard Hook
|   +-- Gesture / Shortcut Router
|   +-- Command Mapper
|
+-- Output Layer
|   +-- Speech Output
|   +-- Braille Adapter (مستقبلا)
|   +-- Logging
|   +-- Diagnostic Console
|
+-- AI Layer
|   +-- UI Summarizer
|   +-- Event Relevance Filter
|   +-- Context Compression
|
+-- Plugin Layer
    +-- Plugin Loader
    +-- Plugin Contracts
    +-- Marketplace Metadata
```

## 3. شرح الطبقات

### Core Engine

هذه الطبقة هي القلب الحقيقي للنظام.

- `Event Loop`
  يستقبل أحداث الوصولية، أحداث لوحة المفاتيح، وأوامر المستخدم.
- `Focus Tracker`
  يتابع العنصر الحالي الذي يملك focus ويكشف تغيّراته بدقة.
- `Navigation Controller`
  يدير التنقل الكائني Object Navigation والتنقل المكاني Spatial Navigation.
- `Event Dispatcher`
  يوزع الأحداث على Lua وSpeech وLogging وPlugins.

### Accessibility Layer

- `UIA Client`
  المصدر الرئيسي لقراءة العناصر، الأدوار، القيم، الحالات، والعلاقات.
- `MSAA Adapter`
  يستخدم للتطبيقات القديمة التي لا تقدم UIA جيدًا.
  كما يمكن استخدامه كـ fallback عندما تكون بيانات UIA ناقصة.
- `IAccessible2 Adapter`
  مهم خاصة للمتصفحات وبعض التطبيقات المبنية على محركات خاصة.
  النسخة الحالية في المستودع أضافت probe أوليًا لاكتشاف دعم IA2 ووسم المصدر وتحسين hint،
  بينما القراءة العميقة لواجهات النص والعلاقات ما زالت خطوة لاحقة.
- `OCR / Vision Adapter`
  fallback مرئي عندما تكون العناصر غير مكشوفة وصوليًا.

### Speech Layer

- `SAPI 5 Engine`
  المحرك الأساسي للنطق.
- `SAPI 4 Adapter`
  اختياري للتوافق مع بيئات أقدم.
- `Speech Queue`
  يمنع تقاطع الرسائل وينظم الرسائل حسب الأولوية.
- `Interrupt Controller`
  يوقف النطق الحالي عند حدث أهم، مثل تغيير focus أو alert.

### Script Layer

- `Lua Host`
  يشغل ملفات Lua المضمنة.
- `Script Registry`
  يربط كل تطبيق أو نافذة أو role بملف script مناسب.
- `Event Hooks`
  مثل `onFocusChanged`, `onValueChanged`, `onAnnouncement`.
- `App Profiles`
  قواعد مخصصة لكل تطبيق مثل Chrome أو Word أو Explorer.

### Input Layer

- التقاط لوحة المفاتيح عبر low-level hook
- تعريف أوامر خاصة مثل:
  - قراءة العنصر الحالي
  - قراءة السطر الحالي
  - تلخيص الشاشة بالذكاء الاصطناعي
  - التنقل التالي/السابق

### Output Layer

- النطق
- التسجيل التشخيصي
- نافذة developer inspection داخلية تشبه Inspect

### AI Layer

- `UI Summarizer`
  يلخص الشاشة الحالية بدل قراءة كل العناصر.
- `Event Relevance Filter`
  يقرر هل الحدث مهم للمستخدم أم يجب تجاهله.
- `Context Compression`
  يقلل الضجيج ويحول عشرات الأحداث إلى رسالة مفيدة واحدة.

### Plugin Layer

- تحميل إضافات من مجلد plugins
- تعريف واجهات موحدة للإضافات
- إمكانية Marketplace مستقبلًا

## 4. بنية المجلدات المقترحة

```text
lumina/
├─ docs/
│  └─ architecture-ar.md
├─ samples/
│  ├─ UiaReader.cs
│  ├─ SpeechQueue.cs
│  └─ LuaHost.cs
├─ scripts/
│  └─ focus_profile.lua
├─ src/
│  ├─ Lumina.Host/
│  ├─ Lumina.Core/
│  ├─ Lumina.Accessibility/
│  ├─ Lumina.Speech/
│  ├─ Lumina.Scripting/
│  ├─ Lumina.Input/
│  ├─ Lumina.Output/
│  ├─ Lumina.AI/
│  └─ Lumina.Plugins/
└─ README.md
```

## 5. مسؤوليات الوحدات الأساسية

### Lumina.Host

- نقطة تشغيل التطبيق
- تهيئة الخدمات
- إدارة دورة الحياة

### Lumina.Core

- تعريف الأحداث المشتركة
- منسق الأوامر
- تتبع السياق الحالي

### Lumina.Accessibility

- التعامل مع UIA وMSAA وIA2
- تحويل كل واجهة إلى نموذج موحد `AccessibleNode`

### Lumina.Speech

- محركات النطق
- queue
- priority
- interruption rules

### Lumina.Scripting

- دمج Lua
- hooks
- app profiles

### Lumina.Input

- hotkeys
- key gestures
- command router

### Lumina.Output

- logging
- inspection UI
- traces

### Lumina.AI

- تلخيص الشاشة
- تحليل أهمية الأحداث
- fallback vision

### Lumina.Plugins

- عقود الإضافات
- discovery
- loading

## 6. نموذج البيانات الداخلي

يفضل توحيد كل العناصر المقروءة في كائن داخلي واحد:

```text
AccessibleNode
- Id
- SourceApi (UIA / MSAA / IA2 / OCR)
- Name
- Role
- Value
- State
- Bounds
- Parent
- Children
- Actions
- Metadata
```

هذا يمنع بقية الطبقات من الارتباط المباشر مع API واحد.

## 7. مثال قراءة عنصر UIA

الملف: [samples/UiaReader.cs](/d:/flutterProjects/lumina/samples/UiaReader.cs)

الفكرة:

- الحصول على العنصر الذي يملك focus
- قراءة الاسم `Name`
- قراءة الدور `ControlType`
- محاولة قراءة القيمة عبر `ValuePattern`

## 8. مثال النطق

الملف: [samples/SpeechQueue.cs](/d:/flutterProjects/lumina/samples/SpeechQueue.cs)

الفكرة:

- كل رسالة نطق تدخل queue
- كل رسالة لها priority
- الرسائل الحرجة تقطع الرسائل الأقل أهمية

## 9. مثال تشغيل Lua

الملف: [samples/LuaHost.cs](/d:/flutterProjects/lumina/samples/LuaHost.cs)

الفكرة:

- تمرير حدث focus إلى Lua
- Lua يقرر: هل ينطق؟ ماذا ينطق؟ هل يتجاهل؟

## 10. مثال ملف Lua

الملف: [scripts/focus_profile.lua](/d:/flutterProjects/lumina/scripts/focus_profile.lua)

الفكرة:

- عند انتقال focus إلى زر أو حقل نصي
- يبني نصًا منطوقًا مناسبًا

## 11. استراتيجية الأداء وتقليل flood

هذه نقطة حاسمة لأن قارئ الشاشة قد يتعرض لآلاف الأحداث في وقت قصير.

- استخدام `debounce` للأحداث المتكررة بسرعة
- استخدام `coalescing` لدمج عدة أحداث متشابهة في حدث واحد
- تجاهل الأحداث منخفضة القيمة إذا لم تغيّر تجربة المستخدم
- مقارنة الحالة السابقة بالجديدة قبل النطق
- إعطاء أولوية عالية للأحداث التالية:
  - focus changed
  - alert
  - dialog opened
  - selection changed إذا كانت نتيجة أمر مباشر من المستخدم
- تشغيل AI فقط عند الطلب أو عند العجز عن التفسير الطبيعي

قاعدة عملية:

- `Focus` لا يتأخر
- `ValueChanged` يمر عبر filter
- `StructureChanged` لا يُنطق مباشرة غالبًا

## 12. دعم المتصفحات والتطبيقات المعقدة

المتصفحات تحتاج طبقة خاصة لأن الشجرة الوصولية فيها كبيرة ومعقدة.

الخطة:

- إنشاء `Browser Adapter`
- دعم Chrome / Edge / Firefox عبر:
  - UIA عندما يكون جيدًا
  - IA2 عند الحاجة خاصة في Firefox وبعض المحتويات
  المستودع الحالي يحتوي الآن على Browser Adapter أولي يضيف `semantic roles`
  مثل `web_link` و `web_heading` و `web_edit` إلى النموذج الداخلي.
- إنشاء browse mode منفصل عن focus mode
  التطبيق الحالي أضاف أوامر أولية قريبة من `browse mode`
  مثل قراءة عنوان الصفحة وقراءة ملخص العنصر الحالي داخل سياق المتصفح.
  كما أضيف تنقل أولي بين الفئات الدلالية الشائعة:
  العناوين والروابط وحقول الإدخال.
  ويوجد الآن ملخص صفحة سريع يجمع عدد العناصر الدلالية الأساسية
  تمهيدًا للانتقال لاحقًا إلى AI summarization أعمق.
  كما أضيف `virtual buffer` أولي يحتفظ بلقطة مرتبة لعناصر الويب
  ويمكن التنقل داخله بشكل مستقل عن التركيز الحي.
  التحديث الحالي أضاف مزامنة أولية بين هذا المخزن والتركيز الحقيقي
  بحيث لا ينفصل مسار القراءة الظاهري عن مسار التركيز الفعلي.
- إضافة virtual buffer اختياري لاحقًا
- تعريف profiles خاصة:
  - صفحات الويب
  - تطبيقات Electron
  - Office
  - Java apps

## 13. ميزات متقدمة مقترحة

- `Smart Summary`
  يصف الشاشة مثل: "نافذة إعدادات، 3 تبويبات، زر حفظ، حقل بحث نشط"
- `Intent-aware event filtering`
  إذا كان المستخدم يكتب، لا تقرأ كل تغير ثانوي
- `Vision fallback`
  OCR أو CV عند غياب الوصولية
- `Spatial navigation`
  الانتقال بحسب الاتجاهات الفعلية على الشاشة
- `Developer Inspector`
  أداة داخلية لعرض:
  - API المستخدم
  - role
  - name
  - patterns
  - bounding rectangle
  - script selected

## 14. لماذا هذا التصميم جيد إنتاجيًا

- يفصل المصدر الوصولي عن منطق القراءة
- يسمح بتبديل محرك النطق أو Lua أو AI بدون إعادة كتابة القلب
- يسهل اختبار كل طبقة وحدها
- مناسب لمشروع مفتوح المصدر أو منتج تجاري طويل العمر

## 15. توصية التنفيذ العملي

ابدأ بهذا الترتيب:

1. `Lumina.Accessibility` مع UIA فقط
2. `Lumina.Speech` مع SAPI 5
3. `Lumina.Core` للأحداث والتنقل
4. `Lumina.Scripting` مع Lua hooks
5. `Lumina.Input` للاختصارات
6. fallback APIs
7. AI summarization
8. plugin marketplace

## 16. خلاصة عربية مبسطة

المشروع المقترح ليس مجرد قارئ شاشة يقرأ النصوص، بل منصة وصولية ذكية:

- تقرأ الواجهة عبر UIA
- وتستدعي MSAA عندما تكون بيانات UIA غير كافية
- وتكشف دعم IA2 في التطبيقات المعقدة لتوجيه fallback المناسب
- تتصرف حسب التطبيق عبر Lua
- تنطق بذكاء عبر queue وأولويات
- تقلل الإزعاج عبر filtering
- تستخدم AI وOCR عندما تفشل الطرق التقليدية

وهذا يجعله أقرب إلى جيل جديد من قارئات الشاشة، بدل مجرد نسخة مكررة من الحلول الحالية.
