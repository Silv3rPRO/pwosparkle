name = "Leveling: Route 22 (near Viridian)"
author = "Silv3r"

function onPathAction()
	if getPokemonHealth(1) > 0 then
		if getMapName() == "Pokecenter - Viridian" then
			moveToCell(8, 14)
			waitForTeleportation()
		elseif getMapName() == "Viridian City" then
			moveToRectangle(14, 25, 14, 28)
			waitForTeleportation()
		elseif getMapName() == "Route 22" then
			moveToRectangle(44, 15, 49, 19)
		end
	else
		if getMapName() == "Route 22" then
			moveToRectangle(59, 12, 59, 15)
			waitForTeleportation()
		elseif getMapName() == "Viridian City" then
			moveToCell(40, 35)
			waitForTeleportation()
		elseif getMapName() == "Pokecenter - Viridian" then
			talkToNpcOnCell(8, 10)
		end
	end
end

function onBattleAction()
	attack()
end
