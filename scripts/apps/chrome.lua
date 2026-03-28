local previous_handler = on_focus_changed

function on_focus_changed(event)
  if event.context_kind == "browser" then
    if event.semantic_role == "web_link" then
      return {
        action = "speak",
        text = "رابط ويب " .. event.name
      }
    end

    if event.semantic_role == "web_heading" then
      return {
        action = "speak",
        text = "عنوان في الصفحة " .. event.name
      }
    end

    if event.semantic_role == "web_edit" then
      return {
        action = "speak",
        text = "حقل إدخال ويب " .. event.name
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
