local previous_handler = on_focus_changed

function on_focus_changed(event)
  if event.context_kind == "browser" then
    if event.semantic_role == "web_link" then
      return {
        action = "speak",
        text = "رابط في Edge " .. event.name
      }
    end

    if event.semantic_role == "web_heading" then
      return {
        action = "speak",
        text = "عنوان صفحة " .. event.name
      }
    end

    if event.semantic_role == "web_document" then
      return {
        action = "speak",
        text = "مستند ويب " .. event.name
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
