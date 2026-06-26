global c, gLOprops

on exitFrame(me)
  if (checkMinimize()) then
    _player.appMinimize()
  end if
  if (checkExit()) then
    _player.quit()
  end if
  if (checkExitRender()) then
    _movie.go(9)
  end if
  cols = gLOprops.size.loch
  rows = gLOprops.size.locv
  repeat with q = 0 to 29
    strq = string(q)
    member("layer" & strq).image = image(800+cols*20, 400+rows*20, 32)
    member("gradientA" & strq).image = image(800+cols*20, 400+rows*20, 16)
    member("gradientB" & strq).image = image(800+cols*20, 400+rows*20, 16)
    member("layer" & strq & "dc").image = image(800+cols*20, 400+rows*20, 32)
  end repeat
  member("rainBowMask").image = image(800+cols*20, 400+rows*20, 32)
  renderLevel()
  c = 1
end