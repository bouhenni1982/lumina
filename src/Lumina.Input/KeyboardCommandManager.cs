using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace Lumina.Input;

public sealed class KeyboardCommandManager : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const int WmQuit = 0x0012;

    private const uint VkInsert = 0x2D;
    private const uint VkShift = 0x10;
    private const uint VkControl = 0x11;
    private const uint VkMenu = 0x12;
    private const uint VkLWin = 0x5B;
    private const uint VkRWin = 0x5C;

    private const uint VkF = 0x46;
    private const uint VkL = 0x4C;
    private const uint VkI = 0x49;
    private const uint VkT = 0x54;
    private const uint VkW = 0x57;
    private const uint VkS = 0x53;
    private const uint VkR = 0x52;
    private const uint VkB = 0x42;
    private const uint VkY = 0x59;
    private const uint VkUp = 0x26;
    private const uint VkDown = 0x28;
    private const uint VkLeft = 0x25;
    private const uint VkRight = 0x27;

    private const uint VkH = 0x48;
    private const uint VkK = 0x4B;
    private const uint VkE = 0x45;

    private readonly Action _speakCurrentFocus;
    private readonly Action _repeatLastSpeech;
    private readonly Action _toggleInspector;
    private readonly Action _speakPageTitle;
    private readonly Action _speakWebSummary;
    private readonly Action _moveToNextHeading;
    private readonly Action _moveToPreviousHeading;
    private readonly Action _moveToNextLink;
    private readonly Action _moveToPreviousLink;
    private readonly Action _moveToNextEditField;
    private readonly Action _moveToPreviousEditField;
    private readonly Action _summarizeCurrentPage;
    private readonly Action _refreshVirtualBuffer;
    private readonly Action _summarizeVirtualBuffer;
    private readonly Action _syncVirtualBufferToFocus;
    private readonly Action _readPreviousLine;
    private readonly Action _readNextLine;
    private readonly Action _readPreviousCharacter;
    private readonly Action _readNextCharacter;

    private readonly HookProc _hookProc;
    private Thread? _messageLoopThread;
    private IntPtr _hookHandle;
    private uint _threadId;
    private volatile bool _running;
    private bool _insertDown;

    public KeyboardCommandManager(
        Action speakCurrentFocus,
        Action repeatLastSpeech,
        Action toggleInspector,
        Action speakPageTitle,
        Action speakWebSummary,
        Action moveToNextHeading,
        Action moveToPreviousHeading,
        Action moveToNextLink,
        Action moveToPreviousLink,
        Action moveToNextEditField,
        Action moveToPreviousEditField,
        Action summarizeCurrentPage,
        Action refreshVirtualBuffer,
        Action summarizeVirtualBuffer,
        Action syncVirtualBufferToFocus,
        Action readPreviousLine,
        Action readNextLine,
        Action readPreviousCharacter,
        Action readNextCharacter)
    {
        _speakCurrentFocus = speakCurrentFocus;
        _repeatLastSpeech = repeatLastSpeech;
        _toggleInspector = toggleInspector;
        _speakPageTitle = speakPageTitle;
        _speakWebSummary = speakWebSummary;
        _moveToNextHeading = moveToNextHeading;
        _moveToPreviousHeading = moveToPreviousHeading;
        _moveToNextLink = moveToNextLink;
        _moveToPreviousLink = moveToPreviousLink;
        _moveToNextEditField = moveToNextEditField;
        _moveToPreviousEditField = moveToPreviousEditField;
        _summarizeCurrentPage = summarizeCurrentPage;
        _refreshVirtualBuffer = refreshVirtualBuffer;
        _summarizeVirtualBuffer = summarizeVirtualBuffer;
        _syncVirtualBufferToFocus = syncVirtualBufferToFocus;
        _readPreviousLine = readPreviousLine;
        _readNextLine = readNextLine;
        _readPreviousCharacter = readPreviousCharacter;
        _readNextCharacter = readNextCharacter;
        _hookProc = HookCallback;
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
            Name = "LuminaKeyboardHook"
        };
        _messageLoopThread.SetApartmentState(ApartmentState.STA);
        _messageLoopThread.Start();
    }

    public void Dispose()
    {
        _running = false;
        if (_threadId != 0)
        {
            PostThreadMessage(_threadId, WmQuit, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private void MessageLoop()
    {
        _threadId = GetCurrentThreadId();
        _hookHandle = SetWindowsHookEx(WhKeyboardLl, _hookProc, GetModuleHandle(null), 0);

        try
        {
            while (_running && GetMessage(out Msg _, IntPtr.Zero, 0, 0) > 0)
            {
            }
        }
        finally
        {
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        int message = wParam.ToInt32();
        bool isKeyDown = message is WmKeyDown or WmSysKeyDown;
        bool isKeyUp = message is WmKeyUp or WmSysKeyUp;
        KbdLlHookStruct keyInfo = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
        uint vkCode = keyInfo.vkCode;

        if (vkCode == VkInsert)
        {
            if (isKeyDown)
            {
                _insertDown = true;
            }
            else if (isKeyUp)
            {
                _insertDown = false;
            }

            return (IntPtr)1;
        }

        if (!isKeyDown)
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        bool shiftDown = IsKeyCurrentlyDown(VkShift);
        bool controlDown = IsKeyCurrentlyDown(VkControl);
        bool altDown = IsKeyCurrentlyDown(VkMenu);
        bool winDown = IsKeyCurrentlyDown(VkLWin) || IsKeyCurrentlyDown(VkRWin);

        if (_insertDown && !controlDown && !altDown && !winDown)
        {
            if (TryHandleScreenReaderCommand(vkCode))
            {
                return (IntPtr)1;
            }
        }

        if (!_insertDown &&
            !controlDown &&
            !altDown &&
            !winDown &&
            IsBrowserNavigationContext())
        {
            if (TryHandleBrowserNavigation(vkCode, shiftDown))
            {
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private bool TryHandleScreenReaderCommand(uint vkCode)
    {
        Action? action = vkCode switch
        {
            VkF => _speakCurrentFocus,
            VkL => _repeatLastSpeech,
            VkI => _toggleInspector,
            VkT => _speakPageTitle,
            VkW => _speakWebSummary,
            VkS => _summarizeCurrentPage,
            VkR => _refreshVirtualBuffer,
            VkB => _summarizeVirtualBuffer,
            VkY => _syncVirtualBufferToFocus,
            VkUp => _readPreviousLine,
            VkDown => _readNextLine,
            VkLeft => _readPreviousCharacter,
            VkRight => _readNextCharacter,
            _ => null
        };

        if (action is null)
        {
            return false;
        }

        ThreadPool.QueueUserWorkItem(_ => action());
        return true;
    }

    private bool TryHandleBrowserNavigation(uint vkCode, bool shiftDown)
    {
        Action? action = vkCode switch
        {
            VkH when shiftDown => _moveToPreviousHeading,
            VkH => _moveToNextHeading,
            VkK when shiftDown => _moveToPreviousLink,
            VkK => _moveToNextLink,
            VkE when shiftDown => _moveToPreviousEditField,
            VkE => _moveToNextEditField,
            _ => null
        };

        if (action is null)
        {
            return false;
        }

        ThreadPool.QueueUserWorkItem(_ => action());
        return true;
    }

    private static bool IsBrowserNavigationContext()
    {
        AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
        if (focused is null || !FocusSnapshotReader.IsBrowserContext(focused))
        {
            return false;
        }

        return FocusSnapshotReader.ResolveWebSemanticRole(focused) != "web_edit";
    }

    private static bool IsKeyCurrentlyDown(uint virtualKey) => (GetAsyncKeyState((int)virtualKey) & 0x8000) != 0;

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

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

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
