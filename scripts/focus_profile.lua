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
  local name = has_text(event.name) and event.name or "عنصر غير مسمى"
  local text = nil
  local include_value = false

  if event.type == "liveRegionChanged" or event.type == "liveTextChanged" then
    local live_parts = {}

    if has_text(event.name) then
      table.insert(live_parts, event.name)
    end

    if has_text(event.value) and event.value ~= event.name then
      table.insert(live_parts, event.value)
    end

    if has_text(event.state) then
      table.insert(live_parts, event.state)
    end

    if #live_parts == 0 then
      if event.semantic_role == "web_dialog" then
        text = "تم تحديث حوار في الصفحة"
      else
        text = "تم تحديث محتوى في الصفحة"
      end
    else
      text = table.concat(live_parts, ". ")
    end

    return {
      action = "speak",
      text = text
    }
  end

  if event.context_kind == "browser" then
    if event.semantic_role == "web_link" then
      text = "رابط " .. name
    elseif event.semantic_role == "web_heading" then
      text = "عنوان " .. name
    elseif event.semantic_role == "web_edit" then
      text = "حقل ويب " .. name
      include_value = true
    elseif event.semantic_role == "web_button" then
      text = "زر ويب " .. name
    elseif event.semantic_role == "web_checkbox" then
      text = "خانة اختيار ويب " .. name
    elseif event.semantic_role == "web_radio" then
      text = "زر اختيار ويب " .. name
    elseif event.semantic_role == "web_combobox" then
      text = "مربع خيارات ويب " .. name
      include_value = true
    elseif event.semantic_role == "web_table" then
      text = "جدول " .. name
    elseif event.semantic_role == "web_list" then
      text = "قائمة ويب " .. name
    elseif event.semantic_role == "web_listitem" then
      text = "عنصر قائمة ويب " .. name
    elseif event.semantic_role == "web_dialog" then
      text = "حوار ويب " .. name
    elseif event.semantic_role == "web_landmark" then
      text = "معلم صفحة " .. name
    end
  end

  if text == nil and event.role == "button" then
    text = "زر " .. name
  elseif text == nil and event.role == "edit" then
    text = "حقل تحرير " .. name
    include_value = true
  elseif text == nil and event.role == "menu" then
    text = "قائمة " .. name
  elseif text == nil and event.role == "menuitem" then
    text = "عنصر قائمة " .. name
  elseif text == nil and event.role == "checkbox" then
    text = "خانة اختيار " .. name
  elseif text == nil and event.role == "radiobutton" then
    text = "زر اختيار " .. name
  elseif text == nil and event.role == "combobox" then
    text = "مربع خيارات " .. name
    include_value = true
  elseif text == nil and event.role == "tabitem" then
    text = "علامة تبويب " .. name
  elseif text == nil and event.role == "tab" then
    text = "تبويب " .. name
  elseif text == nil and event.role == "listitem" then
    text = "عنصر قائمة " .. name
  elseif text == nil and event.role == "treeitem" then
    text = "عنصر شجرة " .. name
  elseif text == nil and event.role == "group" then
    text = "مجموعة " .. name
  elseif text == nil and event.role == "pane" then
    text = "جزء " .. name
  elseif text == nil and event.role == "toolbar" then
    text = "شريط أدوات " .. name
  elseif text == nil and event.role == "statusbar" then
    text = "شريط حالة " .. name
  elseif text == nil and event.role == "document" then
    text = "مستند " .. name
  end

  if text == nil then
    text = event.role .. " " .. name
  end

  return {
    action = "speak",
    text = append_details(text, event, include_value)
  }
end
