function on_focus_changed(event)
  if event.role == "button" then
    return {
      action = "speak",
      text = "زر " .. event.name
    }
  end

  if event.role == "edit" then
    return {
      action = "speak",
      text = "حقل تحرير " .. event.name
    }
  end

  return {
    action = "speak",
    text = event.role .. " " .. event.name
  }
end
