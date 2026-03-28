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

local function looks_like_search(name)
  if not has_text(name) then
    return false
  end

  local lower_name = string.lower(name)
  return string.find(lower_name, "search", 1, true) ~= nil
    or string.find(lower_name, "find", 1, true) ~= nil
    or string.find(lower_name, "replace", 1, true) ~= nil
end

function on_focus_changed(event)
  if event.process == "notepad++" then
    local name = has_text(event.name) and event.name or "عنصر غير مسمى"
    local text = nil
    local include_value = false

    if event.role == "edit" then
      if looks_like_search(name) then
        text = "حقل بحث " .. name
      else
        text = "محرر نص " .. name
      end
      include_value = true
    elseif event.role == "tabitem" then
      text = "تبويب ملف " .. name
    elseif event.role == "menuitem" then
      text = "أمر " .. name
    elseif event.role == "button" then
      text = "زر " .. name
    elseif event.role == "combobox" then
      text = "مربع خيارات " .. name
      include_value = true
    elseif event.role == "statusbar" then
      text = "شريط الحالة " .. name
    elseif event.role == "toolbar" then
      text = "شريط أدوات " .. name
    elseif event.role == "document" then
      text = "مستند " .. name
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
