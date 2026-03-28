local previous_handler = on_focus_changed

local function has_text(value)
  return value ~= nil and value ~= ""
end

local function looks_like_settings(event)
  local hint = has_text(event.hint) and string.lower(event.hint) or ""
  local name = has_text(event.name) and string.lower(event.name) or ""
  return string.find(hint, "settings", 1, true) ~= nil
    or (string.find(hint, "semantic:", 1, true) == nil and string.find(name, "settings", 1, true) ~= nil)
    or string.find(name, "الإعدادات", 1, true) ~= nil
end

local function append_details(text, event, include_value)
  if include_value and has_text(event.value) then
    text = text .. ". القيمة " .. event.value
  end

  if has_text(event.state) then
    text = text .. ". " .. event.state
  end

  return text
end

function on_focus_changed(event)
  if event.process == "applicationframehost" and looks_like_settings(event) then
    local name = has_text(event.name) and event.name or "عنصر غير مسمى"
    local text = nil
    local include_value = false

    if event.role == "tabitem" then
      text = "تبويب إعدادات " .. name
    elseif event.role == "group" then
      text = "مجموعة إعدادات " .. name
    elseif event.role == "checkbox" then
      text = "خانة اختيار " .. name
    elseif event.role == "combobox" then
      text = "مربع خيارات " .. name
      include_value = true
    elseif event.role == "slider" then
      text = "منزلق " .. name
      include_value = true
    elseif event.role == "button" then
      text = "زر " .. name
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
