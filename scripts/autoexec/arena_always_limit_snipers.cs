// This script forces a sniper limit for Arena matches.
// Arena didn't 'refund' snipers on death. I've added that. You can disable it in the preferences below.
// I've changed the sniper limit text from "for this round will be" to "for this round is" so you can tell this script is enabled.

// === Preferences follow ===

$Arena::EnableLaserLimit = 1; // change to 0 to disable this script
$Arena::MaxLasersPerTeam = 2; // change to whatever you want. dunno if you can change it before the next round, haven't tried
$Arena::RefundLaserOnDeath = 1; // Change to 0 if you don't want teams to get laser refunds when someone with a laser dies

// === Preferences end ===

// tribes 2: travels timeless
// v1: 23/01/2024
// loop@tribesforever.com
package ArenaGame_loop_sniperlimit {
    function ArenaGame::startRound( %game ) {
        parent::startRound(%game);
        if ($Arena::EnableLaserLimit != 1) {
            return;
        }
        // Overwrite the default laser limit system and disable it
        %game.arenaLaserLimit = 0;

        // Reset all team laser trackers
        for ( %iTeam = 1; %iTeam <= Game.numTeams; %iTeam++ ) {
            %game.arenaLasersAvailable[%iTeam] = $Arena::MaxLasersPerTeam;
            echo(%iTeam @ ": laser# " @ %game.arenaLasersAvailable[%iTeam]);
        }
            
        // Tell players what the NEW laser limit is. 2024 is new for a game made in 2001
        MessageAll( 0, "\c1The laser purchase limit for this round is " @ $Arena::MaxLasersPerTeam @ "." );
    }

    function ArenaGame::stationOnEnterTrigger( %game, %data, %obj, %colObj ) {
        $ok = parent::stationOnEnterTrigger(%game, %data, %obj, %colObj);
        %client = %colObj.client;
        if ($ok == false) {
            return false;
        } else if ($Arena::EnableLaserLimit == 1 && Game.arenaLasersAvailable[%client.team] < 1) {
            //echo("STATION ENTER TRIGGER_CHECKED: t: " @ %client.team @ " l: " @ Game.arenaLasersAvailable[%client.team]);
            for ( %iWep = 0; %iWep < getFieldCount( %client.weaponIndex ); %iWep++ ) {
                %inv = $NameToInv[%client.favorites[getField( %client.weaponIndex, %iWep )]];
                if ( %inv $= "SniperRifle" )  {
                    // Check if they're carrying a rifle already
                    // (don't want to prevent reloading of snipers or returning of rifles)
                    if ( %client.player.getInventory(SniperRifle) < 1 ) {
                        // Client wants rifle, no rifles left, client has no rifle already
                        // They can't use the station
                        messageClient( %client, 0, "\c2Access Denied -- There are no more sniper rifles available (maximum is "@ $Arena::MaxLasersPerTeam @"). Choose a loadout without a rifle.~wfx/powered/station_denied.wav" );
                        return false;
                    }
                }
            }
            return true;
        } else {
            //echo("STATION ENTER TRIGGER: " @ Game.arenaLasersAvailable[%client.team]);
            return true;
        }
    }

    function StationInventory::stationReady(%data, %obj) {
        %player = %obj.triggeredBy;
        %client = %player.client;

        // Implement tourney mode laser rifle limit

        if ( $Arena::EnableLaserLimit && $Arena::MaxLasersPerTeam > 0 )
        {
            // If the client is entering with a laser rifle increase the available lasers
            if ( %client.player.getInventory(SniperRifle) > 0 ) {
                Game.arenaLasersAvailable[%client.team]++;
                //messageClient(%client, 0, "You already have a sniper, apparently, so we're increasing the count: " @ Game.arenaLasersAvailable[%client.team] @ " snipers left.");
            }

            // If the client is buying a laser rifle decrement the available lasers
            for ( %iWep = 0; %iWep < getFieldCount( %client.weaponIndex ); %iWep++ )
            {
                %inv = $NameToInv[%client.favorites[getField( %client.weaponIndex, %iWep )]];

                if ( %inv $= "SniperRifle" ) {
                    //messageClient(%client, 0, "Your team has " @ Game.arenaLasersAvailable[%client.team] @ " snipers left.");
                    Game.arenaLasersAvailable[%client.team]--;
                    //messageClient(%client, 0, "Your team " @ Game.arenaLasersAvailable[%client.team] @ " snipers left.");
                }
            }
        }
        parent::stationReady(%data, %obj);
    }

    function ArenaGame::onClientKilled(%game, %clVictim, %clKiller, %damageType, %implement, %damageLocation) {
        parent::onClientKilled(%game, %clVictim, %clKiller, %damageType, %implement, %damageLocation);
        if ($Arena::EnableLaserLimit != 1) {
            return;
        }
        if ($Arena::RefundLaserOnDeath != 1) {
            //echo("Refusing to refund - feature disabled");
            return;
        }
        if ( %clVictim.player.getInventory(SniperRifle) < 1 ) {
            //MessageAll( 0, "\c1Sniper client died, so team " @ %clVictim.team @ " gets another sniper." );
            Game.arenaLasersAvailable[%clVictim.team]++;
        }
    }

    function ArenaGame::setupLaserLimit( %game ) {
        // Don't call the parent to avoid duplicate (potentially conflicting) messages about laser purchase limits.
        if ($Arena::EnableLaserLimit == 1) {
            return;
        }
        parent::setupLaserLimit();
    }
};
activatePackage(ArenaGame_loop_sniperlimit);
