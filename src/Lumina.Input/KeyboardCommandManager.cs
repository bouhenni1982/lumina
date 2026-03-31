using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using Lumina.Core.Services;

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
    private const uint VkRMenu = 0xA5;
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
    private const uint VkV = 0x56;
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
    private const uint VkF7 = 0x76;
    private const uint Vk1 = 0x31;
    private const uint Vk2 = 0x32;
    private const uint Vk3 = 0x33;
    private const uint Vk4 = 0x34;
    private const uint Vk5 = 0x35;
    private const uint Vk6 = 0x36;
    private const uint Vk7 = 0x37;
    private const uint Vk8 = 0x38;
    private const uint Vk9 = 0x39;
    private const uint VkComma = 0xBC;

    private const uint VkH = 0x48;
    private const uint VkK = 0x4B;
    private const uint VkE = 0x45;
    private const uint VkLBrowser = 0x4C;

    private readonly Action _speakCurrentFocus;
    private readonly Action _repeatLastSpeech;
    private readonly Action _speakLatestErrorSummary;
    private readonly Action _openLogDirectory;
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
    private readonly Action<int> _moveToNextHeadingLevel;
    private readonly Action<int> _moveToPreviousHeadingLevel;
    private readonly Action _moveToNextLink;
    private readonly Action _moveToPreviousLink;
    private readonly Action _moveToNextVisitedLink;
    private readonly Action _moveToPreviousVisitedLink;
    private readonly Action _moveToNextUnvisitedLink;
    private readonly Action _moveToPreviousUnvisitedLink;
    private readonly Action _moveToNextEditField;
    private readonly Action _moveToPreviousEditField;
    private readonly Action _moveToNextGraphic;
    private readonly Action _moveToPreviousGraphic;
    private readonly Action _moveToNextFrame;
    private readonly Action _moveToPreviousFrame;
    private readonly Action _moveToNextSeparator;
    private readonly Action _moveToPreviousSeparator;
    private readonly Action _moveToNextBlockQuote;
    private readonly Action _moveToPreviousBlockQuote;
    private readonly Action _moveToNextEmbeddedObject;
    private readonly Action _moveToPreviousEmbeddedObject;
    private readonly Action _moveToNextTextParagraph;
    private readonly Action _moveToPreviousTextParagraph;
    private readonly Action _moveToNextNotLinkBlock;
    private readonly Action _moveToPreviousNotLinkBlock;
    private readonly Action _moveToNextButton;
    private readonly Action _moveToPreviousButton;
    private readonly Action _moveToNextCheckbox;
    private readonly Action _moveToPreviousCheckbox;
    private readonly Action _moveToNextRadioButton;
    private readonly Action _moveToPreviousRadioButton;
    private readonly Action _moveToNextComboBox;
    private readonly Action _moveToPreviousComboBox;
    private readonly Action _moveToNextLandmark;
    private readonly Action _moveToPreviousLandmark;
    private readonly Action _moveToNextTable;
    private readonly Action _moveToPreviousTable;
    private readonly Action _moveToNextList;
    private readonly Action _moveToPreviousList;
    private readonly Action _moveToNextListItem;
    private readonly Action _moveToPreviousListItem;
    private readonly Action _moveToNextDialog;
    private readonly Action _moveToPreviousDialog;
    private readonly Action _moveToNextFormField;
    private readonly Action _moveToPreviousFormField;
    private readonly Action _moveToStartOfContainer;
    private readonly Action _movePastEndOfContainer;
    private readonly Action _moveToNextFocusableElement;
    private readonly Action _moveToPreviousFocusableElement;
    private readonly Action _activateCurrentBrowserElement;
    private readonly Action _readCurrentTableContext;
    private readonly Action _moveToNextTableCell;
    private readonly Action _moveToPreviousTableCell;
    private readonly Action _moveToTableCellBelow;
    private readonly Action _moveToTableCellAbove;
    private readonly Action _summarizeCurrentPage;
    private readonly Action _showBrowserElementsList;
    private readonly Action _refreshVirtualBuffer;
    private readonly Action _summarizeVirtualBuffer;
    private readonly Action _syncVirtualBufferToFocus;
    private readonly Action _speakLoggingStatus;
    private readonly Action _cycleLoggingVerbosity;
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
    private readonly SemaphoreSlim _browserCommandExecutionGate = new(1, 1);

    private readonly HookProc _hookProc;
    private Thread? _messageLoopThread;
    private IntPtr _hookHandle;
    private uint _threadId;
    private volatile bool _running;
    private bool _insertDown;
    private bool _textReviewMode;
    private bool _browserBrowseMode = true;
    private bool _browserManualFocusMode;
    private bool _browserSingleLetterNavigationEnabled = true;
    private bool _browserAutoFocusOnEdit;
    private bool _browserEditDirty;
    private DateTime _lastElementDetailsCommandUtc = DateTime.MinValue;
    private DateTime _lastAutoFocusModeAnnouncementUtc = DateTime.MinValue;
    private DateTime _lastBrowserSpeechUtc = DateTime.MinValue;
    private string _lastBrowserSpeechText = string.Empty;
    private string _lastBrowserSpeechFocusKey = string.Empty;
    private uint _lastBrowserSpeechKey;

    private static readonly TimeSpan AdvancedDetailsRepeatWindow = TimeSpan.FromMilliseconds(900);
    private static readonly TimeSpan AutoFocusModeAnnouncementWindow = TimeSpan.FromMilliseconds(1200);
    private static readonly TimeSpan BrowserDuplicateSpeechWindow = TimeSpan.FromMilliseconds(850);
    private static readonly HashSet<string> AlwaysPassThroughSemanticRoles = new(StringComparer.Ordinal)
    {
        "web_combobox",
        "web_edit",
        "web_radio",
        "web_tab",
        "web_menuitem",
        "web_treeitem"
    };
    private static readonly HashSet<string> AlwaysPassThroughRoles = new(StringComparer.Ordinal)
    {
        "combobox",
        "edit",
        "list",
        "listitem",
        "slider",
        "tab",
        "menu",
        "menubar",
        "tree",
        "treeitem",
        "spinner",
        "dataitem",
        "headeritem"
    };
    private static readonly HashSet<string> PassThroughOnFocusSemanticRoles = new(StringComparer.Ordinal)
    {
        "web_radio",
        "web_tab",
        "web_menuitem",
        "web_treeitem"
    };
    private static readonly HashSet<string> IgnoreEscapeToBrowseSemanticRoles = new(StringComparer.Ordinal)
    {
        "web_menuitem"
    };

    public KeyboardCommandManager(
        Action speakCurrentFocus,
        Action repeatLastSpeech,
        Action speakLatestErrorSummary,
        Action openLogDirectory,
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
        Action<int> moveToNextHeadingLevel,
        Action<int> moveToPreviousHeadingLevel,
        Action moveToNextLink,
        Action moveToPreviousLink,
        Action moveToNextVisitedLink,
        Action moveToPreviousVisitedLink,
        Action moveToNextUnvisitedLink,
        Action moveToPreviousUnvisitedLink,
        Action moveToNextEditField,
        Action moveToPreviousEditField,
        Action moveToNextGraphic,
        Action moveToPreviousGraphic,
        Action moveToNextFrame,
        Action moveToPreviousFrame,
        Action moveToNextSeparator,
        Action moveToPreviousSeparator,
        Action moveToNextBlockQuote,
        Action moveToPreviousBlockQuote,
        Action moveToNextEmbeddedObject,
        Action moveToPreviousEmbeddedObject,
        Action moveToNextTextParagraph,
        Action moveToPreviousTextParagraph,
        Action moveToNextNotLinkBlock,
        Action moveToPreviousNotLinkBlock,
        Action moveToNextButton,
        Action moveToPreviousButton,
        Action moveToNextCheckbox,
        Action moveToPreviousCheckbox,
        Action moveToNextRadioButton,
        Action moveToPreviousRadioButton,
        Action moveToNextComboBox,
        Action moveToPreviousComboBox,
        Action moveToNextLandmark,
        Action moveToPreviousLandmark,
        Action moveToNextTable,
        Action moveToPreviousTable,
        Action moveToNextList,
        Action moveToPreviousList,
        Action moveToNextListItem,
        Action moveToPreviousListItem,
        Action moveToNextDialog,
        Action moveToPreviousDialog,
        Action moveToNextFormField,
        Action moveToPreviousFormField,
        Action moveToStartOfContainer,
        Action movePastEndOfContainer,
        Action moveToNextFocusableElement,
        Action moveToPreviousFocusableElement,
        Action activateCurrentBrowserElement,
        Action readCurrentTableContext,
        Action moveToNextTableCell,
        Action moveToPreviousTableCell,
        Action moveToTableCellBelow,
        Action moveToTableCellAbove,
        Action summarizeCurrentPage,
        Action showBrowserElementsList,
        Action refreshVirtualBuffer,
        Action summarizeVirtualBuffer,
        Action syncVirtualBufferToFocus,
        Action speakLoggingStatus,
        Action cycleLoggingVerbosity,
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
        _speakLatestErrorSummary = speakLatestErrorSummary;
        _openLogDirectory = openLogDirectory;
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
        _moveToNextHeadingLevel = moveToNextHeadingLevel;
        _moveToPreviousHeadingLevel = moveToPreviousHeadingLevel;
        _moveToNextLink = moveToNextLink;
        _moveToPreviousLink = moveToPreviousLink;
        _moveToNextVisitedLink = moveToNextVisitedLink;
        _moveToPreviousVisitedLink = moveToPreviousVisitedLink;
        _moveToNextUnvisitedLink = moveToNextUnvisitedLink;
        _moveToPreviousUnvisitedLink = moveToPreviousUnvisitedLink;
        _moveToNextEditField = moveToNextEditField;
        _moveToPreviousEditField = moveToPreviousEditField;
        _moveToNextGraphic = moveToNextGraphic;
        _moveToPreviousGraphic = moveToPreviousGraphic;
        _moveToNextFrame = moveToNextFrame;
        _moveToPreviousFrame = moveToPreviousFrame;
        _moveToNextSeparator = moveToNextSeparator;
        _moveToPreviousSeparator = moveToPreviousSeparator;
        _moveToNextBlockQuote = moveToNextBlockQuote;
        _moveToPreviousBlockQuote = moveToPreviousBlockQuote;
        _moveToNextEmbeddedObject = moveToNextEmbeddedObject;
        _moveToPreviousEmbeddedObject = moveToPreviousEmbeddedObject;
        _moveToNextTextParagraph = moveToNextTextParagraph;
        _moveToPreviousTextParagraph = moveToPreviousTextParagraph;
        _moveToNextNotLinkBlock = moveToNextNotLinkBlock;
        _moveToPreviousNotLinkBlock = moveToPreviousNotLinkBlock;
        _moveToNextButton = moveToNextButton;
        _moveToPreviousButton = moveToPreviousButton;
        _moveToNextCheckbox = moveToNextCheckbox;
        _moveToPreviousCheckbox = moveToPreviousCheckbox;
        _moveToNextRadioButton = moveToNextRadioButton;
        _moveToPreviousRadioButton = moveToPreviousRadioButton;
        _moveToNextComboBox = moveToNextComboBox;
        _moveToPreviousComboBox = moveToPreviousComboBox;
        _moveToNextLandmark = moveToNextLandmark;
        _moveToPreviousLandmark = moveToPreviousLandmark;
        _moveToNextTable = moveToNextTable;
        _moveToPreviousTable = moveToPreviousTable;
        _moveToNextList = moveToNextList;
        _moveToPreviousList = moveToPreviousList;
        _moveToNextListItem = moveToNextListItem;
        _moveToPreviousListItem = moveToPreviousListItem;
        _moveToNextDialog = moveToNextDialog;
        _moveToPreviousDialog = moveToPreviousDialog;
        _moveToNextFormField = moveToNextFormField;
        _moveToPreviousFormField = moveToPreviousFormField;
        _moveToStartOfContainer = moveToStartOfContainer;
        _movePastEndOfContainer = movePastEndOfContainer;
        _moveToNextFocusableElement = moveToNextFocusableElement;
        _moveToPreviousFocusableElement = moveToPreviousFocusableElement;
        _activateCurrentBrowserElement = activateCurrentBrowserElement;
        _readCurrentTableContext = readCurrentTableContext;
        _moveToNextTableCell = moveToNextTableCell;
        _moveToPreviousTableCell = moveToPreviousTableCell;
        _moveToTableCellBelow = moveToTableCellBelow;
        _moveToTableCellAbove = moveToTableCellAbove;
        _summarizeCurrentPage = summarizeCurrentPage;
        _showBrowserElementsList = showBrowserElementsList;
        _refreshVirtualBuffer = refreshVirtualBuffer;
        _summarizeVirtualBuffer = summarizeVirtualBuffer;
        _syncVirtualBufferToFocus = syncVirtualBufferToFocus;
        _speakLoggingStatus = speakLoggingStatus;
        _cycleLoggingVerbosity = cycleLoggingVerbosity;
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
        bool altGrDown = IsKeyCurrentlyDown(VkRMenu);
        long browserKeyCapturedAt = 0;

        if (IsBrowserDiagnosticKey(vkCode))
        {
            browserKeyCapturedAt = Stopwatch.GetTimestamp();
            LogBrowserKeyCapture(vkCode, browserKeyCapturedAt, shiftDown, controlDown, altDown, winDown, altGrDown);
        }

        try
        {
            SyncBrowserModeToFocusedContext();
            if (browserKeyCapturedAt != 0)
            {
                BrowserFocusSnapshot snapshot = CaptureBrowserFocusSnapshot();
                LogBrowserContextSnapshot(vkCode, browserKeyCapturedAt, "بعد مزامنة سياق المتصفح", snapshot);
            }
        }
        catch (Exception exception)
        {
            if (browserKeyCapturedAt != 0)
            {
                LogBrowserCommandDecision(vkCode, "failed", $"فشل أثناء مزامنة سياق المتصفح: {exception.GetType().Name}: {exception.Message}", browserKeyCapturedAt);
            }

            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        bool shouldLeaveAutoFocusedEdit;
        BrowserFocusSnapshot currentFocusSnapshot = CaptureBrowserFocusSnapshot();
        try
        {
            shouldLeaveAutoFocusedEdit = ShouldLeaveAutoFocusedEdit(vkCode, controlDown, altDown, winDown, currentFocusSnapshot);
            if (browserKeyCapturedAt != 0)
            {
                LogBrowserContextSnapshot(vkCode, browserKeyCapturedAt, $"فحص الخروج من edit التلقائي: shouldLeaveAutoFocusedEdit={shouldLeaveAutoFocusedEdit}", currentFocusSnapshot);
            }
        }
        catch (Exception exception)
        {
            if (browserKeyCapturedAt != 0)
            {
                LogBrowserCommandDecision(vkCode, "failed", $"فشل أثناء فحص auto-focused edit: {exception.GetType().Name}: {exception.Message}", browserKeyCapturedAt);
            }

            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        if (shouldLeaveAutoFocusedEdit)
        {
            _browserBrowseMode = true;
            _browserAutoFocusOnEdit = false;
            _browserEditDirty = false;
            SyncOrRefreshBrowserBufferToFocus();
            currentFocusSnapshot = CaptureBrowserFocusSnapshot();

            if (TryHandleBrowserArrowReading(vkCode, controlDown, browserKeyCapturedAt))
            {
                return (IntPtr)1;
            }
        }

        bool shouldMarkBrowserEditDirty;
        try
        {
            shouldMarkBrowserEditDirty = ShouldMarkBrowserEditAsDirty(vkCode, controlDown, altDown, winDown, currentFocusSnapshot);
            if (browserKeyCapturedAt != 0)
            {
                LogBrowserContextSnapshot(vkCode, browserKeyCapturedAt, $"فحص editDirty: shouldMarkBrowserEditDirty={shouldMarkBrowserEditDirty}", currentFocusSnapshot);
            }
        }
        catch (Exception exception)
        {
            if (browserKeyCapturedAt != 0)
            {
                LogBrowserCommandDecision(vkCode, "failed", $"فشل أثناء فحص editDirty: {exception.GetType().Name}: {exception.Message}", browserKeyCapturedAt);
            }

            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        if (shouldMarkBrowserEditDirty)
        {
            _browserEditDirty = true;
        }

        if (_insertDown && !controlDown && !altDown && !winDown)
        {
            if (shiftDown && vkCode == VkSpace && IsBrowserContext())
            {
                _browserSingleLetterNavigationEnabled = !_browserSingleLetterNavigationEnabled;
                string singleLetterText = _browserSingleLetterNavigationEnabled
                    ? "تم تفعيل التنقل بالحروف المفردة."
                    : "تم تعطيل التنقل بالحروف المفردة.";
                ThreadPool.QueueUserWorkItem(_ => _speakBrowserMessage(singleLetterText));
                return (IntPtr)1;
            }

            if (vkCode == VkSpace && IsBrowserContext())
            {
                _browserBrowseMode = !_browserBrowseMode;
                _browserManualFocusMode = !_browserBrowseMode;
                _browserAutoFocusOnEdit = false;
                _browserEditDirty = false;
                if (_browserBrowseMode)
                {
                    SyncOrRefreshBrowserBufferToFocus();
                }

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
            !winDown &&
            !_textReviewMode &&
            IsBrowserTableNavigationContext(vkCode, controlDown, altDown, altGrDown, currentFocusSnapshot))
        {
            if (browserKeyCapturedAt != 0)
            {
                LogBrowserContextSnapshot(vkCode, browserKeyCapturedAt, "تم التعرف على سياق تنقل الجدول.", currentFocusSnapshot);
            }

            if (TryHandleBrowserTableNavigation(vkCode))
            {
                return (IntPtr)1;
            }
        }

        if (!_insertDown &&
            !altDown &&
            !winDown &&
            !_textReviewMode &&
            IsDirectionalReadingKey(vkCode))
        {
            bool canArrowRead = IsBrowserArrowReadingContext(vkCode, currentFocusSnapshot);
            if (browserKeyCapturedAt != 0)
            {
                LogBrowserContextSnapshot(vkCode, browserKeyCapturedAt, $"فحص قراءة الأسهم: canArrowRead={canArrowRead}", currentFocusSnapshot);
            }

            if (canArrowRead)
            {
                if (TryHandleBrowserArrowReading(vkCode, controlDown, browserKeyCapturedAt))
                {
                    return (IntPtr)1;
                }
            }
            else if (currentFocusSnapshot.Client.BrowserContext)
            {
                LogBrowserCommandDecision(vkCode, "ignored", "الأسهم ليست في سياق قراءة ويب صالح.", browserKeyCapturedAt);
            }
        }

        if (!_insertDown &&
            !controlDown &&
            !altDown &&
            !winDown &&
            !_textReviewMode &&
            _browserBrowseMode &&
            currentFocusSnapshot.Client.BrowserContext &&
            vkCode is VkReturn or VkSpace)
        {
            if (TryHandleBrowseModeActivationKey(vkCode))
            {
                return (IntPtr)1;
            }
        }

        if (!_insertDown &&
            !controlDown &&
            !altDown &&
            !winDown &&
            !_textReviewMode &&
            _browserBrowseMode &&
            currentFocusSnapshot.Client.BrowserContext &&
            vkCode == VkTab)
        {
            if (browserKeyCapturedAt != 0)
            {
                bool synced = BrowserVirtualBuffer.IsSyncedToFocusedElement();
                LogBrowserContextSnapshot(vkCode, browserKeyCapturedAt, $"فحص Tab في وضع التصفح: syncedToFocus={synced}", currentFocusSnapshot);
                if (synced)
                {
                    LogBrowserCommandDecision(vkCode, "execute", "Tab مع مزامنة لاحقة للتركيز.", browserKeyCapturedAt);
                }
                else
                {
                    LogBrowserCommandDecision(vkCode, "execute", "Tab مع تنقل تفاعلي من موضع المؤشر.", browserKeyCapturedAt);
                }
            }

            if (BrowserVirtualBuffer.IsSyncedToFocusedElement())
            {
                QueueBrowserFocusSyncAfterTab();
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                (shiftDown ? _moveToPreviousFocusableElement : _moveToNextFocusableElement)();
                TryAutoPassThroughForFocusedElement();
            });
            return (IntPtr)1;
        }

        if (!_insertDown &&
            !controlDown &&
            !altDown &&
            !winDown &&
            !_textReviewMode &&
            !_browserBrowseMode &&
            currentFocusSnapshot.Client.BrowserContext &&
            vkCode == VkTab)
        {
            if (browserKeyCapturedAt != 0)
            {
                LogBrowserCommandDecision(vkCode, "execute", "Tab في وضع التركيز مع مزامنة لاحقة للتركيز.", browserKeyCapturedAt);
            }

            QueueBrowserFocusSyncAfterTab();
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        if (!_insertDown &&
            !controlDown &&
            !altDown &&
            !winDown &&
            !_textReviewMode &&
            IsPotentialBrowserNavigationKey(vkCode))
        {
            bool canNavigate = IsBrowserNavigationContext(currentFocusSnapshot);
            if (browserKeyCapturedAt != 0)
            {
                LogBrowserContextSnapshot(vkCode, browserKeyCapturedAt, $"فحص التنقل بالحروف: canNavigate={canNavigate}", currentFocusSnapshot);
            }

            if (canNavigate)
            {
                if (TryHandleBrowserNavigation(vkCode, shiftDown, browserKeyCapturedAt))
                {
                    return (IntPtr)1;
                }
            }
            else if (currentFocusSnapshot.Client.BrowserContext)
            {
                LogBrowserCommandDecision(vkCode, "ignored", "الحرف أو الأمر السريع خارج سياق التصفح الحالي.", browserKeyCapturedAt);
            }
        }

        if (!_insertDown &&
            !controlDown &&
            !altDown &&
            !winDown &&
            vkCode == VkEscape &&
            currentFocusSnapshot.Client.BrowserContext &&
            !_browserBrowseMode)
        {
            AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
            if (focused is not null && ShouldPassEscapeThroughToElement(focused))
            {
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
            }

            _browserBrowseMode = true;
            _browserManualFocusMode = false;
            _browserAutoFocusOnEdit = false;
            _browserEditDirty = false;
            SyncOrRefreshBrowserBufferToFocus();
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

        if (browserKeyCapturedAt != 0)
        {
            LogBrowserCommandDecision(vkCode, "passthrough", "تم تمرير المفتاح بدون اعتراض.", browserKeyCapturedAt);
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

        Action? action = (vkCode, shiftDown) switch
        {
            (VkF, _) => _speakCurrentFocus,
            (VkL, _) => _repeatLastSpeech,
            (VkH, true) => _openLogDirectory,
            (VkH, false) => _speakLatestErrorSummary,
            (VkI, _) => _toggleInspector,
            (VkT, true) => _readCurrentTableContext,
            (VkT, false) => _speakPageTitle,
            (VkHome, _) => _speakCurrentWindowSummary,
            (VkEnd, _) => _speakCurrentStatusSummary,
            (VkW, _) => _speakWebSummary,
            (VkM, _) => _speakMenuContext,
            (VkG, _) => _speakSettingsContext,
            (VkN, _) => _moveToNextContextItem,
            (VkP, _) => _moveToPreviousContextItem,
            (VkQ, _) => _readEditorStatusSummary,
            (VkC, true) => _moveToPreviousSettingsCheckbox,
            (VkC, false) => _moveToNextSettingsCheckbox,
            (VkA, true) => _moveToPreviousSettingsButton,
            (VkA, false) => _moveToNextSettingsButton,
            (VkO, true) => _moveToPreviousSettingsComboBox,
            (VkO, false) => _moveToNextSettingsComboBox,
            (VkU, true) => _moveToPreviousSettingsRadioButton,
            (VkU, false) => _moveToNextSettingsRadioButton,
            (VkD, true) => _moveToPreviousSettingsTab,
            (VkD, false) => _moveToNextSettingsTab,
            (VkZ, true) => _moveToPreviousSettingsSlider,
            (VkZ, false) => _moveToNextSettingsSlider,
            (VkX, true) => _moveToPreviousSettingsText,
            (VkX, false) => _moveToNextSettingsText,
            (VkJ, true) => _moveToPreviousSettingsGroup,
            (VkJ, false) => _moveToNextSettingsGroup,
            (VkS, _) => _summarizeCurrentPage,
            (VkF7, _) => _showBrowserElementsList,
            (VkR, _) => _refreshVirtualBuffer,
            (VkB, _) => _summarizeVirtualBuffer,
            (VkV, true) => _cycleLoggingVerbosity,
            (VkV, false) => _speakLoggingStatus,
            (VkY, _) => _syncVirtualBufferToFocus,
            (VkUp, _) => _readCurrentLine,
            (VkDown, _) => _sayAllFromReviewCursor,
            (VkLeft, _) => _readPreviousWord,
            (VkRight, _) => _readNextWord,
            (VkPageUp, _) => _readPreviousSentence,
            (VkPageDown, _) => _readNextSentence,
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

    private bool TryHandleBrowserNavigation(uint vkCode, bool shiftDown, long capturedAt)
    {
        if (!_browserSingleLetterNavigationEnabled)
        {
            LogBrowserCommandDecision(vkCode, "ignored", "التنقل بالحروف المفردة معطل.", capturedAt);
            return false;
        }

        Action? action = vkCode switch
        {
            VkH when shiftDown => _moveToPreviousHeading,
            VkH => _moveToNextHeading,
            VkK when shiftDown => _moveToPreviousLink,
            VkK => _moveToNextLink,
            VkV when shiftDown => _moveToPreviousVisitedLink,
            VkV => _moveToNextVisitedLink,
            VkU when shiftDown => _moveToPreviousUnvisitedLink,
            VkU => _moveToNextUnvisitedLink,
            VkE when shiftDown => _moveToPreviousEditField,
            VkE => _moveToNextEditField,
            VkG when shiftDown => _moveToPreviousGraphic,
            VkG => _moveToNextGraphic,
            VkQ when shiftDown => _moveToPreviousBlockQuote,
            VkQ => _moveToNextBlockQuote,
            VkB when shiftDown => _moveToPreviousButton,
            VkB => _moveToNextButton,
            VkM when shiftDown => _moveToPreviousFrame,
            VkM => _moveToNextFrame,
            VkO when shiftDown => _moveToPreviousEmbeddedObject,
            VkO => _moveToNextEmbeddedObject,
            VkP when shiftDown => _moveToPreviousTextParagraph,
            VkP => _moveToNextTextParagraph,
            VkN when shiftDown => _moveToPreviousNotLinkBlock,
            VkN => _moveToNextNotLinkBlock,
            VkS when shiftDown => _moveToPreviousSeparator,
            VkS => _moveToNextSeparator,
            VkX when shiftDown => _moveToPreviousCheckbox,
            VkX => _moveToNextCheckbox,
            VkR when shiftDown => _moveToPreviousRadioButton,
            VkR => _moveToNextRadioButton,
            VkC when shiftDown => _moveToPreviousComboBox,
            VkC => _moveToNextComboBox,
            VkD when shiftDown => _moveToPreviousLandmark,
            VkD => _moveToNextLandmark,
            VkT when shiftDown => _moveToPreviousTable,
            VkT => _moveToNextTable,
            VkY when shiftDown => _moveToPreviousTable,
            VkY => _moveToNextTable,
            VkLBrowser when shiftDown => _moveToPreviousList,
            VkLBrowser => _moveToNextList,
            VkI when shiftDown => _moveToPreviousListItem,
            VkI => _moveToNextListItem,
            VkA when shiftDown => _moveToPreviousDialog,
            VkA => _moveToNextDialog,
            VkF when shiftDown => _moveToPreviousFormField,
            VkF => _moveToNextFormField,
            VkComma when shiftDown => _moveToStartOfContainer,
            VkComma => _movePastEndOfContainer,
            _ => null
        };

        if (action is null && TryResolveHeadingLevelNavigation(vkCode, shiftDown, out Action? headingAction))
        {
            action = headingAction;
        }

        if (action is null)
        {
            LogBrowserCommandDecision(vkCode, "ignored", "لا يوجد أمر تصفح مربوط لهذا المفتاح.", capturedAt);
            return false;
        }

        LogBrowserCommandDecision(vkCode, "execute", shiftDown ? "تنقل سابق." : "تنقل تال.", capturedAt);
        QueueSerializedBrowserWork(vkCode, capturedAt, shiftDown ? "تنقل سابق." : "تنقل تال.", () =>
        {
            action();
            TryAutoPassThroughForFocusedElement();
        });
        return true;
    }

    private bool TryResolveHeadingLevelNavigation(uint vkCode, bool shiftDown, out Action? action)
    {
        int level = vkCode switch
        {
            Vk1 => 1,
            Vk2 => 2,
            Vk3 => 3,
            Vk4 => 4,
            Vk5 => 5,
            Vk6 => 6,
            Vk7 => 7,
            Vk8 => 8,
            Vk9 => 9,
            _ => 0
        };

        if (level == 0)
        {
            action = null;
            return false;
        }

        action = () =>
        {
            if (shiftDown)
            {
                _moveToPreviousHeadingLevel(level);
            }
            else
            {
                _moveToNextHeadingLevel(level);
            }
        };
        return true;
    }

    private bool TryHandleBrowserArrowReading(uint vkCode, bool controlDown, long capturedAt)
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
            LogBrowserCommandDecision(vkCode, "ignored", controlDown ? "لا توجد قراءة ويب معرفة لهذا السهم مع Ctrl." : "لا توجد قراءة ويب معرفة لهذا السهم.", capturedAt);
            return false;
        }

        if (BrowserVirtualBuffer.IsCurrentEditField())
        {
            EnterAutoFocusOnEditField();
        }

        if (ShouldSuppressRepeatedBrowserSpeech(vkCode, text))
        {
            LogBrowserCommandDecision(vkCode, "ignored", "تم تجاهل قراءة مكررة لنفس العنصر خلال نافذة زمنية قصيرة.", capturedAt);
            return true;
        }

        LogBrowserCommandDecision(vkCode, "execute", controlDown ? "قراءة ويب مع Ctrl." : "قراءة ويب.", capturedAt);
        QueueSerializedBrowserWork(vkCode, capturedAt, controlDown ? "قراءة ويب مع Ctrl." : "قراءة ويب.", () => _speakBrowserMessage(text));
        return true;
    }

    private void QueueBrowserFocusSyncAfterTab()
    {
        QueueSerializedBrowserWork(() =>
        {
            try
            {
                Thread.Sleep(60);
            }
            catch
            {
            }

            string syncText = SyncOrRefreshBrowserBufferToFocus();

            TryAutoPassThroughForFocusedElement();
        });
    }

    private bool TryHandleBrowserTableNavigation(uint vkCode)
    {
        Action? action = vkCode switch
        {
            VkLeft => _moveToPreviousTableCell,
            VkRight => _moveToNextTableCell,
            VkUp => _moveToTableCellAbove,
            VkDown => _moveToTableCellBelow,
            _ => null
        };

        if (action is null)
        {
            LogBrowserCommandDecision(vkCode, "ignored", "لا توجد حركة جدول معرفة لهذا المفتاح.");
            return false;
        }

        LogBrowserCommandDecision(vkCode, "execute", "تنقل داخل جدول.");
        QueueSerializedBrowserWork(action);
        return true;
    }

    private bool IsBrowserNavigationContext(BrowserFocusSnapshot snapshot)
    {
        if (!_browserBrowseMode || !snapshot.Client.BrowserContext || !snapshot.Client.Exists || snapshot.Client.Element is null)
        {
            return false;
        }

        if (snapshot.Client.SemanticRole != "web_edit")
        {
            return true;
        }

        return snapshot.Client.IsDocumentRole;
    }

    private bool IsBrowserContext() => CaptureBrowserFocusSnapshot().Client.BrowserContext;

    private bool IsBrowserTableNavigationContext(
        uint vkCode,
        bool controlDown,
        bool altDown,
        bool altGrDown,
        BrowserFocusSnapshot snapshot)
    {
        if (!_browserBrowseMode ||
            !altDown ||
            !controlDown ||
            !altGrDown)
        {
            return false;
        }

        if (!snapshot.Client.Exists || !snapshot.Client.BrowserContext)
        {
            return false;
        }

        return vkCode is VkUp or VkDown or VkLeft or VkRight &&
            BrowserNavigator.IsFocusedElementInsideTable();
    }

    private bool IsBrowserArrowReadingContext(uint vkCode, BrowserFocusSnapshot snapshot)
    {
        if (!_browserBrowseMode || !IsDirectionalReadingKey(vkCode) || !snapshot.Client.BrowserContext)
        {
            return false;
        }

        return true;
    }

    private void SyncBrowserModeToFocusedContext()
    {
        AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
        if (focused is null || !FocusSnapshotReader.IsBrowserContext(focused))
        {
            BrowserVirtualBuffer.Clear();
            _browserBrowseMode = true;
            _browserManualFocusMode = false;
            _browserAutoFocusOnEdit = false;
            _browserEditDirty = false;
            return;
        }

        if (_browserManualFocusMode)
        {
            _browserBrowseMode = false;
            _browserAutoFocusOnEdit = IsEditableElement(focused);
            if (!_browserAutoFocusOnEdit)
            {
                _browserEditDirty = false;
            }

            return;
        }

        if (ShouldAutoPassThroughForElement(focused))
        {
            if (FocusSnapshotReader.ResolveWebSemanticRole(focused) == "web_edit" &&
                !IsDocumentRole(focused))
            {
                _browserBrowseMode = false;
                _browserAutoFocusOnEdit = true;
                _browserEditDirty = false;
                return;
            }
        }

        _browserBrowseMode = true;
        _browserAutoFocusOnEdit = false;
        _browserEditDirty = false;
        SyncOrRefreshBrowserBufferToFocus();
    }

    private void EnterAutoFocusOnEditField()
    {
        _browserBrowseMode = false;
        _browserManualFocusMode = false;
        _browserAutoFocusOnEdit = true;
        _browserEditDirty = false;
        PlayNavigationAlertTone();
    }

    private void EnterAutoFocusOnInteractiveElement()
    {
        _browserBrowseMode = false;
        _browserManualFocusMode = false;
        _browserAutoFocusOnEdit = false;
        _browserEditDirty = false;
        PlayNavigationAlertTone();
        DateTime now = DateTime.UtcNow;
        if (now - _lastAutoFocusModeAnnouncementUtc > AutoFocusModeAnnouncementWindow)
        {
            _lastAutoFocusModeAnnouncementUtc = now;
            ThreadPool.QueueUserWorkItem(_ => _speakBrowserMessage("تم تفعيل وضع التركيز تلقائيا."));
        }
    }

    private void TryAutoPassThroughForFocusedElement()
    {
        if (!_browserBrowseMode)
        {
            return;
        }

        AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
        if (focused is null || !FocusSnapshotReader.IsBrowserContext(focused))
        {
            return;
        }

        if (!ShouldAutoPassThroughForElement(focused))
        {
            return;
        }

        if (FocusSnapshotReader.ResolveWebSemanticRole(focused) == "web_edit" &&
            !IsDocumentRole(focused))
        {
            EnterAutoFocusOnEditField();
            return;
        }

        EnterAutoFocusOnInteractiveElement();
    }

    private static bool ShouldAutoPassThroughForElement(AutomationElement element)
    {
        string semanticRole = FocusSnapshotReader.ResolveWebSemanticRole(element);
        string role = FocusSnapshotReader.ResolveRole(element);
        bool focusableOrFocused = element.Current.IsKeyboardFocusable || element.Current.HasKeyboardFocus;

        if (!element.Current.IsEnabled)
        {
            return false;
        }

        if (IsEditableElement(element))
        {
            // Keep browse mode active on document-like web surfaces so arrows keep reading
            // instead of frequently flipping to focus mode.
            if (role == "document")
            {
                return false;
            }

            return true;
        }

        if (!focusableOrFocused && role != "menu")
        {
            return false;
        }

        if (IsReadOnlyButNotInteractive(element, semanticRole, role))
        {
            return false;
        }

        if (AlwaysPassThroughSemanticRoles.Contains(semanticRole) || AlwaysPassThroughRoles.Contains(role))
        {
            return true;
        }

        if (IsInteractiveGridCell(element))
        {
            return true;
        }

        if (PassThroughOnFocusSemanticRoles.Contains(semanticRole) && element.Current.HasKeyboardFocus)
        {
            return true;
        }

        if (HasToolbarAncestor(element))
        {
            return true;
        }

        if (semanticRole is "web_link" or "web_button" or "web_togglebutton" or "web_checkbox")
        {
            return false;
        }

        // List items in rich widgets often need arrow-key interaction once focused,
        // but plain web list items should stay in browse mode.
        return role == "listitem" && element.Current.IsKeyboardFocusable;
    }

    private bool TryHandleBrowseModeActivationKey(uint vkCode)
    {
        AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
        if (focused is null || !FocusSnapshotReader.IsBrowserContext(focused))
        {
            return false;
        }

        if (ShouldAutoPassThroughForElement(focused))
        {
            QueueSerializedBrowserWork(HandleBrowseModeActivationKey);
            return true;
        }

        if (!ShouldActivateFocusedElementInBrowseMode(focused, vkCode))
        {
            LogBrowserCommandDecision(vkCode, "ignored", "مفتاح التفعيل غير مناسب للعنصر الحالي في وضع التصفح.");
            return false;
        }

        LogBrowserCommandDecision(vkCode, "execute", "تفعيل العنصر الحالي من وضع التصفح.");
        QueueSerializedBrowserWork(_activateCurrentBrowserElement);
        return true;
    }

    private void HandleBrowseModeActivationKey()
    {
        AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
        if (focused is null || !FocusSnapshotReader.IsBrowserContext(focused))
        {
            _activateCurrentBrowserElement();
            return;
        }

        if (ShouldAutoPassThroughForElement(focused))
        {
            SyncOrRefreshBrowserBufferToFocus();
            if (FocusSnapshotReader.ResolveWebSemanticRole(focused) == "web_edit")
            {
                EnterAutoFocusOnEditField();
            }
            else
            {
                EnterAutoFocusOnInteractiveElement();
            }

            return;
        }

        _activateCurrentBrowserElement();
    }

    private static bool ShouldActivateFocusedElementInBrowseMode(AutomationElement element, uint vkCode)
    {
        string semanticRole = FocusSnapshotReader.ResolveWebSemanticRole(element);

        return vkCode switch
        {
            VkReturn => semanticRole is
                "web_link" or
                "web_button" or
                "web_togglebutton" or
                "web_checkbox" or
                "web_radio",
            VkSpace => semanticRole is
                "web_button" or
                "web_togglebutton" or
                "web_checkbox" or
                "web_radio",
            _ => false
        };
    }

    private static bool IsEditableElement(AutomationElement element)
    {
        if (FocusSnapshotReader.IsEditableBrowserDocument(element))
        {
            return true;
        }

        string semanticRole = FocusSnapshotReader.ResolveWebSemanticRole(element);
        if (semanticRole == "web_edit")
        {
            return true;
        }

        try
        {
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object? valuePatternObject))
            {
                return !((ValuePattern)valuePatternObject).Current.IsReadOnly;
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool IsReadOnlyButNotInteractive(AutomationElement element, string semanticRole, string role)
    {
        bool isReadOnly = false;
        try
        {
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object? valuePatternObject))
            {
                isReadOnly = ((ValuePattern)valuePatternObject).Current.IsReadOnly;
            }
        }
        catch
        {
        }

        if (!isReadOnly)
        {
            return false;
        }

        if (semanticRole == "web_combobox" || semanticRole == "web_edit")
        {
            return false;
        }

        if (role is "dataitem" or "headeritem")
        {
            return false;
        }

        return true;
    }

    private static bool IsInteractiveGridCell(AutomationElement element)
    {
        string role = FocusSnapshotReader.ResolveRole(element);
        if (role is not "dataitem" and not "custom")
        {
            return false;
        }

        try
        {
            if (element.TryGetCurrentPattern(GridItemPattern.Pattern, out _))
            {
                return element.Current.IsKeyboardFocusable;
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool HasToolbarAncestor(AutomationElement element)
    {
        AutomationElement? toolbar = FocusSnapshotReader.FindAncestor(
            element,
            current => current != element && FocusSnapshotReader.ResolveRole(current) == "toolbar");
        return toolbar is not null;
    }

    private static bool ShouldPassEscapeThroughToElement(AutomationElement element)
    {
        string semanticRole = FocusSnapshotReader.ResolveWebSemanticRole(element);
        if (IgnoreEscapeToBrowseSemanticRoles.Contains(semanticRole))
        {
            return true;
        }

        string role = FocusSnapshotReader.ResolveRole(element);
        return role is "menuitem" or "dataitem" or "headeritem";
    }

    private bool ShouldLeaveAutoFocusedEdit(
        uint vkCode,
        bool controlDown,
        bool altDown,
        bool winDown,
        BrowserFocusSnapshot snapshot)
    {
        if (_browserBrowseMode ||
            !_browserAutoFocusOnEdit ||
            _browserEditDirty ||
            altDown ||
            winDown ||
            !IsDirectionalReadingKey(vkCode) ||
            !IsBrowserEditFocused(snapshot))
        {
            return false;
        }

        return vkCode is VkUp or VkDown or VkLeft or VkRight;
    }

    private bool ShouldMarkBrowserEditAsDirty(uint vkCode, bool controlDown, bool altDown, bool winDown, BrowserFocusSnapshot snapshot)
    {
        if (_browserBrowseMode ||
            !_browserAutoFocusOnEdit ||
            !IsBrowserEditFocused(snapshot) ||
            altDown ||
            winDown)
        {
            return false;
        }

        if (controlDown)
        {
            return false;
        }

        return IsLikelyTextInputKey(vkCode);
    }

    private bool IsBrowserEditFocused(BrowserFocusSnapshot snapshot)
    {
        if (!snapshot.Client.Exists || !snapshot.Client.BrowserContext)
        {
            return false;
        }

        if (snapshot.Client.SemanticRole != "web_edit")
        {
            return false;
        }

        return !snapshot.Client.IsDocumentRole;
    }

    private static BrowserFocusSnapshot CaptureBrowserFocusSnapshot()
    {
        return new BrowserFocusSnapshot(UiaElementClient.ForFocusedElement());
    }

    private static bool IsDocumentRole(AutomationElement element) =>
        string.Equals(FocusSnapshotReader.ResolveRole(element), "document", StringComparison.Ordinal);

    private static bool IsLikelyTextInputKey(uint vkCode)
    {
        if (vkCode is >= 0x30 and <= 0x5A)
        {
            return true;
        }

        if (vkCode is >= 0x60 and <= 0x6F)
        {
            return true;
        }

        return vkCode is 0x08 or 0x20 or 0x2E or 0xBA or 0xBB or 0xBC or 0xBD or 0xBE or 0xBF or 0xC0 or 0xDB or 0xDC or 0xDD or 0xDE;
    }

    private static void PlayNavigationAlertTone()
    {
        try
        {
            MessageBeep(0x00000040);
        }
        catch
        {
        }
    }

    private static bool IsDirectionalReadingKey(uint vkCode) => vkCode is VkUp or VkDown or VkLeft or VkRight;

    private static bool IsPotentialBrowserNavigationKey(uint vkCode) =>
        vkCode is
            VkH or VkK or VkV or VkU or VkE or VkG or VkM or VkS or VkQ or VkO or VkP or VkN or
            VkB or VkX or VkR or VkC or VkD or VkT or VkY or VkLBrowser or VkI or VkA or VkF or
            VkComma or Vk1 or Vk2 or Vk3 or Vk4 or Vk5 or Vk6 or Vk7 or Vk8 or Vk9;

    private void QueueSerializedBrowserWork(Action action)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            bool gateAcquired = false;
            try
            {
                gateAcquired = _browserCommandExecutionGate.Wait(TimeSpan.FromMilliseconds(700));
                if (!gateAcquired)
                {
                    return;
                }

                action();
            }
            catch
            {
            }
            finally
            {
                if (gateAcquired)
                {
                    _browserCommandExecutionGate.Release();
                }
            }
        });
    }

    private void QueueSerializedBrowserWork(uint vkCode, long capturedAt, string detail, Action action)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            long queuedAt = Stopwatch.GetTimestamp();
            bool gateAcquired = false;
            try
            {
                gateAcquired = _browserCommandExecutionGate.Wait(TimeSpan.FromMilliseconds(700));
                if (!gateAcquired)
                {
                    LogBrowserCommandDecision(vkCode, "timeout", "تعذر الحصول على قفل تنفيذ أوامر التصفح خلال 700ms.", capturedAt);
                    return;
                }

                double queueWaitMs = ElapsedMilliseconds(queuedAt);
                long executionStartedAt = Stopwatch.GetTimestamp();
                action();
                double executionMs = ElapsedMilliseconds(executionStartedAt);
                double captureLatencyMs = capturedAt == 0 ? 0 : ElapsedMilliseconds(capturedAt);
                LogBrowserCommandCompleted(vkCode, detail, captureLatencyMs, queueWaitMs, executionMs);
            }
            catch (Exception exception)
            {
                LogBrowserCommandDecision(vkCode, "failed", $"حدث استثناء أثناء تنفيذ أمر التصفح: {exception.Message}", capturedAt);
            }
            finally
            {
                if (gateAcquired)
                {
                    _browserCommandExecutionGate.Release();
                }
            }
        });
    }

    private void LogBrowserCommandDecision(uint vkCode, string decision, string detail, long capturedAt = 0)
    {
        AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
        string process = focused is null ? "none" : FocusSnapshotReader.ResolveProcessName(focused);
        string role = focused is null ? "none" : FocusSnapshotReader.ResolveRole(focused);
        string semanticRole = focused is null ? "none" : FocusSnapshotReader.ResolveWebSemanticRole(focused);
        string name = focused is null ? "none" : FocusSnapshotReader.ResolveName(focused);
        string latency = capturedAt == 0
            ? string.Empty
            : $" latencyMs={ElapsedMilliseconds(capturedAt):0.0},";

        ErrorLogger.LogInfo(
            nameof(KeyboardCommandManager),
            $"BrowserCommand {decision}: key={FormatVirtualKey(vkCode)},{latency} browseMode={_browserBrowseMode}, singleLetter={_browserSingleLetterNavigationEnabled}, textReview={_textReviewMode}, process={process}, role={role}, semanticRole={semanticRole}, name={name}. {detail}");
    }

    private void LogBrowserKeyCapture(
        uint vkCode,
        long capturedAt,
        bool shiftDown,
        bool controlDown,
        bool altDown,
        bool winDown,
        bool altGrDown)
    {
        AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
        string process = focused is null ? "none" : FocusSnapshotReader.ResolveProcessName(focused);
        string role = focused is null ? "none" : FocusSnapshotReader.ResolveRole(focused);
        string semanticRole = focused is null ? "none" : FocusSnapshotReader.ResolveWebSemanticRole(focused);
        string name = focused is null ? "none" : FocusSnapshotReader.ResolveName(focused);

        ErrorLogger.LogInfo(
            nameof(KeyboardCommandManager),
            $"BrowserCommand captured: key={FormatVirtualKey(vkCode)}, captureTs={capturedAt}, shift={shiftDown}, ctrl={controlDown}, alt={altDown}, win={winDown}, altGr={altGrDown}, browseMode={_browserBrowseMode}, singleLetter={_browserSingleLetterNavigationEnabled}, textReview={_textReviewMode}, process={process}, role={role}, semanticRole={semanticRole}, name={name}.");
    }

    private void LogBrowserContextSnapshot(uint vkCode, long capturedAt, string detail, BrowserFocusSnapshot? snapshot = null)
    {
        BrowserFocusSnapshot value = snapshot ?? CaptureBrowserFocusSnapshot();

        ErrorLogger.LogInfo(
            nameof(KeyboardCommandManager),
            $"BrowserCommand state: key={FormatVirtualKey(vkCode)}, latencyMs={ElapsedMilliseconds(capturedAt):0.0}, browserContext={value.Client.BrowserContext}, browseMode={_browserBrowseMode}, manualFocus={_browserManualFocusMode}, autoFocusEdit={_browserAutoFocusOnEdit}, editDirty={_browserEditDirty}, singleLetter={_browserSingleLetterNavigationEnabled}, process={value.Client.Process}, role={value.Client.Role}, semanticRole={value.Client.SemanticRole}, name={value.Client.Name}. {detail}");
    }

    private void LogBrowserCommandCompleted(
        uint vkCode,
        string detail,
        double captureLatencyMs,
        double queueWaitMs,
        double executionMs)
    {
        AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
        string process = focused is null ? "none" : FocusSnapshotReader.ResolveProcessName(focused);
        string role = focused is null ? "none" : FocusSnapshotReader.ResolveRole(focused);
        string semanticRole = focused is null ? "none" : FocusSnapshotReader.ResolveWebSemanticRole(focused);
        string name = focused is null ? "none" : FocusSnapshotReader.ResolveName(focused);
        double totalLatencyMs = queueWaitMs + executionMs;

        ErrorLogger.LogInfo(
            nameof(KeyboardCommandManager),
            $"BrowserCommand completed: key={FormatVirtualKey(vkCode)}, totalLatencyMs={totalLatencyMs:0.0}, queueWaitMs={queueWaitMs:0.0}, executionMs={executionMs:0.0}, captureLatencyMs={captureLatencyMs:0.0}, browseMode={_browserBrowseMode}, process={process}, role={role}, semanticRole={semanticRole}, name={name}. {detail}");
    }

    private static bool IsBrowserDiagnosticKey(uint vkCode) =>
        IsDirectionalReadingKey(vkCode) ||
        IsPotentialBrowserNavigationKey(vkCode) ||
        vkCode is VkTab or VkReturn or VkSpace or VkEscape;

    private static double ElapsedMilliseconds(long startTimestamp) =>
        (Stopwatch.GetTimestamp() - startTimestamp) * 1000d / Stopwatch.Frequency;

    private bool ShouldSuppressRepeatedBrowserSpeech(uint vkCode, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        BrowserFocusSnapshot snapshot = CaptureBrowserFocusSnapshot();
        string focusKey = string.Join(
            "|",
            snapshot.Client.Process,
            snapshot.Client.Role,
            snapshot.Client.SemanticRole,
            snapshot.Client.Name);

        DateTime now = DateTime.UtcNow;
        bool suppress =
            _lastBrowserSpeechKey == vkCode &&
            string.Equals(_lastBrowserSpeechText, text, StringComparison.Ordinal) &&
            string.Equals(_lastBrowserSpeechFocusKey, focusKey, StringComparison.Ordinal) &&
            now - _lastBrowserSpeechUtc <= BrowserDuplicateSpeechWindow;

        if (!suppress)
        {
            _lastBrowserSpeechUtc = now;
            _lastBrowserSpeechText = text;
            _lastBrowserSpeechFocusKey = focusKey;
            _lastBrowserSpeechKey = vkCode;
        }

        return suppress;
    }

    private readonly record struct BrowserFocusSnapshot(UiaElementClient Client);

    private string SyncOrRefreshBrowserBufferToFocus()
    {
        string syncText = BrowserVirtualBuffer.SyncToFocusedElement();
        if (syncText.Contains("غير موجود داخل المخزن الظاهري", StringComparison.Ordinal) ||
            syncText.Contains("غير جاهز", StringComparison.Ordinal))
        {
            BrowserVirtualBuffer.Refresh();
            syncText = BrowserVirtualBuffer.SyncToFocusedElement();
        }

        return syncText;
    }

    private static string FormatVirtualKey(uint vkCode) =>
        vkCode switch
        {
            VkUp => "Up",
            VkDown => "Down",
            VkLeft => "Left",
            VkRight => "Right",
            VkReturn => "Enter",
            VkSpace => "Space",
            VkTab => "Tab",
            VkEscape => "Escape",
            VkComma => ",",
            _ when vkCode is >= 0x30 and <= 0x5A => ((char)vkCode).ToString(),
            _ => $"VK_{vkCode:X2}"
        };

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
    private static extern bool MessageBeep(uint uType);

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
