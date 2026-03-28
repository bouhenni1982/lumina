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
  elseif text == nil and event.role == "listitem" then
    text = "عنصر قائمة " .. name
  elseif text == nil and event.role == "treeitem" then
    text = "عنصر شجرة " .. name
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
