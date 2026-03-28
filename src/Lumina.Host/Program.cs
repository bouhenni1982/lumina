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
    Console.WriteLine("داخل المتصفح: H/K/E للعنصر التالي وShift+H/K/E للعنصر السابق.");
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
