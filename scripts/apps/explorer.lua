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

local function looks_like_folder(name)
  if not has_text(name) then
    return false
  end

  local lower_name = string.lower(name)
  return string.find(lower_name, "desktop", 1, true) ~= nil
    or string.find(lower_name, "documents", 1, true) ~= nil
    or string.find(lower_name, "downloads", 1, true) ~= nil
    or string.find(lower_name, "music", 1, true) ~= nil
    or string.find(lower_name, "pictures", 1, true) ~= nil
    or string.find(lower_name, "videos", 1, true) ~= nil
    or string.find(lower_name, "this pc", 1, true) ~= nil
    or string.find(lower_name, "network", 1, true) ~= nil
    or string.find(name, "\\", 1, true) ~= nil
end

local function looks_like_file(name)
  if not has_text(name) then
    return false
  end

  return string.find(name, "%.[^%.\\/%s]+$") ~= nil
end

function on_focus_changed(event)
  if event.process == "explorer" then
    local name = has_text(event.name) and event.name or "عنصر غير مسمى"
    local text = nil
    local include_value = false

    if event.role == "listitem" then
      if looks_like_folder(name) or (has_text(event.state) and string.find(event.state, "موسع", 1, true) ~= nil) then
        text = "مجلد " .. name
      elseif looks_like_file(name) then
        text = "ملف " .. name
      else
        text = "عنصر " .. name
      end
    elseif event.role == "treeitem" then
      text = "عنصر تنقل " .. name
    elseif event.role == "edit" then
      if string.find(string.lower(name), "search", 1, true) ~= nil then
        text = "بحث الملفات " .. name
      else
        text = "حقل مسار " .. name
      end
      include_value = true
    elseif event.role == "button" then
      text = "زر مستكشف " .. name
    elseif event.role == "menuitem" then
      text = "أمر " .. name
    elseif event.role == "tabitem" then
      text = "تبويب مستكشف " .. name
    elseif event.role == "toolbar" then
      text = "شريط أدوات " .. name
    elseif event.role == "text" and has_text(event.value) then
      text = "نص " .. name
      include_value = true
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
