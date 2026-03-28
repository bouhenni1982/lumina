using System.ComponentModel;
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
    private const uint LlkhfExtended = 0x01;

    private const uint VkInsert = 0x2D;
    private const uint VkShift = 0x10;
    private const uint VkControl = 0x11;
    private const uint VkMenu = 0x12;
    private const uint VkLWin = 0x5B;
    private const uint VkRWin = 0x5C;

    private const uint VkF = 0x46;
    private const uint VkG = 0x47;
    private const uint VkC = 0x43;
    private const uint VkA = 0x41;
    private const uint VkJ = 0x4A;
    private const uint VkL = 0x4C;
    private const uint VkI = 0x49;
    private const uint VkM = 0x4D;
    private const uint VkN = 0x4E;
    private const uint VkO = 0x4F;
    private const uint VkP = 0x50;
    private const uint VkQ = 0x51;
    private const uint VkU = 0x55;
    private const uint VkD = 0x44;
    private const uint VkX = 0x58;
    private const uint VkZ = 0x5A;
    private const uint VkReturn = 0x0D;
    private const uint VkTab = 0x09;
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
    private const uint VkHome = 0x24;
    private const uint VkEnd = 0x23;
    private const uint VkPageUp = 0x21;
    private const uint VkPageDown = 0x22;
    private const uint VkSpace = 0x20;
    private const uint VkEscape = 0x1B;

    private const uint VkH = 0x48;
    private const uint VkK = 0x4B;
    private const uint VkE = 0x45;
    private const uint VkLBrowser = 0x4C;

    private readonly Action _speakCurrentFocus;
    private readonly Action _repeatLastSpeech;
    private readonly Action _speakCurrentElementDetails;
    private readonly Action _speakCurrentElementAdvancedDetails;
    private readonly Action _toggleInspector;
    private readonly Action _speakPageTitle;
    private readonly Action _speakCurrentWindowSummary;
    private readonly Action _speakCurrentStatusSummary;
    private readonly Action _speakWebSummary;
    private readonly Action _speakMenuContext;
    private readonly Action _speakSettingsContext;
    private readonly Action _moveToNextContextItem;
    private readonly Action _moveToPreviousContextItem;
    private readonly Action _moveToNextSettingsCheckbox;
    private readonly Action _moveToPreviousSettingsCheckbox;
    private readonly Action _moveToNextSettingsButton;
    private readonly Action _moveToPreviousSettingsButton;
    private readonly Action _moveToNextSettingsComboBox;
    private readonly Action _moveToPreviousSettingsComboBox;
    private readonly Action _moveToNextSettingsRadioButton;
    private readonly Action _moveToPreviousSettingsRadioButton;
    private readonly Action _moveToNextSettingsTab;
    private readonly Action _moveToPreviousSettingsTab;
    private readonly Action _moveToNextSettingsSlider;
    private readonly Action _moveToPreviousSettingsSlider;
    private readonly Action _moveToNextSettingsText;
    private readonly Action _moveToPreviousSettingsText;
    private readonly Action _moveToNextSettingsGroup;
    private readonly Action _moveToPreviousSettingsGroup;
    private readonly Action _readEditorStatusSummary;
    private readonly Action _moveToNextHeading;
    private readonly Action _moveToPreviousHeading;
    private readonly Action _moveToNextLink;
    private readonly Action _moveToPreviousLink;
    private readonly Action _moveToNextEditField;
    private readonly Action _moveToPreviousEditField;
    private readonly Action _moveToNextButton;
    private readonly Action _moveToPreviousButton;
    private readonly Action _moveToNextCheckbox;
    private readonly Action _moveToPreviousCheckbox;
    private readonly Action _moveToNextLandmark;
    private readonly Action _moveToPreviousLandmark;
    private readonly Action _moveToNextTable;
    private readonly Action _moveToPreviousTable;
    private readonly Action _moveToNextList;
    private readonly Action _moveToPreviousList;
    private readonly Action _moveToNextDialog;
    private readonly Action _moveToPreviousDialog;
    private readonly Action _moveToNextFormField;
    private readonly Action _moveToPreviousFormField;
    private readonly Action _readCurrentTableContext;
    private readonly Action _moveToNextTableCell;
    private readonly Action _moveToPreviousTableCell;
    private readonly Action _moveToTableCellBelow;
    private readonly Action _moveToTableCellAbove;
    private readonly Action _summarizeCurrentPage;
    private readonly Action _refreshVirtualBuffer;
    private readonly Action _summarizeVirtualBuffer;
    private readonly Action _syncVirtualBufferToFocus;
    private readonly Action<bool> _announceTextReviewMode;
    private readonly Action _readCurrentLine;
    private readonly Action _readPreviousLine;
    private readonly Action _readNextLine;
    private readonly Action _readPreviousCharacter;
    private readonly Action _readNextCharacter;
    private readonly Action _readPreviousWord;
    private readonly Action _readNextWord;
    private readonly Action _readPreviousParagraph;
    private readonly Action _readNextParagraph;
    private readonly Action _readPreviousSentence;
    private readonly Action _readNextSentence;
    private readonly Action _moveToStartOfLine;
    private readonly Action _moveToEndOfLine;
    private readonly Action _sayAllFromReviewCursor;
    private readonly Action<string> _speakBrowserMessage;

    private readonly HookProc _hookProc;
    private Thread? _messageLoopThread;
    private IntPtr _hookHandle;
    private uint _threadId;
    private volatile bool _running;
    private bool _insertDown;
    private bool _textReviewMode;
    private bool _browserBrowseMode = true;
    private DateTime _lastElementDetailsCommandUtc = DateTime.MinValue;

    private static readonly TimeSpan AdvancedDetailsRepeatWindow = TimeSpan.FromMilliseconds(900);

    public KeyboardCommandManager(
        Action speakCurrentFocus,
        Action repeatLastSpeech,
        Action speakCurrentElementDetails,
        Action speakCurrentElementAdvancedDetails,
        Action toggleInspector,
        Action speakPageTitle,
        Action speakCurrentWindowSummary,
        Action speakCurrentStatusSummary,
        Action speakWebSummary,
        Action speakMenuContext,
        Action speakSettingsContext,
        Action moveToNextContextItem,
        Action moveToPreviousContextItem,
        Action moveToNextSettingsCheckbox,
        Action moveToPreviousSettingsCheckbox,
        Action moveToNextSettingsButton,
        Action moveToPreviousSettingsButton,
        Action moveToNextSettingsComboBox,
        Action moveToPreviousSettingsComboBox,
        Action moveToNextSettingsRadioButton,
        Action moveToPreviousSettingsRadioButton,
        Action moveToNextSettingsTab,
        Action moveToPreviousSettingsTab,
        Action moveToNextSettingsSlider,
        Action moveToPreviousSettingsSlider,
        Action moveToNextSettingsText,
        Action moveToPreviousSettingsText,
        Action moveToNextSettingsGroup,
        Action moveToPreviousSettingsGroup,
        Action readEditorStatusSummary,
        Action moveToNextHeading,
        Action moveToPreviousHeading,
        Action moveToNextLink,
        Action moveToPreviousLink,
        Action moveToNextEditField,
        Action moveToPreviousEditField,
        Action moveToNextButton,
        Action moveToPreviousButton,
        Action moveToNextCheckbox,
        Action moveToPreviousCheckbox,
        Action moveToNextLandmark,
        Action moveToPreviousLandmark,
        Action moveToNextTable,
        Action moveToPreviousTable,
        Action moveToNextList,
        Action moveToPreviousList,
        Action moveToNextDialog,
        Action moveToPreviousDialog,
        Action moveToNextFormField,
        Action moveToPreviousFormField,
        Action readCurrentTableContext,
        Action moveToNextTableCell,
        Action moveToPreviousTableCell,
        Action moveToTableCellBelow,
        Action moveToTableCellAbove,
        Action summarizeCurrentPage,
        Action refreshVirtualBuffer,
        Action summarizeVirtualBuffer,
        Action syncVirtualBufferToFocus,
        Action<bool> announceTextReviewMode,
        Action readCurrentLine,
        Action readPreviousLine,
        Action readNextLine,
        Action readPreviousCharacter,
        Action readNextCharacter,
        Action readPreviousWord,
        Action readNextWord,
        Action readPreviousParagraph,
        Action readNextParagraph,
        Action readPreviousSentence,
        Action readNextSentence,
        Action moveToStartOfLine,
        Action moveToEndOfLine,
        Action sayAllFromReviewCursor,
        Action<string> speakBrowserMessage)
    {
        _speakCurrentFocus = speakCurrentFocus;
        _repeatLastSpeech = repeatLastSpeech;
        _speakCurrentElementDetails = speakCurrentElementDetails;
        _speakCurrentElementAdvancedDetails = speakCurrentElementAdvancedDetails;
        _toggleInspector = toggleInspector;
        _speakPageTitle = speakPageTitle;
        _speakCurrentWindowSummary = speakCurrentWindowSummary;
        _speakCurrentStatusSummary = speakCurrentStatusSummary;
        _speakWebSummary = speakWebSummary;
        _speakMenuContext = speakMenuContext;
        _speakSettingsContext = speakSettingsContext;
        _moveToNextContextItem = moveToNextContextItem;
        _moveToPreviousContextItem = moveToPreviousContextItem;
        _moveToNextSettingsCheckbox = moveToNextSettingsCheckbox;
        _moveToPreviousSettingsCheckbox = moveToPreviousSettingsCheckbox;
        _moveToNextSettingsButton = moveToNextSettingsButton;
        _moveToPreviousSettingsButton = moveToPreviousSettingsButton;
        _moveToNextSettingsComboBox = moveToNextSettingsComboBox;
        _moveToPreviousSettingsComboBox = moveToPreviousSettingsComboBox;
        _moveToNextSettingsRadioButton = moveToNextSettingsRadioButton;
        _moveToPreviousSettingsRadioButton = moveToPreviousSettingsRadioButton;
        _moveToNextSettingsTab = moveToNextSettingsTab;
        _moveToPreviousSettingsTab = moveToPreviousSettingsTab;
        _moveToNextSettingsSlider = moveToNextSettingsSlider;
        _moveToPreviousSettingsSlider = moveToPreviousSettingsSlider;
        _moveToNextSettingsText = moveToNextSettingsText;
        _moveToPreviousSettingsText = moveToPreviousSettingsText;
        _moveToNextSettingsGroup = moveToNextSettingsGroup;
        _moveToPreviousSettingsGroup = moveToPreviousSettingsGroup;
        _readEditorStatusSummary = readEditorStatusSummary;
        _moveToNextHeading = moveToNextHeading;
        _moveToPreviousHeading = moveToPreviousHeading;
        _moveToNextLink = moveToNextLink;
        _moveToPreviousLink = moveToPreviousLink;
        _moveToNextEditField = moveToNextEditField;
        _moveToPreviousEditField = moveToPreviousEditField;
        _moveToNextButton = moveToNextButton;
        _moveToPreviousButton = moveToPreviousButton;
        _moveToNextCheckbox = moveToNextCheckbox;
        _moveToPreviousCheckbox = moveToPreviousCheckbox;
        _moveToNextLandmark = moveToNextLandmark;
        _moveToPreviousLandmark = moveToPreviousLandmark;
        _moveToNextTable = moveToNextTable;
        _moveToPreviousTable = moveToPreviousTable;
        _moveToNextList = moveToNextList;
        _moveToPreviousList = moveToPreviousList;
        _moveToNextDialog = moveToNextDialog;
        _moveToPreviousDialog = moveToPreviousDialog;
        _moveToNextFormField = moveToNextFormField;
        _moveToPreviousFormField = moveToPreviousFormField;
        _readCurrentTableContext = readCurrentTableContext;
        _moveToNextTableCell = moveToNextTableCell;
        _moveToPreviousTableCell = moveToPreviousTableCell;
        _moveToTableCellBelow = moveToTableCellBelow;
        _moveToTableCellAbove = moveToTableCellAbove;
        _summarizeCurrentPage = summarizeCurrentPage;
        _refreshVirtualBuffer = refreshVirtualBuffer;
        _summarizeVirtualBuffer = summarizeVirtualBuffer;
        _syncVirtualBufferToFocus = syncVirtualBufferToFocus;
        _announceTextReviewMode = announceTextReviewMode;
        _readCurrentLine = readCurrentLine;
        _readPreviousLine = readPreviousLine;
        _readNextLine = readNextLine;
        _readPreviousCharacter = readPreviousCharacter;
        _readNextCharacter = readNextCharacter;
        _readPreviousWord = readPreviousWord;
        _readNextWord = readNextWord;
        _readPreviousParagraph = readPreviousParagraph;
        _readNextParagraph = readNextParagraph;
        _readPreviousSentence = readPreviousSentence;
        _readNextSentence = readNextSentence;
        _moveToStartOfLine = moveToStartOfLine;
        _moveToEndOfLine = moveToEndOfLine;
        _sayAllFromReviewCursor = sayAllFromReviewCursor;
        _speakBrowserMessage = speakBrowserMessage;
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
        if (_hookHandle == IntPtr.Zero)
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Failed to install the global keyboard hook.");
        }

        try
        {
            while (_running)
            {
                int messageResult = GetMessage(out Msg _, IntPtr.Zero, 0, 0);
                if (messageResult == -1)
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error(),
                        "The keyboard hook message loop failed.");
                }

                if (messageResult == 0)
                {
                    break;
                }
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
        bool isExtendedNavigationKey = (keyInfo.flags & LlkhfExtended) != 0;

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
            if (vkCode == VkSpace && IsBrowserContext())
            {
                _browserBrowseMode = !_browserBrowseMode;
                string modeText = _browserBrowseMode
                    ? "تم تفعيل وضع التصفح."
                    : "تم تفعيل وضع التركيز.";
                ThreadPool.QueueUserWorkItem(_ => _speakBrowserMessage(modeText));
                return (IntPtr)1;
            }

            if (vkCode == VkReturn)
            {
                _textReviewMode = !_textReviewMode;
                bool enabled = _textReviewMode;
                ThreadPool.QueueUserWorkItem(_ => _announceTextReviewMode(enabled));
                return (IntPtr)1;
            }

            if (TryHandleScreenReaderCommand(vkCode, shiftDown))
            {
                return (IntPtr)1;
            }
        }

        if (!_insertDown &&
            !altDown &&
            !winDown &&
            !_textReviewMode &&
            IsBrowserArrowReadingContext(vkCode, isExtendedNavigationKey))
        {
            if (TryHandleBrowserArrowReading(vkCode, controlDown))
            {
                return (IntPtr)1;
            }
        }

        if (!_insertDown &&
            !controlDown &&
            !altDown &&
            !winDown &&
            !_textReviewMode &&
            IsBrowserNavigationContext())
        {
            if (TryHandleBrowserNavigation(vkCode, shiftDown))
            {
                return (IntPtr)1;
            }
        }

        if (!_insertDown &&
            !controlDown &&
            !altDown &&
            !winDown &&
            vkCode == VkEscape &&
            IsBrowserContext() &&
            !_browserBrowseMode)
        {
            _browserBrowseMode = true;
            ThreadPool.QueueUserWorkItem(_ => _speakBrowserMessage("تم الرجوع إلى وضع التصفح."));
            return (IntPtr)1;
        }

        if (!_insertDown &&
            !altDown &&
            !winDown &&
            _textReviewMode)
        {
            if (TryHandleTextReview(vkCode, controlDown))
            {
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private bool TryHandleScreenReaderCommand(uint vkCode, bool shiftDown)
    {
        if (vkCode == VkTab)
        {
            Action detailsAction = ResolveElementDetailsAction();
            ThreadPool.QueueUserWorkItem(_ => detailsAction());
            return true;
        }

        Action? action = vkCode switch
        {
            VkF => _speakCurrentFocus,
            VkL => _repeatLastSpeech,
            VkI => _toggleInspector,
            VkT => _speakPageTitle,
            VkHome => _speakCurrentWindowSummary,
            VkEnd => _speakCurrentStatusSummary,
            VkW => _speakWebSummary,
            VkM => _speakMenuContext,
            VkG => _speakSettingsContext,
            VkN => _moveToNextContextItem,
            VkP => _moveToPreviousContextItem,
            VkQ => _readEditorStatusSummary,
            VkT when shiftDown => _moveToPreviousTableCell,
            VkT => _readCurrentTableContext,
            VkC when shiftDown => _moveToPreviousSettingsCheckbox,
            VkC => _moveToNextSettingsCheckbox,
            VkA when shiftDown => _moveToPreviousSettingsButton,
            VkA => _moveToNextSettingsButton,
            VkO when shiftDown => _moveToPreviousSettingsComboBox,
            VkO => _moveToNextSettingsComboBox,
            VkU when shiftDown => _moveToPreviousSettingsRadioButton,
            VkU => _moveToNextSettingsRadioButton,
            VkD when shiftDown => _moveToPreviousSettingsTab,
            VkD => _moveToNextSettingsTab,
            VkZ when shiftDown => _moveToPreviousSettingsSlider,
            VkZ => _moveToNextSettingsSlider,
            VkX when shiftDown => _moveToPreviousSettingsText,
            VkX => _moveToNextSettingsText,
            VkJ when shiftDown => _moveToPreviousSettingsGroup,
            VkJ => _moveToNextSettingsGroup,
            VkS => _summarizeCurrentPage,
            VkR => _refreshVirtualBuffer,
            VkB => _summarizeVirtualBuffer,
            VkY => _syncVirtualBufferToFocus,
            VkUp => _readCurrentLine,
            VkDown => _sayAllFromReviewCursor,
            VkLeft => _readPreviousWord,
            VkRight => _readNextWord,
            VkPageUp => _readPreviousSentence,
            VkPageDown => _readNextSentence,
            _ => null
        };

        if (action is null)
        {
            return false;
        }

        ThreadPool.QueueUserWorkItem(_ => action());
        return true;
    }

    private Action ResolveElementDetailsAction()
    {
        DateTime now = DateTime.UtcNow;
        bool useAdvancedDetails = now - _lastElementDetailsCommandUtc <= AdvancedDetailsRepeatWindow;
        _lastElementDetailsCommandUtc = now;
        return useAdvancedDetails ? _speakCurrentElementAdvancedDetails : _speakCurrentElementDetails;
    }

    private bool TryHandleTextReview(uint vkCode, bool controlDown)
    {
        Action? action = (vkCode, controlDown) switch
        {
            (VkLeft, false) => _readPreviousCharacter,
            (VkRight, false) => _readNextCharacter,
            (VkUp, false) => _readPreviousLine,
            (VkDown, false) => _readNextLine,
            (VkHome, false) => _moveToStartOfLine,
            (VkEnd, false) => _moveToEndOfLine,
            (VkLeft, true) => _readPreviousWord,
            (VkRight, true) => _readNextWord,
            (VkUp, true) => _readPreviousParagraph,
            (VkDown, true) => _readNextParagraph,
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
            VkB when shiftDown => _moveToPreviousButton,
            VkB => _moveToNextButton,
            VkX when shiftDown => _moveToPreviousCheckbox,
            VkX => _moveToNextCheckbox,
            VkD when shiftDown => _moveToPreviousLandmark,
            VkD => _moveToNextLandmark,
            VkT when shiftDown => _moveToPreviousTable,
            VkT => _moveToNextTable,
            VkLBrowser when shiftDown => _moveToPreviousList,
            VkLBrowser => _moveToNextList,
            VkA when shiftDown => _moveToPreviousDialog,
            VkA => _moveToNextDialog,
            VkF when shiftDown => _moveToPreviousFormField,
            VkF => _moveToNextFormField,
            VkLeft when controlDown => _moveToPreviousTableCell,
            VkRight when controlDown => _moveToNextTableCell,
            VkUp when controlDown => _moveToTableCellAbove,
            VkDown when controlDown => _moveToTableCellBelow,
            _ => null
        };

        if (action is null)
        {
            return false;
        }

        ThreadPool.QueueUserWorkItem(_ => action());
        return true;
    }

    private bool TryHandleBrowserArrowReading(uint vkCode, bool controlDown)
    {
        string? text = (vkCode, controlDown) switch
        {
            (VkUp, false) => BrowserVirtualBuffer.ReadPreviousLine(),
            (VkDown, false) => BrowserVirtualBuffer.ReadNextLine(),
            (VkLeft, false) => BrowserVirtualBuffer.ReadPreviousCharacter(),
            (VkRight, false) => BrowserVirtualBuffer.ReadNextCharacter(),
            (VkLeft, true) => BrowserVirtualBuffer.ReadPreviousWord(),
            (VkRight, true) => BrowserVirtualBuffer.ReadNextWord(),
            _ => null
        };

        if (text is null)
        {
            return false;
        }

        ThreadPool.QueueUserWorkItem(_ => _speakBrowserMessage(text));
        return true;
    }

    private bool IsBrowserNavigationContext()
    {
        if (!_browserBrowseMode)
        {
            return false;
        }

        AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
        if (focused is null || !FocusSnapshotReader.IsBrowserContext(focused))
        {
            return false;
        }

        return FocusSnapshotReader.ResolveWebSemanticRole(focused) != "web_edit";
    }

    private bool IsBrowserContext()
    {
        AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
        if (focused is null)
        {
            return false;
        }

        return FocusSnapshotReader.IsBrowserContext(focused);
    }

    private bool IsBrowserArrowReadingContext(uint vkCode, bool isExtendedNavigationKey)
    {
        if (!_browserBrowseMode || !isExtendedNavigationKey || !IsBrowserContext())
        {
            return false;
        }

        return vkCode is VkUp or VkDown or VkLeft or VkRight;
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
