using Lumina.Accessibility.Windows;
using Lumina.Core.Models;
using Lumina.Core.Services;
using Lumina.Input;
using Lumina.Output.Inspection;
using Lumina.Scripting.Rules;
using Lumina.Speech;

try
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    Console.WriteLine("Lumina prototype started.");
    Console.WriteLine("يتابع تغيّر التركيز focus في Windows وينطق العنصر الحالي.");
    Console.WriteLine("يسجل Inspector الأحداث في inspector/focus-events.jsonl.");
    Console.WriteLine("ويعرض نافذة Inspector حيّة لآخر الأحداث.");
    Console.WriteLine("Insert+F لقراءة العنصر الحالي.");
    Console.WriteLine("Insert+Tab لقراءة معلومات موسعة عن العنصر الحالي.");
    Console.WriteLine("كرر Insert+Tab بسرعة لقراءة تفاصيل تشخيصية أعمق عن العنصر الحالي.");
    Console.WriteLine("Insert+L لإعادة آخر رسالة منطوقة.");
    Console.WriteLine("Insert+I لتبديل Inspector.");
    Console.WriteLine("Insert+T لقراءة عنوان الصفحة الحالية في المتصفح.");
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
    Console.WriteLine("Insert+R لتحديث المخزن الظاهري للصفحة.");
    Console.WriteLine("Insert+B لملخص حالة المخزن الظاهري.");
    Console.WriteLine("Insert+Y لمزامنة المخزن الظاهري مع العنصر الحالي.");
    Console.WriteLine("Insert+Enter لتفعيل أو تعطيل وضع المراجعة النصية.");
    Console.WriteLine("Insert+Up لقراءة السطر الحالي.");
    Console.WriteLine("Insert+Down لقراءة متصلة من موضع المراجعة.");
    Console.WriteLine("Insert+Left/Right للكلمة السابقة أو اللاحقة.");
    Console.WriteLine("Insert+PageUp/PageDown للجملة السابقة أو اللاحقة.");
    Console.WriteLine("داخل وضع المراجعة: الأسهم للحرف والسطر وCtrl+الأسهم للكلمة والفقرة وHome/End لبداية ونهاية السطر.");
    Console.WriteLine("داخل المتصفح: H/K/E/B/X/D/T للتنقل بين العناوين والروابط والحقول والأزرار وخانات الاختيار والمعالم والجداول.");
    Console.WriteLine("Insert+Space داخل المتصفح للتبديل بين وضع التصفح ووضع التركيز. وEscape للرجوع إلى وضع التصفح.");
    Console.WriteLine("اضغط Ctrl+C للإيقاف.");

    var speechService = new SapiSpeechService();
    var inspectorSink = new CompositeInspectorSink(
        new JsonInspectorSink(),
        new LiveInspectorSink());
    using var runtime = new LuminaRuntime(
        new UiaAccessibilityService(),
        new SimpleLuaStyleScriptEngine(),
        speechService,
        inspectorSink);

    using var hotKeys = new KeyboardCommandManager(
        speakCurrentFocus: () =>
        {
            string text = FocusSnapshotReader.ReadCurrentFocusSummary();
            speechService.Enqueue(new SpeechRequest(text, 100, true));
        },
        repeatLastSpeech: speechService.RepeatLast,
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
        summarizeCurrentPage: () =>
        {
            string text = BrowserNavigator.SummarizeCurrentPage();
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
    Console.Error.WriteLine("Lumina failed to start.");
    Console.Error.WriteLine(exception);
    Environment.ExitCode = 1;
}
