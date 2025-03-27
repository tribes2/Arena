$Arena::giveMax = 3;
$Arena::giveMin = 0;
 package arenaStuff{
   function Beacon::onUse(%data, %obj){
      %pos = %obj.getMuzzlePoint(0);
      %vec = %obj.getMuzzleVector(0);
      %raycast = containerRayCast(%pos, VectorAdd(%pos, VectorScale(VectorNormalize(%vec), 8)), $TypeMasks::PlayerObjectType, %obj);
      if(%raycast){
         %tgClient = getWord(%raycast,0).client;
         if(isObject(%tgClient) && %tgClient.team == %obj.client.team){
            %usesLeft = $Arena::Pref::StationLimit - %obj.client.stationUseCount;
            %targetUsesLeft = $Arena::Pref::StationLimit - %tgClient.stationUseCount;
            if(%targetUsesLeft < $Arena::giveMax && %usesLeft > $Arena::giveMin){
               %tgClient.stationUseCount--;
               %obj.client.stationUseCount++;
               messageClient(%tgClient, 'MsgInvyGive', '\c2%1 gave you an extra station use.~wgui/objective_notification.wav',%obj.client.name );
               messageClient(%obj.client, 'MsgInvyGive', '\c2You gave %1 an extra station use too.~wgui/objective_notification.wav',%tgClient.name );
               bottomprint(%tgClient,  getTaggedString(%obj.client.name) SPC "gave you an extra station use.", 4, 1);
            }
            else{
               if(%targetUsesLeft >= $Arena::giveMax && !%usesLeft <= $Arena::giveMin){
                  messageClient(%obj.client, 'MsgInvyGive', '\c2Has max staton uses already and no station uses to give.~wfx/misc/misc.error.wav');   
               }
               else if(%targetUsesLeft >= $Arena::giveMax){
                  messageClient(%obj.client, 'MsgInvyGive', '\c2Has max staton uses already.~wfx/misc/misc.error.wav');    
               }
               else if(!%usesLeft <= $Arena::giveMin){
                  messageClient(%obj.client, 'MsgInvyGive', '\c2No station uses to give.~wfx/misc/misc.error.wav');  
               }
            }
         }
         return;
      }
      parent::onUse(%data, %obj);
   }
};
if (!isActivePackage(arenaStuff)){
activatePackage(arenaStuff);
}
