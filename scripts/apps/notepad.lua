local previous_handler = on_focus_changed

function on_focus_changed(event)
  if event.process == "notepad" or event.process == "notepad++" then
    if event.role == "edit" then
      return {
        action = "speak",
        text = "محرر نص " .. event.name
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
