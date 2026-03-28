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

function on_focus_changed(event)
  if event.process == "systemsettings" then
    local name = has_text(event.name) and event.name or "عنصر غير مسمى"
    local text = nil
    local include_value = false

    if event.role == "tabitem" then
      text = "تبويب إعدادات " .. name
    elseif event.role == "group" then
      text = "مجموعة إعدادات " .. name
    elseif event.role == "checkbox" then
      text = "خانة اختيار " .. name
    elseif event.role == "radiobutton" then
      text = "زر اختيار " .. name
    elseif event.role == "combobox" then
      text = "مربع خيارات " .. name
      include_value = true
    elseif event.role == "slider" then
      text = "منزلق " .. name
      include_value = true
    elseif event.role == "button" then
      text = "زر " .. name
    elseif event.role == "edit" then
      text = "حقل إعداد " .. name
      include_value = true
    elseif event.role == "text" and has_text(event.value) then
      text = "نص " .. name
      include_value = true
    elseif event.role == "text" then
      text = "نص " .. name
    elseif event.role == "listitem" then
      text = "عنصر إعداد " .. name
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
