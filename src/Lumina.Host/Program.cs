using Lumina.Accessibility.Windows;
using Lumina.Core.Models;
using Lumina.Core.Services;
using Lumina.Input;
using Lumina.Output.Inspection;
using Lumina.Scripting.Rules;
using Lumina.Speech;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("Lumina prototype started.");
Console.WriteLine("يتابع تغيّر التركيز focus في Windows وينطق العنصر الحالي.");
Console.WriteLine("يسجل Inspector الأحداث في inspector/focus-events.jsonl.");
Console.WriteLine("ويعرض نافذة Inspector حيّة لآخر الأحداث.");
Console.WriteLine("Insert+F لقراءة العنصر الحالي.");
Console.WriteLine("Insert+L لإعادة آخر رسالة منطوقة.");
Console.WriteLine("Insert+I لتبديل Inspector.");
Console.WriteLine("Insert+T لقراءة عنوان الصفحة الحالية في المتصفح.");
Console.WriteLine("Insert+W لقراءة ملخص ويب سريع للعنصر الحالي.");
Console.WriteLine("Insert+S لقراءة ملخص الصفحة الحالية.");
Console.WriteLine("Insert+R لتحديث المخزن الظاهري للصفحة.");
Console.WriteLine("Insert+B لملخص حالة المخزن الظاهري.");
Console.WriteLine("Insert+Y لمزامنة المخزن الظاهري مع العنصر الحالي.");
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
    toggleInspector: inspectorSink.Toggle,
    speakPageTitle: () =>
    {
        string text = FocusSnapshotReader.ReadCurrentPageTitle();
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
