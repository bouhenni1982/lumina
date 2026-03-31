using System.Windows.Automation;

namespace Lumina.Input;

public static class BrowserNavigator
{
    internal sealed record ElementsListItem(
        string Type,
        string Label,
        string Summary,
        string ParentContext,
        IReadOnlyList<string> ContextPath,
        AutomationElement Element,
        bool CanActivate,
        bool IsCurrent);

    public static string ActivateCurrentElement()
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        if (!FocusSnapshotReader.IsBrowserContext(current))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        if (TryInvoke(current) || TryToggle(current) || TryExpandCollapse(current))
        {
            return $"تم تفعيل {FocusSnapshotReader.BuildWebSummary(current)}";
        }

        try
        {
            current.SetFocus();
            return FocusSnapshotReader.BuildWebSummary(current);
        }
        catch
        {
            return "تعذر تفعيل العنصر الحالي.";
        }
    }

    public static string SummarizeCurrentPage()
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null)
        {
            return "لا توجد صفحة نشطة حاليا.";
        }

        if (!FocusSnapshotReader.IsBrowserContext(current))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        AutomationElement root = ResolveNavigationRoot(current);
        List<AutomationElement> elements = EnumerateElements(root).ToList();
        if (elements.Count == 0)
        {
            return "تعذر تحليل الصفحة الحالية.";
        }

        int headings = 0;
        int links = 0;
        int editFields = 0;
        int buttons = 0;
        int checkboxes = 0;
        int graphics = 0;
        int frames = 0;
        int separators = 0;
        int blockQuotes = 0;
        int embeddedObjects = 0;
        int landmarks = 0;
        int tables = 0;
        int lists = 0;
        int treeItems = 0;
        int dialogs = 0;
        int articles = 0;
        int figures = 0;
        int groupings = 0;
        int formFields = 0;
        int tabs = 0;
        int menuItems = 0;
        int progressBars = 0;

        foreach (AutomationElement element in elements)
        {
            switch (UiaElementClient.FromElement(element).SemanticRole)
            {
                case "web_heading":
                    headings++;
                    break;
                case "web_link":
                    links++;
                    break;
                case "web_edit":
                    editFields++;
                    formFields++;
                    break;
                case "web_graphic":
                    graphics++;
                    break;
                case "web_frame":
                    frames++;
                    break;
                case "web_separator":
                    separators++;
                    break;
                case "web_blockquote":
                    blockQuotes++;
                    break;
                case "web_embeddedobject":
                    embeddedObjects++;
                    break;
                case "web_button":
                case "web_togglebutton":
                    buttons++;
                    formFields++;
                    break;
                case "web_checkbox":
                    checkboxes++;
                    formFields++;
                    break;
                case "web_radio":
                case "web_combobox":
                    formFields++;
                    break;
                case "web_landmark":
                    landmarks++;
                    break;
                case "web_tab":
                    tabs++;
                    break;
                case "web_menuitem":
                    menuItems++;
                    break;
                case "web_table":
                    tables++;
                    break;
                case "web_list":
                    lists++;
                    break;
                case "web_treeitem":
                    treeItems++;
                    break;
                case "web_dialog":
                    dialogs++;
                    break;
                case "web_article":
                    articles++;
                    break;
                case "web_figure":
                    figures++;
                    break;
                case "web_grouping":
                    groupings++;
                    break;
                case "web_progressbar":
                    progressBars++;
                    break;
            }
        }

        string pageTitle = FocusSnapshotReader.ResolveWindowTitle(current);
        List<string> parts = new();
        if (!string.IsNullOrWhiteSpace(pageTitle))
        {
            parts.Add($"الصفحة {pageTitle}");
        }

        parts.Add($"العناوين {headings}");
        parts.Add($"الروابط {links}");
        parts.Add($"حقول الإدخال {editFields}");
        parts.Add($"الأزرار {buttons}");

        if (checkboxes > 0)
        {
            parts.Add($"خانات الاختيار {checkboxes}");
        }

        if (graphics > 0)
        {
            parts.Add($"الرسومات {graphics}");
        }

        if (frames > 0)
        {
            parts.Add($"الإطارات {frames}");
        }

        if (separators > 0)
        {
            parts.Add($"الفواصل {separators}");
        }

        if (blockQuotes > 0)
        {
            parts.Add($"الاقتباسات الكتلية {blockQuotes}");
        }

        if (embeddedObjects > 0)
        {
            parts.Add($"العناصر المضمنة {embeddedObjects}");
        }

        if (landmarks > 0)
        {
            parts.Add($"المعالم {landmarks}");
        }

        if (tables > 0)
        {
            parts.Add($"الجداول {tables}");
        }

        if (lists > 0)
        {
            parts.Add($"القوائم {lists}");
        }

        if (dialogs > 0)
        {
            parts.Add($"الحوارات {dialogs}");
        }

        if (tabs > 0)
        {
            parts.Add($"علامات التبويب {tabs}");
        }

        if (menuItems > 0)
        {
            parts.Add($"عناصر القوائم التفاعلية {menuItems}");
        }

        if (treeItems > 0)
        {
            parts.Add($"عناصر الشجرة {treeItems}");
        }

        if (articles > 0)
        {
            parts.Add($"المقالات {articles}");
        }

        if (figures > 0)
        {
            parts.Add($"الأشكال {figures}");
        }

        if (groupings > 0)
        {
            parts.Add($"المجموعات {groupings}");
        }

        if (progressBars > 0)
        {
            parts.Add($"أشرطة التقدم {progressBars}");
        }

        if (formFields > 0)
        {
            parts.Add($"عناصر النماذج {formFields}");
        }

        return string.Join(". ", parts);
    }

    public static string MoveToNextLink() => MoveToNextSemanticRole("web_link", "لا يوجد رابط تال في الصفحة.", "أنت على آخر رابط في الصفحة.");
    public static string MoveToPreviousLink() => MoveToPreviousSemanticRole("web_link", "لا يوجد رابط سابق في الصفحة.", "أنت على أول رابط في الصفحة.");
    public static string MoveToNextVisitedLink() => MoveToNextMatchingElement(IsVisitedLink, "لا يوجد رابط تمت زيارته تال في الصفحة.", "أنت على آخر رابط تمت زيارته في الصفحة.");
    public static string MoveToPreviousVisitedLink() => MoveToPreviousMatchingElement(IsVisitedLink, "لا يوجد رابط تمت زيارته سابق في الصفحة.", "أنت على أول رابط تمت زيارته في الصفحة.");
    public static string MoveToNextUnvisitedLink() => MoveToNextMatchingElement(IsUnvisitedLink, "لا يوجد رابط غير مزور تال في الصفحة.", "أنت على آخر رابط غير مزور في الصفحة.");
    public static string MoveToPreviousUnvisitedLink() => MoveToPreviousMatchingElement(IsUnvisitedLink, "لا يوجد رابط غير مزور سابق في الصفحة.", "أنت على أول رابط غير مزور في الصفحة.");

    public static string MoveToNextHeading() => MoveToNextSemanticRole("web_heading", "لا يوجد عنوان تال في الصفحة.", "أنت على آخر عنوان في الصفحة.");
    public static string MoveToPreviousHeading() => MoveToPreviousSemanticRole("web_heading", "لا يوجد عنوان سابق في الصفحة.", "أنت على أول عنوان في الصفحة.");
    public static string MoveToNextHeadingLevel(int level) => MoveToNextMatchingElement(
        element => FocusSnapshotReader.IsWebHeadingLevel(element, level),
        $"لا يوجد عنوان تال من المستوى {level} في الصفحة.",
        $"أنت على آخر عنوان من المستوى {level} في الصفحة.");
    public static string MoveToPreviousHeadingLevel(int level) => MoveToPreviousMatchingElement(
        element => FocusSnapshotReader.IsWebHeadingLevel(element, level),
        $"لا يوجد عنوان سابق من المستوى {level} في الصفحة.",
        $"أنت على أول عنوان من المستوى {level} في الصفحة.");

    public static string MoveToNextEditField() => MoveToNextSemanticRole("web_edit", "لا يوجد حقل إدخال تال في الصفحة.", "أنت على آخر حقل إدخال في الصفحة.");
    public static string MoveToPreviousEditField() => MoveToPreviousSemanticRole("web_edit", "لا يوجد حقل إدخال سابق في الصفحة.", "أنت على أول حقل إدخال في الصفحة.");
    public static string MoveToNextGraphic() => MoveToNextSemanticRole("web_graphic", "لا يوجد رسم تال في الصفحة.", "أنت على آخر رسم في الصفحة.");
    public static string MoveToPreviousGraphic() => MoveToPreviousSemanticRole("web_graphic", "لا يوجد رسم سابق في الصفحة.", "أنت على أول رسم في الصفحة.");
    public static string MoveToNextFrame() => MoveToNextSemanticRole("web_frame", "لا يوجد إطار تال في الصفحة.", "أنت على آخر إطار في الصفحة.");
    public static string MoveToPreviousFrame() => MoveToPreviousSemanticRole("web_frame", "لا يوجد إطار سابق في الصفحة.", "أنت على أول إطار في الصفحة.");
    public static string MoveToNextSeparator() => MoveToNextSemanticRole("web_separator", "لا يوجد فاصل تال في الصفحة.", "أنت على آخر فاصل في الصفحة.");
    public static string MoveToPreviousSeparator() => MoveToPreviousSemanticRole("web_separator", "لا يوجد فاصل سابق في الصفحة.", "أنت على أول فاصل في الصفحة.");
    public static string MoveToNextBlockQuote() => MoveToNextSemanticRole("web_blockquote", "لا يوجد اقتباس كتلي تال في الصفحة.", "أنت على آخر اقتباس كتلي في الصفحة.");
    public static string MoveToPreviousBlockQuote() => MoveToPreviousSemanticRole("web_blockquote", "لا يوجد اقتباس كتلي سابق في الصفحة.", "أنت على أول اقتباس كتلي في الصفحة.");
    public static string MoveToNextEmbeddedObject() => MoveToNextSemanticRole("web_embeddedobject", "لا يوجد عنصر مضمن تال في الصفحة.", "أنت على آخر عنصر مضمن في الصفحة.");
    public static string MoveToPreviousEmbeddedObject() => MoveToPreviousSemanticRole("web_embeddedobject", "لا يوجد عنصر مضمن سابق في الصفحة.", "أنت على أول عنصر مضمن في الصفحة.");
    public static string MoveToNextTextParagraph() => MoveToNextMatchingElement(IsTextParagraphCandidate, "لا توجد فقرة نصية تالية في الصفحة.", "أنت على آخر فقرة نصية في الصفحة.");
    public static string MoveToPreviousTextParagraph() => MoveToPreviousMatchingElement(IsTextParagraphCandidate, "لا توجد فقرة نصية سابقة في الصفحة.", "أنت على أول فقرة نصية في الصفحة.");
    public static string MoveToNextNotLinkBlock() => MoveToNextMatchingElement(IsNotLinkBlockCandidate, "لا يوجد نص تال بعد كتلة روابط في الصفحة.", "أنت على آخر كتلة نص خارج الروابط في الصفحة.");
    public static string MoveToPreviousNotLinkBlock() => MoveToPreviousMatchingElement(IsNotLinkBlockCandidate, "لا يوجد نص سابق قبل كتلة روابط في الصفحة.", "أنت على أول كتلة نص خارج الروابط في الصفحة.");

    public static string MoveToNextButton() => MoveToNextSemanticRole("web_button", "لا يوجد زر تال في الصفحة.", "أنت على آخر زر في الصفحة.");
    public static string MoveToPreviousButton() => MoveToPreviousSemanticRole("web_button", "لا يوجد زر سابق في الصفحة.", "أنت على أول زر في الصفحة.");

    public static string MoveToNextCheckbox() => MoveToNextSemanticRole("web_checkbox", "لا توجد خانة اختيار تالية في الصفحة.", "أنت على آخر خانة اختيار في الصفحة.");
    public static string MoveToPreviousCheckbox() => MoveToPreviousSemanticRole("web_checkbox", "لا توجد خانة اختيار سابقة في الصفحة.", "أنت على أول خانة اختيار في الصفحة.");
    public static string MoveToNextRadioButton() => MoveToNextSemanticRole("web_radio", "لا يوجد زر اختيار تال في الصفحة.", "أنت على آخر زر اختيار في الصفحة.");
    public static string MoveToPreviousRadioButton() => MoveToPreviousSemanticRole("web_radio", "لا يوجد زر اختيار سابق في الصفحة.", "أنت على أول زر اختيار في الصفحة.");
    public static string MoveToNextComboBox() => MoveToNextSemanticRole("web_combobox", "لا يوجد مربع خيارات تال في الصفحة.", "أنت على آخر مربع خيارات في الصفحة.");
    public static string MoveToPreviousComboBox() => MoveToPreviousSemanticRole("web_combobox", "لا يوجد مربع خيارات سابق في الصفحة.", "أنت على أول مربع خيارات في الصفحة.");
    public static string MoveToNextToggleButton() => MoveToNextSemanticRole("web_togglebutton", "لا يوجد زر تبديل تال في الصفحة.", "أنت على آخر زر تبديل في الصفحة.");
    public static string MoveToPreviousToggleButton() => MoveToPreviousSemanticRole("web_togglebutton", "لا يوجد زر تبديل سابق في الصفحة.", "أنت على أول زر تبديل في الصفحة.");
    public static string MoveToNextTab() => MoveToNextSemanticRole("web_tab", "لا توجد علامة تبويب تالية في الصفحة.", "أنت على آخر علامة تبويب في الصفحة.");
    public static string MoveToPreviousTab() => MoveToPreviousSemanticRole("web_tab", "لا توجد علامة تبويب سابقة في الصفحة.", "أنت على أول علامة تبويب في الصفحة.");
    public static string MoveToNextMenuItem() => MoveToNextSemanticRole("web_menuitem", "لا يوجد عنصر قائمة تفاعلي تال في الصفحة.", "أنت على آخر عنصر قائمة تفاعلي في الصفحة.");
    public static string MoveToPreviousMenuItem() => MoveToPreviousSemanticRole("web_menuitem", "لا يوجد عنصر قائمة تفاعلي سابق في الصفحة.", "أنت على أول عنصر قائمة تفاعلي في الصفحة.");

    public static string MoveToNextLandmark() => MoveToNextSemanticRole("web_landmark", "لا يوجد معلم تال في الصفحة.", "أنت على آخر معلم في الصفحة.");
    public static string MoveToPreviousLandmark() => MoveToPreviousSemanticRole("web_landmark", "لا يوجد معلم سابق في الصفحة.", "أنت على أول معلم في الصفحة.");

    public static string MoveToNextTable() => MoveToNextSemanticRole("web_table", "لا يوجد جدول تال في الصفحة.", "أنت على آخر جدول في الصفحة.");
    public static string MoveToPreviousTable() => MoveToPreviousSemanticRole("web_table", "لا يوجد جدول سابق في الصفحة.", "أنت على أول جدول في الصفحة.");

    public static string MoveToNextList() => MoveToNextSemanticRole("web_list", "لا توجد قائمة تالية في الصفحة.", "أنت على آخر قائمة في الصفحة.");
    public static string MoveToPreviousList() => MoveToPreviousSemanticRole("web_list", "لا توجد قائمة سابقة في الصفحة.", "أنت على أول قائمة في الصفحة.");
    public static string MoveToNextListItem() => MoveToNextSemanticRole("web_listitem", "لا يوجد عنصر قائمة تال في الصفحة.", "أنت على آخر عنصر قائمة في الصفحة.");
    public static string MoveToPreviousListItem() => MoveToPreviousSemanticRole("web_listitem", "لا يوجد عنصر قائمة سابق في الصفحة.", "أنت على أول عنصر قائمة في الصفحة.");
    public static string MoveToNextTreeItem() => MoveToNextSemanticRole("web_treeitem", "لا يوجد عنصر شجرة تال في الصفحة.", "أنت على آخر عنصر شجرة في الصفحة.");
    public static string MoveToPreviousTreeItem() => MoveToPreviousSemanticRole("web_treeitem", "لا يوجد عنصر شجرة سابق في الصفحة.", "أنت على أول عنصر شجرة في الصفحة.");

    public static string MoveToNextDialog() => MoveToNextSemanticRole("web_dialog", "لا يوجد حوار تال في الصفحة.", "أنت على آخر حوار في الصفحة.");
    public static string MoveToPreviousDialog() => MoveToPreviousSemanticRole("web_dialog", "لا يوجد حوار سابق في الصفحة.", "أنت على أول حوار في الصفحة.");
    public static string MoveToNextArticle() => MoveToNextSemanticRole("web_article", "لا توجد مقالة تالية في الصفحة.", "أنت على آخر مقالة في الصفحة.");
    public static string MoveToPreviousArticle() => MoveToPreviousSemanticRole("web_article", "لا توجد مقالة سابقة في الصفحة.", "أنت على أول مقالة في الصفحة.");
    public static string MoveToNextFigure() => MoveToNextSemanticRole("web_figure", "لا يوجد شكل تال في الصفحة.", "أنت على آخر شكل في الصفحة.");
    public static string MoveToPreviousFigure() => MoveToPreviousSemanticRole("web_figure", "لا يوجد شكل سابق في الصفحة.", "أنت على أول شكل في الصفحة.");
    public static string MoveToNextGrouping() => MoveToNextSemanticRole("web_grouping", "لا توجد مجموعة تالية في الصفحة.", "أنت على آخر مجموعة في الصفحة.");
    public static string MoveToPreviousGrouping() => MoveToPreviousSemanticRole("web_grouping", "لا توجد مجموعة سابقة في الصفحة.", "أنت على أول مجموعة في الصفحة.");
    public static string MoveToNextProgressBar() => MoveToNextSemanticRole("web_progressbar", "لا يوجد شريط تقدم تال في الصفحة.", "أنت على آخر شريط تقدم في الصفحة.");
    public static string MoveToPreviousProgressBar() => MoveToPreviousSemanticRole("web_progressbar", "لا يوجد شريط تقدم سابق في الصفحة.", "أنت على أول شريط تقدم في الصفحة.");

    public static string MoveToNextFormField() => MoveToNextFormFieldCore(moveNext: true);
    public static string MoveToPreviousFormField() => MoveToNextFormFieldCore(moveNext: false);
    public static string MoveToStartOfContainer() => MoveToContainerBoundary(moveToEnd: false);
    public static string MovePastEndOfContainer() => MoveToContainerBoundary(moveToEnd: true);
    public static string MoveToNextFocusableElement() => MoveToFocusableElement(moveNext: true);
    public static string MoveToPreviousFocusableElement() => MoveToFocusableElement(moveNext: false);
    public static string ReadCurrentTableContext() => DescribeCurrentTableContext();
    internal static List<ElementsListItem> GetElementsListItems(string itemType)
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null || !FocusSnapshotReader.IsBrowserContext(current))
        {
            return [];
        }

        AutomationElement root = ResolveNavigationRoot(current);
        List<AutomationElement> elements = EnumerateElements(root).ToList();
        if (elements.Count == 0)
        {
            return [];
        }

        return elements
            .Where(element => MatchesElementsListType(element, itemType))
            .Select(element => new ElementsListItem(
                Type: itemType,
                Label: BuildElementsListLabel(element, itemType),
                Summary: FocusSnapshotReader.BuildWebSummary(element),
                ParentContext: BuildParentContextLabel(element),
                ContextPath: BuildContextPath(element),
                Element: element,
                CanActivate: CanActivateElement(element),
                IsCurrent: SameElement(element, current)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Label))
            .ToList();
    }

    public static string GetPreferredElementsListType()
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null || !FocusSnapshotReader.IsBrowserContext(current))
        {
            return "link";
        }

        string role = SafeResolveSemanticRole(current);
        return role switch
        {
            "web_heading" => "heading",
            "web_button" => "button",
            "web_togglebutton" => "toggleButton",
            "web_graphic" => "graphic",
            "web_frame" => "frame",
            "web_separator" => "separator",
            "web_blockquote" => "blockQuote",
            "web_embeddedobject" => "embeddedObject",
            "web_landmark" => "landmark",
            "web_table" => "table",
            "web_list" => "list",
            "web_listitem" => "listItem",
            "web_treeitem" => "treeItem",
            "web_tab" => "tab",
            "web_menuitem" => "menuItem",
            "web_article" => "article",
            "web_figure" => "figure",
            "web_grouping" => "grouping",
            "web_progressbar" => "progressBar",
            _ when IsFormFieldRole(role) => "formField",
            _ => "link"
        };
    }
    public static bool IsFocusedElementInsideTable()
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null || !FocusSnapshotReader.IsBrowserContext(current))
        {
            return false;
        }

        return FindAncestorBySemanticRole(current, "web_table") is not null;
    }

    public static string MoveToNextTableCell() => MoveToAdjacentTableCell(moveNext: true);
    public static string MoveToPreviousTableCell() => MoveToAdjacentTableCell(moveNext: false);
    public static string MoveToTableCellBelow() => MoveToVerticalTableCell(moveDown: true);
    public static string MoveToTableCellAbove() => MoveToVerticalTableCell(moveDown: false);

    private static string MoveToNextSemanticRole(string semanticRole, string missingMessage, string? boundaryMessage = null)
        => MoveToNextMatchingElement(
            element => SafeResolveSemanticRole(element) == semanticRole,
            missingMessage,
            boundaryMessage);

    private static string MoveToPreviousSemanticRole(string semanticRole, string missingMessage, string? boundaryMessage = null)
        => MoveToPreviousMatchingElement(
            element => SafeResolveSemanticRole(element) == semanticRole,
            missingMessage,
            boundaryMessage);

    private static string MoveToNextMatchingElement(
        Func<AutomationElement, bool> matcher,
        string missingMessage,
        string? boundaryMessage = null)
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        if (!FocusSnapshotReader.IsBrowserContext(current))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        AutomationElement root = ResolveNavigationRoot(current);
        List<AutomationElement> elements = EnumerateElements(root)
            .Where(IsPageNavigationCandidate)
            .ToList();
        if (elements.Count == 0)
        {
            return missingMessage;
        }

        int currentIndex = elements.FindIndex(element => SameElement(element, current));
        List<AutomationElement> matchingElements = FindMatchingElements(elements, matcher);

        if (matchingElements.Count == 0)
        {
            return missingMessage;
        }

        IEnumerable<AutomationElement> orderedCandidates = EnumerateAfterCurrent(elements, currentIndex)
            .Where(element => MatchesElementSafely(element, matcher));

        foreach (AutomationElement candidate in orderedCandidates)
        {
            if (TryFocusAndSync(candidate, current, out string summary))
            {
                return summary;
            }
        }

        bool currentMatches = currentIndex >= 0 && MatchesElementSafely(elements[currentIndex], matcher);
        return currentMatches ? boundaryMessage ?? missingMessage : missingMessage;
    }

    private static string MoveToPreviousMatchingElement(
        Func<AutomationElement, bool> matcher,
        string missingMessage,
        string? boundaryMessage = null)
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        if (!FocusSnapshotReader.IsBrowserContext(current))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        AutomationElement root = ResolveNavigationRoot(current);
        List<AutomationElement> elements = EnumerateElements(root)
            .Where(IsPageNavigationCandidate)
            .ToList();
        if (elements.Count == 0)
        {
            return missingMessage;
        }

        int currentIndex = elements.FindIndex(element => SameElement(element, current));
        List<AutomationElement> matchingElements = FindMatchingElements(elements, matcher);

        if (matchingElements.Count == 0)
        {
            return missingMessage;
        }

        IEnumerable<AutomationElement> orderedCandidates = EnumerateBeforeCurrent(elements, currentIndex)
            .Where(element => MatchesElementSafely(element, matcher));

        foreach (AutomationElement candidate in orderedCandidates)
        {
            if (TryFocusAndSync(candidate, current, out string summary))
            {
                return summary;
            }
        }

        bool currentMatches = currentIndex >= 0 && MatchesElementSafely(elements[currentIndex], matcher);
        return currentMatches ? boundaryMessage ?? missingMessage : missingMessage;
    }

    private static string MoveToNextFormFieldCore(bool moveNext)
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        if (!FocusSnapshotReader.IsBrowserContext(current))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        AutomationElement root = ResolveNavigationRoot(current);
        List<AutomationElement> elements = EnumerateElements(root)
            .Where(IsPageNavigationCandidate)
            .ToList();
        if (elements.Count == 0)
        {
            return moveNext
                ? "لا يوجد عنصر نموذج تال في الصفحة."
                : "لا يوجد عنصر نموذج سابق في الصفحة.";
        }

        int currentIndex = elements.FindIndex(element => SameElement(element, current));
        IEnumerable<AutomationElement> orderedCandidates = moveNext
            ? EnumerateAfterCurrent(elements, currentIndex)
            : EnumerateBeforeCurrent(elements, currentIndex);

        foreach (AutomationElement candidate in orderedCandidates)
        {
            string role = SafeResolveSemanticRole(candidate);
            if (!IsFormFieldRole(role))
            {
                continue;
            }

            if (TryFocusAndSync(candidate, current, out string summary))
            {
                return summary;
            }
        }

        return moveNext
            ? CurrentElementMatchesFormField(elements, currentIndex)
                ? "أنت على آخر عنصر نموذج في الصفحة."
                : "لا يوجد عنصر نموذج تال في الصفحة."
            : CurrentElementMatchesFormField(elements, currentIndex)
                ? "أنت على أول عنصر نموذج في الصفحة."
                : "لا يوجد عنصر نموذج سابق في الصفحة.";
    }

    private static string DescribeCurrentTableContext()
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        if (!FocusSnapshotReader.IsBrowserContext(current))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        AutomationElement? table = FindAncestorBySemanticRole(current, "web_table");
        if (table is null)
        {
            return "العنصر الحالي ليس داخل جدول معروف.";
        }

        List<string> segments = [];
        UiaElementClient tableClient = UiaElementClient.FromElement(table);
        segments.Add(tableClient.WebSummary);

        if (TryDescribeCurrentCell(current, out string? cellSummary))
        {
            segments.Add(cellSummary!);
        }

        try
        {
            if (tableClient.TryGetGridPattern(out GridPattern? grid) && grid is not null)
            {
                segments.Add($"الصفوف {grid.Current.RowCount}");
                segments.Add($"الأعمدة {grid.Current.ColumnCount}");
            }
        }
        catch
        {
        }

        return string.Join(". ", segments);
    }

    private static string MoveToContainerBoundary(bool moveToEnd)
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        if (!FocusSnapshotReader.IsBrowserContext(current))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        AutomationElement? container = ResolveContainerForElement(current);
        if (container is null)
        {
            return "العنصر الحالي ليس داخل حاوية معروفة.";
        }

        List<AutomationElement> descendants = EnumerateElements(container)
            .Where(IsBufferCandidate)
            .ToList();

        if (descendants.Count == 0)
        {
            return "تعذر تحديد حدود الحاوية الحالية.";
        }

        AutomationElement target = moveToEnd ? descendants[^1] : descendants[0];
        try
        {
            target.SetFocus();
            BrowserVirtualBuffer.SyncToElement(target);
            return moveToEnd
                ? $"تم تجاوز نهاية الحاوية. {FocusSnapshotReader.BuildWebSummary(target)}"
                : $"بداية الحاوية. {FocusSnapshotReader.BuildWebSummary(target)}";
        }
        catch
        {
            return "تعذر التنقل داخل الحاوية الحالية.";
        }
    }

    private static string MoveToFocusableElement(bool moveNext)
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        if (!FocusSnapshotReader.IsBrowserContext(current))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        AutomationElement root = ResolveNavigationRoot(current);
        List<AutomationElement> elements = EnumerateElements(root)
            .Where(IsFocusableWebElement)
            .ToList();
        if (elements.Count == 0)
        {
            return "لا توجد عناصر قابلة للتفاعل في الصفحة.";
        }

        int currentIndex = FindClosestElementIndex(elements, current);
        int targetIndex = moveNext ? currentIndex + 1 : currentIndex - 1;
        if (targetIndex < 0 || targetIndex >= elements.Count)
        {
            return moveNext
                ? "لا يوجد عنصر تفاعلي تال في الصفحة."
                : "لا يوجد عنصر تفاعلي سابق في الصفحة.";
        }

        int step = moveNext ? 1 : -1;
        for (int index = targetIndex; index >= 0 && index < elements.Count; index += step)
        {
            if (TryFocusAndSync(elements[index], current, out string summary))
            {
                return summary;
            }
        }

        return "تعذر التنقل إلى العنصر التفاعلي المطلوب.";
    }

    private static string MoveToAdjacentTableCell(bool moveNext)
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        if (!FocusSnapshotReader.IsBrowserContext(current))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        AutomationElement? table = FindAncestorBySemanticRole(current, "web_table");
        if (table is null)
        {
            return "العنصر الحالي ليس داخل جدول معروف.";
        }

        if (TryMoveUsingGridPattern(current, table, moveNext, out string? gridMoveText))
        {
            return gridMoveText!;
        }

        if (TryMoveUsingTableDescendants(current, table, moveNext, out string? descendantMoveText))
        {
            return descendantMoveText!;
        }

        return moveNext
            ? "لا توجد خلية تالية في هذا الجدول."
            : "لا توجد خلية سابقة في هذا الجدول.";
    }

    private static string MoveToVerticalTableCell(bool moveDown)
    {
        AutomationElement? current = FocusSnapshotReader.GetFocusedElement();
        if (current is null)
        {
            return "لا يوجد عنصر نشط حاليا.";
        }

        if (!FocusSnapshotReader.IsBrowserContext(current))
        {
            return "العنصر الحالي ليس ضمن سياق ويب معروف.";
        }

        AutomationElement? table = FindAncestorBySemanticRole(current, "web_table");
        if (table is null)
        {
            return "العنصر الحالي ليس داخل جدول معروف.";
        }

        if (TryMoveVerticallyUsingGridPattern(current, table, moveDown, out string? text))
        {
            return text!;
        }

        return moveDown
            ? "لا توجد خلية أسفل الموضع الحالي."
            : "لا توجد خلية أعلى الموضع الحالي.";
    }

    internal static AutomationElement ResolveNavigationRootForBuffer(AutomationElement current) => ResolveNavigationRoot(current);

    internal static IEnumerable<AutomationElement> EnumerateBufferCandidates(AutomationElement root) =>
        EnumerateElements(root).Where(IsBufferCandidate);

    private static AutomationElement ResolveNavigationRoot(AutomationElement current)
    {
        AutomationElement? document = FocusSnapshotReader.FindAncestor(
            current,
            element => element.Current.ControlType == ControlType.Document);

        if (document is not null)
        {
            return document;
        }

        AutomationElement? window = FocusSnapshotReader.FindAncestor(
            current,
            element => element.Current.ControlType == ControlType.Window);

        return window ?? current;
    }

    private static IEnumerable<AutomationElement> EnumerateElements(AutomationElement root)
    {
        Stack<AutomationElement> stack = new();
        stack.Push(root);

        while (stack.Count > 0)
        {
            AutomationElement current = stack.Pop();
            yield return current;

            List<AutomationElement> children = [];
            AutomationElement? child;
            try
            {
                child = TreeWalker.ControlViewWalker.GetFirstChild(current);
            }
            catch (ElementNotAvailableException)
            {
                continue;
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            while (child is not null)
            {
                children.Add(child);
                try
                {
                    child = TreeWalker.ControlViewWalker.GetNextSibling(child);
                }
                catch (ElementNotAvailableException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    break;
                }
            }

            for (int index = children.Count - 1; index >= 0; index--)
            {
                stack.Push(children[index]);
            }
        }
    }

    private static bool IsBufferCandidate(AutomationElement element)
        => IsBufferCandidate(UiaElementClient.FromElement(element));

    private static bool IsBufferCandidate(UiaElementClient client)
    {
        if (!client.Exists || client.Element is null || !IsPageNavigationCandidate(client))
        {
            return false;
        }

        if (client.IsDocumentRole)
        {
            return false;
        }

        string semanticRole = client.SemanticRole;
        if (semanticRole is "web_control")
        {
            return false;
        }

        string name = client.Name;
        if (!string.IsNullOrWhiteSpace(name) && name != "عنصر غير مسمى")
        {
            return true;
        }

        string text = GetReadableElementText(client.Element);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // Keep structural containers with no readable label out of the virtual buffer
        // unless they expose enough text to be meaningfully read as content.
        if (semanticRole is "web_landmark" or "web_table" or "web_list" or "web_dialog")
        {
            int wordCount = text
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Count(word => word.Any(char.IsLetterOrDigit));
            return wordCount >= 4;
        }

        return true;
    }

    private static bool IsFormFieldRole(string semanticRole) =>
        semanticRole is "web_edit" or "web_combobox" or "web_checkbox" or "web_radio" or "web_button" or "web_togglebutton";

    private static bool IsContainerRole(string semanticRole) =>
        semanticRole is "web_list" or "web_table" or "web_dialog" or "web_landmark" or "web_article" or "web_grouping";

    private static bool IsFocusableWebElement(AutomationElement element)
    {
        try
        {
            UiaElementClient client = UiaElementClient.FromElement(element);
            if (!client.Exists || client.Element is null || !IsPageNavigationCandidate(client))
            {
                return false;
            }

            if (client.IsDocumentRole)
            {
                return false;
            }

            string semanticRole = client.SemanticRole;
            if (semanticRole == "web_control")
            {
                return false;
            }

            if (client.Element.Current.IsKeyboardFocusable)
            {
                return true;
            }

            return semanticRole is "web_link" or "web_button" or "web_edit" or "web_checkbox" or "web_radio" or "web_combobox" or "web_tab" or "web_listitem";
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool MatchesElementsListType(AutomationElement element, string itemType)
    {
        string role = UiaElementClient.FromElement(element).SemanticRole;
        return itemType switch
        {
            "link" => role == "web_link",
            "visitedLink" => IsVisitedLink(element),
            "unvisitedLink" => IsUnvisitedLink(element),
            "heading" => role == "web_heading",
            "formField" => IsFormFieldRole(role),
            "button" => role == "web_button",
            "toggleButton" => role == "web_togglebutton",
            "graphic" => role == "web_graphic",
            "frame" => role == "web_frame",
            "separator" => role == "web_separator",
            "blockQuote" => role == "web_blockquote",
            "embeddedObject" => role == "web_embeddedobject",
            "textParagraph" => IsTextParagraphCandidate(element),
            "notLinkBlock" => IsNotLinkBlockCandidate(element),
            "landmark" => role == "web_landmark",
            "table" => role == "web_table",
            "list" => role == "web_list",
            "listItem" => role == "web_listitem",
            "treeItem" => role == "web_treeitem",
            "tab" => role == "web_tab",
            "menuItem" => role == "web_menuitem",
            "article" => role == "web_article",
            "figure" => role == "web_figure",
            "grouping" => role == "web_grouping",
            "progressBar" => role == "web_progressbar",
            _ => false
        };
    }

    private static string BuildElementsListLabel(AutomationElement element, string itemType)
    {
        UiaElementClient client = UiaElementClient.FromElement(element);
        string name = client.Name;
        string role = FocusSnapshotReader.DescribeRole(element);
        string? state = client.StateSummary;
        string baseLabel = itemType switch
        {
            "heading" => name,
            "link" => name,
            "visitedLink" => name,
            "unvisitedLink" => name,
            "landmark" => name,
            "button" => name,
            "toggleButton" => name,
            "graphic" => name,
            "frame" => name,
            "separator" => name,
            "blockQuote" => name,
            "embeddedObject" => name,
            "textParagraph" => name,
            "notLinkBlock" => name,
            "table" => name,
            "list" => name,
            "listItem" => name,
            "treeItem" => name,
            "tab" => name,
            "menuItem" => name,
            "article" => name,
            "figure" => name,
            "grouping" => name,
            "progressBar" => name,
            "formField" => $"{name}; {role}",
            _ => name
        };

        if (string.IsNullOrWhiteSpace(baseLabel) || baseLabel == "عنصر غير مسمى")
        {
            baseLabel = role;
        }

        return string.IsNullOrWhiteSpace(state)
            ? baseLabel
            : $"{baseLabel}; {state}";
    }

    private static string BuildParentContextLabel(AutomationElement element)
    {
        return string.Join("، ", BuildContextPath(element));
    }

    private static IReadOnlyList<string> BuildContextPath(AutomationElement element)
    {
        List<string> parts = [];
        AutomationElement? landmark = FindAncestorBySemanticRole(element, "web_landmark");
        AutomationElement? dialog = FindAncestorBySemanticRole(element, "web_dialog");
        AutomationElement? article = FindAncestorBySemanticRole(element, "web_article");
        AutomationElement? grouping = FindAncestorBySemanticRole(element, "web_grouping");
        AutomationElement? list = FindAncestorBySemanticRole(element, "web_list");
        AutomationElement? table = FindAncestorBySemanticRole(element, "web_table");

        AddContextPart(parts, landmark, "معلم");
        AddContextPart(parts, dialog, "حوار");
        AddContextPart(parts, article, "مقالة");
        AddContextPart(parts, grouping, "مجموعة");
        AddContextPart(parts, list, "قائمة");
        AddContextPart(parts, table, "جدول");
        return parts;
    }

    private static void AddContextPart(List<string> parts, AutomationElement? element, string prefix)
    {
        if (element is null)
        {
            return;
        }

        string name = UiaElementClient.FromElement(element).Name;
        if (string.IsNullOrWhiteSpace(name) || name == "عنصر غير مسمى")
        {
            parts.Add(prefix);
            return;
        }

        parts.Add($"{prefix} {name}");
    }

    internal static bool FocusElement(AutomationElement element)
    {
        try
        {
            element.SetFocus();
            BrowserVirtualBuffer.SyncToElement(element);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryFocusAndSync(AutomationElement candidate, AutomationElement? previous, out string summary)
    {
        summary = string.Empty;

        try
        {
            candidate.SetFocus();
            AutomationElement? focused = FocusSnapshotReader.GetFocusedElement();
            if (focused is not null && previous is not null && SameElement(focused, previous))
            {
                return false;
            }

            AutomationElement target = focused ?? candidate;
            BrowserVirtualBuffer.SyncToElement(target);
            summary = FocusSnapshotReader.BuildWebSummary(target);
            return true;
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    internal static bool CanActivateElement(AutomationElement element)
    {
        string role = UiaElementClient.FromElement(element).SemanticRole;
        return role is "web_link" or "web_button" or "web_togglebutton" or "web_tab" or "web_menuitem" or "web_treeitem";
    }

    private static bool IsVisitedLink(AutomationElement element) =>
        SafeResolveSemanticRole(element) == "web_link" &&
        FocusSnapshotReader.HasBrowserState(element, "تمت زيارته");

    private static bool IsUnvisitedLink(AutomationElement element) =>
        SafeResolveSemanticRole(element) == "web_link" &&
        !FocusSnapshotReader.HasBrowserState(element, "تمت زيارته");

    private static bool IsTextParagraphCandidate(AutomationElement element)
    {
        UiaElementClient client = UiaElementClient.FromElement(element);
        if (!client.Exists || client.Element is null || !IsPageNavigationCandidate(client))
        {
            return false;
        }

        string semanticRole = client.SemanticRole;
        if (semanticRole is "web_link" or "web_button" or "web_togglebutton" or "web_checkbox" or "web_radio" or "web_combobox" or "web_menuitem" or "web_tab" or "web_separator" or "web_progressbar")
        {
            return false;
        }

        string text = GetReadableElementText(client.Element);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        int wordCount = text
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Count(word => word.Any(char.IsLetterOrDigit));

        if (wordCount < 6)
        {
            return false;
        }

        string role = client.Role;
        return semanticRole is "web_article" or "web_grouping" or "web_blockquote" ||
               role is "text" or "listitem" or "group";
    }

    private static bool IsPageNavigationCandidate(AutomationElement element)
        => IsPageNavigationCandidate(UiaElementClient.FromElement(element));

    private static bool IsPageNavigationCandidate(UiaElementClient client)
    {
        try
        {
            if (!client.Exists || client.Element is null || !client.BrowserContext)
            {
                return false;
            }

            if (client.Element.Current.IsOffscreen)
            {
                return false;
            }

            if (client.IsDocumentRole)
            {
                return false;
            }

            string semanticRole = client.SemanticRole;
            if (semanticRole is "web_control" or "web_document")
            {
                return false;
            }

            if (semanticRole is "web_landmark" or "web_table" or "web_list" or "web_dialog" or "web_article" or "web_figure" or "web_grouping")
            {
                string containerName = client.Name;
                if (!string.IsNullOrWhiteSpace(containerName) && containerName != "عنصر غير مسمى")
                {
                    return true;
                }
            }

            string readableText = GetReadableElementText(client.Element);
            return !string.IsNullOrWhiteSpace(readableText);
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool IsNotLinkBlockCandidate(AutomationElement element)
    {
        if (!IsTextParagraphCandidate(element))
        {
            return false;
        }

        if (SafeResolveSemanticRole(element) == "web_link")
        {
            return false;
        }

        string text = GetReadableElementText(element);
        if (text.Length < 30)
        {
            return false;
        }

        string lowered = text.ToLowerInvariant();
        return !lowered.Contains("رابط ويب", StringComparison.Ordinal) &&
               !lowered.Contains("رابط ", StringComparison.Ordinal);
    }

    private static string GetReadableElementText(AutomationElement element)
    {
        string textPatternText = FocusSnapshotReader.TryReadTextPatternText(element);
        if (!string.IsNullOrWhiteSpace(textPatternText))
        {
            return NormalizeReadableText(textPatternText);
        }

        string value = FocusSnapshotReader.TryReadValue(element);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return NormalizeReadableText(value);
        }

        string name = FocusSnapshotReader.ResolveName(element);
        return name == "عنصر غير مسمى" ? string.Empty : NormalizeReadableText(name);
    }

    private static string NormalizeReadableText(string? text) =>
        (text ?? string.Empty)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal)
            .Replace('\u00A0', ' ')
            .Replace('\n', ' ')
            .Trim();

    private static bool CurrentElementMatchesFormField(IReadOnlyList<AutomationElement> elements, int currentIndex) =>
        currentIndex >= 0 && IsFormFieldRole(SafeResolveSemanticRole(elements[currentIndex]));

    private static AutomationElement? ResolveContainerForElement(AutomationElement element) =>
        FocusSnapshotReader.FindAncestor(
            element,
            current => IsContainerRole(SafeResolveSemanticRole(current)));

    private static bool TryInvoke(AutomationElement element)
    {
        try
        {
            UiaElementClient client = UiaElementClient.FromElement(element);
            if (!client.TryGetInvokePattern(out InvokePattern? invokePattern) || invokePattern is null)
            {
                return false;
            }

            invokePattern.Invoke();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryToggle(AutomationElement element)
    {
        try
        {
            UiaElementClient client = UiaElementClient.FromElement(element);
            if (!client.TryGetTogglePattern(out TogglePattern? togglePattern) || togglePattern is null)
            {
                return false;
            }

            togglePattern.Toggle();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryExpandCollapse(AutomationElement element)
    {
        try
        {
            UiaElementClient client = UiaElementClient.FromElement(element);
            if (!client.TryGetExpandCollapsePattern(out ExpandCollapsePattern? pattern) || pattern is null)
            {
                return false;
            }

            switch (pattern.Current.ExpandCollapseState)
            {
                case ExpandCollapseState.Collapsed:
                case ExpandCollapseState.PartiallyExpanded:
                    pattern.Expand();
                    return true;
                case ExpandCollapseState.Expanded:
                    pattern.Collapse();
                    return true;
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private static AutomationElement? FindAncestorBySemanticRole(AutomationElement element, string semanticRole) =>
        FocusSnapshotReader.FindAncestor(
            element,
            current => SafeResolveSemanticRole(current) == semanticRole);

    private static bool TryDescribeCurrentCell(AutomationElement current, out string? text)
    {
        text = null;

        try
        {
            UiaElementClient client = UiaElementClient.FromElement(current);
            if (!client.TryGetGridItemPattern(out GridItemPattern? gridItem) || gridItem is null)
            {
                return false;
            }

            string cellName = client.Name;
            List<string> segments =
            [
                $"الخلية {cellName}",
                $"الصف {gridItem.Current.Row + 1}",
                $"العمود {gridItem.Current.Column + 1}"
            ];

            AddHeaderSegments(current, segments);
            text = string.Join(". ", segments);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryMoveUsingGridPattern(
        AutomationElement current,
        AutomationElement table,
        bool moveNext,
        out string? text)
    {
        text = null;

        try
        {
            UiaElementClient currentClient = UiaElementClient.FromElement(current);
            UiaElementClient tableClient = UiaElementClient.FromElement(table);
            if (!currentClient.TryGetGridItemPattern(out GridItemPattern? gridItem) ||
                gridItem is null ||
                !tableClient.TryGetGridPattern(out GridPattern? grid) ||
                grid is null)
            {
                return false;
            }

            int row = gridItem.Current.Row;
            int column = gridItem.Current.Column + (moveNext ? 1 : -1);

            if (column < 0 || column >= grid.Current.ColumnCount)
            {
                return false;
            }

            AutomationElement target = grid.GetItem(row, column);
            target.SetFocus();
            text = BuildTableCellSummary(target);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryMoveVerticallyUsingGridPattern(
        AutomationElement current,
        AutomationElement table,
        bool moveDown,
        out string? text)
    {
        text = null;

        try
        {
            UiaElementClient currentClient = UiaElementClient.FromElement(current);
            UiaElementClient tableClient = UiaElementClient.FromElement(table);
            if (!currentClient.TryGetGridItemPattern(out GridItemPattern? gridItem) ||
                gridItem is null ||
                !tableClient.TryGetGridPattern(out GridPattern? grid) ||
                grid is null)
            {
                return false;
            }

            int row = gridItem.Current.Row + (moveDown ? 1 : -1);
            int column = gridItem.Current.Column;

            if (row < 0 || row >= grid.Current.RowCount)
            {
                return false;
            }

            AutomationElement target = grid.GetItem(row, column);
            target.SetFocus();
            text = BuildTableCellSummary(target);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryMoveUsingTableDescendants(
        AutomationElement current,
        AutomationElement table,
        bool moveNext,
        out string? text)
    {
        text = null;

        try
        {
            List<AutomationElement> descendants = EnumerateElements(table)
                .Where(candidate =>
                {
                    string role = UiaElementClient.FromElement(candidate).Role;
                    return role is "dataitem" or "listitem" or "text" or "edit" or "button";
                })
                .ToList();

            if (descendants.Count == 0)
            {
                return false;
            }

            int currentIndex = descendants.FindIndex(element => SameElement(element, current));
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = moveNext ? currentIndex + 1 : currentIndex - 1;
            if (nextIndex < 0 || nextIndex >= descendants.Count)
            {
                return false;
            }

            AutomationElement target = descendants[nextIndex];
            target.SetFocus();
            text = BuildTableCellSummary(target);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildTableCellSummary(AutomationElement element)
    {
        if (TryDescribeCurrentCell(element, out string? cellText))
        {
            return cellText!;
        }

        return UiaElementClient.FromElement(element).WebSummary;
    }

    private static void AddHeaderSegments(AutomationElement element, List<string> segments)
    {
        try
        {
            UiaElementClient client = UiaElementClient.FromElement(element);
            if (!client.TryGetTableItemPattern(out TableItemPattern? tableItem) || tableItem is null)
            {
                return;
            }

            string[] rowHeaders = tableItem.Current.GetRowHeaderItems()
                .Cast<AutomationElement>()
                .Select(header => UiaElementClient.FromElement(header).Name)
                .Where(name => !string.IsNullOrWhiteSpace(name) && name != "عنصر غير مسمى")
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            string[] columnHeaders = tableItem.Current.GetColumnHeaderItems()
                .Cast<AutomationElement>()
                .Select(header => UiaElementClient.FromElement(header).Name)
                .Where(name => !string.IsNullOrWhiteSpace(name) && name != "عنصر غير مسمى")
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (rowHeaders.Length > 0)
            {
                segments.Add($"رأس الصف {string.Join("، ", rowHeaders)}");
            }

            if (columnHeaders.Length > 0)
            {
                segments.Add($"رأس العمود {string.Join("، ", columnHeaders)}");
            }
        }
        catch
        {
        }
    }

    private static IEnumerable<AutomationElement> EnumerateAfterCurrent(IReadOnlyList<AutomationElement> elements, int currentIndex)
    {
        int startIndex = currentIndex < 0 ? 0 : currentIndex + 1;

        for (int index = startIndex; index < elements.Count; index++)
        {
            yield return elements[index];
        }
    }

    private static IEnumerable<AutomationElement> EnumerateBeforeCurrent(IReadOnlyList<AutomationElement> elements, int currentIndex)
    {
        int startIndex = currentIndex < 0 ? elements.Count - 1 : currentIndex - 1;

        for (int index = startIndex; index >= 0; index--)
        {
            yield return elements[index];
        }
    }

    private static bool SameElement(AutomationElement first, AutomationElement second)
    {
        try
        {
            int[]? left = first.GetRuntimeId();
            int[]? right = second.GetRuntimeId();
            if (left is null || right is null || left.Length != right.Length)
            {
                return false;
            }

            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static int FindClosestElementIndex(IReadOnlyList<AutomationElement> elements, AutomationElement current)
    {
        int exactIndex = -1;
        for (int index = 0; index < elements.Count; index++)
        {
            if (SameElement(elements[index], current))
            {
                exactIndex = index;
                break;
            }
        }

        if (exactIndex >= 0)
        {
            return exactIndex;
        }

        AutomationElement? ancestorMatch = FocusSnapshotReader.FindAncestor(
            current,
            candidate => elements.Any(element => SameElement(element, candidate)));
        if (ancestorMatch is null)
        {
            return -1;
        }

        for (int index = 0; index < elements.Count; index++)
        {
            if (SameElement(elements[index], ancestorMatch))
            {
                return index;
            }
        }

        return -1;
    }

    private static List<AutomationElement> FindMatchingElements(
        IEnumerable<AutomationElement> elements,
        Func<AutomationElement, bool> matcher) =>
        elements.Where(element => MatchesElementSafely(element, matcher)).ToList();

    private static bool MatchesElementSafely(
        AutomationElement element,
        Func<AutomationElement, bool> matcher)
    {
        try
        {
            return matcher(element);
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static string SafeResolveSemanticRole(AutomationElement element)
    {
        try
        {
            return FocusSnapshotReader.ResolveWebSemanticRole(element);
        }
        catch (ElementNotAvailableException)
        {
            return "web_control";
        }
        catch (InvalidOperationException)
        {
            return "web_control";
        }
    }
}
