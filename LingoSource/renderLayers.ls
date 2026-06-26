global c

on exitFrame(me)
  global gLOprops
  if (checkMinimize()) then
    _player.appMinimize()
  end if
  if (checkExit()) then
    _player.quit()
  end if
  if (checkExitRender()) then
    _movie.go(9)
  end if
  lvlW = (gLOprops.size.locH + 30) * 20
  lvlH = (gLOprops.size.locV + 20) * 20
  repeat with q = 0 to 29
    strq = string(q)
    member("layer" & strq).image = image(lvlW, lvlH, 32)
    member("gradientA" & strq).image = image(lvlW, lvlH, 16)
    member("gradientB" & strq).image = image(lvlW, lvlH, 16)
    member("layer" & strq & "dc").image = image(lvlW, lvlH, 32)
  end repeat
  member("rainBowMask").image = image(lvlW, lvlH, 32)
  renderLevel()
  c = 1
end