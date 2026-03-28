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
Console.WriteLine("Ctrl+Alt+F لقراءة العنصر الحالي.");
Console.WriteLine("Ctrl+Alt+L لإعادة آخر رسالة منطوقة.");
Console.WriteLine("Ctrl+Alt+I لتبديل Inspector.");
Console.WriteLine("Ctrl+Alt+T لقراءة عنوان الصفحة الحالية في المتصفح.");
Console.WriteLine("Ctrl+Alt+W لقراءة ملخص ويب سريع للعنصر الحالي.");
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

using var hotKeys = new GlobalHotKeyManager(
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
