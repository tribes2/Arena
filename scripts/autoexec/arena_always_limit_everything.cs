// This script forces a sniper limit for Arena matches.
// Arena didn't 'refund' snipers on death. I've added that. You can disable it in the preferences below.
// I've changed the sniper limit text from "for this round will be" to "for this round is" so you can tell this script is enabled.

// === Preferences follow ===
$Arena::TeamCounts = 1;// use the smallest team count as the factor 
$Arena::EnableItemLimits = 1; // change to 0 to disable this script
$Arena::RefundOnDeath = 1; // Change to 0 if you don't want teams to get laser refunds when someone with a laser dies
  
// note per team , playerCountMin is the enable point max is when it caps out  and how it scales 
// itemMin TAB itemMax TAB playerCountMin TAB playerCountMax
// $InvBanList is its own system and takes prio over this system 

// use team size numbers for min and max players
// armors
$ArenaLimits["Light"] = 0;
$ArenaLimits["Medium"] = 0; 
$ArenaLimits["Heavy"] = 1 TAB 4 TAB 6 TAB 16; 

// weapons 
$ArenaLimits["Blaster"] = 0;
$ArenaLimits["Plasma"] = 0;
$ArenaLimits["Chaingun"] = 0;
$ArenaLimits["Disc"] = 0;
$ArenaLimits["GrenadeLauncher"] = 0;
$ArenaLimits["SniperRifle"] = 1 TAB 4 TAB 4 TAB 16; 
$ArenaLimits["ELFGun"] = 0;
$ArenaLimits["Mortar"] = 0;
$ArenaLimits["MissileLauncher"] = 0;
$ArenaLimits["ShockLance"] = 0;

$ArenaLimits["EnergyPack"] = 0;
$ArenaLimits["RepairPack"] = 0;
$ArenaLimits["ShieldPack"] = 0;
$ArenaLimits["CloakingPack"] = 0;
$ArenaLimits["SensorJammerPack"] = 0;
$ArenaLimits["AmmoPack"] = 0;
$ArenaLimits["SatchelCharge"] = 0;

$ArenaLimits["Grenade"] = 0;
$ArenaLimits["FlashGrenade"] = 0;
$ArenaLimits["ConcussionGrenade"] = 0;
$ArenaLimits["FlareGrenade"] = 0;
$ArenaLimits["Mine"] = 0;
// === Preferences end ===

// tribes 2: travels timeless
// v1: 23/01/2024
// loop@tribesforever.com

//Expanded to all weapons and things
//v2: DarkTiger 25/03/2025

function mClamp(%value, %min, %max){
   if (%value < %min)
      return %min;
   if (%value > %max)
      return %max;
   return %value;
}

function mLerp(%a, %b, %t){
   return %a + (%b - %a) * %t;
}


function testWepLerp(%plc, %field){//0 baned or not used -1 disabled  > 0  is item limit
   if(getFieldCount(%field) > 0){
      %minItem = getField(%field, 0);
      %maxItem = getField(%field, 1);
      %minPlayer = getField(%field, 2);
      %maxPlayer = getField(%field, 3);
      %lerpFactor = mClamp((%plc - %minPlayer) / (%maxPlayer - %minPlayer), 0, 1);
      %engPT = mFloor(mLerp(%minItem, %maxItem, %lerpFactor));
      %engPT = %plc >= %minPlayer ? %engPT : -1;// -1 is disabled  else are current limit 
      error(%engPT);
      return;
   }
   error(%engPT);
   return;//0 is not in are list 
} 

function wepLerp(%item){//0 baned or not used -1 disabled  > 0  is item limit
   if(getFieldCount($ArenaLimits[%item]) > 0 && !$InvBanList[Arena, %item]){
      %plc = $ArenaPLC;
      %minItem = getField($ArenaLimits[%item], 0);
      %maxItem = getField($ArenaLimits[%item], 1);
      %minPlayer = getField($ArenaLimits[%item], 2);
      %maxPlayer = getField($ArenaLimits[%item], 3);
      %lerpFactor = mClamp((%plc - %minPlayer) / (%maxPlayer - %minPlayer), 0, 1);
      %engPT = mFloor(mLerp(%minItem, %maxItem, %lerpFactor));
      %engPT = %plc >= %minPlayer ? %engPT : -1;// -1 is disabled  else are current limit 
      return %engPT;
   }
   return 0;//0 is not in are list 
}

function GameConnection::hasItem(%this,%item){
   if(%this.player.getArmorSize() $= %item){
      return true;  
   }
   return (%this.player.getInventory(%item) > 0);
}
function buildWildWepCount(){ // how many weapons and items are in the wild 
   deleteVariables("$wildCounts::*");
   for(%i = 0; %i < ClientGroup.getCount(); %i++){
      %client = ClientGroup.getObject(%i);
      if(isObject(%client.player)){
         $wildCounts::WepItems[%client.player.getArmorSize(),%client.team]++;
         for(%y = 0; $InvWeapon[%y] !$= ""; %y++){
            %WInvy = $NameToInv[$InvWeapon[%y]];
            if(%client.hasItem(%WInvy)){
               $wildCounts::WepItems[%WInvy,%client.team]++;
            }
         }
         for ( %y = 0; $InvPack[%y] !$= ""; %y++ ){
            %PInv = $NameToInv[$InvPack[%y]];
            if(%client.hasItem(%PInv)){
               $wildCounts::WepItems[%PInv,%client.team]++;
            }
         }
         for ( %y = 0; $InvGrenade[%y] !$= ""; %y++ ) {    
            %GInv = $NameToInv[$InvGrenade[%y]];
            if(%client.hasItem(%GInv)){
               $wildCounts::WepItems[%GInv,%client.team]++;
            }  
         }
         for ( %y = 0; $InvMine[%y] !$= "" ; %y++ ){   
            %MInv = $NameToInv[$InvMine[%y]];
            if(%client.hasItem(%MInv)){
               $wildCounts::WepItems[%MInv,%client.team]++;
            }  
         }
      }
   }
}

package ArenaGame_loop_sniperlimit {
    function ArenaGame::startRound( %game ) {
      parent::startRound(%game);
      if ($Arena::EnableItemLimits != 1) {
         return;
      }
      if($Arena::TeamCounts){
         for(%i = 0; %i < ClientGroup.getCount(); %i++){
            %client = ClientGroup.getObject(%i);
            %team[%client.team]++;
         }
         %minCount = 128;
         for( %i = 1; %i <= Game.numTeams; %i++ ){
             if ( %team[%i] < %minCount ){
               %minCount = %team[%i];
             }
         }
         error("smallest player count" SPC %minCount);
         $ArenaPLC = %minCount;
      }
      else{
         $ArenaPLC = mFloor(ClientGroup.getCount() /  Game.numTeams);// update are player count for the round
      }
      for(%y = 0; $InvArmor[%y] !$= ""; %y++){
         %AInvy = $NameToInv[$InvArmor[%y]];
         if($Arena::Pref::RestrictArmor == 0 || ($Arena::Pref::RestrictArmor == 1 && (%AInvy $= "Light" || %AInvy $= "Medium"))){
            %count = wepLerp(%AInvy);
            if(%count > 0){
               MessageAll( 0, '\c1The %1 purchase limit for this round is %2.',$InvArmor[%y], %count);
            }
            else if(%count == -1){
               MessageAll( 0, '\c1The %1 is disabled this round do to player count.',$InvArmor[%y], %count);
            }
         }
      }
      // Tell players what the NEW laser limit is. 2024 is new for a game made in 2001
      for(%y = 0; $InvWeapon[%y] !$= ""; %y++){
         %WInvy = $NameToInv[$InvWeapon[%y]];
         %count = wepLerp(%WInvy);
         if(%count > 0){
            MessageAll( 0, '\c1The %1 purchase limit for this round is %2.',$InvWeapon[%y], %count);
         }
         else if(%count == -1){
            MessageAll( 0, '\c1The %1 is disabled this round do to player count.',$InvWeapon[%y], %count);
         }
      }
      for ( %y = 0; $InvPack[%y] !$= ""; %y++ ){
         %PInv = $NameToInv[$InvPack[%y]];
         %count = wepLerp(%PInv);
         if(%count > 0){
            MessageAll( 0, '\c1The %1 purchase limit for this round is %2.',$InvPack[%y], %count);
         }
         else if(%count == -1){
            MessageAll( 0, '\c1The %1 is disabled this round do to player count.',$InvPack[%y], %count);
         }
      }
      for ( %y = 0; $InvGrenade[%y] !$= ""; %y++ ) {    
         %GInv = $NameToInv[$InvGrenade[%y]];
         %count = wepLerp(%GInv);
         if(%count > 0){
            MessageAll( 0, '\c1The %1 purchase limit for this round is %2.',$InvGrenade[%y], %count);
         }
         else if(%count == -1){
            MessageAll( 0, '\c1The %1 is disabled this round do to player count.',$InvGrenade[%y], %count);
         }
      }
      for ( %y = 0; $InvMine[%y] !$= "" ; %y++ ){   
         %MInv = $NameToInv[$InvMine[%y]];
         %count = wepLerp(%MInv);
         if(%count > 0){
            MessageAll( 0, '\c1The %1 purchase limit for this round is %2.',$InvMine[%y], %count);
         }
         else if(%count == -1){
            MessageAll( 0, '\c1The %1 is disabled this round do to player count.',$InvMine[%y], %count);
         }
      }
   }

   function ArenaGame::stationOnEnterTrigger( %game, %data, %obj, %colObj ) {
      %ret = parent::stationOnEnterTrigger(%game, %data, %obj, %colObj);
      if (!%ret) {
         return false;
      } 
      %client = %colObj.client;
      if ($Arena::EnableItemLimits == 1) {
         for ( %i = 0; %i < getFieldCount( %client.curFavList ); %i++ ) {//curFavList is the list they are going to buy`
            %name = getField( %client.curFavList, %i );
            if(%name !$= "armor" && %name !$= "weapon" && %name !$= "pack" && %name !$= "grenade" && %name !$= "armor"){
               %inv = $NameToInv[%name];
               if (getFieldCount($ArenaLimits[%inv]) > 1 && !%client.hasItem(%inv)) {// is this a restricted item and does not have item
                  %itemLimit = wepLerp(%inv);
                  if($wildCounts::WepItems[%inv, %client.team] >= %itemLimit){// check team counts
                     // They can't use the station
                     if(%itemLimit != -1){// Client wants item, no itemss left, client has no item already
                        messageClient( %client, 0, '\c2Access Denied -- There are no more %1 available (maximum is %2).~wfx/powered/station_denied.wav', %name, %itemLimit);
                        return false;
                     }
                     else{
                         messageClient( %client, 0, '\c2Access Denied -- %1 is currently disabled do to player count.~wfx/powered/station_denied.wav', %name);
                        return false;    
                     }
                  }
               }
            }
         }
      }
      return true;
   }

    function StationInventory::stationReady(%data, %obj) {
        parent::stationReady(%data, %obj);
        if ($Arena::EnableItemLimits){
            buildWildWepCount();// update are counts just easy to count all then trying to figure out what player all got rid of and now has 
        }
    }

    function ArenaGame::onClientKilled(%game, %clVictim, %clKiller, %damageType, %implement, %damageLocation) {
        parent::onClientKilled(%game, %clVictim, %clKiller, %damageType, %implement, %damageLocation);
        if ($Arena::EnableItemLimits && $Arena::RefundOnDeath) {
            buildWildWepCount();
        }
    }

    function ArenaGame::setupLaserLimit( %game ) {
        // Don't call the parent to avoid duplicate (potentially conflicting) messages about laser purchase limits.
        if ($Arena::EnableItemLimits == 1) {
            return;
        }
        parent::setupLaserLimit();
    }
};
activatePackage(ArenaGame_loop_sniperlimit);
