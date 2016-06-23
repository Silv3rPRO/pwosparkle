name = "Leveling: Route 30 (near Cherrygrove)"
author = "Silv3r"

function onPathAction()
	if getPokemonHealth(1) > 0 then
		if getMapName() == "Pokecenter A" then
			moveToCell(8, 14)
			waitForTeleportation()
		elseif getMapName() == "Cherrygrove City" then
			moveToRectangle(37, 12, 38, 12)
			waitForTeleportation()
		elseif getMapName() == "Route 30" then
			moveToRectangle(21, 57, 26, 58)
		end
	else
		if getMapName() == "Route 30" then
			moveToRectangle(19, 63, 20, 63)
			waitForTeleportation()
		elseif getMapName() == "Cherrygrove City" then
			moveToCell(51, 21)
			waitForTeleportation()
		elseif getMapName() == "Pokecenter A" then
			talkToNpcOnCell(8, 10)
		end
	end
end

function onBattleAction()
	attack()
end
