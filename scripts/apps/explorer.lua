local previous_handler = on_focus_changed

function on_focus_changed(event)
  if event.process == "explorer" then
    if event.role == "listitem" then
      return {
        action = "speak",
        text = "عنصر ملف " .. event.name
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
