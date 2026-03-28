using Lumina.Accessibility.Windows;
using Lumina.Core.Services;
using Lumina.Output.Inspection;
using Lumina.Scripting.Rules;
using Lumina.Speech;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("Lumina prototype started.");
Console.WriteLine("يتابع تغيّر التركيز focus في Windows وينطق العنصر الحالي.");
Console.WriteLine("يسجل Inspector الأحداث في inspector/focus-events.jsonl.");
Console.WriteLine("اضغط Ctrl+C للإيقاف.");

using var runtime = new LuminaRuntime(
    new UiaAccessibilityService(),
    new SimpleLuaStyleScriptEngine(),
    new SapiSpeechService(),
    new JsonInspectorSink());

runtime.Start();

using ManualResetEvent shutdown = new(initialState: false);
Console.CancelKeyPress += (_, args) =>
{
    args.Cancel = true;
    shutdown.Set();
};

shutdown.WaitOne();
