local previous_handler = on_focus_changed

local function has_text(value)
  return value ~= nil and value ~= ""
end

local function append_details(text, event, include_value)
  if include_value and has_text(event.value) then
    text = text .. ". القيمة " .. event.value
  end

  if has_text(event.state) then
    text = text .. ". " .. event.state
  end

  if has_text(event.shortcut) then
    text = text .. ". اختصار " .. event.shortcut
  end

  return text
end

local function should_include_value(event)
  if not has_text(event.value) then
    return false
  end

  if string.find(event.value, "\n", 1, true) ~= nil
    or string.find(event.value, "\r", 1, true) ~= nil then
    return false
  end

  return string.len(event.value) <= 120
end

local function looks_like_search(name)
  if not has_text(name) then
    return false
  end

  local lower_name = string.lower(name)
  return string.find(lower_name, "search", 1, true) ~= nil
    or string.find(lower_name, "find", 1, true) ~= nil
    or string.find(lower_name, "replace", 1, true) ~= nil
end

local function looks_like_explorer(name)
  if not has_text(name) then
    return false
  end

  local lower_name = string.lower(name)
  return string.find(lower_name, "explorer", 1, true) ~= nil
    or string.find(lower_name, "files", 1, true) ~= nil
    or string.find(lower_name, "folders", 1, true) ~= nil
end

local function looks_like_extensions(name)
  if not has_text(name) then
    return false
  end

  local lower_name = string.lower(name)
  return string.find(lower_name, "extensions", 1, true) ~= nil
end

local function looks_like_terminal(name)
  if not has_text(name) then
    return false
  end

  local lower_name = string.lower(name)
  return string.find(lower_name, "terminal", 1, true) ~= nil
    or string.find(lower_name, "problems", 1, true) ~= nil
    or string.find(lower_name, "output", 1, true) ~= nil
end

function on_focus_changed(event)
  if event.process == "code" then
    local name = has_text(event.name) and event.name or "عنصر غير مسمى"
    local text = nil
    local include_value = false

    if event.role == "edit" then
      if looks_like_search(name) then
        local lower_name = string.lower(name)
        if string.find(lower_name, "replace", 1, true) ~= nil then
          text = "حقل استبدال " .. name
        else
          text = "حقل بحث " .. name
        end
        include_value = true
      else
        text = "محرر كود " .. name
        include_value = should_include_value(event)
      end
    elseif event.role == "tabitem" then
      text = "تبويب محرر " .. name
    elseif event.role == "treeitem" then
      if looks_like_extensions(name) then
        text = "امتداد " .. name
      else
        text = "عنصر مستكشف " .. name
      end
    elseif event.role == "listitem" then
      if looks_like_search(name) then
        text = "نتيجة بحث " .. name
      elseif looks_like_extensions(name) then
        text = "عنصر امتدادات " .. name
      else
        text = "عنصر قائمة " .. name
      end
    elseif event.role == "button" then
      if looks_like_search(name) then
        text = "زر بحث " .. name
      else
        text = "زر " .. name
      end
    elseif event.role == "menuitem" then
      text = "أمر " .. name
    elseif event.role == "combobox" then
      text = "مربع خيارات " .. name
      include_value = true
    elseif event.role == "checkbox" then
      text = "خانة اختيار " .. name
    elseif event.role == "radiobutton" then
      text = "زر اختيار " .. name
    elseif event.role == "statusbar" then
      text = "شريط الحالة " .. name
    elseif event.role == "toolbar" then
      if looks_like_search(name) then
        text = "شريط أدوات البحث " .. name
      elseif looks_like_explorer(name) then
        text = "شريط أدوات المستكشف " .. name
      elseif looks_like_terminal(name) then
        text = "شريط أدوات الطرفية " .. name
      else
        text = "شريط أدوات " .. name
      end
    elseif event.role == "group" then
      if looks_like_search(name) then
        text = "مجموعة بحث " .. name
      elseif looks_like_explorer(name) then
        text = "مجموعة مستكشف " .. name
      elseif looks_like_extensions(name) then
        text = "مجموعة امتدادات " .. name
      elseif looks_like_terminal(name) then
        text = "مجموعة طرفية " .. name
      else
        text = "مجموعة " .. name
      end
    elseif event.role == "document" then
      text = "مستند كود " .. name
    elseif event.role == "text" and has_text(event.value) then
      text = "نص " .. name
      include_value = should_include_value(event)
    elseif event.role == "tab" then
      text = "لوحة " .. name
    elseif event.role == "pane" then
      if looks_like_search(name) then
        text = "جزء البحث " .. name
      elseif looks_like_explorer(name) then
        text = "جزء المستكشف " .. name
      elseif looks_like_extensions(name) then
        text = "جزء الامتدادات " .. name
      elseif looks_like_terminal(name) then
        text = "جزء الطرفية " .. name
      else
        text = "جزء " .. name
      end
    end

    if text ~= nil then
      return {
        action = "speak",
        text = append_details(text, event, include_value)
      }
    end
  end

  if previous_handler ~= nil then
    return previous_handler(event)
  end

  return {
    action = "none",
    text = ""
  }
end
