using System.Runtime.InteropServices;

namespace Lumina.Input;

public sealed class GlobalHotKeyManager : IDisposable
{
    private const int ModAlt = 0x0001;
    private const int ModControl = 0x0002;
    private const int WmHotKey = 0x0312;
    private const uint VkF = 0x46;
    private const uint VkL = 0x4C;
    private const uint VkI = 0x49;

    private readonly Action _speakCurrentFocus;
    private readonly Action _repeatLastSpeech;
    private readonly Action _toggleInspector;
    private Thread? _messageLoopThread;
    private volatile bool _running;
    private uint _threadId;

    public GlobalHotKeyManager(
        Action speakCurrentFocus,
        Action repeatLastSpeech,
        Action toggleInspector)
    {
        _speakCurrentFocus = speakCurrentFocus;
        _repeatLastSpeech = repeatLastSpeech;
        _toggleInspector = toggleInspector;
    }

    public void Start()
    {
        if (_running)
        {
            return;
        }

        _running = true;
        _messageLoopThread = new Thread(MessageLoop)
        {
            IsBackground = true,
            Name = "LuminaHotKeys"
        };
        _messageLoopThread.SetApartmentState(ApartmentState.STA);
        _messageLoopThread.Start();
    }

    public void Dispose()
    {
        _running = false;
        if (_threadId != 0)
        {
            PostThreadMessage(_threadId, 0x0012, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private void MessageLoop()
    {
        _threadId = GetCurrentThreadId();
        RegisterHotKey(IntPtr.Zero, 1, ModControl | ModAlt, VkF);
        RegisterHotKey(IntPtr.Zero, 2, ModControl | ModAlt, VkL);
        RegisterHotKey(IntPtr.Zero, 3, ModControl | ModAlt, VkI);

        try
        {
            while (_running && GetMessage(out Msg message, IntPtr.Zero, 0, 0) > 0)
            {
                if (message.message == WmHotKey)
                {
                    HandleHotKey(message.wParam.ToInt32());
                }
            }
        }
        finally
        {
            UnregisterHotKey(IntPtr.Zero, 1);
            UnregisterHotKey(IntPtr.Zero, 2);
            UnregisterHotKey(IntPtr.Zero, 3);
        }
    }

    private void HandleHotKey(int id)
    {
        switch (id)
        {
            case 1:
                _speakCurrentFocus();
                break;
            case 2:
                _repeatLastSpeech();
                break;
            case 3:
                _toggleInspector();
                break;
        }
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public Point pt;
        public uint lPrivate;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }
}
