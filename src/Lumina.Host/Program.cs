using Lumina.Accessibility.Windows;
using Lumina.Core.Models;
using Lumina.Core.Services;
using Lumina.Input;
using Lumina.Output.Inspection;
using Lumina.Scripting.Rules;
using Lumina.Speech;

try
{
    AppDomain.CurrentDomain.UnhandledException += (_, args) =>
    {
        if (args.ExceptionObject is Exception exception)
        {
            ErrorLogger.LogError(
                source: "AppDomain.CurrentDomain.UnhandledException",
                message: "استثناء غير معالج على مستوى التطبيق.",
                exception: exception,
                context: new { args.IsTerminating });
        }
    };

    TaskScheduler.UnobservedTaskException += (_, args) =>
    {
        ErrorLogger.LogError(
            source: "TaskScheduler.UnobservedTaskException",
            message: "استثناء مهمة غير مراقب.",
            exception: args.Exception);
        args.SetObserved();
    };

    Console.OutputEncoding = System.Text.Encoding.UTF8;
    Console.WriteLine("Lumina prototype started.");
    Console.WriteLine("يتابع تغيّر التركيز focus في Windows وينطق العنصر الحالي.");
    Console.WriteLine("يسجل Inspector الأحداث في inspector/focus-events.jsonl.");
    Console.WriteLine($"يسجل الأخطاء في {ErrorLogger.GetLogDirectory()}.");
    Console.WriteLine("ويعرض نافذة Inspector حيّة لآخر الأحداث.");
    Console.WriteLine("Insert+F لقراءة العنصر الحالي.");
    Console.WriteLine("Insert+Tab لقراءة معلومات موسعة عن العنصر الحالي.");
    Console.WriteLine("كرر Insert+Tab بسرعة لقراءة تفاصيل تشخيصية أعمق عن العنصر الحالي.");
    Console.WriteLine("Insert+L لإعادة آخر رسالة منطوقة.");
    Console.WriteLine("Insert+H لقراءة آخر خطأ مسجل، وShift+Insert+H لفتح مجلد السجلات.");
    Console.WriteLine("Insert+I لتبديل Inspector.");
    Console.WriteLine("Insert+T لقراءة عنوان الصفحة الحالية في المتصفح، وShift+Insert+T لقراءة سياق الجدول الحالي.");
    Console.WriteLine("Insert+Home لقراءة ملخص النافذة الحالية.");
    Console.WriteLine("Insert+End لقراءة حالة العنصر الحالي.");
    Console.WriteLine("Insert+W لقراءة ملخص ويب سريع للعنصر الحالي.");
    Console.WriteLine("Insert+M لقراءة مسار القائمة الحالية.");
    Console.WriteLine("Insert+G لقراءة سياق الإعدادات الحالي.");
    Console.WriteLine("Insert+N للانتقال إلى العنصر التالي داخل القائمة أو القسم الحالي.");
    Console.WriteLine("Insert+P للانتقال إلى العنصر السابق داخل القائمة أو القسم الحالي.");
    Console.WriteLine("Insert+Q لقراءة حالة المحرر: السطر والعمود والتحديد.");
    Console.WriteLine("Insert+C أو Shift+Insert+C للتنقل بين خانات الاختيار داخل الإعدادات.");
    Console.WriteLine("Insert+A أو Shift+Insert+A للتنقل بين الأزرار داخل الإعدادات.");
    Console.WriteLine("Insert+O أو Shift+Insert+O للتنقل بين مربعات الخيارات داخل الإعدادات.");
    Console.WriteLine("Insert+U أو Shift+Insert+U للتنقل بين أزرار الاختيار داخل الإعدادات.");
    Console.WriteLine("Insert+D أو Shift+Insert+D للتنقل بين علامات التبويب داخل الإعدادات.");
    Console.WriteLine("Insert+Z أو Shift+Insert+Z للتنقل بين المنزلقات داخل الإعدادات.");
    Console.WriteLine("Insert+X أو Shift+Insert+X للتنقل بين النصوص داخل الإعدادات.");
    Console.WriteLine("Insert+J أو Shift+Insert+J للتنقل بين المجموعات داخل الإعدادات.");
    Console.WriteLine("Insert+S لقراءة ملخص الصفحة الحالية.");
    Console.WriteLine("Insert+F7 لفتح قائمة عناصر الصفحة الحالية، مع عرض هرمي للسياق ودعم التصفية والبحث بالحروف وتذكر آخر نوع مختار وآخر نص تصفية ومحاولة الحفاظ على التحديد السابق.");
    Console.WriteLine("Insert+R لتحديث المخزن الظاهري للصفحة.");
    Console.WriteLine("Insert+B لملخص حالة المخزن الظاهري.");
    Console.WriteLine("Insert+V لقراءة حالة السجل، وShift+Insert+V لتبديل مستوى التسجيل.");
    Console.WriteLine("Insert+Y لمزامنة المخزن الظاهري مع العنصر الحالي.");
    Console.WriteLine("Insert+Enter لتفعيل أو تعطيل وضع المراجعة النصية.");
    Console.WriteLine("Insert+Up لقراءة السطر الحالي.");
    Console.WriteLine("Insert+Down لقراءة متصلة من موضع المراجعة.");
    Console.WriteLine("Insert+Left/Right للكلمة السابقة أو اللاحقة.");
    Console.WriteLine("Insert+PageUp/PageDown للجملة السابقة أو اللاحقة.");
    Console.WriteLine("داخل وضع المراجعة: الأسهم الرئيسية أو أسهم لوحة الأرقام مع NumLock مغلق للحرف والسطر وCtrl+الأسهم للكلمة والفقرة وHome/End لبداية ونهاية السطر، مع محاولة مزامنة المؤشر النصي.");
    Console.WriteLine("داخل المتصفح: H/K/V/U/E/G/M/S/Q/O/P/N/B/X/R/C/D/T/L/I/A/F للتنقل بين العناوين والروابط والروابط المزورة وغير المزورة والحقول والرسومات والإطارات والفواصل والاقتباسات الكتلية والعناصر المضمنة والفقرات النصية وكتل النص خارج الروابط والأزرار وخانات الاختيار وأزرار الاختيار ومربعات الخيارات والمعالم والجداول والقوائم وعناصر القوائم والحوارات وعناصر النماذج.");
    Console.WriteLine("الأنواع الإضافية مثل tabs وmenu items وtree items وarticles وfigures وgroupings وprogress bars أصبحت مدعومة في قائمة العناصر وطبقة التنقل الداخلية، وبعضها بلا حرف افتراضي مثل NVDA.");
    Console.WriteLine("الأرقام 1 إلى 9 للتنقل بين العناوين حسب المستوى، وShift+الرقم للرجوع.");
    Console.WriteLine("Insert+Space داخل المتصفح للتبديل بين وضع التصفح ووضع التركيز. وShift+Insert+Space لتبديل التنقل بالحروف المفردة. وEscape للرجوع إلى وضع التصفح.");
    Console.WriteLine("داخل وضع التصفح: Enter يفعّل الروابط والأزرار ومعظم عناصر النماذج، وSpace مناسب خصوصا للأزرار وخانات الاختيار وأزرار الاختيار.");
    Console.WriteLine("داخل وضع التصفح: Tab وShift+Tab للتنقل بين العناصر التفاعلية من موضع المؤشر، و, وShift+, للتنقل داخل الحاوية الحالية.");
    Console.WriteLine("قد ينتقل التطبيق تلقائيا إلى وضع التركيز عند الوصول إلى عناصر تفاعلية مثل حقول الإدخال ومربعات الخيارات وعناصر القوائم وعلامات التبويب، وكذلك بعض المحررات الغنية داخل صفحات الويب.");
    Console.WriteLine("داخل الجداول: Shift+Insert+T لقراءة سياق الجدول الحالي، وAltGr مع الأسهم للتنقل بين الخلايا.");
    Console.WriteLine("اضغط Ctrl+C للإيقاف.");
    ErrorLogger.LogInfo("Program.Main", $"بدأ Lumina. {ErrorLogger.GetStatusSummary()}");

    var speechService = new SapiSpeechService();
    var inspectorSink = new CompositeInspectorSink(
        new JsonInspectorSink(),
        new LiveInspectorSink());
    using var accessibilityService = new UiaAccessibilityService();
    accessibilityService.EventRaised += (_, screenEvent) =>
    {
        BrowserVirtualBuffer.NotifyAccessibilityEvent(screenEvent);
    };

    using var runtime = new LuminaRuntime(
        accessibilityService,
        new SimpleLuaStyleScriptEngine(),
        speechService,
        inspectorSink);
    using var browserElementsDialog = new BrowserElementsDialog();

    using var hotKeys = new KeyboardCommandManager(
        speakCurrentFocus: () =>
        {
            string text = FocusSnapshotReader.ReadCurrentFocusSummary();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        repeatLastSpeech: speechService.RepeatLast,
        speakLatestErrorSummary: () =>
        {
            string text = ErrorLogger.GetLatestErrorSummary();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        openLogDirectory: () =>
        {
            string text = ErrorLogger.OpenLogDirectory();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        speakCurrentElementDetails: () =>
        {
            string text = FocusSnapshotReader.ReadCurrentElementDetails();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        speakCurrentElementAdvancedDetails: () =>
        {
            string text = FocusSnapshotReader.ReadCurrentElementAdvancedDetails();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        toggleInspector: inspectorSink.Toggle,
        speakPageTitle: () =>
        {
            string text = FocusSnapshotReader.ReadCurrentPageTitle();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        speakCurrentWindowSummary: () =>
        {
            string text = FocusSnapshotReader.ReadCurrentWindowSummary();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        speakCurrentStatusSummary: () =>
        {
            string text = FocusSnapshotReader.ReadCurrentStatusSummary();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        speakWebSummary: () =>
        {
            string text = FocusSnapshotReader.ReadCurrentWebSummary();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        speakMenuContext: () =>
        {
            string text = FocusSnapshotReader.ReadCurrentMenuContext();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        speakSettingsContext: () =>
        {
            string text = FocusSnapshotReader.ReadCurrentSettingsContext();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextContextItem: () =>
        {
            string text = FocusSnapshotReader.MoveToNextContextItem();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousContextItem: () =>
        {
            string text = FocusSnapshotReader.MoveToPreviousContextItem();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextSettingsCheckbox: () =>
        {
            string text = FocusSnapshotReader.MoveToNextSettingsCheckbox();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousSettingsCheckbox: () =>
        {
            string text = FocusSnapshotReader.MoveToPreviousSettingsCheckbox();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextSettingsButton: () =>
        {
            string text = FocusSnapshotReader.MoveToNextSettingsButton();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousSettingsButton: () =>
        {
            string text = FocusSnapshotReader.MoveToPreviousSettingsButton();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextSettingsComboBox: () =>
        {
            string text = FocusSnapshotReader.MoveToNextSettingsComboBox();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousSettingsComboBox: () =>
        {
            string text = FocusSnapshotReader.MoveToPreviousSettingsComboBox();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextSettingsRadioButton: () =>
        {
            string text = FocusSnapshotReader.MoveToNextSettingsRadioButton();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousSettingsRadioButton: () =>
        {
            string text = FocusSnapshotReader.MoveToPreviousSettingsRadioButton();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextSettingsTab: () =>
        {
            string text = FocusSnapshotReader.MoveToNextSettingsTab();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousSettingsTab: () =>
        {
            string text = FocusSnapshotReader.MoveToPreviousSettingsTab();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextSettingsSlider: () =>
        {
            string text = FocusSnapshotReader.MoveToNextSettingsSlider();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousSettingsSlider: () =>
        {
            string text = FocusSnapshotReader.MoveToPreviousSettingsSlider();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextSettingsText: () =>
        {
            string text = FocusSnapshotReader.MoveToNextSettingsText();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousSettingsText: () =>
        {
            string text = FocusSnapshotReader.MoveToPreviousSettingsText();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextSettingsGroup: () =>
        {
            string text = FocusSnapshotReader.MoveToNextSettingsGroup();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousSettingsGroup: () =>
        {
            string text = FocusSnapshotReader.MoveToPreviousSettingsGroup();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        readEditorStatusSummary: () =>
        {
            string text = TextReviewCursor.ReadEditorStatusSummary();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextHeading: () =>
        {
            string text = BrowserNavigator.MoveToNextHeading();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousHeading: () =>
        {
            string text = BrowserNavigator.MoveToPreviousHeading();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextHeadingLevel: level =>
        {
            string text = BrowserNavigator.MoveToNextHeadingLevel(level);
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousHeadingLevel: level =>
        {
            string text = BrowserNavigator.MoveToPreviousHeadingLevel(level);
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextLink: () =>
        {
            string text = BrowserNavigator.MoveToNextLink();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousLink: () =>
        {
            string text = BrowserNavigator.MoveToPreviousLink();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextVisitedLink: () =>
        {
            string text = BrowserNavigator.MoveToNextVisitedLink();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousVisitedLink: () =>
        {
            string text = BrowserNavigator.MoveToPreviousVisitedLink();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextUnvisitedLink: () =>
        {
            string text = BrowserNavigator.MoveToNextUnvisitedLink();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousUnvisitedLink: () =>
        {
            string text = BrowserNavigator.MoveToPreviousUnvisitedLink();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextEditField: () =>
        {
            string text = BrowserNavigator.MoveToNextEditField();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousEditField: () =>
        {
            string text = BrowserNavigator.MoveToPreviousEditField();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextGraphic: () =>
        {
            string text = BrowserNavigator.MoveToNextGraphic();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousGraphic: () =>
        {
            string text = BrowserNavigator.MoveToPreviousGraphic();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextFrame: () =>
        {
            string text = BrowserNavigator.MoveToNextFrame();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousFrame: () =>
        {
            string text = BrowserNavigator.MoveToPreviousFrame();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextSeparator: () =>
        {
            string text = BrowserNavigator.MoveToNextSeparator();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousSeparator: () =>
        {
            string text = BrowserNavigator.MoveToPreviousSeparator();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextBlockQuote: () =>
        {
            string text = BrowserNavigator.MoveToNextBlockQuote();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousBlockQuote: () =>
        {
            string text = BrowserNavigator.MoveToPreviousBlockQuote();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextEmbeddedObject: () =>
        {
            string text = BrowserNavigator.MoveToNextEmbeddedObject();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousEmbeddedObject: () =>
        {
            string text = BrowserNavigator.MoveToPreviousEmbeddedObject();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextTextParagraph: () =>
        {
            string text = BrowserNavigator.MoveToNextTextParagraph();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousTextParagraph: () =>
        {
            string text = BrowserNavigator.MoveToPreviousTextParagraph();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextNotLinkBlock: () =>
        {
            string text = BrowserNavigator.MoveToNextNotLinkBlock();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousNotLinkBlock: () =>
        {
            string text = BrowserNavigator.MoveToPreviousNotLinkBlock();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextButton: () =>
        {
            string text = BrowserNavigator.MoveToNextButton();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousButton: () =>
        {
            string text = BrowserNavigator.MoveToPreviousButton();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextCheckbox: () =>
        {
            string text = BrowserNavigator.MoveToNextCheckbox();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousCheckbox: () =>
        {
            string text = BrowserNavigator.MoveToPreviousCheckbox();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextRadioButton: () =>
        {
            string text = BrowserNavigator.MoveToNextRadioButton();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousRadioButton: () =>
        {
            string text = BrowserNavigator.MoveToPreviousRadioButton();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextComboBox: () =>
        {
            string text = BrowserNavigator.MoveToNextComboBox();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousComboBox: () =>
        {
            string text = BrowserNavigator.MoveToPreviousComboBox();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextLandmark: () =>
        {
            string text = BrowserNavigator.MoveToNextLandmark();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousLandmark: () =>
        {
            string text = BrowserNavigator.MoveToPreviousLandmark();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextTable: () =>
        {
            string text = BrowserNavigator.MoveToNextTable();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousTable: () =>
        {
            string text = BrowserNavigator.MoveToPreviousTable();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextList: () =>
        {
            string text = BrowserNavigator.MoveToNextList();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousList: () =>
        {
            string text = BrowserNavigator.MoveToPreviousList();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextListItem: () =>
        {
            string text = BrowserNavigator.MoveToNextListItem();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousListItem: () =>
        {
            string text = BrowserNavigator.MoveToPreviousListItem();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextDialog: () =>
        {
            string text = BrowserNavigator.MoveToNextDialog();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousDialog: () =>
        {
            string text = BrowserNavigator.MoveToPreviousDialog();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextFormField: () =>
        {
            string text = BrowserNavigator.MoveToNextFormField();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousFormField: () =>
        {
            string text = BrowserNavigator.MoveToPreviousFormField();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToStartOfContainer: () =>
        {
            string text = BrowserNavigator.MoveToStartOfContainer();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        movePastEndOfContainer: () =>
        {
            string text = BrowserNavigator.MovePastEndOfContainer();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextFocusableElement: () =>
        {
            string text = BrowserNavigator.MoveToNextFocusableElement();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousFocusableElement: () =>
        {
            string text = BrowserNavigator.MoveToPreviousFocusableElement();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        activateCurrentBrowserElement: () =>
        {
            string text = BrowserNavigator.ActivateCurrentElement();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        readCurrentTableContext: () =>
        {
            string text = BrowserNavigator.ReadCurrentTableContext();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToNextTableCell: () =>
        {
            string text = BrowserNavigator.MoveToNextTableCell();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToPreviousTableCell: () =>
        {
            string text = BrowserNavigator.MoveToPreviousTableCell();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToTableCellBelow: () =>
        {
            string text = BrowserNavigator.MoveToTableCellBelow();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToTableCellAbove: () =>
        {
            string text = BrowserNavigator.MoveToTableCellAbove();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        summarizeCurrentPage: () =>
        {
            string text = BrowserNavigator.SummarizeCurrentPage();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        showBrowserElementsList: () =>
        {
            string text = browserElementsDialog.ShowOrFocus();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        refreshVirtualBuffer: () =>
        {
            string text = BrowserVirtualBuffer.Refresh();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        summarizeVirtualBuffer: () =>
        {
            string text = BrowserVirtualBuffer.SummarizeBuffer();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        syncVirtualBufferToFocus: () =>
        {
            string text = BrowserVirtualBuffer.SyncToFocusedElement();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        speakLoggingStatus: () =>
        {
            string text = ErrorLogger.GetStatusSummary();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        cycleLoggingVerbosity: () =>
        {
            string text = ErrorLogger.CycleVerbosity();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        announceTextReviewMode: enabled =>
        {
            string text = enabled
                ? "تم تفعيل وضع المراجعة النصية"
                : "تم تعطيل وضع المراجعة النصية";
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        readCurrentLine: () =>
        {
            string text = TextReviewCursor.ReadCurrentLine();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        readPreviousLine: () =>
        {
            string text = TextReviewCursor.ReadPreviousLine();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        readNextLine: () =>
        {
            string text = TextReviewCursor.ReadNextLine();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        readPreviousCharacter: () =>
        {
            string text = TextReviewCursor.ReadPreviousCharacter();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        readNextCharacter: () =>
        {
            string text = TextReviewCursor.ReadNextCharacter();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        readPreviousWord: () =>
        {
            string text = TextReviewCursor.ReadPreviousWord();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        readNextWord: () =>
        {
            string text = TextReviewCursor.ReadNextWord();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        readPreviousParagraph: () =>
        {
            string text = TextReviewCursor.ReadPreviousParagraph();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        readNextParagraph: () =>
        {
            string text = TextReviewCursor.ReadNextParagraph();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        readPreviousSentence: () =>
        {
            string text = TextReviewCursor.ReadPreviousSentence();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        readNextSentence: () =>
        {
            string text = TextReviewCursor.ReadNextSentence();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToStartOfLine: () =>
        {
            string text = TextReviewCursor.MoveToStartOfLine();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        moveToEndOfLine: () =>
        {
            string text = TextReviewCursor.MoveToEndOfLine();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        sayAllFromReviewCursor: () =>
        {
            string text = TextReviewCursor.SayAllFromReviewCursor();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        speakBrowserMessage: text =>
        {
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        });

    runtime.Start();
    hotKeys.Start();

    using ManualResetEvent shutdown = new(initialState: false);
    Console.CancelKeyPress += (_, args) =>
    {
        args.Cancel = true;
        shutdown.Set();
    };

    shutdown.WaitOne();
}
catch (Exception exception)
{
    ErrorLogger.LogError(
        source: "Program.Main",
        message: "فشل تشغيل Lumina.",
        exception: exception);
    Console.Error.WriteLine("Lumina failed to start.");
    Console.Error.WriteLine(exception);
    Environment.ExitCode = 1;
}
