$Host::AllowAdmin2Admin = $Host::AllowAdmin2Admin $= "" ? ($Host::AllowAdmin2Admin = 0) : $Host::AllowAdmin2Admin;
$Host::AllowAdminBan = $Host::AllowAdminBan $= "" ? ($Host::AllowAdminBan = 0) : $Host::AllowAdminBan;
$Host::AllowAdminKick = $Host::AllowAdminKick $= "" ? ($Host::AllowAdminKick = 1) : $Host::AllowAdminKick;
$Host::AllowAdminVotes = $Host::AllowAdminVotes $= "" ? ($Host::AllowAdminVotes = 1) : $Host::AllowAdminVotes;
$Host::AllowAdminStopVote = $Host::AllowAdminStopVote $= "" ? ($Host::AllowAdminStopVote = 1) : $Host::AllowAdminStopVote;
$Host::AllowAdminPassVote = $Host::AllowAdminPassVote $= "" ? ($Host::AllowAdminPassVote = 1) : $Host::AllowAdminPassVote;
$Host::AllowPlayerVoteChangeMission = $Host::AllowPlayerVoteChangeMission $= "" ? ($Host::AllowPlayerVoteChangeMission = 1) : $Host::AllowPlayerVoteChangeMission;
$Host::AllowPlayerVoteSkipMission = $Host::AllowPlayerVoteSkipMission $= "" ? ($Host::AllowPlayerVoteSkipMission = 1) : $Host::AllowPlayerVoteSkipMission;
$Host::AllowPlayerVoteTimeLimit = $Host::AllowPlayerVoteTimeLimit $= "" ? ($Host::AllowPlayerVoteTimeLimit = 1) : $Host::AllowPlayerVoteTimeLimit;
$Host::AllowPlayerVoteTournamentMode = $Host::AllowPlayerVoteTournamentMode $= "" ? ($Host::AllowPlayerVoteTournamentMode = 1) : $Host::AllowPlayerVoteTournamentMode;
$Host::AllowPlayerVoteTeamDamage = $Host::AllowPlayerVoteTeamDamage $= "" ? ($Host::AllowPlayerVoteTeamDamage = 0) : $Host::AllowPlayerVoteTeamDamage;
$Host::AllowPlayerVoteNextMission = $Host::AllowPlayerVoteNextMission $= "" ? ($Host::AllowPlayerVoteNextMission = 1) : $Host::AllowPlayerVoteNextMission;
//Vote Delay
//Delay the ability to vote (For everyone) at the beginning of the match
$Host::VoteDelayTime = $Host::VoteDelayTime $= "" ? ($Host::VoteDelayTime = 120) : $Host::VoteDelayTime;
//Vote Cooldown
//Time cooldown that dosnt allow a player to vote again after theyve initiated a vote
$Host::VoteCooldown = $Host::VoteCooldown $= "" ? ($Host::VoteCooldown = 120) : $Host::VoteCooldown;

$Host::ClassicMaxVotes = $Host::ClassicMaxVotes $= "" ? ($Host::ClassicMaxVotes = 1) : $Host::ClassicMaxVotes;
$Host::VoteNextPlayerLimit = $Host::VoteNextPlayerLimit $= "" ? ($Host::VoteNextPlayerLimit = 4) : $Host::VoteNextPlayerLimit;
$Host::PUGPassword = $Host::PUGPassword $= "" ? ($Host::PUGPassword = "shazbot") : $Host::PUGPassword;
$Host::EnableVoteSoundReminders = $Host::EnableVoteSoundReminders $= "" ? ($Host::EnableVoteSoundReminders = 3) : $HostEnableVoteSoundReminders;
$Host::voteMenuLog = $Host::voteMenuLog $= "" ? ($Host::voteMenuLog = 0) : $Host::voteMenuLog; 
$Host::AllowAdminSwitchTeams = $Host::AllowAdminSwitchTeams $= "" ? ($Host::AllowAdminSwitchTeams = 1) : $Host::AllowAdminSwitchTeams; 
$Host::voteParticipationPct = $Host::voteParticipationPct  $= "" ? ($Host::voteParticipationPct  = 0.5) : $Host::voteParticipationPct ; 

package ExtraVoteMenu
{

function DefaultGame::evalVote(%game, %typeName, %admin, %arg1, %arg2, %arg3, %arg4)
{
   switch$ (%typeName)
   {
      case "cancelMatchStart":
         %game.cancelMatchStart(%admin, %arg1, %arg2, %arg3, %arg4);
      case "VoteNextMission":
          %game.setNextMission(%admin, %arg1, %arg2, %arg3, %arg4);
      case "voteSkipMission":
         %game.voteSkipMission(%admin, %arg1, %arg2, %arg3, %arg4);
      default:
         Parent::evalVote(%game, %typeName, %admin, %arg1, %arg2, %arg3, %arg4);
   }
}
function DefaultGame::voteSkipMission(%game, %admin, %arg1, %arg2, %arg3, %arg4)
{
   if(%admin) 
   {
      messageAll('MsgAdminForce', '\c2The Admin %1 has skipped to the next mission.',$AdminCl.name );
      echo($AdminCl.nameBase @ ": mission skipped (admin)");
      %game.gameOver();
      //loadMission( findNextCycleMission(), $CurrentMissionType, false );
      cycleMissions();
   }
   else
   {
      %totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
      // Added people who dont vote into the equation, now if you do not vote, it doesn't count as a no. - z0dd - ZOD
      if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone)) > ($Host::VotePasspercent / 100))
      {
         messageAll('MsgVotePassed', '\c2The mission was skipped to next by vote.'); 
         echo("mission skipped (vote)");
         %game.gameOver();
         //loadMission( findNextCycleMission(), $CurrentMissionType, false );
         cycleMissions();   
      }
      else
         messageAll('MsgVoteFailed', '\c2Skip mission vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone) * 100));
   }
}

function DefaultGame::sendGameVoteMenu(%game, %client, %key)
{
   %isAdmin = (%client.isAdmin || %client.isSuperAdmin);
   %multipleTeams = %game.numTeams > 1;
   %client.k = %key; //For set next mission

   if(%client.ForceVote > 0)
      %client.ForceVote = %client.ForceVote - 1;
   if(%client.ForceVote $= "skip_confirm+")
      %client.ForceVote = 0;

   //Admin Submenu
   if(%client.ForceVote > 0)
   {
      if(!$Host::TournamentMode)
         messageClient(%client, 'MsgVoteItem', "", %key, 'VoteTournamentMode', 'change server to Tournament.', 'Vote Tournament Mode');
         messageClient(%client, 'MsgVoteItem', "", %key, 'VoteChangeMission', 'change the mission to', 'Vote to Change the Mission');
         messageClient(%client, 'MsgVoteItem', "", %key, 'VoteNextMission', 'set next mission to', 'Vote to Set the Next Mission');
         messageClient(%client, 'MsgVoteItem', "", %key, 'VoteChangeTimeLimit', 'change the time limit', 'Vote to Change the Time Limit');
         messageClient(%client, 'MsgVoteItem', "", %key, 'VoteSkipMission', 'skip the mission to', 'Vote to Skip Mission' );
      if(%multipleTeams)
      {
         if($teamDamage)
            messageClient(%client, 'MsgVoteItem', "", %key, 'VoteTeamDamage', 'disable team damage', 'Vote to Disable Team Damage');
         else
            messageClient(%client, 'MsgVoteItem', "", %key, 'VoteTeamDamage', 'enable team damage', 'Vote to Enable Team Damage');
      }
      messageClient(%client, 'MsgVoteItem',"",  %key, 'ForceVote', 'Cancel Force Vote', "Cancel 'Vote To...'");
      return;
   }
   else if (%client.ForceVote $= "skip_confirm") //Skip mission Confirm cuz its so annoying
   {
      messageClient(%client, 'MsgVoteItem', "", %key, 'VoteSkipConfirm', 'Are you sure you want to Skip the Mission?','Are you sure you want to Skip the Mission?');
      messageClient(%client, 'MsgVoteItem', "", %key, 'VoteSkipMission', 'Yes, Skip the Mission', 'Yes, Skip the Mission' );
      %client.ForceVote = "skip_confirm+";
      return;
   }

   //Pass Stop Menu
   if(%client.isAdmin && %game.scheduleVote !$= "")
   {
      if(%client.isSuperAdmin || (%client.isAdmin && $Host::AllowAdminStopVote)) // allow admins to stop votes
         messageClient( %client, 'MsgVoteItem', "", %key, 'stopRunningVote', 'stop current vote', 'Stop the Vote');
      if(%client.isSuperAdmin || (%client.isAdmin && $Host::AllowAdminPassVote)) // allow admins to pass votes
         messageClient( %client, 'MsgVoteItem', "", %key, 'passRunningVote', 'pass current vote', 'Pass the Vote');
      return;
   }

   //Mission Info Header - Mission Name, Type, Caps to Win
   if(%client.canVote && %game.scheduleVote $= "")
   {
      switch$($CurrentMissionType)
      {
			case CTF or SCtF or LCTF:
				if($Host::TournamentMode)
               %showTL = " - Time Limit:" SPC $Host::TimeLimit SPC "Minutes";
            if($voteNext)
               %showNM = " - Next Map:" SPC $HostMissionName[$voteNextMap] SPC "(" @ $HostTypeName[$voteNextType] @ ")";
            messageClient(%client, 'MsgVoteItem', "", %key, '', $MissionDisplayName SPC "(" @ $MissionTypeDisplayName @ "):" SPC MissionGroup.CTF_scoreLimit SPC "Caps to Win",
            $MissionDisplayName SPC "(" @ $MissionTypeDisplayName @ "):" SPC MissionGroup.CTF_scoreLimit SPC "Caps to Win" @ %showTL @ %showNM);
         case LakRabbit:
            %cap = "2000 Points to Win";
            messageClient(%client, 'MsgVoteItem', "", %key, '', $MissionDisplayName SPC "(" @ $MissionTypeDisplayName @ "):" SPC %cap,
            $MissionDisplayName SPC "(" @ $MissionTypeDisplayName @ "):" SPC %cap);
         case DM:
            %cap = "25 Points to Win";
            messageClient(%client, 'MsgVoteItem', "", %key, '', $MissionDisplayName SPC "(" @ $MissionTypeDisplayName @ "):" SPC %cap,
            $MissionDisplayName SPC "(" @ $MissionTypeDisplayName @ "):" SPC %cap);
      }
   }

   // TEAM OPTIONS
   if(%client.team != 0) //he isn't an observer
   {
      if(!%client.isAdmin && !$Host::TournamentMode)
      {
         if(%multipleTeams)
            messageClient(%client, 'MsgVoteItem', "", %key, 'ChooseTeam', "", 'Change your Team');
         if($MatchStarted)
            messageClient(%client, 'MsgVoteItem', "", %key, 'MakeObserver', "", 'Become an Observer');
      }
      else if(%client.isAdmin) //only admins can change team during tournament mode
      {
         if(%multipleTeams)
            messageClient(%client, 'MsgVoteItem', "", %key, 'ChooseTeam', "", 'Change your Team');
         messageClient(%client, 'MsgVoteItem', "", %key, 'MakeObserver', "", 'Become an Observer');
      }
   }

   //Beginning match Vote Delay
   if(!%client.isAdmin && (getSimTime() - $VoteDelay) < ($Host::VoteDelayTime * 1000))
         return;

   if(!%client.canVote && !%isAdmin)
      return;

   //Standard Vote Options
   if(%game.scheduleVote $= "")
   {
      //Start Match
      if(!$MatchStarted && !$CountdownStarted && $Host::TournamentMode)
      {
         if(!%client.isAdmin)
            messageClient(%client, 'MsgVoteItem', "", %key, 'VoteMatchStart', 'Start Match', 'Vote to Start the Match');
         else
            messageClient(%client, 'MsgVoteItem', "", %key, 'VoteMatchStart', 'Start Match', 'Start Match');
      }
      //Cancel Match Start
      if(!$MatchStarted && $CountdownStarted && $Host::TournamentMode && %client.isAdmin)
         messageClient(%client, 'MsgVoteItem', "", %key, 'cancelMatchStart', 'Cancel Match Start', 'Cancel Match Start');
      //Vote Free For All Mode
      if($Host::TournamentMode)
      {
         if(!%client.isAdmin)
               messageClient(%client, 'MsgVoteItem', "", %key, 'VoteFFAMode', 'Change server to Free For All.', 'Vote Free For All Mode');
         else
            messageClient(%client, 'MsgVoteItem', "", %key, 'VoteFFAMode', 'Change server to Free For All.', 'Change FFA Mode');
      }
      //Vote Tournament Mode
      if(!$Host::TournamentMode)
      {
         if(!%client.isAdmin)
         {
            if($Host::AllowPlayerVoteTournamentMode && !$RestrictedVoting)
               messageClient(%client, 'MsgVoteItem', "", %key, 'VoteTournamentMode', 'Change server to Tournament.', 'Vote Tournament Mode');
         }
         else
            messageClient(%client, 'MsgVoteItem', "", %key, 'VoteTournamentMode', 'Change server to Tournament.', 'Tournament Mode');
      }
      //Vote Change Mission
      if(!%client.isAdmin)
      {
         if($Host::AllowPlayerVoteChangeMission && !$RestrictedVoting)
            messageClient(%client, 'MsgVoteItem', "", %key, 'VoteChangeMission', 'change the mission to', 'Vote to Change the Mission');
      }
      else
         messageClient(%client, 'MsgVoteItem', "", %key, 'VoteChangeMission', 'change the mission to', 'Change the Mission');
      //Vote Change Next Mission
      if(!%client.isAdmin)
      {
         if($Host::AllowPlayerVoteNextMission && !$RestrictedVoting)
            messageClient(%client, 'MsgVoteItem', "", %key, 'VoteNextMission', 'set next mission to', 'Vote to Set the Next Mission');
      }
      else
      {
         if(!$voteNext)
            messageClient(%client, 'MsgVoteItem', "", %key, 'VoteNextMission', 'set next mission to', 'Set the Next Mission');
         else
            messageClient(%client, 'MsgVoteItem', "", %key, 'ClearNextMap', 'Clear Next Set Map', 'Clear Next Set Map');
      }
      //Vote Change Time
      if(!%client.isAdmin)
      {
         if($Host::AllowPlayerVoteTimeLimit && !$RestrictedVoting)
            messageClient(%client, 'MsgVoteItem', "", %key, 'VoteChangeTimeLimit', 'change the time limit', 'Vote to Change the Time Limit');
      }
      else
         messageClient(%client, 'MsgVoteItem', "", %key, 'VoteChangeTimeLimit', 'change the time limit', 'Change the Time Limit');
      //Vote Skip Mission
      if(!%client.isAdmin)
      {
         if($Host::AllowPlayerVoteSkipMission && !$RestrictedVoting)
            messageClient(%client, 'MsgVoteItem', "", %key, 'VoteSkipMission', 'skip the mission to', 'Vote to Skip Mission' );
      }
      else
         messageClient(%client, 'MsgVoteItem', "", %key, 'VoteSkipConfirm', 'skip the mission to', 'Skip the Mission' );
      //Admin Vote Menu
      if(%client.isAdmin && $Host::AllowAdminVotes)
            messageClient(%client, 'MsgVoteItem', "", %key, 'ForceVote', 'Vote to ...', 'Vote to ...');
      //PUG Password
      if(%client.isAdmin && $Host::TournamentMode)
      {
         if(!$Host::PUGpasswordAlwaysOn) //Password is already set
         {
            if($Host::Password !$= "")
               messageClient(%client, 'MsgVoteItem', "", %key, 'TogglePUGpassword', 'Disable PUG Password', 'Disable PUG Password');
            else
               messageClient(%client, 'MsgVoteItem', "", %key, 'TogglePUGpassword', 'Enable PUG Password', 'Enable PUG Password');
         }
      }
      //Locked Teams
      if(%client.isAdmin && $Host::TournamentMode)
      {
         if($LockedTeams)
            messageClient(%client, 'MsgVoteItem', "", %key, 'ToggleLockedTeams', 'Disable Locked Teams', 'Disable Locked Teams');
         else
            messageClient(%client, 'MsgVoteItem', "", %key, 'ToggleLockedTeams', 'Enable Locked Teams', 'Enable Locked Teams');
      }
      //Restricted Voting
      if(%client.isAdmin && $Host::TournamentMode)
      {
         if($RestrictedVoting)
            messageClient(%client, 'MsgVoteItem', "", %key, 'ToggleRestrictedVoting', 'Disable Restricted Voting', 'Disable Restricted Voting');
         else
            messageClient(%client, 'MsgVoteItem', "", %key, 'ToggleRestrictedVoting', 'Enable Restricted Voting', 'Enable Restricted Voting');
      }
      //Vote Team Damage
      if(%multipleTeams)
      {
         if(!%client.isAdmin)
         {
            if($Host::AllowPlayerVoteTeamDamage && !$RestrictedVoting)
            {
               if($teamDamage)
                  messageClient(%client, 'MsgVoteItem', "", %key, 'VoteTeamDamage', 'disable team damage', 'Vote to Disable Team Damage');
               else
                  messageClient(%client, 'MsgVoteItem', "", %key, 'VoteTeamDamage', 'enable team damage', 'Vote to Enable Team Damage');
            }
         }
         else
         {
            if($teamDamage)
               messageClient(%client, 'MsgVoteItem', "", %key, 'VoteTeamDamage', 'disable team damage', 'Disable Team Damage');
            else
               messageClient(%client, 'MsgVoteItem', "", %key, 'VoteTeamDamage', 'enable team damage', 'Enable Team Damage');
         }
      }

      //Super Admin Only
      if(%client.isSuperAdmin)
      {
         if($Host::AllowAdmin2Admin) //Toggle Admin2Admin
            messageClient( %client, 'MsgVoteItem', "", %key, 'Admins2Admin', 'Disable Admins to Admin', "Disable Admins to Admin" );
         else
            messageClient( %client, 'MsgVoteItem', "", %key, 'Admins2Admin', 'Enable Admins to Admin', "Enable Admins to Admin" );
      }
      //Disabled for now
      // if(%client.isSuperAdmin)
      // {
      //    if($Host::EnableNetTourneyClient) //Toggle Tournament Net Client
      //       messageClient( %client, 'MsgVoteItem', "", %key, 'ToggleTourneyNetClient', 'Disable Tournament Net Client', "Disable Tournament Net Client" );
      //    else
      //       messageClient( %client, 'MsgVoteItem', "", %key, 'ToggleTourneyNetClient', 'Enable Tournament Net Client', "Enable Tournament Net Client" );
      // }

      //Everyone
      if ($Host::ServerRules[1] !$= "" )
         messageClient( %client, 'MsgVoteItem', "", %key, 'showServerRules', 'show server rules', "Show Server Rules" );
         
      if(%client.isSuperAdmin && (Game.class $= "CTFGame" || Game.class $= "SCtFGame" || Game.class $= "LCTFGame")){
         messageClient( %client, 'MsgVoteItem', "", %key, 'FlagCapLimit', 'FlagCapLimit', "Set Flag Cap Limit" );   
      }
      
      if(Game.class $= "ArenaGame" && (%client.isSuperAdmin)){
         messageClient( %client, 'MsgVoteItem', "", %key, 'ForceRoundEnd', 'ForceRoundEnd', "Force Round End" );
         messageClient( %client, 'MsgVoteItem', "", %key, 'SetRoundLimit', 'RoundLimit', "Set Round Limit" );
         messageClient( %client, 'MsgVoteItem', "", %key, 'SetRoundTimeLimit', 'TimeLimit', "Set Round Time Lmit" );
         if($InvBanList[Arena, "SniperRifle"]){
            messageClient( %client, 'MsgVoteItem', "", %key, 'LaserBan', 0, "Enable Lasers" );      
         }
         else{
            messageClient( %client, 'MsgVoteItem', "", %key, 'LaserBan', 1, "Disable Lasers" );      
         }
         messageClient( %client, 'MsgVoteItem', "", %key, 'StationLimit', 'StationLimit', "Set Station Limits" );   
         messageClient( %client, 'MsgVoteItem', "", %key, 'ArmorRes', 'ArmorRes', "Armor Restriction Level" ); 
      }
   }
}

// Eolk - completely re-wrote this.
function serverCmdStartNewVote(%client, %typeName, %arg1, %arg2, %arg3, %arg4, %playerVote)
{
	%isAdmin = ( %client.isAdmin || %client.isSuperAdmin );
	if(!%client.canVote && !%isAdmin)
		return;

	if(Game.scheduleVote !$= "" && (!%isAdmin || (%isAdmin && %client.ForceVote)))
	{
		messageClient(%client, 'voteAlreadyRunning', "\c2A vote is already in progress.");
      %client.ForceVote = 0;
		return;
	}

	%teamSpecific = 0;
	switch$(%typeName)
	{
		case "VoteKickPlayer":
			%curTimeLeftMS = ($Host::TimeLimit * 60 * 1000) + $missionStartTime - getSimTime();
         if(%curTimeLeftMS <= 120000 && $countdownStarted && $MatchStarted && !%isAdmin)
         {
            messageClient(%client, "", "\c2Kick votes are restricted at this time.");
				return;
         }

         if(%client == %arg1) // client is trying to votekick himself
				return; // Use the leave button instead, pal.

			if(%isAdmin) // Admin is trying to kick
			{
				if((%arg1.isAdmin && !%client.isSuperAdmin) || %arg1.isSuperAdmin) // target is an admin and kicker is just an admin, or if target is a super admin
				{
					messageClient(%client, "MsgClient", "\c2You cannot kick "@%arg1.nameBase@".");
					return;
				}
			}
			else // Player is voting to kick
			{
				if($Host::TournamentMode && $RestrictedVoting) // Dont allow Votekicks in Tournament Mode
            {
               messageClient(%client, "", "\c2No votekicks in Tournament Mode at this time.");
               return;
            }

            if(%arg1.isAdmin) // target is an admin
				{
					messageClient(%client, "MsgClient", "\c2You cannot vote to kick "@%arg1.nameBase@", "@(%arg1.sex $= "Male" ? "he" : "she")@" is an admin!");
					return;
				}

				if(%arg1.team != %client.team)
				{
					messageClient(%client, "MsgClient", "\c2Kick votes must be team based.");
					return;
				}

				if($CMHasVoted[%client.guid] >= $Host::ClassicMaxVotes && !%isAdmin) // they've voted too many times
				{
					messageClient(%client, "", "\c2You have exhausted your voting rights for this mission.");
					return;
				}

				%msg = %client.nameBase @ " initiated a vote to kick player " @ %arg1.nameBase @ ".";

				//Notify any admins on the other team
				for(%i = 0; %i < ClientGroup.getCount(); %i++)
				{
					%cl = ClientGroup.getObject(%i);
					if(%cl.isAdmin == true && %cl.team !$= %arg1.team) //Not on admins team
						messageClient(%cl, '', '\c5[A]\c0%1 initiated a vote to kick player %2 on the other team.~wgui/objective_notification.wav', %client.nameBase, %arg1.nameBase);
				}

				$CMHasVoted[%client.guid]++;
			}

			if(%arg1.team != 0)
				%teamSpecific = 1;

			Game.kickClient = %arg1;
			Game.kickClientName = %arg1.name;
			Game.kickClientNameBase = %arg1.nameBase;
			Game.kickGuid = %arg1.guid;
			Game.kickTeam = %arg1.team;

		case "VoteAdminPlayer":
			if(%arg1.isAdmin) // target is already an admin
				return; // can't vote to admin an admin!

         if(%isAdmin)
         {
            if(!%client.isSuperAdmin && !$Host::AllowAdmin2Admin)
               return;
         }
         else
         {
            if(!$host::allowadminplayervotes) // admin player votes are NOT enabled
				   return; // can't do that pal

				%msg = %client.nameBase @ " initiated a vote to admin player " @ %arg1.nameBase @ ".";
         }

		case "BanPlayer":
			if(%client.isSuperAdmin && !%arg1.isSuperAdmin) // we're a super admin, and our target isn't a super admin
			{
				ban(%arg1, %client); // ban 'em
				adminLog(%client, " has banned" SPC %arg1.nameBase @ "(" @ %arg1.guid @ ").");
			}
			return; // stop the function in its tracks

		case "VoteChangeMission":
			%curTimeLeftMS = ($Host::TimeLimit * 60 * 1000) + $missionStartTime - getSimTime();
         if(%curTimeLeftMS <= 120000 && $countdownStarted && $MatchStarted && !%isAdmin)
         {
            messageClient(%client, "", "\c2Change mission votes are restricted at this time.");
				return;
         }

         // Vote-spoof prevention right here
			%arg1 = $HostMissionFile[%arg3];
			%arg2 = $HostTypeName[%arg4];
			if(!checkMapExist(%arg1, %arg2))
				return;

			// We passed the spoof check, give it the fancy label
			%arg1 = $HostMissionName[%arg3];
			%arg2 = $HostTypeDisplayName[%arg4];
			if((!%isAdmin && $Host::AllowPlayerVoteChangeMission) || (%isAdmin && %client.ForceVote)) // not admin
			{
				if($CMHasVoted[%client.guid] >= $Host::ClassicMaxVotes && !%isAdmin) // they've voted too many times
				{
					messageClient(%client, "", "\c2You have exhausted your voting rights for this mission.");
					return;
				}

				%msg = %client.nameBase @ " initiated a vote to change the mission to " @ %arg1 @ " (" @ %arg2 @ ").";
				$CMHasVoted[%client.guid]++;
			}

		case "VoteTeamDamage":
			if((!%isAdmin && $Host::AllowPlayerVoteTeamDamage) || (%isAdmin && %client.ForceVote)) // not admin
			{
				%msg = %client.nameBase @ " initiated a vote to " @ ($TeamDamage == 0 ? "enable" : "disable") @ " team damage.";
			}

		case "VoteTournamentMode":
			if((!%isAdmin && $Host::AllowPlayerVoteTournamentMode) || (%isAdmin && %client.ForceVote))
			{
				if(getAdmin() == 0)
				{
					messageClient(%client, 'clientMsg', "There must be a server admin to play in Tournament Mode.");
					return;
				}

				%msg = %client.nameBase @ " initiated a vote to switch the server to Tournament Mode (" @ %arg1 @ ").";
			}

		case "VoteFFAMode":
			if(!%isAdmin || (%isAdmin && %client.ForceVote))
			{
				%msg = %client.nameBase @ " initiated a vote to switch the server to Free For All mode.";
			}

		case "VoteChangeTimeLimit":
			if($CMHasVoted[%client.guid] >= $Host::ClassicMaxVotes && !%isAdmin)
			{
				messageClient(%client, "", "\c2You have exhausted your voting rights for this mission.");
				return;
			}

         if($CurrentMissionType $= "Siege") // Can't change time in this one
			{
				messageClient(%client, "", "\c2Cannot change the time limit in this gametype.");
				return;
			}

         // 30 minutes Only in Tournament mode
			if($Host::TournamentMode && %arg1 !$= "30" && !%client.isSuperAdmin)
			{
				messageClient(%client, "", "\c2Only 30 minute time limit allowed in tournament mode.");
				return;
			}

			// Eolk - don't let votes/admins end the map immediately by switching the time limit below the elapsed time
			if((%arg1 * 60 * 1000) < (getSimTime() - $missionStartTime))
			{
				messageClient(%client, "", "\c2Switching to this time would cause the game to end immediately.");
				return;
			}

			if(%arg1 == $Host::TimeLimit)
			{
				messageClient(%client, "", "\c2Switching to this time wouldn't affect the time limit at all.");
				return;
			}

			//If proposed time is lower than server set or higher than unlimited
			if(%arg1 < $Host::TimeLimit || %arg1 > 999)
         {
            if(!%isAdmin)
            {
               messageClient(%client, "", "\c2Invalid time selection.");
               return;
            }
            else //is an admin
            {
               if($Host::TournamentMode) //Admins still have the option to set the time to 30 minutes in Tourney Mode
               {
                  if(%arg1 !$= "30") //30 minutes only
                  {
                     messageClient(%client, "", "\c2Invalid time selection.");
                     return;
                  }
               }
               else
               {
                  messageClient(%client, "", "\c2Invalid time selection.");
                  return;
               }
            }
         }

			//If proposed time is something other than what is selectable
			if(%arg1 !$= "30" && %arg1 !$= "45" && %arg1 !$= "60" && %arg1 !$= "75" && %arg1 !$= "90" && %arg1 !$= "180" && %arg1 !$= "360" && %arg1 !$= "999")
			{
				messageClient(%client, "", "\c2Only selectable times allowed.");
				return;
			}

			if((!%isAdmin && $Host::AllowPlayerVoteTimeLimit) || (%isAdmin && %client.ForceVote))
			{
				if(%arg1 $= "999") %time = "unlimited"; else %time = %arg1;
				%msg = %client.nameBase @ " initiated a vote to change the time limit to " @ %time SPC "minutes.";
				// VoteOvertime
				StartVOTimeVote(%game);

				$CMHasVoted[%client.guid]++;
			}

		case "VoteMatchStart":
			if(!%isAdmin)
			{
				if($MatchStarted || $CountdownStarted)
					return;

				%msg = %client.nameBase @ " initiated a vote to start the match.";
			}

	    case "CancelMatchStart":
			if(%isAdmin) // only admins can cancel match start
			{
				adminStartNewVote(%client, %typename, %arg1, %arg2, %arg3, %arg4);
				adminLog(%client, " canceled match start.");
			}

		case "VoteGreedMode":
			if($CurrentMissionType !$= "Hunters" || $CurrentMissionType !$= "TeamHunters")
				return;

			if(!%isAdmin || (%isAdmin && %client.ForceVote))
				%msg = %client.nameBase @ " initiated a vote to " @ (Game.greedMode == 0 ? "enable" : "disable") @ " greed mode.";

		case "VoteHoardMode":
			if($CurrentMissionType !$= "Hunters" || $CurrentMissionType !$= "TeamHunters")
				return;

			if(!%isAdmin || (%isAdmin && %client.ForceVote))
				%msg = %client.nameBase @ " initiated a vote to " @ (Game.hoardMode == 0 ? "enable" : "disable") @ " hoard mode.";

		case "VoteRandomTeams":
			if(!%isAdmin || (%isAdmin && %client.ForceVote))
			{
				%msg = %client.nameBase @ " initiated a vote to " @ ($RandomTeams == 0 ? "enable" : "disable") @ " random teams.";
			}

		case "VoteFairTeams":
			if(!%isAdmin || (%isAdmin && %client.ForceVote))
			{
				%msg = %client.nameBase @ " initiated a vote to " @ ($FairTeams == 0 ? "enable" : "disable") @ " fair teams.";
			}

		case "VoteSkipConfirm":
			if(%client.ForceVote $= "skip_confirm+")
            %client.ForceVote = 0;
         else if(%isAdmin && %client.ForceVote !$= "skip_confirm")
			{
            %client.ForceVote = "skip_confirm";
            return;
			}

		case "VoteSkipMission":
			if((!%isAdmin && $Host::AllowPlayerVoteSkipMission) || (%isAdmin && %client.ForceVote))
			{
				if($CMHasVoted[%client.guid] >= $Host::ClassicMaxVotes && !%isAdmin)
				{
					messageClient(%client, "", "\c2You have exhausted your voting rights for this mission.");
					return;
				}

				%msg = %client.nameBase @ " initiated a vote to skip the current mission.";
				$CMHasVoted[%client.guid]++;
			}

		case "passRunningVote":
			if (%client.isSuperAdmin || (%client.isAdmin && $Host::AllowAdminPassVote))
			{
				passCurrentVote(%client);
				adminLog(%client, " passed the vote in progress.");
            return;
			}

		case "stopRunningVote":
         if(%client.isSuperAdmin || (%client.isAdmin && $Host::AllowAdminStopVote))
         {
            if($VOStatus $="InProgress") //Dont allow a stop vote after time has expired, then no new time is set - VoteOverTime
            {
               messageClient(%client, "", "\c2Can't stop time vote after time has expired.");
               return;
            }

            stopCurrentVote(%client);
            adminLog(%client, " stopped the vote in progress.");
            return;
         }

		case "TogglePUGpassword":
			if (%client.isAdmin && $Host::TournamentMode)
			{
				if($Host::Password !$= "")
				{
				   $Host::Password = "";
				   messageAdmins('', %client.nameBase @ " has disabled the PUG password.~wfx/powered/vehicle_screen_on.wav");
				   adminLog(%client, " has disabled PUG password." );
				}
				else
				{
				   $Host::Password = $Host::PUGPassword;
				   messageAdmins('', %client.nameBase @ " has enabled the PUG password (\"" @ $Host::PUGPassword @ "\").~wfx/powered/vehicle_screen_on.wav");
				   adminLog(%client, " has enabled PUG password." );
				}
			}
			return;

		case "ToggleLockedTeams":
			if (%client.isAdmin && $Host::TournamentMode)
			{
				if(!$LockedTeams)
				{
					if(!isActivePackage(LockedTeams))
						activatePackage(LockedTeams);
					$LockedTeams = 1;
					messageAdmins('', %client.nameBase @ " has enabled Locked Teams.~wfx/powered/vehicle_screen_on.wav" );
					adminLog(%client, " has enabled Locked Teams.");
				}
				else
				{
					if(isActivePackage(LockedTeams))
						deactivatePackage(LockedTeams);
					$LockedTeams = 0;
					messageAdmins('', %client.nameBase @ " has disabled Locked Teams.~wfx/powered/vehicle_screen_on.wav" );
					adminLog(%client, " has disabled Locked Teams.");
				}
			}
			return;

      case "ToggleRestrictedVoting":
         if(%client.isAdmin && $Host::TournamentMode)
			{
				if(!$RestrictedVoting)
				{
				   $RestrictedVoting = 1;
				   messageAdmins('', %client.nameBase @ " has enabled restricted voting (No Map, Time, Kick, or Team Damage Votes).~wfx/powered/vehicle_screen_on.wav");
				   adminLog(%client, " has enabled restricted voting." );
				}
				else
				{
				   $RestrictedVoting = 0;
				   messageAdmins('', %client.nameBase @ " has disabled restricted voting.~wfx/powered/vehicle_screen_on.wav" );
				   adminLog(%client, " has disabled restricted voting." );
				}
			}
			return;

      case "Admins2Admin":
			if(%client.isSuperAdmin)
			{
				if($Host::AllowAdmin2Admin)
				{
				   $Host::AllowAdmin2Admin = 0;

				   messageAdmins('', %client.nameBase @ " has disabled Admins to Admin.~wfx/powered/vehicle_screen_on.wav" );
				   adminLog(%client, " has disabled Admins to Admin.");
				}
				else
				{
				   $Host::AllowAdmin2Admin = 1;

				   messageAdmins('', %client.nameBase @ " has enabled Admins to Admin.~wfx/powered/vehicle_screen_on.wav" );
				   adminLog(%client, " has enabled Admins to Admin.");
				}
			}
			return;

		case "ToggleTourneyNetClient":
			if(%client.isSuperAdmin)
			{
				if($Host::EnableNetTourneyClient)
				{
				   $Host::EnableNetTourneyClient = 0;

				   if(isActivePackage(checkver))
						deactivatePackage(checkver);

				   messageAdmins('', %client.nameBase @ " has disabled Net Tourney Client checking.~wfx/powered/vehicle_screen_on.wav" );
				   adminLog(%client, " has disabled Net Tourney Client checking.");
				}
				else
				{
				   $Host::EnableNetTourneyClient = 1;

				   if(!isActivePackage(checkver))
						activatePackage(checkver);

				   //Boot Offenders into Obs
				   CheckVerObserver(%client);

				   messageAdmins('', %client.nameBase @ " has enabled Net Tourney Client checking.~wfx/powered/vehicle_screen_on.wav" );
				   ResetGetCountsStatus();
				   adminLog(%client, " has enabled Net Tourney Client checking.");
				}
			}
			return;

		case "ForceVote":
			if (!%client.isAdmin)
            return;

         if(%client.isAdmin && !$Host::AllowAdminVotes)
         return;

         if (%client.ForceVote)
         {
            %client.ForceVote = 0;
            messageClient( %client, '', 'Vote to ... cancelled.' );
         }
         else
         {
            %client.ForceVote = 2;
            messageClient( %client, '', "Now select what to vote on, please." );
         }

			return;

	   case "VoteNextMission":
         %curTimeLeftMS = ($Host::TimeLimit * 60 * 1000) + $missionStartTime - getSimTime();
         if(%curTimeLeftMS <= 120000 && $countdownStarted && $MatchStarted && !%isAdmin)
         {
            messageClient(%client, "", "\c2Set next mission votes are restricted at this time.");
				return;
         }

         if(!%client.isAdmin && ($HostGamePlayerCount - $HostGameBotCount) < $Host::VoteNextPlayerLimit)
         {
            messageClient( %client, '', "Need at least" SPC $Host::VoteNextPlayerLimit SPC  "players on teams to set the next map." );
            return;
         }

         %foundMap = 0;
         %client.lockVMenu = 1;
         if(strpos(strlwr(%arg1),"next mission") != -1)
         {
            %key = %client.k++;
            for ( %type = 0; %type < $HostTypeCount; %type++ )
               messageClient( %client, 'MsgVoteItem', "", %key, 'VoteNextMission', $HostTypeDisplayName[%type], $HostTypeDisplayName[%type], true );
            return;
         }
         %key = %client.k++;
         for ( %type = 0; %type < $HostTypeCount; %type++ )
         {
            if($HostTypeDisplayName[%type] $= %arg1)
            {
               $HostNextTypeIndex = %type;


               for ( %i = $HostMissionCount[%type] - 1; %i >= 0; %i-- )
               {
                  %idx = $HostMission[%type, %i];
                  // If we have bots, don't change to a mission that doesn't support bots:
                  if ( $HostGameBotCount > 0 )
                  {
                     if( !$BotEnabled[%idx] )
                        continue;
                  }
                  messageClient( %client, 'MsgVoteItem', "", %key,'VoteNextMission',$HostMissionName[%idx],$HostMissionName[%idx], true );
               }
               return;
            }
         }
         %type = $HostNextTypeIndex;
         for ( %i = $HostMissionCount[%type] - 1; %i >= 0; %i-- )
         {
            %idx = $HostMission[%type, %i];
            if ( $HostGameBotCount > 0 )
            {
               if( !$BotEnabled[%idx] )
                  continue;
            }
            if(%arg1 $= $HostMissionName[%idx])
            {
             $hostNextMapIndex = %idx;
             %foundMap = 1;
             break;
            }
         }
         //error("found map" SPC %foundMap SPC $HostNextTypeIndex  SPC $hostNextMapIndex);
         if(%foundMap)
         {
            %arg3 = $hostNextMapIndex;
            %arg4 = $HostNextTypeIndex;
            // Vote-spoof prevention right here
            %arg1 = $HostMissionFile[%arg3];
            %arg2 = $HostTypeName[%arg4];
            if(!checkMapExist(%arg1, %arg2))
               return;

            // We passed the spoof check, give it the fancy label
            %arg1 = $HostMissionName[%arg3];
            %arg2 = $HostTypeDisplayName[%arg4];
             %client.lockVMenu = 0;
            if((!%isAdmin && $Host::AllowPlayerVoteNextMission) || (%isAdmin && %client.ForceVote)) // not admin
            {
               if($CMHasVoted[%client.guid] >= $Host::ClassicMaxVotes && !%isAdmin) // they've voted too many times
               {
                  messageClient(%client, "", "\c2You have exhausted your voting rights for this mission.");
                  return;
               }
               %msg = %client.nameBase @ " initiated a vote to set the next mission to " @ %arg1 @ " (" @ %arg2 @ ").";
               $CMHasVoted[%client.guid]++;
            }
         }

      case "ClearNextMap":
         if (%client.isAdmin && $voteNext)
			{
            messageAll('MsgAdminForce', "\c2The Admin " @ %client.nameBase @ " has cleared the next set mission.");
            adminLog(%client, " has cleared" SPC $HostMissionFile[$voteNextMap] SPC "(" @ $HostTypeName[$voteNextType] @ ") from the next set mission.");
            $voteNextType = 0;
            $voteNextMap = 0;
            $voteNext = 0;
         }
         return;
       case "FlagCapLimit":
         %client.lockVMenu = 1;
         if(strpos(strlwr(%arg1),strlwr("FlagCapLimit")) != -1)
         {
            %key = %client.k++;
            for ( %r = 1; %r <= 20; %r++){
               %msg = %r SPC "Flag Caps";
               messageClient( %client, 'MsgVoteItem', "", %key, 'FlagCapLimit', %r, %msg, false );
            }
            return;
         }
          else{
            if(%arg1 !$= ""){ 
               %client.lockVMenu = 0; 
               MissionGroup.CTF_scoreLimit = %arg1;
               messageAll('MsgAdminForce', "Flag cap limit is now" SPC %arg1); 
            }
         }
         return;
      case "ForceRoundEnd":
         %client.lockVMenu = 1;
         error(%arg1);
         if(strpos(strlwr(%arg1),strlwr("ForceRoundEnd")) != -1)
         {
            %key = %client.k++;
            for ( %r = 0; %r <= Game.numTeams; %r++){
               if(!%r){
                  %msg = "Force Round End";
               }
               else{
                  %teamName = getTaggedString(Game.getTeamName(%r));
                  %msg = "Force Win For Team" SPC %teamName;
               }
               messageClient( %client, 'MsgVoteItem', "", %key, 'ForceRoundEnd', %r, %msg, false );
            } 
            return;
         }
         else{
            if(%arg1 !$= ""){
               %client.lockVMenu = 0; 
               messageAll('MsgAdminForce', ""); 
              serverCmdArenaForceRoundEnd(%client,%arg1);
            }
         }
         return;
      case "SetRoundLimit":
         %client.lockVMenu = 1;
         if(strpos(strlwr(%arg1),"roundlimit") != -1)
         {
            %key = %client.k++;
            for ( %r = 1; %r <= 12; %r++){
               %msg = %r SPC "Rounds";
               messageClient( %client, 'MsgVoteItem', "", %key, 'SetRoundLimit',%r, %msg, false );
            }
            return;
         }
         else{
            if(%arg1 !$= ""){
                %client.lockVMenu = 0; 
                messageAll('MsgAdminForce', "");  
               serverCmdArenaSetCurrentRoundLimit(%client, %arg1);  
            }
         }
         return;
      case "SetRoundTimeLimit":
         %client.lockVMenu = 1;
         error(%arg1);
         if(strpos(strlwr(%arg1),strlwr("TimeLimit")) != -1)
         {
            %key = %client.k++;
            for ( %r = 2; %r <= 15; %r++){
               %msg = %r SPC "Minutes";
               messageClient( %client, 'MsgVoteItem', "", %key, 'SetRoundTimeLimit', %r, %msg, false );
            }
            return;
         }
          else{
            if(%arg1 !$= ""){
               %client.lockVMenu = 0; 
               messageAll('MsgAdminForce', ""); 
              serverCmdArenaSetCurrentTimeLimit(%client,%arg1);
            }
         }
         return;
      case "StationLimit":
         %client.lockVMenu = 1;
         error(%arg1);
         if(strpos(strlwr(%arg1),strlwr("StationLimit")) != -1)
         {
            %key = %client.k++;
            for ( %r = 1; %r <= 12; %r++){
               %msg = %r SPC "Station Uses";
               messageClient( %client, 'MsgVoteItem', "", %key, 'StationLimit', %r, %msg, false );
            }
            return;
         }
          else{
            if(%arg1 !$= ""){
               %client.lockVMenu = 0; 
               $Arena::Pref::StationLimit = %arg1;
               messageAll('MsgAdminForce', "Station limit is now" SPC %arg1); 
            }
         }
         return;
      case "ArmorRes":
         %client.lockVMenu = 1;
         error(%arg1);
         if(strpos(strlwr(%arg1),strlwr("ArmorRes")) != -1)
         {
            %key = %client.k++;
            messageClient( %client, 'MsgVoteItem', "", %key, 'ArmorRes', 2, "Scout Only", false );
            messageClient( %client, 'MsgVoteItem', "", %key, 'ArmorRes', 1, "Scout And Assault Only", false );
            messageClient( %client, 'MsgVoteItem', "", %key, 'ArmorRes', 0, "All Armors", false );
            return;
         }
          else{
            if(%arg1 !$= ""){
               %client.lockVMenu = 0; 
               $Arena::Pref::RestrictArmor = %arg1;
               if(%arg1 == 2){
                  messageAll('MsgAdminForce', "\c2Armor Restrictions Set To Scout Only"); 
               }
               else if(%arg1 == 1){
                  messageAll('MsgAdminForce', "\c2Armor Restrictions Set To Scout And Assault Only"); 
               }
               else{
                  messageAll('MsgAdminForce', "\c2Armor Restrictions Removed"); 
               }
            }
         }
         return;
      case "LaserBan":
         %status = %arg1 == 1 ? "Disabled" : "Enabled";
         messageAll('MsgAdminForce', "\c2Lasers Are Now" SPC %status); 
         $InvBanList[Arena, "SniperRifle"] = %arg1;
         return;
         
         
		// LakRabbit Stuff
		case "VoteDuelMode":
			if(!$CurrentMissionType $= "LakRabbit")
				return;

			if(!%isAdmin || (%isAdmin && %client.ForceVote))
				%msg = %client.nameBase @ " initiated a vote to " @ (Game.duelMode == 0 ? "enable" : "disable") @ " duel mode.";

		case "VoteSplashDamage":
			if(!$CurrentMissionType $= "LakRabbit")
				return;

			if(!%isAdmin || (%isAdmin && %client.ForceVote))
				%msg = %client.nameBase @ " initiated a vote to " @ (Game.noSplashDamage == 1 ? "enable" : "disable") @ " splash damage.";

		case "VotePro":
			if(!$CurrentMissionType $= "LakRabbit")
				return;

			if(!%isAdmin || (%isAdmin && %client.ForceVote))
				%msg = %client.nameBase @ " initiated a vote to " @ (Game.pubPro == 0 ? "enable" : "disable") @ " pro mode.";

      //Deathmatch Stuff
		case "DMSLOnlyMode":
			if(!$CurrentMissionType $= "DM")
				return;

			if(!%isAdmin || (%isAdmin && %client.ForceVote))
				%msg = %client.nameBase @ " initiated a vote to " @ (Game.DMSLOnlyMode == 0 ? "enable" : "disable") @ " shocklance only mode.";

      //LCTF Stuff
		case "LCTFProMode":
			if(!$CurrentMissionType $= "LCTF")
				return;

			if(!%isAdmin || (%isAdmin && %client.ForceVote))
				%msg = %client.nameBase @ " initiated a vote to " @ (Game.LCTFProMode == 0 ? "enable" : "disable") @ " pro mode.";

		case "showServerRules":
			if (($Host::ServerRules[1] !$= "") && (!%client.CantView))
			{
				for ( %i = 1; $Host::ServerRules[%i] !$= ""; %i++ )
				{
				   messageClient(%client, 'ServerRule', '\c2%1', $Host::ServerRules[%i] );
				}
				%client.cantView = true;
				%client.schedViewRules = schedule( 10000, %client, "resetViewSchedule", %client );
			}
			return;

		default:
			return;
	}

	if(%isAdmin && !%client.ForceVote)
		adminStartNewVote(%client, %typeName, %arg1, %arg2, %arg3, %arg4);
	else
		playerStartNewVote(%client, %typeName, %arg1, %arg2, %arg3, %arg4, %teamSpecific, %msg);
}

//exec("scripts/autoexec/VoteMenu.cs");
function playerStartNewVote(%client, %typeName, %arg1, %arg2, %arg3, %arg4, %teamSpecific, %msg)
{
	%clientsVoting = 0;
	%count = ClientGroup.getCount();
	if(%teamSpecific)
	{
		for(%i = 0; %i < %count; %i++)
		{
			%cl = ClientGroup.getObject(%i);
			if(%cl.team == %client.team && !%cl.isAIControlled())
			{
				//Specifically for votehud compatibility
				switch$(%typeName)
				{
					case "VoteKickPlayer":
						messageClient( %cl, 'VoteStarted', "\c2" @ %msg, %client.name, "kick player", %arg1.name);
					default:
						messageClient( %cl, 'VoteStarted', "\c2" @ %msg, %client.name, %arg1);
				}
				%clientsVoting++;
			}
		}

		for(%i = 0; %i < %count; %i++)
		{
			%cl = ClientGroup.getObject(%i);
			if(%cl.team == %client.team && !%cl.isAIControlled())
				messageClient(%cl, 'openVoteHud', "", %clientsVoting, ($Host::VotePassPercent / 100));
		}
	}
	else
	{
		%count = ClientGroup.getCount();
		for(%i = 0; %i < %count; %i++)
		{
			%cl = ClientGroup.getObject(%i);
			if(!%cl.isAIControlled())
			{
				//Specifically for votehud compatibility
				switch$(%typeName)
				{
					case "VoteChangeMission":
						messageClient( %cl, 'VoteStarted', "\c2" @ %msg, %client.name, "change the mission to", %arg1, %arg2);
               case "VoteNextMission":
						messageClient( %cl, 'VoteStarted', "\c2" @ %msg, %client.name, "set next mission to", %arg1, %arg2);
					case "VoteSkipMission":
						messageClient( %cl, 'VoteStarted', "\c2" @ %msg, %client.name, "skip the mission");
					case "VoteChangeTimeLimit":
						if(%arg1 $= "999") %time = "Unlimited"; else %time = %arg1;
						messageClient( %cl, 'VoteStarted', "\c2" @ %msg, %client.name, "change the time limit to", %time);
					case "VoteKickPlayer":
						messageClient( %cl, 'VoteStarted', "\c2" @ %msg, %client.name, "kick player", %arg1.name);
					case "VoteTournamentMode":
						messageClient( %cl, 'VoteStarted', "\c2" @ %msg, %client.name, "Tournament Mode", %arg1);
					case "VoteMatchStart":
						messageClient( %cl, 'VoteStarted', "\c2" @ %msg, %client.name, "Start Match");
					case "VoteFFAMode":
						messageClient( %cl, 'VoteStarted', "\c2" @ %msg, %client.name, "Free For All Mode");
					default:
						messageClient( %cl, 'VoteStarted', "\c2" @ %msg, %client.name, %arg1);
				}
				%clientsVoting++;
			}
		}

		for(%i = 0; %i < %count; %i++)
		{
			%cl = ClientGroup.getObject(%i);
			if(!%cl.isAIControlled())
				messageClient(%cl, 'openVoteHud', "", %clientsVoting, ($Host::VotePassPercent / 100));
		}
	}

	clearVotes();
	Game.voteType = %typeName;
	Game.scheduleVote = schedule(($Host::VoteTime * 1000), 0, "calcVotes", %typeName, %arg1, %arg2, %arg3, %arg4);

	// Eolk - Voting control variables
	Game.votingArgs[typeName] = %typeName;
	Game.votingArgs[arg1] = %arg1;
	Game.votingArgs[arg2] = %arg2;
	Game.votingArgs[arg3] = %arg3;
	Game.votingArgs[arg4] = %arg4;

	%client.vote = true;
	messageAll('addYesVote', "");
	if(%client.team != 0)
		clearBottomPrint(%client);

   %client.canVote = false;
   %client.rescheduleVote = schedule(($Host::VoteCooldown * 1000) + ($Host::VoteTime * 1000) , 0, "resetVotePrivs", %client);

   // Log Vote
   voteLog(%client, %typeName, %arg1, %arg2, %arg3, %arg4);
   echo(%msg);

   if($Host::EnableVoteSoundReminders > 0)
   {
		%time = mFloor($Host::VoteTime / ($Host::EnableVoteSoundReminders + 1)) * 1000;
		//echo(%time);
		for(%i = 0; %i < $Host::EnableVoteSoundReminders; %i++)
				Game.voteReminder[%i] = schedule((%time * (%i + 1)), 0, "VoteSound", %teamSpecific, %typename, %arg1, %arg2, %msg);
   }
}

function DefaultGame::gameOver(%game)
{
	Parent::gameOver(%game);

	//Reset ClassicMaxMapChangeVotes
	deleteVariables("$CMHasVoted*"); // Eolk - let people who have voted vote again

	//Beginning match Vote Delay
	$VoteDelay = getSimTime();
}

function DefaultGame::cancelMatchStart(%game, %admin)
{
   if(%admin && $Host::TournamentMode && !$MatchStarted && $CountdownStarted)
   {
      CancelCountdown();
      for(%i = 0; %i < ClientGroup.getCount(); %i++)
      {
         %cl = ClientGroup.getObject(%i);
         messageClient(%cl, 'MsgAdminForce', "\c2Match Start Canceled.");
         messageClient(%cl, 'MsgSystemClock', "", 0, 0);

         %cl.notready = 1;
         %cl.notReadyCount = "";
         centerprint(%cl, "\nPress FIRE when ready.", 0, 3);
      }
   }
}

//------------------------------------------------------------------------------
// all team based votes here
function DefaultGame::voteKickPlayer(%game, %admin, %client)
{
   %cause = "";
   %typeName = "VoteKickPlayer";

   if(isObject(%admin))
   {
      kick(%client, %admin, %client.guid );
      %cause = "(admin)";
   }
   else
   {
      %team = %client.team;
      %totalVotes = %game.votesFor[%game.kickTeam] + %game.votesAgainst[%game.kickTeam];
	  %cause = "(vote)";
      if(%totalVotes > 0 && (%game.votesFor[%game.kickTeam] / %totalVotes) > ($Host::VotePasspercent / 100))
      {
		 kick(%client, %admin, %game.kickGuid);

		 %key = "Passed";
      }
      else
      {

		 for ( %idx = 0; %idx < ClientGroup.getCount(); %idx++ )
         {
            %cl = ClientGroup.getObject( %idx );

            if (%cl.team == %game.kickTeam && !%cl.isAIControlled())
               messageClient( %cl, 'MsgVoteFailed', '\c2Kick player vote did not pass.' );
         }
		 %key = "Failed";
      }
   }

   //Vote Only
   if(%cause $= "(vote)")
   {
	  //Log Vote %
	  votePercentLog(%client, %typeName, %key, %game.votesFor[%game.kickTeam], %game.votesAgainst[%game.kickTeam], %totalVotes, %game.totalVotesNone);
	  //Show Vote %
      messageTeam(%game.kickTeam, "", '\c1Vote %6: \c0Yea: %1 Nay: %2 Total: %3 [%4%5]', %game.votesFor[%game.kickTeam], %game.votesAgainst[%game.kickTeam], %totalVotes, mfloor((%game.votesFor[%game.kickTeam] / %totalVotes) * 100), "%", %key);
   }

   if(%cause $= "(admin)")
	  adminLog(%admin, " kicked " @ %game.kickClientNameBase @ " (" @ %game.kickGuid @ ").");

   %game.kickTeam = "";
   %game.kickGuid = "";
   %game.kickClientName = "";
}

//------------------------------------------------------------------------------
function DefaultGame::voteChangeMission(%game, %admin, %missionDisplayName, %typeDisplayName, %missionId, %missionTypeId)
{
   %typeName = "VoteChangeMission";

   %mission = $HostMissionFile[%missionId];
   if ( %mission $= "" )
   {
      error( "Invalid mission index passed to DefaultGame::voteChangeMission!" );
      return;
   }

   %missionType = $HostTypeName[%missionTypeId];
   if ( %missionType $= "" )
   {
      error( "Invalid mission type id passed to DefaultGame::voteChangeMission!" );
      return;
   }

   // Eolk - Part of $admincl fix.
   if(isObject(%admin))
   {
      messageAll('MsgAdminChangeMission', '\c2The Admin %3 has changed the mission to %1 (%2).', %missionDisplayName, %typeDisplayName, %admin.name );
      $voteNext = 0;
      %game.gameOver();
      loadMission( %mission, %missionType, false );
	  adminLog(%admin, " has changed the mission to " @ %missionDisplayName @ " (" @ %typeDisplayName @ ")");
   }
   else
   {
	  %totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
      // Added people who dont vote into the equation, now if you do not vote, it doesn't count as a no. - z0dd - ZOD
      if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone)) > ($Host::VotePasspercent / 100))
      {
         messageAll('MsgVotePassed', '\c2The mission was changed to %1 (%2) by vote.', %missionDisplayName, %typeDisplayName );

		 //Log Vote % - Must be before Game Over
		 %key = "Passed";
		 votePercentLog(%missionDisplayName, %typeName, %key, %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, %game.totalVotesNone);
		 //Show Vote %
		 messageAll('', '\c1Vote %6: \c0Yea: %1 Nay: %2 Abstain: %7 Total: %3 [%4%5]', %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, mfloor((%game.totalVotesFor/(ClientGroup.getCount() - %game.totalVotesNone)) * 100), "%", %key, %game.totalVotesNone);

         $voteNext = 0;
         %game.gameOver();
         loadMission( %mission, %missionType, false );
      }
      else
	  {
         messageAll('MsgVoteFailed', '\c2Change mission vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone) * 100));

		 //Log Vote %
		 %key = "Failed";
		 votePercentLog(%missionDisplayName, %typeName, %key, %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, %game.totalVotesNone);
		 //Show Vote %
		 messageAll('', '\c1Vote %6: \c0Yea: %1 Nay: %2 Abstain: %7 Total: %3 [%4%5]', %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, mfloor((%game.totalVotesFor/(ClientGroup.getCount() - %game.totalVotesNone)) * 100), "%", %key, %game.totalVotesNone);
	  }
   }
}

//------------------------------------------------------------------------------
function DefaultGame::voteTournamentMode( %game, %admin, %missionDisplayName, %typeDisplayName, %missionId, %missionTypeId )
{
   %typeName = "VoteTournamentMode";

   %mission = $HostMissionFile[%missionId];
   if ( %mission $= "" )
   {
      error( "Invalid mission index passed to DefaultGame::voteTournamentMode!" );
      return;
   }

   %missionType = $HostTypeName[%missionTypeId];
   if ( %missionType $= "" )
   {
      error( "Invalid mission type id passed to DefaultGame::voteTournamentMode!" );
      return;
   }

   %cause = "";
   if (isObject(%admin))
   {
      messageAll( 'MsgAdminForce', '\c2The Admin %2 has switched the server to Tournament mode (%1).', %missionDisplayName, %admin.name );
	  adminLog(%admin, " has switched the server to Tournament mode. " @ %missionDisplayName @ " (" @ %typeDisplayName @ ")");
      setModeTournament( %mission, %missionType );
   }
   else
   {
      %totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
      // Added people who dont vote into the equation, now if you do not vote, it doesn't count as a no. - z0dd - ZOD
      if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone)) > ($Host::VotePasspercent / 100))
      {
         messageAll('MsgVotePassed', '\c2Server switched to Tournament mode by vote (%1): %2 percent.', %missionDisplayName, mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone) * 100));

		 //Log Vote % - Must be before Game Over
		 %key = "Passed";
		 votePercentLog(%missionDisplayName, %typeName, %key, %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, %game.totalVotesNone);
		 //Show Vote %
		 messageAll('', '\c1Vote %6: \c0Yea: %1 Nay: %2 Abstain: %7 Total: %3 [%4%5]', %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, mfloor((%game.totalVotesFor/(ClientGroup.getCount() - %game.totalVotesNone)) * 100), "%", %key, %game.totalVotesNone);

         setModeTournament( %mission, %missionType );
      }
      else
	  {
		 //Log Vote %
		 %key = "Failed";
		 votePercentLog(%missionDisplayName, %typeName, %key, %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, %game.totalVotesNone);
		 //Show Vote %
		 messageAll('', '\c1Vote %6: \c0Yea: %1 Nay: %2 Abstain: %7 Total: %3 [%4%5]', %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, mfloor((%game.totalVotesFor/(ClientGroup.getCount() - %game.totalVotesNone)) * 100), "%", %key, %game.totalVotesNone);

		 messageAll('MsgVoteFailed', '\c2Tournament mode vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone) * 100));
	  }
   }

   if(%cause !$= "")
      logEcho($AdminCl.nameBase @ ": tournament mode set "@%cause, 1);
}

//------------------------------------------------------------------------------
function DefaultGame::voteChangeTimeLimit( %game, %admin, %newLimit )
{
   %typeName = "VoteChangeTimeLimit";

   if( %newLimit == 999 )
      %display = "unlimited";
   else
      %display = %newLimit;

   %cause = "";
   if ( %admin )
   {
      messageAll( 'MsgAdminForce', '\c2The Admin %2 changed the mission time limit to %1 minutes.', %display, %admin.name );
      $Host::TimeLimit = %newLimit;
	  adminLog(%admin, " has changed the mission time limit to " @ %display @ " minutes.");
   }
   else
   {
      %totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
      // Added people who dont vote into the equation, now if you do not vote, it doesn't count as a no. - z0dd - ZOD
      if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone)) > ($Host::VotePasspercent / 100))
      {
         messageAll('MsgVotePassed', '\c2The mission time limit was set to %1 minutes by vote.', %display);
         $Host::TimeLimit = %newLimit;
		 // VoteOvertime
		 ResetVOTimeChanged(%game);
		 // Reset the voted time limit when changing mission
         $TimeLimitChanged = 1;

		 //Log Vote %
		 %key = "Passed";
		 votePercentLog(%newLimit, %typeName, %key, %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, %game.totalVotesNone);
		 //Show Vote %
		 messageAll('', '\c1Vote %6: \c0Yea: %1 Nay: %2 Abstain: %7 Total: %3 [%4%5]', %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, mfloor((%game.totalVotesFor/(ClientGroup.getCount() - %game.totalVotesNone)) * 100), "%", %key, %game.totalVotesNone);
      }
      else
	  {
         messageAll('MsgVoteFailed', '\c2The vote to change the mission time limit did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone) * 100));

		 //Log Vote %
		 %key = "Failed";
	     votePercentLog(%newLimit, %typeName, %key, %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, %game.totalVotesNone);
		 //Show Vote %
		 messageAll('', '\c1Vote %6: \c0Yea: %1 Nay: %2 Abstain: %7 Total: %3 [%4%5]', %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, mfloor((%game.totalVotesFor/(ClientGroup.getCount() - %game.totalVotesNone)) * 100), "%", %key, %game.totalVotesNone);

		 // VoteOvertime
		 ResetVOall(%game);
	  }
   }

   //if the match has been started, reset the end of match countdown
   if ($matchStarted)
   {
      //schedule the end of match countdown
      %elapsedTimeMS = getSimTime() - $missionStartTime;
      %curTimeLeftMS = ($Host::TimeLimit * 60 * 1000) - %elapsedTimeMS;
      error("time limit="@$Host::TimeLimit@", elapsed="@(%elapsedTimeMS / 60000)@", curtimeleftms="@%curTimeLeftMS);
      CancelEndCountdown();
      EndCountdown(%curTimeLeftMS);
      cancel(%game.timeSync);
      %game.checkTimeLimit(true);
   }
}

//------------------------------------------------------------------------------
function DefaultGame::voteFFAMode( %game, %admin, %client )
{
   %cause = "";
   %name = getTaggedString(%client.name);

   if(isObject(%admin))
   {
      messageAll('MsgAdminForce', '\c2The Admin %1 has switched the server to Free For All mode.', %admin.name);
	  adminLog(%admin, " has switched the server to Free For All mode.");
      setModeFFA($CurrentMission, $CurrentMissionType);
   }
   else
   {
      %totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
      // Added people who dont vote into the equation, now if you do not vote, it doesn't count as a no. - z0dd - ZOD
      if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone)) > ($Host::VotePasspercent / 100))
      {
         messageAll('MsgVotePassed', '\c2Server switched to Free For All mode by vote.', %client);
         setModeFFA($CurrentMission, $CurrentMissionType);
      }
      else
         messageAll('MsgVoteFailed', '\c2Free For All mode vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone) * 100));
   }
}

function DefaultGame::voteSkipMission(%game, %admin, %arg1, %arg2, %arg3, %arg4)
{
   %typeName = "VoteSkipMission";

   if(isObject(%admin))
   {
      messageAll('MsgAdminForce', '\c2The Admin %1 has skipped to the next mission.',%admin.name );
	  adminLog(%admin, " has skipped to the next mission.");
      %game.gameOver();
      //loadMission( findNextCycleMission(), $CurrentMissionType, false );
      cycleMissions();
   }
   else
   {
      %totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
      // Added people who dont vote into the equation, now if you do not vote, it doesn't count as a no. - z0dd - ZOD
      if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone)) > ($Host::VotePasspercent / 100))
      {
         messageAll('MsgVotePassed', '\c2The mission was skipped to next by vote.');

		 //Log Vote % - Must be before Game Over
		 %key = "Passed";
		 votePercentLog("N/A", %typeName, %key, %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, %game.totalVotesNone);
		 //Show Vote %
		 messageAll('', '\c1Vote %6: \c0Yea: %1 Nay: %2 Abstain: %7 Total: %3 [%4%5]', %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, mfloor((%game.totalVotesFor/(ClientGroup.getCount() - %game.totalVotesNone)) * 100), "%", %key, %game.totalVotesNone);

         echo("mission skipped (vote)");
         %game.gameOver();
         //loadMission( findNextCycleMission(), $CurrentMissionType, false );
         cycleMissions();
      }
      else
	  {
         messageAll('MsgVoteFailed', '\c2Skip mission vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone) * 100));

		 //Log Vote %
		 %key = "Failed";
		 votePercentLog("N/A", %typeName, %key, %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, %game.totalVotesNone);
		 //Show Vote %
		 messageAll('', '\c1Vote %6: \c0Yea: %1 Nay: %2 Abstain: %7 Total: %3 [%4%5]', %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, mfloor((%game.totalVotesFor/(ClientGroup.getCount() - %game.totalVotesNone)) * 100), "%", %key, %game.totalVotesNone);
	  }
   }
}

//------------------------------------------------------------------------------
function DefaultGame::voteMatchStart( %game, %admin)
{
   %cause = "";
   %ready = forceTourneyMatchStart();
   if(isObject(%admin))
   {
      if(!%ready)
      {
         // z0dd - ZOD, 5/19/03. This was sending to %client, there is no %client declared, duh
         messageClient( %admin, 'msgClient', '\c2No players are ready yet.');
         return;
      }
      else
      {
         messageAll('msgMissionStart', '\c2The admin %1 has forced the match to start.', %admin.name);
         startTourneyCountdown();
      }
   }
   else
   {
      if(!%ready)
      {
         messageAll( 'msgClient', '\c2Vote passed to start match, but no players are ready yet.');
         return;
      }
      else
      {
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		// Added people who dont vote into the equation, now if you do not vote, it doesn't count as a no. - z0dd - ZOD
		if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone)) > ($Host::VotePasspercent / 100))
         {
            messageAll('MsgVotePassed', '\c2The match has been started by vote: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone) * 100));
            startTourneyCountdown();
         }
         else
            messageAll('MsgVoteFailed', '\c2Start Match vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone) * 100));
      }
   }
}

//------------------------------------------------------------------------------
function DefaultGame::voteTeamDamage(%game, %admin)
{
   %setto = "";
   %cause = "";
   if(isObject(%admin))
   {
      if($teamDamage)
      {
         messageAll('MsgAdminForce', '\c2The Admin %1 has disabled team damage.', %admin.name);
         $Host::TeamDamageOn = $TeamDamage = 0;
         %setto = "disabled";
		 adminLog(%admin, " has disabled team damage.");
      }
      else
      {
         messageAll('MsgAdminForce', '\c2The Admin %1 has enabled team damage.', %admin.name);
         $Host::TeamDamageOn = $TeamDamage = 1;
         %setto = "enabled";
		 adminLog(%admin, " has enabled team damage.");
      }
   }
   else
   {
      %totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
      // Added people who dont vote into the equation, now if you do not vote, it doesn't count as a no. - z0dd - ZOD
      if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone)) > ($Host::VotePasspercent / 100))
      {
         if($teamDamage)
         {
            messageAll('MsgVotePassed', '\c2Team damage was disabled by vote.');
            $Host::TeamDamageOn = $TeamDamage = 0;
            %setto = "disabled";
         }
         else
         {
            messageAll('MsgVotePassed', '\c2Team damage was enabled by vote.');
            $Host::TeamDamageOn = $TeamDamage = 1;
            %setto = "enabled";
         }
      }
      else
      {
         if($teamDamage)
            messageAll('MsgVoteFailed', '\c2Disable team damage vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone) * 100));
         else
            messageAll('MsgVoteFailed', '\c2Enable team damage vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone) * 100));
      }
   }
}

function DefaultGame::sendGamePlayerPopupMenu( %game, %client, %targetClient, %key )
{
   if( !%targetClient.matchStartReady )
      return;

   %isAdmin = ( %client.isAdmin || %client.isSuperAdmin );
   %isSuperAdmin = (%client.isSuperAdmin);
   %isTargetSelf = ( %client == %targetClient );
   %isTargetAdmin = ( %targetClient.isAdmin || %targetClient.isSuperAdmin );
   %isTargetBot = %targetClient.isAIControlled();
   %isTargetObserver = ( %targetClient.team == 0 );
   %outrankTarget = false;

   if ( %client.isSuperAdmin ) // z0dd - ZOD, 7/11/03. Super admins should outrank even themseleves.
      %outrankTarget = 1; //!%targetClient.isSuperAdmin;
   else if ( %client.isAdmin )
      %outrankTarget = !%targetClient.isAdmin;

   if( %client.isSuperAdmin && %targetClient.guid != 0 ) // z0dd - ZOD, 9/29/02. Removed T2 demo code from here
   {
      messageClient( %client, 'MsgPlayerPopupItem', "", %key, "addAdmin", "", 'Add to Server Admin List', 10);
      messageClient( %client, 'MsgPlayerPopupItem', "", %key, "addSuperAdmin", "", 'Add to Server SuperAdmin List', 11);
   }

   //mute options
   if ( !%isTargetSelf )
   {
      if ( %client.muted[%targetClient] )
         messageClient( %client, 'MsgPlayerPopupItem', "", %key, "MutePlayer", "", 'Unmute Text Chat', 1);
      else
         messageClient( %client, 'MsgPlayerPopupItem', "", %key, "MutePlayer", "", 'Mute Text Chat', 1);

      if ( !%isTargetBot && %client.canListenTo( %targetClient ) )
      {
         if ( %client.getListenState( %targetClient ) )
            messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ListenPlayer", "", 'Disable Voice Com', 9 );
         else
            messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ListenPlayer", "", 'Enable Voice Com', 9 );
      }
      // ------------------------------------------
      // z0dd - ZOD 4/4/02. Observe a specific player
      if (%client.team == 0 && !%isTargetObserver)
         messageClient(%client, 'MsgPlayerPopupItem', "", %key, "ObservePlayer", "", 'Observe Player', 12);
   }
   if( !%client.canVote && !%isAdmin )
      return;

   // regular vote options on players
   if ( %game.scheduleVote $= "" && !%isAdmin && !%isTargetAdmin )
   {
      if ( $Host::allowAdminPlayerVotes && !%isTargetBot ) // z0dd - ZOD, 9/29/02. Removed T2 demo code from here
         messageClient( %client, 'MsgPlayerPopupItem', "", %key, "AdminPlayer", "", 'Vote to Make Admin', 2 );

      if ( !%isTargetSelf )
      {
         messageClient( %client, 'MsgPlayerPopupItem', "", %key, "KickPlayer", "", 'Vote to Kick', 3 );
      }
   }
   // Admin only options on players:
   else if ( %isAdmin ) // z0dd - ZOD, 9/29/02. Removed T2 demo code from here
   {
      if ( !%isTargetBot && !%isTargetAdmin && (%isSuperAdmin || %isAdmin && $Host::AllowAdmin2Admin))
         messageClient( %client, 'MsgPlayerPopupItem', "", %key, "AdminPlayer", "", 'Make Admin', 2 );

      if ( !%isTargetSelf && %outrankTarget )
      {
         if(%isSuperAdmin || %isAdmin && $Host::AllowAdminKick)
			   messageClient( %client, 'MsgPlayerPopupItem', "", %key, "KickPlayer", "", 'Kick', 3 );

         if ( !%isTargetBot )
         {
            // ------------------------------------------------------------------------------------------------------
            // z0dd - ZOD - Founder 7/13/03. Bunch of new admin features
            messageClient(%client, 'MsgPlayerPopupItem', "", %key, "Warn", "", 'Warn player', 13);
            if(%isTargetAdmin)
               messageClient( %client, 'MsgPlayerPopupItem', "", %key, "StripAdmin", "", 'Strip admin', 14 );

            messageClient( %client, 'MsgPlayerPopupItem', "", %key, "SendMessage", "", 'Send Private Message', 15 );

            if ( %targetClient.isGagged )
               messageClient( %client, 'MsgPlayerPopupItem', "", %key, "UnGagPlayer", "", 'UnGag Player', 17);
            else
               messageClient( %client, 'MsgPlayerPopupItem', "", %key, "GagPlayer", "", 'Gag Player', 17);
          
			   messageClient( %client, 'MsgPlayerPopupItem', "", %key, "PrintClientInfo", "", 'Client Info', 16 ); // z0dd - ZOD - MeBad, 7/13/03. Send client information.

            if( %client.isSuperAdmin )
            {
               messageClient( %client, 'MsgPlayerPopupItem', "", %key, "BanPlayer", "", 'Ban', 4 );

               if ( %targetClient.isFroze )
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ThawPlayer", "", 'Thaw Player', 18);
               else
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "FreezePlayer", "", 'Freeze Player', 18);

               messageClient( %client, 'MsgPlayerPopupItem', "", %key, "BootPlayer", "", 'Boot to the Rear', 19);
               messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ExplodePlayer", "", 'Explode Player', 20);
            }
         }

         if ( !%isTargetObserver )
             messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ToObserver", "", 'Force observer', 5 );
      }
      if ( %isTargetSelf || %outrankTarget )
      {
         if(%isTargetAdmin)
            messageClient( %client, 'MsgPlayerPopupItem', "", %key, "StripAdmin", "", 'Strip admin', 14 );

         if ( %game.numTeams > 1 )
         {
            if ( %isTargetObserver )
            {
				%action = %isTargetSelf ? "Join " : "Change to ";
				%str1 = %action @ getTaggedString( %game.getTeamName(1) );
				%str2 = %action @ getTaggedString( %game.getTeamName(2) );

				messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str1, 6 );
				messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str2, 7 );
            }
            else if( %isSuperAdmin || ($Host::AllowAdminSwitchTeams && %isAdmin) )
            {
				%changeTo = %targetClient.team == 1 ? 2 : 1;
				%str = "Switch to " @ getTaggedString( %game.getTeamName(%changeTo) );
				%caseId = 5 + %changeTo;

				messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str, %caseId );
            }

            // z0dd - ZOD, 7/11/03. Allow Super admins to force themselves to obs.
            if( %isSuperAdmin )
            {
               if(%isTargetSelf && !%isTargetObserver)
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ToObserver", "", 'Force observer', 5 );
            }
         }
         else if ( %isTargetObserver )
         {
            %str = %isTargetSelf ? 'Join the Game' : 'Add to Game';
            messageClient( %client, 'MsgPlayerPopupItem', "", %key, "JoinGame", "", %str, 8 );
         }
         if ( %game.numTeams > 2 )
         {
            if ( %isTargetObserver )
            {
				%action = %isTargetSelf ? "Join " : "Change to ";
				%str1 = %action @ getTaggedString( %game.getTeamName(1) );
				%str2 = %action @ getTaggedString( %game.getTeamName(2) );
            %str3 = %action @ getTaggedString( %game.getTeamName(3) );
            %str4 = %action @ getTaggedString( %game.getTeamName(4) );
            

				messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str1, 6 );
				messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str2, 7 );
            messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str3, 21 );
            messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str4, 22 );
            }
            else if( %isSuperAdmin || ($Host::AllowAdminSwitchTeams && %isAdmin) )
            {
               %action = %isTargetSelf ? "Join " : "Change to ";
               %str1 = %action @ getTaggedString( %game.getTeamName(1) );
               %str2 = %action @ getTaggedString( %game.getTeamName(2) );
               %str3 = %action @ getTaggedString( %game.getTeamName(3) );
               %str4 = %action @ getTaggedString( %game.getTeamName(4) );
               if ( %targetClient.team == 1) {
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str2, 7 );
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str3, 21 );
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str4, 22 );
               }
               else if ( %targetClient.team == 2) {
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str1, 6 );
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str3, 21 );
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str4, 22 );
               }
               else if ( %targetClient.team == 3) {
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str1, 6 );
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str2, 7 );
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str4, 22 );
               }
               else if ( %targetClient.team == 4) {
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str1, 6 );
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str2, 7 );
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str3, 21 );
               }

            }

            // z0dd - ZOD, 7/11/03. Allow Super admins to force themselves to obs.
            if( %isSuperAdmin )
            {
               if(%isTargetSelf && !%isTargetObserver)
                  messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ToObserver", "", 'Force observer', 5 );
            }
         }
         else if ( %isTargetObserver )
         {
            %str = %isTargetSelf ? 'Join the Game' : 'Add to Game';
            messageClient( %client, 'MsgPlayerPopupItem', "", %key, "JoinGame", "", %str, 8 );
         }
      }
   }
}

function adminStartNewVote(%client, %typeName, %arg1, %arg2, %arg3, %arg4)
{
	if ( Game.scheduleVote !$= "" && Game.voteType $= %typeName )
	{
		messageAll('closeVoteHud', "");
		cancel(Game.scheduleVote);
		Game.scheduleVote = "";

		// Eolk - Voting control variables
		Game.votingArgs[typeName] = "";
		Game.votingArgs[arg1] = "";
		Game.votingArgs[arg2] = "";
		Game.votingArgs[arg3] = "";
		Game.votingArgs[arg4] = "";
	}
	Game.evalVote(%typeName, %client, %arg1, %arg2, %arg3, %arg4);
}

function serverCmdGetVoteMenu( %client, %key )
{
   if (isObject( Game ) && !%client.lockVMenu)
      Game.sendGameVoteMenu( %client, %key );
   %client.lockVMenu = 0;
}

function CycleMissions()
{
   if($voteNext)
   {
      %nextMission = $HostMissionFile[$voteNextMap];
      %type = $HostTypeName[$voteNextType];
      messageAll( 'MsgClient', 'Loading %1 (%2)...', %nextMission, $HostTypeDisplayName[$voteNextType] );
      loadMission( %nextMission, %type );
      $voteNextType = 0;
      $voteNextMap = 0;
      $voteNext = 0;
   }
   else
      parent::CycleMissions();
}

//Reset Set next mission if everyone leaves
function GameConnection::onDrop(%client, %reason)
{
	Parent::onDrop(%client, %reason);

	//Reset SetNextMission
   if($HostGamePlayerCount - $HostGameBotCount == 0 && $voteNext)
   {
      echo("No clients on the server. Set next mission reset...");
      $voteNextType = 0;
      $voteNextMap = 0;
      $voteNext = 0;
   }
}

};

// checkMapExist(%missionName, %missionType)
// Info: check if a map exist in the mission type
function checkMapExist(%missionName, %missionType)
{
	// Find if the mission exists
   for(%mis = 0; %mis < $HostMissionCount; %mis++)
       if($HostMissionFile[%mis] $= %missionName)
           break;

   // Now find if the mission type exists
   for(%type = 0; %type < $HostTypeCount; %type++)
       if($HostTypeName[%type] $= %missionType)
           break;

   // Now find if the mission's index in the mission-type specific sub-list exists
   for(%i = 0; %i < $HostMissionCount[%type]; %i++)
       if($HostMission[%type, %i] == %mis)
           break;

	if($HostMission[%type, %i] !$= "")
		return true; // valid map
   else
   	return false; // invalid map
}

// passCurrentVote()
// Info: passes a vote that is running.
// Eolk - Removed *** classic admin stuff.
function passCurrentVote(%admin)
{
   if(Game.scheduleVote !$= "")
   {
      messageAll('closeVoteHud', "");
      cancel(Game.scheduleVote);
      Game.scheduleVote = "";
      Game.kickClient = "";

      if(Game.votingArgs[typeName] $= "VoteKickPlayer") // special case here
      {
         Game.votesFor[Game.kickTeam] = ClientGroup.getCount() - $HostGameBotCount;
         Game.votesAgainst[Game.kickTeam] = 0;
      }
      else
      {
         Game.totalVotesFor = ClientGroup.getCount() - $HostGameBotCount;
         Game.totalVotesAgainst = 0;
      }

      Game.evalVote(Game.votingArgs[typeName], false, Game.votingArgs[arg1], Game.votingArgs[arg2], Game.votingArgs[arg3], Game.votingArgs[arg4]);
      clearVotes();

		//Stop vote chimes
		for(%i = 0; %i < $Host::EnableVoteSoundReminders; %i++)
		{
			if(isEventPending(Game.voteReminder[%i]))
				cancel(Game.voteReminder[%i]);
			Game.voteReminder[%i] = "";
		}

      messageAll('MsgAdminForce', "\c2The Admin " @ getTaggedString(%admin.name) @ " has passed the vote.");
   }
   else
      messageClient(%admin, "MsgClient", "\c2There is no vote to pass...");
}

// stopCurrentVote()
// Info: stop a vote that is still running
function stopCurrentVote(%admin)
{
   if(Game.scheduleVote !$= "")
   {

      messageAll('closeVoteHud', "");
      cancel(Game.scheduleVote);
      Game.scheduleVote = "";
      Game.kickClient = "";
      clearVotes();

		//Stop vote chimes
		for(%i = 0; %i < $Host::EnableVoteSoundReminders; %i++)
		{
			if(isEventPending(Game.voteReminder[%i]))
				cancel(Game.voteReminder[%i]);
			Game.voteReminder[%i] = "";
		}

      messageAll('MsgAdminForce', "\c2The Admin " @ getTaggedString(%admin.name) @ " has cancelled the vote.");
   }
   else
      messageClient(%admin, "MsgClient", "\c2There is no vote to stop...");
}

// calcVotes(%typeName, %arg1, %arg2, %arg3, %arg4)
// Info: fixed a bug that doesn't close properly the vote hud
function calcVotes(%typeName, %arg1, %arg2, %arg3, %arg4)
{
   if(%typeName $= "voteMatchStart")
   {
      if(($MatchStarted || $countdownStarted) && Game.scheduleVote !$= "")
      {
         stopCurrentVote();
         return;
      }
   }

   for(%idx = 0; %idx < ClientGroup.getCount(); %idx++)
   {
      %cl = ClientGroup.getObject(%idx);
      messageClient(%cl, 'closeVoteHud', "");

      if(%cl.vote !$= "")
      {
         if(%cl.vote)
         {
            Game.votesFor[%cl.team]++;
            Game.totalVotesFor++;
         }
         else
         {
            Game.votesAgainst[%cl.team]++;
            Game.totalVotesAgainst++;
         }
      }
      else
      {
         Game.votesNone[%cl.team]++;
         Game.totalVotesNone++;
      }
   }

   if((Game.totalVotesFor + Game.totalVotesAgainst) >= mFloor(ClientGroup.getCount() * $Host::voteParticipationPct))
      Game.evalVote(%typeName, false, %arg1, %arg2, %arg3, %arg4);
   else
      messageAll('MsgVoteFailed', '\c2Vote minimum participation not reached. Total votes %1 out of %2 required.',(Game.totalVotesFor + Game.totalVotesAgainst),mFloor(ClientGroup.getCount()/2));

   Game.scheduleVote = "";
   Game.scheduleVoteArgs = "";
   Game.kickClient = "";
   clearVotes();
}

function messageAdmins(%msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13)
{
   for(%i = 0; %i < ClientGroup.getCount(); %i++)
   {
      %cl = ClientGroup.getObject(%i);
      if(%cl.isAdmin)
         messageClient(%cl, %msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13);
   }
}

function resetViewSchedule(%client)
{
  %client.cantView = false;
  %client.schedViewRules = "";
}

// Prevent package from being activated if it is already
if (!isActivePackage(ExtraVoteMenu))
    activatePackage(ExtraVoteMenu);

// Locked Teams code (Tournament Mode Only)
// Doesnt allow players Joining the server late to join teams when enable, disables when server if switched back to free for all mode

// Reset in MissionTypeOptions.cs
//
// if(isActivePackage(LockedTeams) && !$LockedTeams)
// 	deactivatePackage(LockedTeams);

package LockedTeams
{

function serverCmdClientJoinGame(%client)
{
	if($LockedTeams)
	{
		messageClient( %client, '', "Teams are locked. Wait to be assigned a team." );
		return;
	}
	Parent::serverCmdClientJoinGame(%client);
}

//exec("Scripts/autoexec/votemenu.cs");
function serverCmdClientPickedTeam(%client, %option)
{
   if(!$Host::TournamentMode)
      return;

   //All roads lead to observer
   if( isObject(%client.player) )
   {
      %client.player.scriptKill(0);
      ClearBottomPrint(%client);
   }
   Game.forceObserver( %client, "playerChoose" );
   %client.observerMode = "observer";
   %client.notReady = false;
   messageClient( %client, '', "Teams are locked. Wait to be assigned a team." );
   return;
}

function serverCmdClientTeamChange(%client, %option)
{
	if($LockedTeams)
	{
		messageClient( %client, '', "Teams are locked. Wait to be assigned a team." );
		return;
	}
	Parent::serverCmdClientTeamChange(%client, %option);
}

//No Pick Team PopUp Box when LockedTeams is enabled
function serverCmdPlayContentSet( %client )
{
	//nothing
}

};

// VoteSound Script
//
// Make a sound every so seconds to make sure everyone votes
//
// Enable or Disable VoteSound
// $Host::EnableVoteSoundReminders = 3;
// 3 for three reminder notifications

function VoteSound(%teamSpecific, %typename, %arg1, %arg2, %msg)
{
	if(Game.scheduleVote !$= "" && $Host::EnableVoteSoundReminders > 0) //Game.scheduleVote !$= "" is if vote is active
	{
		%vip = "Vote in Progress:";
		//%yn = "Press Insert for Yes or Delete for No.";

		switch$(%typeName)
		{
			case "VoteKickPlayer":
				if(%arg1.team != 0 && Game.numTeams > 1) //Not observer
				{
				   for(%i = 0; %i < ClientGroup.getCount(); %i++)
				   {
					  	%cl = ClientGroup.getObject(%i);
						if (%cl.isAdmin == true)
						{
							if(%cl.team !$= %arg1.team) //Not on admins team
								messageClient(%cl, '', '\c5[A]\c1%1 \c0To kick %2 on the other team.~wgui/objective_notification.wav', %vip, %arg1.name);
							else //Is on admins team
								messageClient(%cl, '', '\c1%1 \c0%2 %3~wgui/objective_notification.wav', %vip, %msg, %yn);
						}
						else if(%cl.team $= %arg1.team)
							messageClient(%cl, '', '\c1%1 \c0%2 %3~wgui/objective_notification.wav', %vip, %msg, %yn);
					}
				}
				else //is observer
					messageAll('', '\c1%1 \c0%2 %3~wgui/objective_notification.wav', %vip, %msg, %yn);
				echo(%vip SPC %msg);
			default:
				messageAll('', '\c1%1 \c0%2 %3~wgui/objective_notification.wav', %vip, %msg, %yn);
				echo(%vip SPC %msg);
		}
	}
}


function DefaultGame::setNextMission(%game, %client, %map, %type, %mapIndex, %typeIndex)
{
   if(%client.isAdmin || %client.isSuperAdmin)
   {
      $voteNext = 1;
      $voteNextType = %typeIndex;
      $voteNextMap = %mapIndex;
      messageAll('MsgAdminChangeMission', '\c2The Admin %3 has set the next mission to %1 (%2).~wfx/misc/hunters_greed.wav', $HostMissionName[%mapIndex], $HostTypeDisplayName[%typeIndex], %client.name );
      adminLog(%client, " has set the next mission to" SPC $HostMissionName[%mapIndex] @ "(" @ $HostTypeDisplayName[%typeIndex] @ ").");
   }
   else
   {
      %totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
      // Added people who dont vote into the equation, now if you do not vote, it doesn't count as a no. - z0dd - ZOD
      if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone)) > ($Host::VotePasspercent / 100))
      {
         $voteNext = 1;
         $voteNextType = %typeIndex;
         $voteNextMap = %mapIndex;
         messageAll('MsgVotePassed', '\c2Next mission set to %1 (%2) by vote.', $HostMissionName[%mapIndex], $HostTypeDisplayName[%typeIndex]);

         //Log Vote % - Must be before Game Over
         %key = "Passed";
         votePercentLog(%missionDisplayName, %typeName, %key, %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, %game.totalVotesNone);
         //Show Vote %
         messageAll('', '\c1Vote %6: \c0Yea: %1 Nay: %2 Abstain: %7 Total: %3 [%4%5]', %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, mfloor((%game.totalVotesFor/(ClientGroup.getCount() - %game.totalVotesNone)) * 100), "%", %key, %game.totalVotesNone);
      }
      else
      {
         messageAll('MsgVoteFailed', '\c2Next mission vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount - %game.totalVotesNone) * 100));
         //Log Vote %
         %key = "Failed";
         votePercentLog(%missionDisplayName, %typeName, %key, %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, %game.totalVotesNone);
         //Show Vote %
         messageAll('', '\c1Vote %6: \c0Yea: %1 Nay: %2 Abstain: %7 Total: %3 [%4%5]', %game.totalVotesFor, %game.totalVotesAgainst, %totalVotes, mfloor((%game.totalVotesFor/(ClientGroup.getCount() - %game.totalVotesNone)) * 100), "%", %key, %game.totalVotesNone);
      }
   }
}



function serverCmdStripAdmin(%client, %admin)
{
   if(!%admin.isAdmin || !%client.isAdmin)
      return;

   if(%client $= %admin)
   {
      %admin.isAdmin = 0;
      %admin.isSuperAdmin = 0;
      messageClient(%admin, 'MsgStripAdminPlayer', 'You have stripped yourself of admin privledges.');
      logEcho(%client.nameBase @ " stripped admin from " @ %admin.nameBase, 1);
      messageAll( 'MsgClientDrop', "", %client.name, %client);
      messageAll('MsgClientJoin', "",%client.name, %client, "",%client.isAIControlled(),%client.isAdmin,%client.isSuperAdmin,%client.isSmurf,%client.sendGuid);
      messageAll('MsgClientJoinTeam', "", %client.name, Game.getTeamName(0), %client, %client.team );
      return;
   }
   else if(%client.isSuperAdmin) 
   {
      messageAll( 'MsgStripAdminPlayer', '\c2%1 removed %2\'s admin privledges.', %client.name, %admin.name, %admin );
      messageClient(%admin, 'MsgStripAdminPlayer', 'You are being stripped of your admin privledges by %1.', %client.name);
      %admin.isAdmin = 0;
      %admin.isSuperAdmin = 0;
      logEcho(%client.nameBase @ " stripped admin from " @ %admin.nameBase, 1);
      messageAll( 'MsgClientDrop', "", %client.name, %client);
      messageAll('MsgClientJoin', "",%client.name, %client, "",%client.isAIControlled(),%client.isAdmin,%client.isSuperAdmin,%client.isSmurf,%client.sendGuid);
      messageAll('MsgClientJoinTeam', "", %client.name, Game.getTeamName(0), %client, %client.team );
   }
   else
      messageClient(%client, 'MsgError', '\c2Only Super Admins can use this command.');
}


// z0dd - ZOD 4/18/02. Allow Admins to warn players
function serverCmdWarnPlayer(%client, %target)
{
   trace(0);
   if(%client.isAdmin)
   {
      messageAllExcept(%target, -1, 'MsgAdminForce', '%1 has been warned for inappropriate conduct by %2.', %target.name, %client.name);
      messageClient(%target, 'MsgAdminForce', 'You are recieving this warning for inappropriate conduct by %1. Behave or you will be kicked..~wfx/misc/lightning_impact.wav', %client.name);
      centerprint(%target, "You are recieving this warning for inappropriate conduct.\nBehave or you will be kicked.", 10, 2);
      logEcho(%client.nameBase @ " sent warning to " @ %target.nameBase);
   }
   else
      messageClient(%client, 'MsgError', '\c2Only Admins can use this command.');
}

function serverCmdTogglePlayerGag(%client, %who)
{
   if(%client.isSuperAdmin)
   {
      if(!%who.isGagged && !%who.isSuperAdmin)
      {
         %who.isGagged = true;
         messageClient(%client, 'MsgAdmin', 'You have Gagged %1.', %who.name);
         messageAllExcept(%who, -1, 'MsgAdminForce', '%1 has been Gagged by %2 for talking too much crap.', %who.name, %client.name);
         messageClient(%who, 'MsgAdminAction', 'You have Been Gagged by %1, quit talking trash and play.', %client.name);
         logEcho(%client.nameBase @ " gagged " @ %target.nameBase, 1);
      }
      else if (%who.isGagged)
      {
         %who.isGagged = false;
         messageClient(%client, 'MsgAdmin', 'You have UnGagged %1.', %who.name);
         messageAllExcept(%who, -1, 'MsgAdminAction', '%1 has been UnGagged by %2.', %who.name, %client.name);
         messageClient(%who, 'MsgAdminAction', 'You have Been UnGagged by %1, quit talking trash and play.', %client.name);
         logEcho(%client.nameBase @ " ungagged " @ %who.nameBase, 1);
      }
   }
   else
      messageClient(%client, 'MsgError', '\c2Only Super Admins can use this command.');
}

function serverCmdTogglePlayerFreeze(%client, %who)
{
   if(%client.isSuperAdmin)
   {
      if(!$MatchStarted)
      {
         messageClient(%client, 'MsgError', 'You must wait for the match to start!');
         return;
      }
      if (!%who.isFroze && !%who.isSuperAdmin)
      {
         if(!isobject(%who.player))
         {
            messageClient(%client, 'MsgError', 'You must wait for the player to spawn!');	
            return;
         }
         %who.isFroze = true;
         %who.player.setvelocity("0 0 0");
         %who.player.setMoveState(true);
         %who.player.invincible = true;
         messageClient(%client, 'MsgAdmin', 'You have Frozen %1.', %who.name);
         messageAllExcept(%who, -1, 'MsgAdminForce', '%1 has been Frozen by %2 for being a Llama.', %who.name, %client.name);
         messageClient(%who, 'MsgAdminAction', 'You have Been Frozen by %1, Think about what you have been doing.', %client.name);
         logEcho(%client.nameBase @ " froze " @ %who.nameBase, 1);
      }
      else if (%who.isFroze)
      {
         %who.isFroze = false;
         %who.player.setMoveState(false);
         %who.player.invincible = false;
         messageClient(%client, 'MsgAdmin', 'You have de-iced %1.', %who.name);
         messageAllExcept(%who, -1, 'MsgAdminForce', '%1 has been Un Frozen by %2.', %who.name, %client.name);
         messageClient(%who, 'MsgAdminAction', 'You have Been de-Iced by %1, now behave.', %client.name);
         logEcho(%client.nameBase @ " unfroze " @ %who.nameBase, 1);
      }
   }
   else
      messageClient(%client, 'MsgError', '\c2Only Super Admins can use this command.');
}

function serverCmdBootToTheRear(%client, %who)
{
   if(%client.isSuperAdmin)
   {
      if(!$MatchStarted)
      {
         messageClient(%client, 'MsgError', 'You must wait for the match to start!');
         return;
      }
      if(isObject(%who.player) && !%who.isSuperAdmin)
      {
         %time = getTime();
         %obj = %who.player;
         %vec = "0 0 10";
         %obj.applyImpulse(%obj.position, VectorScale(%vec, %obj.getDataBlock().mass*20));
         messageAllExcept(%who, -1, 'MsgAdminForce', '%1 has been given a boot to the rear by %2.', %who.name, %client.name);
         messageClient(%who, 'MsgAdminAction', 'You have Been given a boot to the ass by %1, now behave.', %client.name);
         logEcho(%client.nameBase @ " gave " @ %who.nameBase @ " a boot to the rear", 1);
      }
      else
      {
         messageClient(%client, 'MsgError', 'You must wait for the player to spawn!');
      }
   }
   else
      messageClient(%client, 'MsgError', '\c2Only Super Admins can use this command.');
}

function serverCmdExplodePlayer(%client, %who)
{
   if(%client.isSuperAdmin)
   {
      if(!$MatchStarted)
      {
         messageClient(%client, 'MsgError', 'You must wait for the match to start!');
         return;
      }
      if(isObject(%who.player) && !%who.isSuperAdmin)
      {
         %who.player.blowup();
         %who.player.scriptKill(0);
         messageAllExcept(%who, -1, 'MsgAdminForce', '%1 found some explosives in his pants planted by %2.', %who.name, %client.name);
         messageClient(%who, 'MsgAdminAction', 'You have Been dissasembled for inspection by the Super Admin %1, now behave.', %client.name);
         logEcho(%client.nameBase @ " exploded " @ %who.nameBase, 1);
      }
      else
      {
         messageClient(%client, 'MsgError', 'You must wait for the player to spawn!');
      }	
   }
   else
      messageClient(%client, 'MsgError', '\c2Only Super Admins can use this command.');
}

// z0dd - ZOD - MeBad, 7/13/03. Send client information.
function ServerCmdPrintClientInfo(%client, %targetClient)
{
   if (%client.isAdmin)
   {
      if ((!%targetClient.isSuperAdmin) && (%client.isSuperAdmin))
      {
         %wonid = getField( %targetClient.getAuthInfo(), 3);
         %ip = %targetClient.getAddress();
      }
      else
      {
         %wonid = "PROTECTED";
         %ip = "PROTECTED";
      }
      MessageClient(%client, '', '---------------------------------------------------------------');
      MessageClient(%client, 'ClientInfo', "\c3Client Info...\n" @ 
                                           "ClientName: \c2" @ %targetClient.nameBase @ "\n" @
                                           "\c3Wonid: \c2" @ %wonid @ "\n" @
                                           "\c3IP: \c2" @ %ip @ "\n\n" @
                                           "\c3TeamKills:\c2 " @ %targetClient.teamkills @ "\n" @
                                           "\c3BK (BaseKills): \c2" @ %targetClient.tkDestroys @ "\n" @
                                           "\c3Suicides:\c2 " @ %targetClient.suicides @ "\n");
      MessageClient(%client, '', '---------------------------------------------------------------');
   }
   else
      messageClient(%client, 'MsgError', '\c2Only Admins can use this command.');
}

function serverCmdPrivateMessageSent(%client, %target, %text)
{
   // Client side:
   //commandToServer('PrivateMessageSent', %target, %text);

   if((%text $= "") || spamAlert(%client))
      return;

   if(%client.isAdmin)
   {
      %snd = '~wfx/misc/diagnostic_on.wav';
      if(strlen(%text) >= $Host::MaxMessageLen)
         %text = getSubStr(%text, 0, $Host::MaxMessageLen);

      messageClient(%target, 'MsgPrivate', '\c5Message from %1: \c3%2%3', %client.name, %text, %snd);
   }
   else
      messageClient(%client, 'MsgError', '\c4Only admins can send private messages');
}


function adminLog(%client, %msg) {
   if($Host::voteMenuLog){

      %authInfo = %client.getAuthInfo();
      %msg =  formatTimeString("M-d") SPC formatTimeString("[hh:nn:a]") SPC %client.nameBase @ " (" @ getField(%authInfo, 0) @ ", " @ %ip @ ", " @ %client.guid @ ", " @ %client.getAddress() @ ")" @ %msg SPC "[" @ $CurrentMission @ "]";
      %filename = "prefs/logs/adminLog.txt";
      if (!IsFile(%filename)) {
         %fobj = new fileobject();
         %fobj.openforwrite(%filename);
         %fobj.writeline(%msg);
         %fobj.close();
         %fobj.delete();
      }
      else {
         %fobj = new fileobject();
         %fobj.openforappend(%filename);
         %fobj.writeline(%msg);
         %fobj.close();
         %fobj.delete();
      }
   }
}

function voteLog(%client, %typeName, %arg1, %arg2, %arg3, %arg4) {
   if($Host::voteMenuLog){
      // get the client info
      %authInfo = %client.getAuthInfo();
      %ip = getField(strreplace(%client.getAddress(),":","\t"),1);

      // show name for Votekick
      if(%typeName $= "VoteKickPlayer")
         %arg1 = %arg1.nameBase @ "[" @ %arg1.teamkills + 1 @ "tks]";

      // this is the info that will be logged
      %msg = "#P[" @ $HostGamePlayerCount @ "]" SPC formatTimeString("M-d") SPC formatTimeString("[hh:nn:a]") SPC %client.nameBase @ " (" @ getField(%authInfo, 0) @ "," SPC %client.guid @ ") Initiated a vote:" SPC %typeName SPC %arg1 SPC %arg2 SPC %arg3 SPC %arg4 SPC "CM[" @ $CurrentMission @ "]";
      %filename = "prefs/logs/voteLog.txt";
      
      if (!IsFile(%filename)) {
         %fobj = new fileobject();
         %fobj.openforwrite(%filename);
         %fobj.writeline(%msg);
         %fobj.close();
         %fobj.delete();
      }
      else {
         %fobj = new fileobject();
         %fobj.openforappend(%filename);
         %fobj.writeline(%msg);
         %fobj.close();
         %fobj.delete();
      }
   }
}

function votePercentLog(%display, %typeName, %key, %voteYea, %voteNay, %voteTotal, %voteNone) {
   if($Host::voteMenuLog){
      if(%typeName $= "VoteKickPlayer"){
         %percent = mFloor((%voteYea/ClientGroup.getCount()) * 100);
         %voteNone = "N/A";
         %display = %typeName SPC "[" @ %display.nameBase @ "]";
      }
      else{
        %percent = mFloor((%voteYea/(ClientGroup.getCount() - %voteNone)) * 100);
        %display = %typeName SPC "[" @ %display @ "]";
      }

      %msg = "[" @ %key @ "]" SPC %display SPC "Yea[" @ %voteYea @ "] Nay[" @ %voteNay @ "] Abstain[" @ %voteNone @ "] Total[" @ %voteTotal @ "] Vote%[" @ %percent @ "]";

      %filename = "prefs/logs/votePercentLog.txt";
      if (!IsFile(%filename)) {
         %fobj = new fileobject();
         %fobj.openforwrite(%filename);
         %fobj.writeline(%msg);
         %fobj.close();
         %fobj.delete();
      }
      else {
         %fobj = new fileobject();
         %fobj.openforappend(%filename);
         %fobj.writeline(%msg);
         %fobj.close();
         %fobj.delete();
      }
   }
}