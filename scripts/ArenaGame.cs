
// Tribes 2 Arena [Rev2] 1.0 Final
// Written By Teribaen (teribaen@planettribes.com)
// http://www.planettribes.com/t2arena/

// DisplayName = Arena

//--- GAME RULES BEGIN ---
//Eliminate the enemy team
//Don't let them eliminate you
//Last team standing wins the round
//Arena 1.0 Final By Teribaen
//http://www.planettribes.com/t2arena/
//http://arena.tribalwar.com
//--- GAME RULES END ---

$Arena::Version = 1.0;
$Arena::VersionString = "1.0";

// TODO: Disable before release
//$terSupport::Debug = true;


// ========================================================================== //
// |                                                                        | //
// |  DEPENDENCIES                                                          | //
// |                                                                        | //
// ========================================================================== //

exec( "scripts/arenaAI.cs" );

// Arena uses various extensions for things like team organization and messaging
// provided by terSupport - So if you can't find something you should look in here
exec( "scripts/terSupport.cs" );

// Load the Arena high scores table
// If this file doesn't exist it's ok
exec( "prefs/arenaHighScores.cs" );


// ========================================================================== //
// |                                                                        | //
// |  PREFERENCES                                                           | //
// |                                                                        | //
// ========================================================================== //

// Attempt to load arenaPrefs file
$Arena::PrefsLoaded = 0;
exec( "prefs/arenaPrefs.cs" );
  
if ( $Arena::PrefsLoaded != $Arena::Version )
{
  // arenaPrefs not loaded or invalid
  
  if ( $Arena::PrefsLoaded == 0 )
    echo( "prefs/arenaPrefs.cs not found - writing new file" );
  else
  {
    // There is a prefs file but it's an old version
    // Make a backup of it before writing out a new one
    
    echo( "prefs/arenaPrefs.cs is wrong version ("@$Arena::PrefsLoaded@") - writing new file" );
      
    if ( tsCopyTextFile( "prefs/arenaPrefs.cs", "prefs/arenaPrefs.old" ) )
      echo( "backup saved to prefs/arenaPrefs.old" );
  }
  
  // Create new arenaPrefs.cs file from arenaPrefs.default file included in distribution
  
  if ( tsCopyTextFile( "scripts/arenaPrefs.default", "prefs/arenaPrefs.cs" ) )
    exec( "prefs/arenaPrefs.cs" );
  else
  {
    // Failed to write new arenaPrefs file - use internal default prefs for now
    
    echo( "ERROR! COULD NOT WRITE NEW ARENAPREFS FILE - using builtin defaults" );

    $Arena::Pref::RoundLimit = 7;
    $Arena::Pref::RoundLimitTourney = 5;
    $Arena::Pref::RoundTimeLimit = 3;
    $Arena::Pref::StationLimit = 2;
    $Arena::Pref::GracePeriod = 15;
    $Arena::Pref::CountdownTime = 12;
    $Arena::Pref::WaypointLastPlayer = 1;
    $Arena::Pref::TrackHighScores = true;
    $Arena::Pref::ShuffleTeams = true;
    
    $Arena::Pref::RestrictArmor = 1;
    $Arena::Pref::SpawnWithEnergyPack = false;
    $Arena::Pref::BanCloakPack = true;
    $Arena::Pref::BanLaserRifle = false;
    $Arena::Pref::LaserDamageReduction = true;

    $Arena::Pref::MOTD = "Welcome to Tribes 2 Arena\nThere seems to have been a problem loading the prefs file\nMake sure the server has readwrite access to 'GameData/prefs/arenaPrefs.cs'";
    $Arena::Pref::MOTDLines = 3;
    $Arena::Pref::MOTDTime = 11;

    $Arena::PrefsLoaded = 0.1;
  }
}


// ========================================================================== //
// |                                                                        | //
// |  INITIALIZATION                                                        | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// ArenaGame::activatePackages()
// Activate all required overrides
// Arena uses this as a general low-level initialization point

function ArenaGame::activatePackages( %game )
{
  // Activate DefaultGame
  //  IMPLEMENTATION: The server code is supposed to activate DefaultGame
  //    but it doesn't do so if this function ("activatePackages") is
  //    overridden. This is very strange. As a result we just do it ourselves.
  activatePackage( DefaultGame );
  
  // Activate arena packages
  activatePackage( ArenaServer );
  activatePackage( ArenaObserver );
  activatePackage( ArenaInventory );
  
  // Need to initialize this here since it's used before initGameVars() is called
  // Check initGameVars() for more explanation
  %game.state = "countdown";
  
  // Sniper Rifle Ban (Server Pref)
  if ( $Arena::Pref::BanLaserRifle )
    $InvBanList[Arena, "SniperRifle"] = true;
  else
    $InvBanList[Arena, "SniperRifle"] = false;
    
  // Cloak Pack Ban (Server Pref)
  if ( $Arena::Pref::BanCloakPack )
    $InvBanList[Arena, "CloakingPack"] = true;
  else
    $InvBanList[Arena, "CloakingPack"] = false;
}

// ------------------------------------------------------------------ //
// ArenaGame::allowsProtectedStatics()
// Option Call from DefaultGame - Prevents base assets from being destroyed

function ArenaGame::allowsProtectedStatics( %game )
{
  return true;
}


// ========================================================================== //
// |                                                                        | //
// |  SHUTDOWN                                                              | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// ArenaGame::deactivatePackages()
// Deactivate all required overrides
// Arena uses this as a general low-level shutdown point

function ArenaGame::deactivatePackages( %game )
{
  // Deactivate arena packages
  //  Note that packages are being deactivated in the reverse of the order
  //  they were activated in
  deactivatePackage( ArenaInventory );
  deactivatePackage( ArenaObserver );
  deactivatePackage( ArenaServer );
  
  // Deactivate DefaultGame
  deactivatePackage( DefaultGame );
}


// ========================================================================== //
// |                                                                        | //
// |  PREGAME                                                               | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// ArenaGame::missionLoadDone()
// Called as the mission finishes loading and the countdown
//  is about to begin

function ArenaGame::missionLoadDone( %game )
{
  // DefaultGame sets up the teams for us
  // Also calls initGameVars (below)
  DefaultGame::missionLoadDone( %game );
  
  // Set team scores to zero
  // Cannot be in initGameVars because teams haven't been setup there
  for( %i = 1; %i < (%game.numTeams + 1); %i++ )
    $teamScore[%i] = 0;
    
  // >2 team round limit adjustment
  // Cannot be in initGameVars because teams haven't been setup there
  if ( Game.numTeams > 2 && %game.roundLimit > 4 )
    %game.roundLimit -= 2;
    
    
  // Make enemy teams in >2 team games different colors
  // This code was submitted by OverWear
  
  if ( Game.numTeams > 2 )
  {
    //Idea transpired from BoneRef Mod, Thx NecroBones - OverWear
    
    for(%i = 0; %i < 32; %i++)
    {
      //Randomly select team colors

      %team = (1 << %i);
      %j = 0;
      %jteam = (1 << %j);
      
	  //Make sure your not changing your teams color
      //If not change the enemy team's sensor color
      
      if(%i!=0) {
        %jteam = (1 << %j);
        setSensorGroupColor(%i,%jteam, "255 255 255 255");//White(Obs)
        %j++;
      }
      if(%i!=1) {
        %jteam = (1 << %j);
        setSensorGroupColor(%i,%jteam, "255 255 255 255");//White
        %j++;
      }
      if(%i!=2) {
        %jteam = (1 << %j);
        setSensorGroupColor(%i,%jteam, "255 255 0 255");//Yellow
        %j++;
      }
      if(%i!=3) {
        %jteam = (1 << %j);
        setSensorGroupColor(%i,%jteam, "255 0 255 255");//Purple
        %j++;
      }
      if(%i!=4) {
        %jteam = (1 << %j);
        setSensorGroupColor(%i,%jteam, "0 0 255 255");//Blue
        %j++;
      }
      if(%i!=5) {
        %jteam = (1 << %j);
        setSensorGroupColor(%i,%jteam, "0 255 255 255");//Teal Blue
        %j++;
      }
      // Set your team color to Green
      setSensorGroupColor(%i,%team, "0 255 0 255");//Green of course
    }
  }

    
  // Store warmup start time (used by billboards)
  
  $Arena::ThisWarmupStartTime = getSimTime(); // (milliseconds)
}

//
// ArenaGame::showArenaBillboard()
// Show pregame bottomprint billboards to clients
//
// Called once per client as they enter a new map
// Continues calling itself via schedule as needed

function ArenaGame::showArenaBillboard( %game, %client )
{
  cancel( %client.billboardThread );
  
  // No billboards in tourney mode
  if ( $Host::TournamentMode )
    return;
    
  // No billboards for observers
  if ( %client.team == 0 )
    return;
    
  // vars stored on client
  //  %client.lastArenaBillboard - id of last billboard (initialized to 0 elsewhere)
  
  // Billboards
  //  1 "Teams have been randomized."  if ( pref && !tourney )
  //  2 "40% laser damage reduction.." if ( pref && !pref && !tourney )
  //  3 "The laser rifle has been.." if ( pref && !tourney )
  //  4 "You don't have arena support" if ( %client.arenaSupport == 0 )
  //  5 "There's a newer version of AS.." if ( %client.arenaSupport < X )
  
  %billboardCount = 5;
  
  %timeLeftMS = $Arena::ThisWarmupStartTime + ( $Host::warmupTime * 1000 ) - getSimTime() + 10000; // 10 seconds into round
  %timeLeft = (%timeLeftMS / 1000) + 3;
  
  if ( %timeLeft >= 4 )
  {
    if ( %timeLeft > 7 )
      %timeShow = 7;
    else
      %timeShow = %timeLeft;
      
    // Show next billboard
    
    while ( !%didBillboard && %loop < %billboardCount )
    {
      if ( %client.lastArenaBillboard == %billboardCount )
        %client.lastArenaBillboard = 0;

      %client.lastArenaBillboard++;
      
      switch ( %client.lastArenaBillboard )
      {
        case 1:
          if ( $Arena::Pref::ShuffleTeams )
          {
            bottomPrint( %client, "\nTeams have been randomized.\n", %timeShow, 3 );
            %didBillboard = true;
          }
          
        case 2:
          if ( $Arena::Pref::LaserDamageReduction && !$Arena::Pref::BanLaserRifle )
          {
            bottomPrint( %client, "\n40% laser damage reduction is in effect.\n", %timeShow, 3 );
            %didBillboard = true;
          }
          
        case 3:
          if ( $Arena::Pref::BanLaserRifle )
          {
            bottomPrint( %client, "\nThe laser rifle has been disabled by the server admin.\n", %timeShow, 3 );
            %didBillboard = true;
          }
          
        case 4:
          if ( %client.arenaSupport == 0 )
          {
            bottomPrint( %client, "You don't have ArenaSupport installed\nArenaSupport is the optional client half of Arena\nYou can download it at http://www.planettribes.com/t2arena/", %timeShow, 3 );
            %didBillboard = true;
          }
          
        case 5:
          echo ( "case 5: " SPC %client.arenaSupport );
          if ( %client.arenaSupport > 0 && %client.arenaSupport < 0.98 )
          {
            bottomPrint( %client, "You have ArenaSupport installed but there is a newer version available\nArenaSupport is the optional client half of Arena\nYou can download it at http://www.planettribes.com/t2arena/", %timeShow, 3 );
            %didBillboard = true;
          }
      }
    }
  }
  
  // See if we should schedule another billboard

  if ( %timeLeft > 6 )
    %client.billboardThread = %game.schedule( 6000, "showArenaBillboard", %client );

}

// ------------------------------------------------------------------ //
// ArenaGame::initGameVars()
// Setup game variables before the countdown begins

function ArenaGame::initGameVars( %game )
{
  // Needed so defaultGame will process scoring updates correctly
  // IMPORTANT: These do not affect the calculation of scores
  // Check how defaultGame uses them if you don't understand
  %game.SCORE_PER_DEATH = -1;
  %game.SCORE_PER_TEAMKILL = -1;
    
  // Determine what the round limit is
  %game.roundLimit = $Arena::Pref::RoundLimit;
  
  if ( %game.roundLimit < 1 && %game.roundLimit != 0 )    // 0=infinite
    TS_ERROR( "$Arena::Pref::RoundLimit out of range - but attempting to continue" );
  
  // Allow the mapper to adjust the round limit
  // But no adjustment in tournament mode
  %roundAdj = MissionGroup.Arena_roundLimitAdjustment;
  if( %roundAdj !$= "" && %game.roundLimit != 0 && !$Host::TournamentMode )
  {
    %roundAdj = mFloor( %roundAdj );
    
    if ( %roundAdj < -3 || %roundAdj > 3 )
      TS_ERROR( "mapper specified round limit adjustment out of range" );
    else
    {
      %game.roundLimit += %roundAdj;
      
      if ( %game.roundLimit <= 0 )
      {
        TS_ERROR( "result of application of round limit adjustment out of range" );
        %game.roundLimit -= %roundAdj;
      }
      else
        TS_INFO( "mapper adjusted round limit by " @ %roundAdj @ " for this map" );
    }
  }
  
  if ( !$Host::TournamentMode )
    TS_INFO( "the round limit for this map is " @ %game.roundLimit );
  else
  {
    // Auto set round limit to $Arena::Pref::RoundLimitTourney in tourney mode
    
    %game.roundLimit = $Arena::Pref::RoundLimitTourney;
    TS_INFO( "round limit set to "@%game.roundLimit@" since the server is in tournament mode" );
  }
  
  // Determine what the base time limit is
  %game.baseTimeLimit = $Arena::Pref::RoundTimelimit;
  
  if ( ( %game.baseTimeLimit < 0.5 || %game.baseTimeLimit > 7 ) && %game.baseTimeLimit != 0 )
    TS_ERROR( "$Arena::Pref::RoundTimelimit out of range - but attempting to continue" );
  
  // Allow the mapper to adjust the base time limit
  %timeAdj = MissionGroup.Arena_timeLimitAdjustment;
  if ( %timeAdj !$= "" && %game.baseTimeLimit != 0 && !$Host::TournamentMode )
  {
    if ( %timeAdj < -2 || %timeAdj > 2 )
      TS_ERROR( "mapper specified time limit adjustment out of range" );
    else
    {
      %game.baseTimeLimit += %timeAdj;
      
      if ( %game.baseTimeLimit < 0.5 )
      {
        TS_ERROR( "result of application of time limit adjustment out of range" );
        %game.baseTimeLimit -= %timeAdj;
      }
      else
        TS_INFO( "mapper adjusted time limit by " @ %timeAdj @ " for this map" );
    }
  }
  
  if ( !$Host::TournamentMode )
    TS_INFO( "the time limit for this map is " @ %game.baseTimeLimit );
    
    
  // Admin Time Limit
  // Allows the admin to lock the time limit to a fixed (non-adaptive) value for one match
  // This is where it's reset between matches
  
  %game.adminTimeLimit = 0;    // 0 = disabled
  
  // Grace Period Init
  // See grace period section for explanation

  %game.gracePeriodActive = false;
  %game.gracePeriodClrSch = "";
  
  // Game.state
  // Tells the arena code what state it's in (this is an important variable)
  //  "countdown" - Either pre-match or pre-round countdown
  //  "play"      - A round is in progress
  //  "wait"      - Short pause after a round is over
  
  %game.state = "countdown";  // redundant - initialized in activatePackages()
}


// ========================================================================== //
// |                                                                        | //
// |  MATCH/ROUND START                                                     | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// ArenaGame::startMatch()
// Called as the countdown ends - beginning the first round

function ArenaGame::startMatch(%game)
{
  TS_INFO( "START MATCH" );
  MessageAll( 'MsgMissionStart', "\c2Match started!" );

  // Clear the team rank array
  for ( %i = 0; %i < 32; %i++ )
    %game.clearTeamRankArray( %i );
      
  // Reset the scores of all the clients and add them to the rank array
  %clCount = ClientGroup.getCount();
  for ( %i = 0; %i < %clCount; %i++ )
  {
    %client = ClientGroup.getObject( %i );
      
    %game.resetScore( %client );
    %game.populateTeamRankArray( %client );
  }

  // Flag that the match has started
  // (Various base T2 handlers use this to do their thing)
  $matchStarted = true;

  // Record the match start time
  $missionStartTime = getSimTime();
  
  // Hook onto unified match/round start code
  %game.startRound();
}


// ------------------------------------------------------------------ //
// ArenaGame::startRound()
// Free all clients and begin a round

function ArenaGame::startRound( %game )
{
  // Give all clients control of their player

  %clCount = ClientGroup.getCount();
  for ( %i = 0; %i < %clCount; %i++ )
  {
    %client = ClientGroup.getObject(%i);
    
    // Record the team this player was on at the beginning of the round
    // (this is used in awarding round win/loss points)
    //  yes, I intend to mark observers too (they must be cleared)
    //  this is also done in the grace period end handler for slickness purposes
    %client.arenaRoundStartTeam = %client.team;
    
    // Reset per-round kill counter
    %client.roundKills = 0;
    
    // Don't touch observers further
    if ( %client.team == 0 )
      continue;

    // Just switch the camera over
    commandToClient( %client, 'setHudMode', 'Standard' );
    %client.setControlObject( %client.player );

  }
  
  
   
  // Just in case some silly map has deployables..
  %game.clearDeployableMaxes();
  
  // For maps with switches
  %game.resetFlipFlops();
  

  // Find out what the time limit is
  %game.setupTimeLimit();
  
  // Record round start time
  %game.roundStartTime = getSimTime();
  
  // Schedule round end due to time
  %game.checkRoundTimeLimit( true );
  
  // Schedule round end countdown messages
  %game.scheduleRoundEndCountdown( %game.timeLimit * 60 * 1000 );
  
  
  // Find out what the laser purchase limit is (tourney mode only)
  if ( $Host::TournamentMode )
    %game.setupLaserLimit();
  
  
  // Update the objectiveHUD
  arenaUpdateObjectiveHUD();
  
  
  // Disable team damage for the first 10 seconds
  if ( $teamDamage )
    %game.tdEnableSch = %game.schedule( 10000, "enableTeamDamage" );

  $teamDamage = false;
  
  
  // Allow the AI to move and stuff
  AISystemEnabled( true );

  // Grace period where clients can still spawn
  %game.setGracePeriod();
  

  // on with the show this is it!
  %game.state = "play";
}

//
// ArenaGame::enableTeamDamage()
// Called on a schedule from startRound to put team damage back on after
// the first 10 seconds of the round

function ArenaGame::enableTeamDamage( %game )
{
  $teamDamage = true;
}

// ------------------------------------------------------------------ //
// ArenaGame::setupTimeLimit()
// Called at the beginning of each round to set Game.timeLimit

function ArenaGame::setupTimeLimit( %game )
{
  if ( %game.adminTimeLimit != 0 )
    // Time limit fixed for this match by admin
    %game.timeLimit = %game.adminTimeLimit;
  else if ( $Host::TournamentMode )
    // Time limit locked to 5 minutes in tourney mode
    %game.timeLimit = 5;
  else
  {
    // Set the time limit from the base found in initGameVars
    %game.timeLimit = %game.baseTimeLimit;
    
    if ( %game.timeLimit == 0 )
      return;
      
    // Adapt time limit to number of players
    // This is completely arbitrary
    if ( ClientGroup.getCount() > 10 )
      %game.timeLimit += 0.5;
    if ( ClientGroup.getCount() > 14 )
      %game.timeLimit += 0.5;
    if ( ClientGroup.getCount() > 19 )
      %game.timeLimit += 0.5;
    if ( ClientGroup.getCount() > 24 )
      %game.timeLimit += 0.5;
      
    // Adapt time limit to 3 or 4 teams
    if ( Game.numTeams > 2 )
      %game.timeLimit += 0.5;
    if ( Game.numTeams > 3 )
      %game.timeLimit += 0.5;
  }

  // Tell players what the time limit is
  %minutes = mFloor( %game.timeLimit );
  %seconds = mFloor( ( %game.timeLimit - %minutes ) * 60 );
  
  %tlPlural = ( %minutes != 1 ? "s" : "" );

  if ( %seconds == 0 )
    MessageAll( 0, "\c1The time limit for this round will be " @ %minutes @ " minute"@%tlPlural@"." );
  else
    MessageAll( 0, "\c1The time limit for this round will be " @ %minutes @ " minute"@%tlPlural@" and " @ %seconds @ " seconds." );
}

//
// ArenaGame::setupLaserLimit()
// Sets up gamewide laser purchase limit vars in preparation for a new round

function ArenaGame::setupLaserLimit( %game )
{
  // Determine what the laser limit will be for this round
  
  %playerCount = tsGetLargestTeamPlayerCount();
  
  %game.arenaLaserLimit = %playerCount / 3;
  
  if ( %game.arenaLaserLimit < 2 )
    %game.arenaLaserLimit = 2;

  // Reset all team laser trackers

  for ( %iTeam = 1; %iTeam <= Game.numTeams; %iTeam++ )
    %game.arenaLasersAvailable[%iTeam] = %game.arenaLaserLimit;
    
  // Tell players what the laser limit is
  
  MessageAll( 0, "\c1The laser purchase limit for this round will be " @ %game.arenaLaserLimit @ "." );
}


// ========================================================================== //
// |                                                                        | //
// |  ROUND EXECUTION                                                       | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// ArenaGame::onClientKilled()
// Called to inform the game that a player has been killed
// Arena's round execution/advancement is called based on this event

function ArenaGame::onClientKilled( %game, %clVictim, %clKiller, %damageType, %implement, %damageLocation )
{
  // Stop OOB damage thread
  cancel( %clVictim.player.alertThread );
  
  // Remove final player objectives from this player
  
  if ( %clVictim.hasArenaWaypointTo )
    %game.waypointRevoke( %clVictim );
  
  // Parent handles notifications and lots of other stuff
  Parent::onClientKilled( %game, %clVictim, %clKiller, %damageType, %implement, %damageLocation );
  
  // Don't force observer for slow respawn
  cancel( %clVictim.respawnTimer );
  
  // If we're inside a countdown we don't need to do much here
  if ( %game.state $= "countdown" )
  {
    %clVictim.isAlive = false;  // Just in case
    arenaUpdateObjectiveHUD( %clVictim.team );
    
    return;
  }
  
  // Handle AI clients
  if ( %clVictim.isAIControlled() )
  {
    // Cancel AI respawn thread
    cancel( %clVictim.respawnThread );
    
    // Cancel AI objective thread
    cancel( %clVictim.objectiveThread );
  }

  // Actions taken only if a round is in progress
  if ( %game.state $= "play" )
  {
    // Mark the victim as dead
    arenaSetClientDead( %clVictim );
    
    // Check for the end of the round
    %game.checkRoundEnd( %clVictim.team );
    
    // Check if the victim's team is down to one player
    %game.checkFinalPlayer( %clVictim.team );
    
    // Update final player objectives to this player

    if ( %clVictim.isFinalPlayer )
    {
      %clVictim.isFinalPlayer = false;

      %game.revokeWaypointsTo( %clVictim );
      %game.evaluateAllClientWaypoints();
    }
  }
  
  // Block observer triggers for the victim temporarily
  // (Allows them to see their death)
  %clVictim.blockDeathTrigger = true;
  %clVictim.unblockTriggerThread = %game.schedule( 3000, "TriggerUnblock", %clVictim );
  
  // Update the objectiveHUD
  arenaUpdateObjectiveHUD( %clVictim.team );
}

// ------------------------------------------------------------------ //
// ArenaGame::TriggerUnblock()
// Clears the death trigger block in onClientKilled (called by schedule)

function ArenaGame::TriggerUnblock( %game, %client )
{
  %client.blockDeathTrigger = false;
}


// ========================================================================== //
// |                                                                        | //
// |  FINAL PLAYER WAYPOINTS                                                | //
// |                                                                        | //
// ========================================================================== //

//
// ArenaGame::checkFinalPlayer()
// Check to see if a team is down to one player and if so perform notifications

function ArenaGame::checkFinalPlayer( %game, %team )
{
  if ( arenaCountTeamLivingClients( %team ) == 1 )
  {
    %clCount = ClientGroup.getCount();
    
    for( %iCl = 0; %iCl < %clCount; %iCl++ )
    {
      %client = ClientGroup.getObject( %iCl );

      if ( %client.team == %team && %client.isAlive )
      {
        %teamName = getTaggedString(%game.getTeamName( %team ));
        
        messageClient( %client, 'MsgArenaYouAreFinalPlayer', "\c2You are the only player left on your team.~wfx/misc/target_waypoint.wav" );
        messageTeamExcept( %client, 'MsgArenaOnePlayerLeft', "\c2Your team has only one player left.~wfx/misc/target_waypoint.wav", %team, %client.name );
        tsMessageExceptTeam( %team, 'MsgArenaOnePlayerLeft', "\c2" @ %teamName @ " has only one player left.~wfx/misc/target_waypoint.wav", %team, %client.name );
        
        // Set final player flag

        %client.isFinalPlayer = true;

        // Give the opposing teams a waypoint

        %game.evaluateAllClientWaypoints();
        
        return;
      }
    }
  }
}

//
// ArenaGame::evaluateAllClientWaypoints()
// Add/Remove waypoints for every active client in the game

function ArenaGame::evaluateAllClientWaypoints( %game )
{
  %clCount = ClientGroup.getCount();
  
  // Implement waypoint pref
  if ( $Arena::Pref::WaypointLastPlayer == 0 ||
       ( $Arena::Pref::WaypointLastPlayer < 2 && $Host::TournamentMode ) )
    return;
  
  // For every team in the game..
  
  for ( %iTeam = 1; %iTeam <= Game.numTeams; %iTeam++ )
  {
    // Check if only one player is left
    
    if ( arenaCountTeamLivingClients( %iTeam ) == 1 && tsPlayerCountTeam( %iTeam ) > 1 )
    {
      %clTarget = "";
      
      // Find that one player
      
      for ( %iClFind = 0; %iClFind < %clCount; %iClFind++ )
      {
        %clFind = ClientGroup.getObject( %iClFind );
        
        if ( %clFind.team == %iTeam && %clFind.isAlive )
        {
          %clTarget = %clFind;
          break;
        }
      }
      
      if ( %iClFind == %clCount || %clTarget $= "" )
        continue;
      
      // Set a waypoint to that player for every active player except them
      
      for ( %iClAssign = 0; %iClAssign < %clCount; %iClAssign++ )
      {
        %clAssign = ClientGroup.getObject( %iClAssign );
        
        if ( %clAssign.team != 0 && %clAssign != %clTarget && %clAssign.isAlive )
          if ( !%clAssign.hasArenaWaypointTo )
            %game.waypointGrant( %clAssign, %clTarget );
      }
    }
  }
}

//
// ArenaGame::waypointGrant()
// Give a player an arena final player waypoint to another player

function ArenaGame::waypointGrant( %game, %client, %target )
{
  // Implement waypoint prefs
  
  if ( $Arena::Pref::WaypointLastPlayer == 0 ||
       ( $Arena::Pref::WaypointLastPlayer < 2 && $Host::TournamentMode ) )
    return;
    
  // Implement client waypoint pref

  if ( !%client.arenaPrefShowWaypoints )
    return;
    
  // Handle waypoint as a bot objective
  
  if ( %client.isAIControlled() )
  {
    // Is this working? Is it causing those "add" error messages?
    // well.. it gives the AI the current position correctly (no updates)
    // So I guess we can keep it for now
    %client.clientDetected( %target );
    return;
  }
  else
  {
    // Grant waypoint to a regular client
  
    %game.enableInfiniteSensorVis( %client, %target );
    %client.ArenaFPVisClearThread = %game.schedule( 5000, "disableInfiniteSensorVis", %client, %target );
    
    %client.setTargetId( %target.target );
    commandToClient( %client, 'TaskInfo', %client, -1, false, "Kill " @ %target.nameBase );
    %client.sendTargetTo( %client, true );
  }
  
  // Flag that they have a waypoint

  %client.hasArenaWaypointTo = %target;
}

//
// ArenaGame::revokeWaypointsTo()
// Revoke all waypoints that to a specified player

function ArenaGame::revokeWaypointsTo( %game, %target )
{
  %clCount = ClientGroup.getCount();
  
  for ( %iCl = 0; %iCl < %clCount; %iCl++ )
  {
    %client = ClientGroup.getObject( %iCl );
    
    if ( %client.hasArenaWaypointTo == %target )
      %game.waypointRevoke( %client );
  }
}

//
// ArenaGame::waypointRevoke()
// Taketh away all arena final player waypoints from a player

function ArenaGame::waypointRevoke( %game, %client )
{
  // Implement waypoint pref
  if ( $Arena::Pref::WaypointLastPlayer == 0 ||
       ( $Arena::Pref::WaypointLastPlayer < 2 && $Host::TournamentMode ) )
    return;
    
  // Remove all assigned task waypoints (no better way?)
  
  if ( !%client.isAIControlled() )
    removeClientTargetType( %client, "AssignedTask" );
    
  // Flag that they have no waypoint

  %client.hasArenaWaypointTo = "";
}

//
// ArenaGame::enableInfiniteSensorVis()
// Enable infinite sensor visibility (used to initially show waypoints)

function ArenaGame::enableInfiniteSensorVis( %game, %client, %target )
{
  %target.player.scopeToClient( %client );
  %visMask = getSensorGroupAlwaysVisMask( %target.getSensorGroup() );
  %visMask |= (1 << %client.getSensorGroup());
  setSensorGroupAlwaysVisMask( %target.getSensorGroup(), %visMask );
}

//
// ArenaGame::disableInfiniteSensorVis()
// Disable infinite sensor visibility

function ArenaGame::disableInfiniteSensorVis( %game, %client, %target )
{
  %visMask = getSensorGroupAlwaysVisMask( %target.getSensorGroup() );
  %visMask &= ~(1 << %client.getSensorGroup());
  setSensorGroupAlwaysVisMask( %target.getSensorGroup(), %visMask );
}


// ========================================================================== //
// |                                                                        | //
// |  TIME LIMITS                                                           | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// ArenaGame::checkTimeLimit()
// Overridden to disable defaultGame match timelimits

function ArenaGame::checkTimeLimit( %game, %forced )
{
  // Do Nothing
}


// ------------------------------------------------------------------ //
// ArenaGame::checkRoundTimeLimit()
// Check if the round time limit has been reached
// Self-Calling: This function will automatically schedule calls to itself
// This function only needs to be called at the beginning of each round

function ArenaGame::checkRoundTimeLimit( %game, %forced )
{
  // Don't add extra checks
  cancel( %game.timeCheck );

  // If there is no time limit check back in 20 seconds to see if one has been set
  if( %game.timeLimit $= "" || %game.timeLimit == 0 )
  {
    %game.timeCheck = %game.schedule( 20000, "checkRoundTimeLimit" );
    return;
  }

  // Calculate how many milliseconds are left in the round
  %curTimeLeftMS = ( %game.timeLimit * 60 * 1000 ) + %game.roundStartTime - getSimTime();

  // Check for time limit reached
  if (%curTimeLeftMS <= 0)
  {
    // time's up, put down your pencils
    %game.roundTimeLimitReached();
  }
  else
  {
    // Schedule another check for 20 seconds or the end of the round
    // (whichever is sooner)
    if( %curTimeLeftMS >= 20000 )
      %game.timeCheck = %game.schedule( 20000, "checkRoundTimeLimit" );
    else
      %game.timeCheck = %game.schedule( %curTimeLeftMS + 1, "checkRoundTimeLimit" );
88
    // Now synchronize everyone's clock
    messageAll( 'MsgSystemClock', "", %game.timeLimit, %curTimeLeftMS );
  }
}

// ------------------------------------------------------------------ //
// ArenaGame::roundTimeLimitReached()
// End the current round after the round time limit was reached

function ArenaGame::roundTimeLimitReached(%game)
{
  TS_INFO( "round ended (timelimit)" );

  // Find the team with the most players still alive
  // Must also detect ties
  
  %winTeam = 1;
  %winCount = arenaCountTeamLivingClients( 1 );
  %winTie = false;
  
  for ( %i = 2; %i <= Game.numTeams; %i++ )
  {
    %curCount = arenaCountTeamLivingClients( %i );
    
    if ( %curCount > %winCount )
    {
      %winTeam = %i;
      %winCount = %curCount;
      %winTie = false;
    }
    else if ( %curCount == %winCount )
    {
      %winTie = true;
    }
  }

  // Team with the most players still alive wins
  
  if ( %winTie )
    %game.endRound( 0, true );
  else
  {
    %game.endRound( %winTeam );
    messageAll( 0, "\c2Round ended because the time limit was reached." );
  }
}


// ========================================================================== //
// |                                                                        | //
// |  ROUND END                                                             | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// ArenaGame::checkRoundEnd()
// Checks to see if specified team has been eliminated
// If only one team is left end the round

function ArenaGame::checkRoundEnd( %game, %teamIndex )
{
  // Check that team has at least one player and that all players are dead
  if ( tsPlayerCountTeam( %teamIndex ) > 0
       && arenaCountTeamLivingClients( %teamIndex ) == 0 )
  {
    // In a game with >2 teams we announce when teams are eliminated
    if ( Game.numTeams > 2 )
    {
      %teamName = getTaggedString(%game.getTeamName( %teamIndex ));
    
      tsMessageExceptTeam( %teamIndex, 0, "\c2The "@ %teamName @ " team has been eliminated.~wfx/misc/target_waypoint.wav" );
      messageTeam( %teamIndex, 0, "\c2Your team has been eliminated.~wfx/misc/target_waypoint.wav" );
    }
  
    // Look for other teams who are still alive
    %livingTeam = 0;
    %livingCount = 0;
    
    for ( %i = 1; %i <= Game.numTeams; %i++ )
    {
      if ( arenaCountTeamLivingClients( %i ) > 0 )
      {
        %livingTeam = %i;
        %livingCount++;
      }
    }
    
    // If no teams are alive the round ends in a tie
    if ( %livingCount == 0 )
      %game.endRound( 0 );
    // If only one team is still alive they win the round
    else if ( %livingCount == 1 )
      %game.endRound( %livingTeam );
  }
}

// ------------------------------------------------------------------ //
// ArenaGame::endRound()
// Start to bring an end to the current round
// This is the entry point for the end/new/start schedule chain
// Pass the index of the team which won - or zero for a tie

function ArenaGame::endRound( %game, %winningTeamIndex, %bCausedByTime )
{
  if ( %bCausedByTime $= "" )
    %bCausedByTime = false;
    
  // No longer in "play" mode
  %game.state = "wait";

  // Cancel round timeout here
  cancel( %game.timeCheck );
  %game.cancelRoundEndCountdown();
  
  // If nobody won we don't have to do much..
  if ( %winningTeamIndex == 0 )
  {
    messageAll( 'MsgArenaRoundEnd', "", %winningTeamIndex );
    
    if ( %bCausedByTime )
    {
      CenterPrintAll( "\nRound ended in a TIE because the time limit was reached.", 5, 3 );
      MessageAll( 0, "\c2Round ended in a TIE because the time limit was reached." );
    }
    else
    {
      CenterPrintAll( "\nRound ended in a TIE", 5, 3 );
      MessageAll( 0, "\c2Round ended in a TIE." );
    }
  }
  else
  // But if someone won we have some rather complex messaging to do
  {
    messageAll( 'MsgArenaRoundEnd', "", %winningTeamIndex, %game.getTeamName(%winningTeamIndex), $teamScore[%winningTeamIndex] + 1 );
    
    %winnerName = getTaggedString(%game.getTeamName( %winningTeamIndex ));
    
    %rsPlural = ( $TeamScore[%winningTeamIndex] + 1 != 1 ? "s" : "" );
    %roundStatus = " won " @ $TeamScore[%winningTeamIndex] + 1 @ " round" @ %rsPlural @ " of " @ %game.roundLimit @ " needed to win the game.";
    %roundStatusVerbose = " won " @ $TeamScore[%winningTeamIndex] + 1 @ " round" @ %rsPlural @ " of the " @ %game.roundLimit @ " rounds needed to win the game.";

    TS_INFO( %winnerName @ " won the round" );

    // Messages to winning team
    tsCenterPrintTeam( %winningTeamIndex, "Your team wins the round!\n" @ "You have" @ %roundStatus, 5, 2 );
    messageTeam( %winningTeamIndex, 0, "\c2Your team wins the round!~wfx/misc/flag_capture.wav" );
    messageTeam( %winningTeamIndex, 0, "\c2Your team has" @ %roundStatusVerbose );
    
    // Messages to everyone else
    tsCenterPrintExceptTeam( %winningTeamIndex, %winnerName @ " wins the round.\n" @ %winnerName @ " has" @ %roundStatus, 5, 2 );
    tsMessageExceptTeam( %winningTeamIndex, 0, "\c2" @ %winnerName @ " wins the round." );
    tsMessageExceptTeam( %winningTeamIndex, 0, "\c2" @ %winnerName @ " has" @ %roundStatus );
    
    // Sounds
    messageTeam( %winningTeamIndex, 0, "~wfx/misc/flag_capture.wav" );
    messageTeam( 0, 0, "~wfx/misc/flag_capture.wav" );
    
    for ( %i = 1; %i <= Game.numTeams; %i++ )
      if ( %i != %winningTeamIndex )
        messageTeam( %i, 0, "~wfx/misc/flag_lost.wav" );
    
    // Update scores
    $TeamScore[%winningTeamIndex]++;
    messageAll( 'MsgTeamScoreIs', "", %winningTeamIndex, $TeamScore[%winningTeamIndex] );
    
    // Award individual win/loss points
    %clCount = ClientGroup.getCount();
    for ( %iCl = 0; %iCl < %clCount; %iCl++ )
    {
      %client = ClientGroup.getObject( %iCl );
      
      if ( %client.team == 0 )
        continue;
        
      // check for team changed since round start
      if ( %client.team != %client.arenaRoundStartTeam )
        continue;

      if ( %client.team == %winningTeamIndex )
        %client.roundsWon++;
      else
        %client.roundsLost++;
        
      %game.recalcScore( %client );
    }
    
    messageTeam( %winningTeamIndex, 0, "\c0You receive a one point bonus for winning the round." );
    
    // Check for win
    if ( %game.checkRoundLimit( %winningTeamIndex ) )
      return;
  }

  // Schedule client cleanup and next round
  %game.newRoundSch = %game.schedule( 5000, "newRound" );
}


// ========================================================================== //
// |                                                                        | //
// |  NEW ROUND                                                             | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// ArenaGame::newRound()
// Clear out the remains of the last round
// Spawn players into the countdown for the next round

function ArenaGame::newRound( %game )
{
  // Update Game.state first (other actions depend on it)
  %game.state = "countdown";

  // Kill all remaining clients
  // If we don't kill the clients this way the game will get confused about
  //  things like inventories for the next round
  %clCount = ClientGroup.getCount();
  
  for ( %j = 0; %j < %clCount; %j++ )
  {
    %client = ClientGroup.getObject( %j );
    if ( %client.team != 0 && %client.isAlive && isObject(%client.player) )
      %client.player.scriptKill( 0 );
  }
  
  // Clear debris (packs, weapons, bodies)
  // This deletes all player objects too
  // IMPORTANT: This uses the housekeeping function from the siege code
  %clean = nameToID( "MissionCleanup" );
  %clean.housekeeping();
  
  // Disable the AI until the round actually starts
  AISystemEnabled( false );

  // Respawn Clients
  %clCount = ClientGroup.getCount();
  
  for ( %iCl = 0; %iCl < %clCount; %iCl++ )
  {
    %client = ClientGroup.getObject( %iCl );
    
    // Don't mess with observers
    if ( %client.team == 0 )
      continue;
      
    // Check for AI controlled players
    if ( %client.isAIControlled() )
    {
      // Hook into the AI respawn code
      onAIRespawn( %client );
    }
    else
    {
      // Respawn the player
      arenaSpawnClient( %client );

      // Clear any observer messages
      clearBottomPrint( %client );
    }
  }
  
  // Update objectiveHUD
  arenaUpdateObjectiveHUD();
  
  if ( !$Host::TournamentMode )
    %countdownTime = $Arena::Pref::CountdownTime * 1000;
  else
    %countdownTime = 32000;

  // Schedule start of next round
  %game.newRoundSch = %game.schedule( %countdownTime, "startRound" );
  
  // Schedule countdown to next round
  %game.scheduleRoundCountdown( %countdownTime );
}


// ========================================================================== //
// |                                                                        | //
// |  ENDGAME                                                               | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// ArenaGame::checkRoundLimit()
// Check whether a team has hit the round (score) limit
// Take appropriate action if it has

function ArenaGame::checkRoundLimit( %game, %team )
{
  // Verify that there is a roundlimit and that the team has met it
  if( %game.roundLimit != 0 && $TeamScore[%team] >= %game.roundLimit )
  {
    // End the match
    %game.roundLimitReached();
    
    return true;
  }
}

// ------------------------------------------------------------------ //
// ArenaGame::roundLimitReached()
// Called when a team hits the round (score) limit

function ArenaGame::roundLimitReached( %game )
{
  TS_INFO( "game over (roundlimit)" );
  %game.gameOver();
  cycleMissions();
}

function ArenaGame::scoreLimitReached( %game )
{
  // Provided for compatibility with any other mods
  %game.roundLimitReached();
}

// ------------------------------------------------------------------ //
// ArenaGame::gameOver()
// Called when the game has ended (for whatever reason)

function ArenaGame::gameOver( %game )
{
  // Cancel new round schedules
  cancel( %game.newRoundSch );
  %game.cancelRoundCountdown();
  
  // Cancel end round schedules
  cancel( %game.timeCheck );
  %game.cancelRoundEndCountdown();
  
  // Record the high scores
  if ( $Arena::Pref::TrackHighScores )
    %game.recordHighScores();

  // Default version clears it's game vars and takes care of the debriefing
  DefaultGame::gameOver( %game );

  // Determine the name of the winning team
  %winner = "";
  %topScore = "";
  %bTied = false;
  for ( %team = 1; %team <= %game.numTeams; %team++ )
  {
    if ( %topScore $= "" || $TeamScore[%team] > %topScore )
    {
      %topScore = $TeamScore[%team];
      %winner = %game.getTeamName(%team);
      %bTied = false;
    }
    else if ( $TeamScore[%team] == %topScore )
    {
      // Multiple teams tied for lead..
      %winner = "";
      %bTied = true;
    }
  }

  // Play the announcer voice
  // Copied from CTFGame
    
  if ( !%bTied )
  {
    if (%winner $= 'Storm')
       messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.stowins.wav" );
    else if (%winner $= 'Inferno')
       messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.infwins.wav" );
    else if (%winner $= 'Starwolf')
       messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.swwin.wav" );
    else if (%winner $= 'Blood Eagle')
       messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.bewin.wav" );
    else if (%winner $= 'Diamond Sword')
       messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.dswin.wav" );
    else if (%winner $= 'Phoenix')
       messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.pxwin.wav" );
    else
       messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.gameover.wav" );
  }
  else
    messageAll('MsgGameOver', "Match has ended.~wvoice/announcer/ann.gameover.wav" );

  // Clear everyone's objectiveHuds
  messageAll( 'MsgClearObjHud', "" );
   
  // Reset the scores of all the clients
  for( %i = 0; %i < ClientGroup.getCount(); %i++ )
  {
    %client = ClientGroup.getObject( %i );
    %game.resetScore( %client );
  }
}

// ------------------------------------------------------------------ //
// ArenaGame::recordHighScores()
// Records the highest score (if it beats the current one) at the end of the game

function ArenaGame::recordHighScores( %game )
{
  if ( %game.class !$= "ArenaGame" )
    return;

  // Check for and record new high scores
  
  // There must be at least 5 humans in the game to set recieve a new high score
  %humanCount = 0;
  for ( %iCl = 0; %iCl < ClientGroup.getCount(); %iCl++ )
    if ( !ClientGroup.getObject(%iCl).isAIControlled() )
      %humanCount++;
  
  // Find the client with the highest score
  if ( ClientGroup.getCount() < 1 )
    return;    // No client = no new high score

  %highClient = ClientGroup.getObject(0);
  for ( %iCl = 1; %iCl < ClientGroup.getCount(); %iCl++ )
    if ( ClientGroup.getObject(%iCl).score > %highClient.score )
      %highClient = ClientGroup.getObject(%iCl);
      
  // If the score is higher than the current one (or no current one exists), record it
  if ( %highClient.score > 0 && ( $Arena::HighScores::Score[$currentMission] $= ""
       || %highClient.score > $Arena::HighScores::Score[$currentMission] ) )
  {
    if ( %humanCount < 5 )
    {
      %game.notEnoughHumansFlag = true;
      return;
    }
    %game.notEnoughHumansFlag = false;
  
    // Notify players of new high score
    if ( $Arena::HighScores::Score[$currentMission] $= ""
         || $Arena::HighScores::Name[$currentMission] $= "" )
    {
      messageAll( 'MsgArenaNewHighScore',
                  "\c2"@%highClient.nameBase@" has set a new individual record for this mission with a score of "@%highClient.score@".",
                  %highClient.name,
                  %highClient.score );
    }
    else
    {
      messageAll( 'MsgArenaNewHighScore',
                  "\c2"@%highClient.nameBase@" has set a new individual record for this mission with a score of "@%highClient.score@". The previous record was held by "@$Arena::HighScores::Name[$currentMission]@" with a score of "@$Arena::HighScores::Score[$currentMission]@".",
                  %highClient.name,
                  %highClient.score );
    }
    
    // Record the score in the high score arrays
    $Arena::HighScores::Score[$currentMission] = %highClient.score;
    $Arena::HighScores::Name[$currentMission] = %highClient.nameBase;
    
    // Export the (entire) high score table to a script file
    // We simply execute the script file later to get the values
    export( "$Arena::HighScores::*", "prefs/arenaHighScores.cs", false );
    
    // Set a flag so that the debriefing displays the score as being new
    %game.newHighScoreFlag = true;
    
    return;
  }

  %game.newHighScoreFlag = false;
}


// ========================================================================== //
// |                                                                        | //
// |  OBSERVER FUNCTIONS                                                    | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// ArenaGame::ObserverOnTrigger()
// Handle trigger messages from players who are observing
// This is supposed to be a game override for Observer::OnTrigger
// But we're using it to reimplement that function without packaging
// IMPORTANT: Always return false so the default function doesn't execute

function ArenaGame::ObserverOnTrigger( %game, %data, %obj, %trigger, %state )
{
  // Trigger Types: 0:fire 1:altTrigger 2:jump 3:jet 4:throw

  // State = 0 means that a trigger key was released
  if ( %state == 0 )
    return false;

  // No observer functions if you press throw
  if ( %trigger >= 4 )
    return false;

  // Get a client handle
  %client = %obj.getControllingClient();
  if ( %client == 0 )
    return false;
    
  // Evaluate based on the observer's current mode
  switch$ (%obj.mode)
  {
    case "justJoined":

      // Press FIRE to join game
      if ( %trigger == 0 )
      {
        // Try to add them into the game
        arenaObserverAddToGame( %client );
      }

      // Press JET to cycle observer spawn points
      else if ( %trigger == 3 )
      {
        // Cycle through the static observer spawn points
        %markerObj = Game.pickObserverSpawn( %client, true );
        %transform = %markerObj.getTransform();
        %obj.setTransform( %transform );
        %obj.setFlyMode();
      }

      // Press JUMP to observe clients
      else if ( %trigger == 2 )
      {
        // Switch the observer mode to observing clients
        if ( isObject( %client.observeFlyClient ) )
          serverCmdObserveClient(%client, %client.observeFlyClient);
        else
          serverCmdObserveClient(%client, -1);

        displayObserverHud(%client, %client.observeClient);

        //OBSN messageClient(%client.observeClient, 'Observer', '\c1%1 is now observing you.', %client.name);
      }

    case "playerDeath":

      // Used to block becomming an observer after death for a couple seconds
      if ( %client.blockDeathTrigger )
        return false;

      // Toss the player over to the arenaDead observer mode

      %client.camera.getDataBlock().setMode( %client.camera, "arenaDead" );
      %client.setControlObject( %client.camera );
      commandToClient( %client, 'setHudMode', 'Observer' );

      messageClient( %client, 0, "\c2You've been killed - You will respawn next round." );

    case "observerFly":

      // Press FIRE to join game
      if ( %trigger == 0 )
      {
        // Try to add them into the game
        arenaObserverAddToGame( %client );
      }

      // Press JET to cycle observer spawn points
      else if ( %trigger ==  3)
      {
        %markerObj = Game.pickObserverSpawn( %client, true );
        %transform = %markerObj.getTransform();
        %obj.setTransform( %transform );
        %obj.setFlyMode();
      }

      // Press JUMP to observe clients
      else if (%trigger == 2)
      {
        // Switch the observer mode to observing clients
        if ( isObject( %client.observeFlyClient ) )
          serverCmdObserveClient( %client, %client.observeFlyClient );
        else
          serverCmdObserveClient( %client, -1 );
          
        displayObserverHud( %client, %client.observeClient );

        //OBSN messageClient(%client.observeClient, 'Observer', '\c1%1 is now observing you.', %client.name);
      }

    case "observerTimeout":
    
      // Player didn't respawn quickly enough
      // This should NEVER happen in arena (timeout is disabled)

      messageClient( %client, 0, "\c2BUG: observerTimeout error" );

    case "observerFollow":

      // Press FIRE to cycle to next client
      if ( %trigger == 0 )
      {
        %nextClient = findNextObserveClient(%client);
        %prevObsClient = %client.observeClient;

        if ( %nextClient > 0 && %nextClient != %client.observeClient )
        {
          // Set the new object
          %transform = %nextClient.player.getTransform();
          %obj.setOrbitMode(%nextClient.player, %transform, 0.5, 4.5, 4.5);
          %client.observeClient = %nextClient;

          // Send the message(s)
          displayObserverHud(%client, %nextClient);

          //OBSN messageClient(%nextClient, 'Observer', '\c1%1 is now observing you.', %client.name);
          //OBSN messageClient(%prevObsClient, 'ObserverEnd', '\c1%1 is no longer observing you.', %client.name);
        }
      }

      // Press JET to cycle to prev client
      else if (%trigger == 3)
      {
        %prevClient = findPrevObserveClient(%client);
        %prevObsClient = %client.observeClient;

        if (%prevClient > 0 && %prevClient != %client.observeClient)
        {
          // Set the new object
          %transform = %prevClient.player.getTransform();
          %obj.setOrbitMode(%prevClient.player, %transform, 0.5, 4.5, 4.5);
          %client.observeClient = %prevClient;

          // Send the message(s)
          displayObserverHud(%client, %prevClient);

          //OBSN messageClient(%prevClient, 'Observer', '\c1%1 is now observing you.', %client.name);
          //OBSN messageClient(%prevObsClient, 'ObserverEnd', '\c1%1 is no longer observing you.', %client.name);
        }
      }

      // Press JUMP to go back to observerFly mode
      else if (%trigger == 2)
      {
        // Jump back to observer fly mode
        %obj.mode = "observerFly";
        %obj.setFlyMode();
        updateObserverFlyHud( %client );

        //OBSN messageClient(%client.observeClient, 'ObserverEnd', '\c1%1 is no longer observing you.', %client.name);
      }

    case "pre-game":

      if( !$Host::TournamentMode || $CountdownStarted )
        return false;

      // Handlers for tournament ready up routine

      if( %client.notReady )
      {
        %client.notReady = "";
        MessageAll( 0, '\c1%1 is READY.', %client.name );
        
        if( %client.notReadyCount < 3 )
          centerprint( %client, "\nWaiting for match start (FIRE if not ready)", 0, 3 );
        else
          centerprint( %client, "\nWaiting for match start", 0, 3 );
      }
      else
      {
        %client.notReadyCount++;
        
        if( %client.notReadyCount < 4 )
        {
          %client.notReady = true;
          MessageAll( 0, '\c1%1 is not READY.', %client.name );
          centerprint( %client, "\nPress FIRE when ready.", 0, 3 );
        }
        
        return false;
      }

      CheckTourneyMatchStart();


    case "arenaDead":
      // Dead player waiting for the next round

      // Press FIRE or JET to cycle observer spawn points
      if ( %trigger == 0 || %trigger == 3)
      {
        %markerObj = Game.pickObserverSpawn( %client, true );
        %transform = %markerObj.getTransform();
        %obj.setTransform( %transform );
        %obj.setFlyMode();
      }

      // Press JUMP to observe clients
      else if (%trigger == 2)
      {
        // Switch the observer mode to observing clients
        if ( isObject(%client.observeFlyClient) )
          serverCmdObserveClient(%client, %client.observeFlyClient);
        else
          serverCmdObserveClient(%client, -1);

        // That put us in observerFollow.. but we want this
        %obj.mode = "arenaDeadFollow";

        displayObserverHud(%client, %client.observeClient);

        //OBSN messageClient(%client.observeClient, 'Observer', '\c1%1 is now observing you.', %client.name);
      }

    case "arenaDeadFollow":
      // Dead observer watching a client

      // Press FIRE to cycle to next client
      if ( %trigger == 0 )
      {
        if ( $Host::TournamentMode )
          %nextClient = arenaFindNextObserveClientST(%client);
        else
          %nextClient = findNextObserveClient(%client);
          
        %prevObsClient = %client.observeClient;

        if (%nextClient > 0 && %nextClient != %client.observeClient)
        {
          // Set the new object
          %transform = %nextClient.player.getTransform();
          %obj.setOrbitMode(%nextClient.player, %transform, 0.5, 4.5, 4.5);
          %client.observeClient = %nextClient;

          // Send the message(s)
          displayObserverHud(%client, %nextClient);

          //OBSN messageClient(%nextClient, 'Observer', '\c1%1 is now observing you.', %client.name);
          //OBSN messageClient(%prevObsClient, 'ObserverEnd', '\c1%1 is no longer observing you.', %client.name);
        }
      }

      // Press JET to cycle to prev client
      else if (%trigger == 3)
      {
        if ( $Host::TournamentMode )
          %prevClient = arenaFindPrevObserveClientST(%client);
        else
          %prevClient = findPrevObserveClient(%client);
          
        %prevObsClient = %client.observeClient;

        if (%prevClient > 0 && %prevClient != %client.observeClient)
        {
          // Set the new object
          %transform = %prevClient.player.getTransform();
          %obj.setOrbitMode(%prevClient.player, %transform, 0.5, 4.5, 4.5);
          %client.observeClient = %prevClient;

          // Send the message(s)
          displayObserverHud(%client, %prevClient);

          //OBSN messageClient(%prevClient, 'Observer', '\c1%1 is now observing you.', %client.name);
          //OBSN messageClient(%prevObsClient, 'ObserverEnd', '\c1%1 is no longer observing you.', %client.name);
        }
      }

      // Press JUMP to go back to arenaDead mode
      else if (%trigger == 2)
      {
        // Jump back to arena dead observer mode
        %obj.mode = "arenaDead";
        %obj.setFlyMode();

        bottomPrint( %client, "Observer Fly Mode\nPress JUMP to observe other players\n(You will respawn next round)" @ getTaggedString(%potentialClient.name), 0, 3 );

        //OBSN messageClient(%client.observeClient, 'ObserverEnd', '\c1%1 is no longer observing you.', %client.name);
      }

    case "pre-round":
      // Waiting during round countdown..
      
    case "observerStaticNoNext":   // Added in Tribes2 BETA 5 patch
         // Non-moving, non-cycling observer camera
  }
  
  return false;
}

// ------------------------------------------------------------------ //
// arenaFindNext(Prev)ObserveClientST()     st=sameteam
// Find a player for a client to observe, but only one on their own team

function arenaFindNextObserveClientST(%client)
{
   %index = -1;
   %count = ClientGroup.getCount();
   if (%count <= 1)
      return -1;

   for (%i = 0; %i < %count; %i++)
   {
      %cl = ClientGroup.getObject(%i);
      if (%cl == %client.observeClient)
      {
         %index = %i;
         break;
      }
   }

   //now find the next client (note, if not found, %index still == -1)
   %index++;
   if (%index >= %count)
      %index = 0;

   %newClient = -1;
   for (%i = %index; %i < %count; %i++)
   {
      %cl = ClientGroup.getObject(%i);
      if (%cl != %client && %cl.player > 0 && %cl.team==%client.team)
      {
         %newClient = %cl;
         break;
      }
   }

   //if we didn't find anyone, search from the beginning again
   if (%newClient < 0)
   {
      for (%i = 0; %i < %count; %i++)
      {
         %cl = ClientGroup.getObject(%i);
         if (%cl != %client && %cl.player > 0 && %cl.team==%client.team)
         {
            %newClient = %cl;
            break;
         }
      }
   }

   //if we still haven't found anyone (new), give up..
   if (%newClient < 0 || %newClient.player == %player)
      return -1;
}

function arenaFindPrevObserveClientST(%client)
{
   %index = -1;
   %count = ClientGroup.getCount();
   if (%count <= 1)
      return -1;

   for (%i = 0; %i < %count; %i++)
   {
      %cl = ClientGroup.getObject(%i);
      if (%cl == %client.observeClient)
      {
         %index = %i;
         break;
      }
   }

   //now find the prev client
   %index--;
   if (%index < 0)
      %index = %count - 1;

   %newClient = -1;
   for (%i = %index; %i >= 0; %i--)
   {
      %cl = ClientGroup.getObject(%i);
      if (%cl != %client && %cl.player > 0 && %cl.team==%client.team)
      {
         %newClient = %cl;
         break;
      }
   }

   //if we didn't find anyone, search from the end again
   if (%newClient < 0)
   {
      for (%i = %count - 1; %i >= 0; %i--)
      {
         %cl = ClientGroup.getObject(%i);
         if (%cl != %client && %cl.player > 0 && %cl.team==%client.team)
         {
            %newClient = %cl;
            break;
         }
      }
   }

   //if we still haven't found anyone (new), give up..
   if (%newClient < 0 || %newClient.player == %player)
      return -1;
}

// ------------------------------------------------------------------ //
// arenaObserverAddToGame()
// Transfer an observer over to the game

function arenaObserverAddToGame( %client )
{
  // Block on tournament mode
  if ( $Host::TournamentMode )
  {
    messageClient( %client, 0, "\c2You may not join teams while the server is in tournament mode" );
    return;
  }

  // Clear any observer message
  clearBottomPrint( %client );

  // Give the client a team and add them to the rank display
  Game.assignClientTeam( %client );
  Game.populateTeamRankArray( %client );

  // Try to spawn the client
  arenaSpawnClient( %client );
  
  // Pregame Billboard
  if ( !$matchStarted )
    Game.showArenaBillboard( %client );
}

// ------------------------------------------------------------------ //
// BEGIN PACKAGE [ ArenaObserver ]
// ------------------------------------------------------------------ //

package ArenaObserver
{

// ------------------------------------------------------------------ //
// Observer::setMode()
// Called to change the mode of an observer

function Observer::setMode( %data, %obj, %mode, %targetObj )
{
  if( %mode $= "" )
    return;

  // Get a client handle
  %client = %obj.getControllingClient();

  // New actions for dead observers
  if ( %mode $= "arenaDead" )
  {
    // Free-flying observer camera

    commandToClient( %client, 'setHudMode', 'Observer' );
    
    //%markerObj = Game.pickObserverSpawn( %client, true );
    //%transform = %markerObj.getTransform();
    //%obj.setTransform( %transform );
    
    %obj.setFlyMode();

    %obj.mode = %mode;

    bottomPrint( %client, "Observer Fly Mode\nPress JUMP to observe other players\n(You will respawn next round)", 0, 3 );
  }
  else if ( %mode $= "pre-round" )
  {
    // Orbiting pre-round camera

    commandToClient( %client, 'setHudMode', 'Observer' );
    %obj.setOrbitMode( %targetObj, %targetObj.getWorldBoxCenter() @ " 0.5 0.5 0.5 1", 0.5, 4.5, 4.5, true );

    %obj.mode = %mode;
  }
  else
    Parent::setMode( %data, %obj, %mode, %targetObj );
}

// ------------------------------------------------------------------ //
// displayObserverHud()
// Generates the bottomPrint "observer hud"

function displayObserverHud( %client, %targetClient, %potentialClient )
{
   if ( %targetClient > 0 )
   {
      // Add a Friend/Enemy/Teamname string to the observer hud
      
      if ( %client.team == 0 )
         %afString = " (" @ getTaggedString(Game.getTeamName(%targetClient.team)) @ ")";
      else if ( %client.team == %targetClient.team )
         %afString = " (Friend)";
      else if ( Game.numTeams > 2 )
         %afString = " (" @ getTaggedString(Game.getTeamName(%targetClient.team)) @ ")";
      else
         %afString = " (ENEMY)";

      bottomPrint( %client, "\nYou are now observing: " @ %targetClient.nameBase @ %afString, 0, 3 );
   }
   else if ( %potentialClient > 0 )
      bottomPrint( %client, "\nObserver Fly Mode\n" @ %potentialClient.nameBase, 0, 3 );
   else
      bottomPrint( %client, "\nObserver Fly Mode", 0, 3);
}

};

// ------------------------------------------------------------------ //
// END PACKAGE [ ArenaObserver ]
// ------------------------------------------------------------------ //


// ========================================================================== //
// |                                                                        | //
// |  SPAWNING                                                              | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// arenaSpawnClient()
// Either spawn a client or set them to be spawned next round
// Pass true for %noHudModeUpdate to suppress setHudMode messages
//  (useful for prematch - this stops the huds appearing on the load screen)

function arenaSpawnClient( %client, %noHudModeUpdate )
{
  if ( %noHudModeUpdate $= "" )
    %noHudModeUpdate = false;

  // Determine if we can spawn the client now
  if ( Game.state $= "countdown" )
  {
    // Inside either a match or round countdown - we can spawn

    Game.newSpawnPlayer( %client, false );

    if ( $CountdownStarted || $matchStarted )
      %client.camera.getDataBlock().setMode( %client.camera, "pre-round", %client.player );
    else
      %client.camera.getDataBlock().setMode( %client.camera, "pre-game", %client.player );
    %client.setControlObject( %client.camera );
    if ( !%noHudModeUpdate )
      commandToClient( %client, 'setHudMode', 'Observer' );

    arenaSetClientAlive( %client );
  }
  else if ( Game.state $= "play"
            && ( Game.checkGracePeriod()
                 || tsPlayerCountTeam( %client.team ) == 1 ) )
  {
    // Currently in spawn grace period (beginning of round)
    // We can also spawn if there's nobody else on the team

    // Spawn directly into the game

    Game.newSpawnPlayer( %client, false );

    %client.camera.setFlyMode();
    %client.setControlObject( %client.player );
    if ( !%noHudModeUpdate )
      commandToClient( %client, 'setHudMode', 'Standard' );

    arenaSetClientAlive( %client );
  }
  else
  {
    // We're inside a round - can't spawn right now
    // Make the client a regular dead observer while they wait for the next round

    %client.camera.setFlyMode();
    %client.camera.getDataBlock().setMode( %client.camera, "arenaDead" );
    %client.setControlObject( %client.camera );
    if ( !%noHudModeUpdate )
      commandToClient( %client, 'setHudMode', 'Observer' );

    messageClient( %client, 0, "\c2You will join the game next round." );
    clearCenterPrint( %client );
    centerPrint( %client, "\nYou will join the game next round.", 4, 3 );

    arenaSetClientDead( %client );
  }

  // Update the objectiveHUD
  arenaUpdateObjectiveHUD( %client.team );
}

// ------------------------------------------------------------------ //
// ArenaGame::newSpawnPlayer()
// Replacement for SpawnPlayer - All Arena spawn calls MUST go through here

function ArenaGame::newSpawnPlayer( %game, %client, %respawn )
{
  %spawnPoint = %game.pickPlayerSpawn( %client, false );
  %game.createPlayer( %client, %spawnPoint, false );
}

// ------------------------------------------------------------------ //
// ArenaGame::spawnPlayer()
// The original base spawn handler - this should not be used

function ArenaGame::spawnPlayer( %game, %client, %respawn )
{
  TS_ERROR( "FAULT: ORIGINAL SPAWN PLAYER HANDLER CALLED" );
  messageClient( %client, 0, "\c2FAULT: The wrong spawn handler was called on you.." );
}

// -------------------------------------------------------- //
// ArenaGame::pickTeamSpawn()
// Refined version of the spawn point picker
// Changed to reduce overlapped spawning

// It's really too bad the only get node interface is randNode
// (Prevents us from just walking all the nodes)
// We could however walk the spheres - but that would be more work than it's worth..

// Changes from defaultGame:
//  Bumped up the number of tries in the inner loop (3x)
//  Bumped up the number of attempts in the outer loop (1.5x)
//  Will try to move to another spawn sphere after a collision for the first 17 attempts

function ArenaGame::pickTeamSpawn( %game, %team )
{
  // early exit if no nav graph
  if ( !navGraphExists() )
  {
    TS_INFO( "No navigation graph is present.  Build one." );
    return -1;
  }

  for (%attempt = 0; %attempt < 30; %attempt++)
  {
    //  finds a random spawn sphere
    //  selects inside/outside on this random sphere
    //  if the navgraph exists, then uses it to grab a random node as spawn
    //   location/rotation

    %sphere = %game.selectSpawnSphere(%team);
    if (%sphere == -1)
    {
      TS_INFO("No spawn spheres found for team " @ %team);
      return -1;
    }

    %zone = %game.selectSpawnZone(%sphere);
    %useIndoor = %zone;
    %useOutdoor = !%zone;
    if (%zone)
      %area = "indoor";
    else
      %area = "outdoor";

    %radius = %sphere.radius;
    %sphereTrans = %sphere.getTransform();
    %sphereCtr = getWord(%sphereTrans, 0) @ " " @ getWord(%sphereTrans, 1) @ " " @ getWord(%sphereTrans, 2);   //don't need full transform here, just x, y, z
    //TS_INFO("Selected Sphere is " @ %sphereCtr @ " with a radius of " @ %radius @ " meters.  Selecting from " @ %area @ " zone.");

    %avoidThese = $TypeMasks::VehicleObjectType  | $TypeMasks::MoveableObjectType |
                  $TypeMasks::PlayerObjectType   | $TypeMasks::TurretObjectType;

    for (%tries = 0; %tries < 30; %tries++)
    {
      %nodeIndex = navGraph.randNode( %sphereCtr, %radius, %useIndoor, %useOutdoor );

      if ( %nodeIndex >= 0 )
      {
        %loc = navGraph.randNodeLoc( %nodeIndex );
        %adjUp = VectorAdd( %loc, "0 0 1.0" );   // don't go much below

        if ( ContainerBoxEmpty( %avoidThese, %adjUp, 2.0 ) )
          break;
      }
    }

    if ( %nodeIndex >= 0
         && ( ContainerBoxEmpty( %avoidThese, %adjUp, 2.0 ) || %attempt > 17 ) )
    {
      %loc = navGraph.randNodeLoc(%nodeIndex);

      if (%zone)
      {
        %trns = %loc @ " 0 0 1 0";
        %spawnLoc = whereToLook(%trns);
      }
      else
      {
        %rot = %game.selectSpawnFacing(%loc, %sphereCtr, %zone);
        %spawnLoc = %loc @ %rot;
      }
      return %spawnLoc;
    }
  }
}


// ========================================================================== //
// |                                                                        | //
// |  SERVER CLIENT AND TEAM MANAGEMENT (Overrides)                         | //
// |                                                                        | //
// ========================================================================== //

// Apparently Dynamix decided that no mods would ever possibly need to control
// when and how their players spawned so they neglected to centralize spawning
// control. Thus we now have to override all of this to enforce arena rules.

// ------------------------------------------------------------------ //
// ArenaGame::clientMissionDropReady()
// Called as a client enters the match

function ArenaGame::clientMissionDropReady(%game, %client)
{
  // Tell the client what gametype this is
    
  messageClient( %client, 'MsgArenaServerState', "", %game.numTeams );
  messageClient( %client, 'MsgClientReady',"", %game.class );

  // Reset client score
  %game.resetScore(%client);

  // Add teams to objective hud
  for( %i = 1; %i <= %game.numTeams; %i++ )
    messageClient( %client, 'MsgArenaAddTeam', "", %i, %game.getTeamName(%i), $TeamScore[%i], arenaCountTeamLivingClients(%i), tsPlayerCountTeam(%i) );

  // Provide mission information
  messageClient(%client, 'MsgMissionDropInfo', '\c0You are in mission %1 (%2).', $MissionDisplayName, $MissionTypeDisplayName, $ServerName );
  
  // Display the map high score if it exists
  if ( $Arena::Pref::TrackHighScores && Game.class $= "ArenaGame" )
  {
    if ( $Arena::HighScores::Score[$currentMission] !$= ""
         && $Arena::HighScores::Score[$currentMission] > 0 )
    {
      messageClient( %client, 'MsgArenaHighScore',
                  "\c0The individual record for this mission is held by "@$Arena::HighScores::Name[$currentMission]@" with a score of "@$Arena::HighScores::Score[$currentMission]@".",
                  $Arena::HighScores::Name[$currentMission],
                  $Arena::HighScores::Score[$currentMission] );
    }
    else
      messageClient( %client, 'MsgArenaHighScore', "\c0There is no high score recorded for this mission.", "", 0 );
  }

  //-------  below is from default version

  // Synchronize the clock HUD
  messageClient( %client, 'MsgSystemClock', "", 0, 0 );

  // Update client displays
  %game.sendClientTeamList( %client );
  %game.setupClientHuds( %client );

  // Player control state and tournament mode join handler
  if( !$Host::TournamentMode )
  {
    if( %client.camera.mode $= "observerFly" || %client.camera.mode $= "justJoined" )
    {
      %client.observerStartTime = getSimTime();
      commandToClient(%client, 'setHudMode', 'Observer');
      %client.setControlObject( %client.camera );
      updateObserverFlyHud( %client );
    }
  }
  else
  {
    // Tournament Mode

    // Stick player into observer mode
    %client.camera.getDataBlock().setMode( %client.camera, "ObserverFly" );
    commandToClient( %client, 'setHudMode', 'Observer' );
    %client.setControlObject( %client.camera );
    messageAll( 'MsgClientJoinTeam', "",%client.name, %game.getTeamName(0), %client, 0 );
    %client.team = 0;

    // Show tournament mode message
    if( !$MatchStarted && !$CountdownStarted)
    {
      if($TeamDamage)
        %damMess = "ENABLED";
      else
        %damMess = "DISABLED";

      if(%game.numTeams > 1)
        BottomPrint(%client, "Server is Running in Tournament Mode.\nPick a Team\nTeam Damage is " @ %damMess, 0, 3 );
    }
    else
    {
      BottomPrint( %client, "\nServer is Running in Tournament Mode", 0, 3 );
    }
  }

  // Check order of team lines in objectiveHud
  messageClient( %client, 'MsgCheckTeamLines', "", %client.team );

  // Ready to go
  %client.matchStartReady = true;
  TS_INFO( "Client" SPC %client SPC "is ready." );

  //-------  above is from default version

  // Didn't setHudMode in arenaSpawnClient() so guis wouldn't show on load screen
  // So do it now instead
  if ( %client.team != 0 )
    commandToClient( %client, 'setHudMode', 'Standard' );

  // Arena Version Info
  %client.arenaSupport = 0;  // Mark client as not having the arenaSupport script until we find otherwise
  %client.lastArenaBillboard = 0;
  %client.arenaPrefShowWaypoints = true;  // Set default prefs until/unless arenaSupport reports them
  %client.arenaPrefShowObservers = false;
  
  messageClient( %client, 'MsgArenaVersion', "", $Arena::Version, $Arena::VersionString );

  // Message of the day
  tsMOTD( %client, $Arena::Pref::MOTD, $Arena::Pref::MOTDLines, $Arena::Pref::MOTDTime );
  
  // Pregame Billboard
  if ( !$matchStarted )
    %game.showArenaBillboard( %client );
}

// ------------------------------------------------------------------ //
// ArenaGame::assignClientTeam()
// Using this oppurtunity to check the objectiveHUD line order

function ArenaGame::assignClientTeam( %game, %client, %respawn )
{
  DefaultGame::assignClientTeam( %game, %client, %respawn );

  // If player's team is not on top of objective hud, switch lines
  messageClient( %client, 'MsgCheckTeamLines', "", %client.team );
}

// ------------------------------------------------------------------ //
// ArenaGame::assignClientRandomTeam()
// Using this oppurtunity to check the objectiveHUD line order

function ArenaGame::assignClientRandomTeam( %game, %client, %respawn )
{
  TS_DEBUG( "assigning client "@%client.nameBase@" random team.." );
  
  // Determine how many players are on each team
  
  for ( %iTeamSize = 1; %iTeamSize <= %game.numTeams; %iTeamSize++ )
    %teamSize[%iTeamSize] = tsPlayerCountTeam( %iTeamSize );
  
  // Don't count this player
  
  if ( %teamSize[%client.team] > 0 ) // ?
    %teamSize[%client.team]--;
    
  // Check if teams are equal
  // Keep track of which team is the smallest just in case they aren't equal
  
  %smallestTeam = 1;
  %teamsEqual = true;
  
  for ( %iTeamCheck = 2; %iTeamCheck <= %game.numTeams; %iTeamCheck++ )
  {
    if ( %teamSize[%iTeamCheck] != %teamSize[%smallestTeam] )
    {
      %teamsEqual = false;
      
      if ( %teamSize[%iTeamCheck] < %teamSize[%smallestTeam] )
        %smallestTeam = %iTeamCheck;
    }
  }
  
  // If the teams were all equal send the player to a random team
  // Otherwise send them to the smallest team
  
  if ( %teamsEqual )
    %game.clientJoinTeam( %client, getRandom( 1, %game.numTeams), false );
  else
    %game.clientJoinTeam( %client, %smallestTeam, false );
}

// -------------------------------------------------------- //
// ArenaGame::clientJoinTeam()
// Called to have an unteamed player join a specific team

function ArenaGame::clientJoinTeam( %game, %client, %team, %respawn )
{
  if ( %team < 1 || %team > %game.numTeams )
    return;

  // Setup new team information
  %client.team = %team;
  %client.lastTeam = %team;

  setTargetSkin( %client.target, %game.getTeamSkin(%team) );
  setTargetSensorGroup( %client.target, %team );
  %client.setSensorGroup( %team );
  updateCanListenState( %client );

  // Spawn the player
  arenaSpawnClient( %client, !$MatchStarted );
  
  // Add the client to the rank array for their new team
  // (Spawn doesn't always do this)
  %game.populateTeamRankArray( %client );

  // Notifications
  messageAllExcept( %client, -1, 'MsgClientJoinTeam', "\c1"@%client.nameBase@" joined "@getTaggedString(%game.getTeamName(%client.team))@".", %client.name, %game.getTeamName(%team), %client, %team );
  messageClient( %client, 'MsgClientJoinTeam', "\c1You joined the "@getTaggedString(%game.getTeamName(%client.team))@" team.", $client.name, %game.getTeamName(%client.team), %client, %client.team );

  TS_INFO(%client.nameBase@" (cl "@%client@") joined team "@%client.team);
  
  // If player's team is not on top of objective hud, switch lines
  messageClient( %client, 'MsgCheckTeamLines', "", %client.team );
}

// -------------------------------------------------------- //
// ArenaGame::clientChangeTeam()
// Switch a client from one team to another

function ArenaGame::clientChangeTeam(%game, %client, %team, %fromObs)
{
  // Remove the client from the team rank array
  %game.removeFromTeamRankArray( %client );

  // Kill the player object if it exists
  %pl = %client.player;
  if( isObject(%pl) )
  {
    if( %pl.isMounted() )
      %pl.getDataBlock().doDismount( %pl );
    %pl.scriptKill( 0 );
  }

  // Point to new team
  %client.team = %team;

  // Update the client's team information
  if ( !%client.isAIControlled() )
    setTargetSkin( %client.target, %game.getTeamSkin(%client.team) );
  setTargetSensorGroup( %client.target, %client.team );
  %client.setSensorGroup( %client.team );
  updateCanListenState( %client );

  // Reset the client's targets and tasks only
  clientResetTargets( %client, true );

  // Notifications
  if( %fromObs $= "" || !%fromObs )
  {
    messageAllExcept( %client, -1, 'MsgClientJoinTeam', "\c1"@%client.nameBase@" switched to team "@getTaggedString(%game.getTeamName(%client.team))@".", %client.name, %game.getTeamName(%client.team), %client, %client.team );
    messageClient( %client, 'MsgClientJoinTeam', "\c1You switched to team "@getTaggedString(%game.getTeamName(%client.team))@".", %client.name, %game.getTeamName(%client.team), %client, %client.team );
  }
  else
  {
    messageAllExcept( %client, -1, 'MsgClientJoinTeam', "\c1"@%client.nameBase@" joined team "@getTaggedString(%game.getTeamName(%client.team))@".", %client.name, %game.getTeamName(%team), %client, %team );
    messageClient( %client, 'MsgClientJoinTeam', "\c1You joined team "@getTaggedString(%game.getTeamName(%client.team))@".", %client.name, %game.getTeamName(%client.team), %client, %client.team );
  }

  TS_INFO(%client.nameBase@" (cl "@%client@") switched to team "@%client.team);

  // Spawn the player
  arenaSpawnClient( %client );

  // Add the client to the rank array for their new team
  // (Spawn doesn't always do this)
  %game.populateTeamRankArray( %client );

  // Switch objective hud lines
  messageClient(%client, 'MsgCheckTeamLines', "", %client.team);

  // Update objectiveHUD
  arenaUpdateObjectiveHUD();
}

// -------------------------------------------------------- //
// ArenaGame::forceObserver()
// Switches a team player over to observer mode
// (Overridden to update objective hud)

function ArenaGame::forceObserver( %game, %client, %reason )
{
  DefaultGame::forceObserver( %game, %client, %reason );

  // Update objectiveHUD
  arenaUpdateObjectiveHUD();
}

// ------------------------------------------------------------------ //
// BEGIN PACKAGE [ ArenaServer ]
// ------------------------------------------------------------------ //

package ArenaServer
{

// -------------------------------------------------------- //
// serverSetClientTeamState()
// Sets up client team states before a new match

function serverSetClientTeamState( %client )
{
  // Set all player states prior to mission drop ready

  // create a new camera for this client
  %client.camera = new Camera()
  {
    dataBlock = Observer;
  };

  if( isObject( %client.rescheduleVote ) )
    Cancel( %client.rescheduleVote );
  %client.canVote = true;
  %client.rescheduleVote = "";

  MissionCleanup.add( %client.camera ); // we get automatic cleanup this way.

  if( !$Host::TournamentMode )
  {
    if( %client.justConnected )
    {
      %client.justConnected = false;
      %client.camera.getDataBlock().setMode( %client.camera, "justJoined" );
    }
    else
    {
      // Check what the client's team was for the last match
      if( %client.lastTeam !$= "" )
      {
        
        if( %client.lastTeam == 0 )
        {
          // This client was an observer from the last match
          // Let them stay observer

          %client.camera.getDataBlock().setMode( %client.camera, "ObserverFly" );
        }
        else
        {
          // Try to put this client on the team they were on last match

          if( Game.numTeams > 1 && %client.lastTeam <= Game.numTeams )
          {
            if ( $Arena::Pref::ShuffleTeams && Game.numTeams > 1 )
            {
              Game.assignClientRandomTeam( %client );
              arenaSpawnClient( %client, true );
            }
            else
              Game.clientJoinTeam( %client, %client.lastTeam, false );
          }
          else
          {
            // Client's team from the last match doesn't exist now
            // Give them a new team

            Game.assignClientTeam( %client );
            arenaSpawnClient( %client, true );
          }
        }
      }
      else
      {
        // No last team information for this client - give them a new team

        Game.assignClientTeam( %client );
        arenaSpawnClient( %client, true );
      }
    }
  }
  else
  {
    // Tournament Mode - don't need to do anything. MissionDrop will handle things from here.
  }
}

// -------------------------------------------------------- //
// serverCmdClientJoinGame()
// Observer joins game with lobby command

function serverCmdClientJoinGame( %client )
{
  if ( isObject( Game ) )
    Game.clientJoinTeam( %client, 0, 1 );

  clearBottomPrint(%client);
}


// -------------------------------------------------------- //
// serverCmdClientJoinTeam()
// Switch a player's team based on their or an admin's command

function serverCmdClientJoinTeam( %client, %team )
{
  if ( %team < -1 || %team == 0 || %team > Game.numTeams )
    return;
    
  if( %team == -1 )
  {
    // Support >2 teams here
    // Cycle to the next team in sequence

    %team = %client.team + 1;

    if ( %team > Game.numTeams )
      %team = 1;
  }

  Parent::serverCmdClientJoinTeam( %client, %team );
}


// -------------------------------------------------------- //
// serverCmdChangePlayersTeam()
// Admin forces player to change teams

function serverCmdChangePlayersTeam( %clientRequesting, %client, %team)
{
  if ( %team < -1 || %team == 0 || %team > Game.numTeams )
    return;

  if( isObject( Game ) && %client != Game.kickClient && %clientRequesting.isAdmin )
  {
    serverCmdClientJoinTeam(%client, %team);
  
    %multiTeam = (Game.numTeams > 1);
    if( %multiTeam )
    {
     messageClient( %client, 'MsgClient', "\c1The Admin has changed your team.");
      messageAllExcept( %client, -1, 'MsgClient', "\c1The Admin forced "@%client.nameBase@" to join the "@getTaggedString(Game.getTeamName(%client.team))@" team.", %client.name, Game.getTeamName(%client.team));
    }
    else
    {
      messageClient( %client, 'MsgClient', "\c1The Admin has added you to the game.");
      messageAllExcept( %client, -1, 'MsgClient', "\c1The Admin added "@%client.nameBase@" to the game.", %client.name);
    }
  }
}


// -------------------------------------------------------- //
// serverCmdClientPickedTeam()
// Client picks a team off the team picking gui in tournament mode

function serverCmdClientPickedTeam( %client, %option )
{
  if( %option == 1 || %option == 2 )
    Game.clientJoinTeam( %client, %option, false );

  else if( %option == 3)
  {
    Game.assignClientTeam( %client, false );
    arenaSpawnClient( %client );
  }
  else
  {
    Game.forceObserver( %client, "playerChoose" );
    %client.observerMode = "observer";
    %client.notReady = false;

    return;
  }

  ClearBottomPrint(%client);

  %client.notReady = true;
  centerprint( %client, "\nPress FIRE when ready.", 0, 3 );
}

//
// updateScoreHudThread()
// Increased time to 7000ms instead of 3000ms since the arena score hud often scrolls

function updateScoreHudThread(%client, %tag)
{
   Game.updateScoreHud(%client, %tag);
   cancel(%client.scoreHudThread);
   %client.scoreHudThread = schedule(7000, %client, "updateScoreHudThread", %client, %tag);
}

};

//
// resetControlObject()
// Prevent players from using the command map to change their control object
//
// Note: This is not packaged because the package bug kept breaking it

function resetControlObject(%client)
{
   if( isObject( %client.comCam ) )
      %client.comCam.delete();

   if( isObject(%client.player) && !%client.player.isDestroyed() && $MatchStarted
       && ( Game.class !$= "ArenaGame" || Game.state $= "play" ) )
      %client.setControlObject(%client.player);
   else
      %client.setControlObject(%client.camera);
}


// ------------------------------------------------------------------ //
// END PACKAGE [ ArenaServer ]
// ------------------------------------------------------------------ //






// ========================================================================== //
// |                                                                        | //
// |  FLIPFLOP (SWITCH) OBJECTIVES                                          | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// ArenaGame::resetFlipFlops()
// Called at the start of rounds to restore all flipflops to the unclaimed state

function ArenaGame::resetFlipFlops( %game )
{
  %ffSet = nameToID("MissionCleanup/FlipFlops");
  
  if( %ffSet >  0)
  {
    %numFF = %ffSet.getCount();
    
    for( %j = 0; %j < %numFF; %j++ )
    {
      %curFF = %ffSet.getObject(%j);

      // Reset the skin on the FlipFlop
      setTargetSkin( %curFF.getTarget(), $teamSkin[0] );
      setTargetSensorGroup( %curFF.getTarget(), 0 );

      // Delete any holos
      if ( %curFF.projector > 0 && %curFF.projector.holo > 0 )
      {
        %curFF.projector.holo.delete();
        %curFF.projector.holo = 0;
      }

      // Transfer the flipflop resources back to team zero
      Game.claimFlipflopResources( %curFF, 0 );
    }
  }
}

// ------------------------------------------------------------------ //
// ArenaGame::countFlips()
// defaultGame uses this to determine if it should send MsgFlipFlopsHeld
// (and it shouldn't for arena so we just return false)

function ArenaGame::countFlips( %game )
{
  return false;
}

// ------------------------------------------------------------------ //
// ArenaGame::AIplayerCaptureFlipFlop()
// Called to alert the AI that a flipflop was captured
// Don't bother - this is just here because defaultGame calls it

function ArenaGame::AIplayerCaptureFlipFlop(%game, %player, %flipFlop)
{
}



// ========================================================================== //
// |                                                                        | //
// |  INVENTORY                                                             | //
// |                                                                        | //
// ========================================================================== //

$InvBanList[Arena, "TurretOutdoorDeployable"] = 1;
$InvBanList[Arena, "TurretIndoorDeployable"] = 1;
$InvBanList[Arena, "ElfBarrelPack"] = 1;
$InvBanList[Arena, "MortarBarrelPack"] = 1;
$InvBanList[Arena, "PlasmaBarrelPack"] = 1;
$InvBanList[Arena, "AABarrelPack"] = 1;
$InvBanList[Arena, "MissileBarrelPack"] = 1;
$InvBanList[Arena, "Mine"] = 1;
$InvBanList[Arena, "SatchelCharge"] = 1;
$InvBanList[Arena, "MissileLauncher"] = 1;
$InvBanList[Arena, "InventoryDeployable"] = 1;
$InvBanList[Arena, "MotionSensorDeployable"] = 1;
$InvBanList[Arena, "PulseSensorDeployable"] = 1;
$InvBanList[Arena, "CameraGrenade"] = 1;

// ------------------------------------------------------------------ //
// ArenaGame::equip()
// Give a player spawn equipment

function ArenaGame::equip( %game, %player )
{
  if ( $Arena::Pref::SpawnWithEnergyPack )
    %player.setInventory( EnergyPack, 1 );

  DefaultGame::equip( %game, %player );
}

// ------------------------------------------------------------------ //
// ArenaGame::stationOnEnterTrigger()
// Blocks excessive station accesses and team zero station accesses

function ArenaGame::stationOnEnterTrigger( %game, %data, %obj, %colObj )
{
  %client = %colObj.client;

  // Ignore this if it's not a station the player can access
  // IMPORTANT: This also blocks use of team zero stations if
  //            MissionGroup.Arena_blockTeamZeroStations has been set to true
  if ( %obj.mainObj.team != %colObj.client.team )
  {
    if ( %obj.mainObj.team != 0
         || ( MissionGroup.Arena_blockTeamZeroStations !$= ""
              && MissionGroup.Arena_blockTeamZeroStations ) )
    {
      messageClient( %colObj.client, 'msgStationDenied', "\c2Access Denied -- Wrong team.~wfx/powered/station_denied.wav" );
    
      return false;
    }
  }

  // Implement station access limit
  
  if ( %client.stationUseCount >= $Arena::Pref::StationLimit && $Arena::Pref::StationLimit >= 0 )
  {
    // Block excessive station use
    
    if ( $Arena::Pref::StationLimit > 0 )
      messageClient( %client, 0, "\c2Access Denied -- Only " @ $Arena::Pref::StationLimit @ " station accesses allowed per round.~wfx/powered/station_denied.wav" );
    else
      messageClient( %client, 0, "\c2Access Denied -- This server does not allow station access.~wfx/powered/station_denied.wav" );
      
    return false;
  }
  
  // Implement tourney laser purchase limit
  // Don't even let them in the station if:
  //  There are no more laser rifles left
  //  They have a laser selected in their loadout
  //  and They aren't carrying one already
  
  if ( $Host::TournamentMode && Game.arenaLaserLimit > 0
       && Game.arenaLasersAvailable[%client.team] < 1 )
  {
      // Check for a laser rifle in the selected loadout
      
      for ( %iWep = 0; %iWep < getFieldCount( %client.weaponIndex ); %iWep++ )
      {
        %inv = $NameToInv[%client.favorites[getField( %client.weaponIndex, %iWep )]];
        
        if ( %inv $= "SniperRifle" )
        {
          // Check if they're carrying a rifle already
          // (don't want to prevent reloading of snipers or returning of rifles)
          
          if ( %client.player.getInventory(SniperRifle) < 1 )
          {
            // Client wants rifle, no rifles left, client has no rifle already
            // They can't use the station
            
            messageClient( %client, 0, "\c2Access Denied -- There are no more sniper rifles available (maximum is "@ Game.arenaLaserLimit @"). Choose a loadout without a rifle.~wfx/powered/station_denied.wav" );
            
            return false;
          }
        }
      }
  }


  // If we passed those checks allow the access
  
  return true;
}

// ------------------------------------------------------------------ //
// BEGIN PACKAGE [ ArenaInventory ]
// ------------------------------------------------------------------ //

package ArenaInventory
{

// ------------------------------------------------------------------ //
// StationInventory::stationReady()
// Called when a player buys their inventory

function StationInventory::stationReady(%data, %obj)
{
  %player = %obj.triggeredBy;
  %client = %player.client;

  // Implement tourney mode laser rifle limit
  
  if ( $Host::TournamentMode && Game.arenaLaserLimit > 0 )
  {
    // If the client is entering with a laser rifle increase the available lasers

    if ( %client.player.getInventory(SniperRifle) > 0 )
      Game.arenaLasersAvailable[%client.team]++;
  
    // If the client is buying a laser rifle decrement the available lasers
  
    for ( %iWep = 0; %iWep < getFieldCount( %client.weaponIndex ); %iWep++ )
    {
      %inv = $NameToInv[%client.favorites[getField( %client.weaponIndex, %iWep )]];

      if ( %inv $= "SniperRifle" )
        Game.arenaLasersAvailable[%client.team]--;
    }
  }
  
  
  // Call the parent to do the inventory changes
  
  Parent::stationReady(%data,%obj);

  // Increment station use count

  %client.stationUseCount++;
   
  // Show station use message
   
  if ( $Arena::Pref::StationLimit >= 0 )  // disabled station limit = negative pref
  {
   
    %usesLeft = $Arena::Pref::StationLimit - %client.stationUseCount;
   
    if ( %usesLeft < 1 )
      messageClient( %client, 0, "\c2Your station access has been recorded. You have none left this round." );
    else
      messageClient( %client, 0, "\c2Your station access has been recorded. You have "@%usesLeft@" left this round." );

  }
}

// ------------------------------------------------------------------ //
// serverCmdSetClientFav()
// Called when the client tries to set it's buy list

function serverCmdSetClientFav( %client, %text )
{
  // Disallow certain armors

  if ( $Arena::Pref::RestrictArmor == 2 )
    %text = setField( %text, 1, "Scout" );
  else if ( $Arena::Pref::RestrictArmor == 1 )
    if ( getField( %text, 1 ) $= "Juggernaut" )
      %text = setField( %text, 1, "Assault" );

  Parent::serverCmdSetClientFav( %client, %text );
}

//
// Armor::damageObject()
// Apply damage to a player
// Overridden to modify laser and disk damage

function Armor::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %mineSC)
{
  // Scale laser damage by 40% in FFA mode
  
  if ( %damageType == $DamageType::Laser && !$Host::TournamentMode && $Arena::Pref::LaserDamageReduction )
    %amount = %amount * 0.6;
  
  Parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %mineSC);
}

};

// ------------------------------------------------------------------ //
// END PACKAGE [ ArenaInventory ]
// ------------------------------------------------------------------ //


// ========================================================================== //
// |                                                                        | //
// |  INDIVIDUAL SCORING                                                    | //
// |                                                                        | //
// ========================================================================== //

// -------------------------------------------------------- //
// ArenaGame::resetScore()
// Zeros out an individual client's score

function ArenaGame::resetScore( %game, %client )
{
  %client.kills = 0;
  %client.deaths = 0;
  %client.suicides = 0;
  %client.teamKills = 0;

  %client.snipeKills = 0;

  %client.roundsWon = 0;
  %client.roundsLost = 0;

  %client.assists = 0;
  
  %client.roundKills = 0;
  %client.hatTricks = 0;

  %client.score = 0;
}

// -------------------------------------------------------- //
// ArenaGame::updateKillScores()
// Awards relevant individual points after a client is killed

function ArenaGame::updateKillScores( %game, %clVictim, %clKiller, %damageType, %implement )
{
  if ( %game.state !$= "play" )
    return;

  // Check for a turret kill (maybe some arena map will have turrets..)
  if( %game.testTurretKill( %implement ) )
  {
    %game.awardScoreTurretKill( %clVictim, %implement );
  }
  // Check for a regular enemy kill
  else if ( %game.testKill( %clVictim, %clKiller ) )
  {
    %game.awardScoreKill( %clKiller );
    %game.awardScoreDeath( %clVictim );

    if ( %damageType == $DamageType::Laser )
      %clKiller.snipeKills++;
      
    %clKiller.roundKills++;
    
    if ( %clKiller.roundKills == 3 )
    {
      messageClient( %clKiller, 0, "You receive a one point bonus for performing a hat trick." );
      %clKiller.hatTricks++;
      
      %game.recalcScore( %clKiller );
    }
  }
  else
  {
    // Otherwise, the killer and victim are on the same team

    // Test for a suicide
    if ( %game.testSuicide( %clVictim, %clKiller, %damageType ) )
    {
      %game.awardScoreSuicide( %clVictim );
    }
    else
    {
      // Otherwise this must be a team kill (bastard)
      if ( %game.testTeamKill( %clVictim, %clKiller ) )
        %game.awardScoreTeamKill( %clVictim, %clKiller );
    }
  }
}

// -------------------------------------------------------- //
// ArenaGame::recalcScore()
// Calculate a client's total score based on the other score vars we track

function ArenaGame::recalcScore( %game, %client )
{
  // Score Calculation

  %client.score = %client.kills + %client.assists + %client.roundsWon + %client.hatTricks - %client.teamKills;

  %game.recalcTeamRanks( %client );
}

//
// ArenaGame::updateScoreHud()
// Render the score hud (F2) to a client

function ArenaGame::updateScoreHud( %game, %client, %tag )
{
  // Clear the HUD
  messageClient( %client, 'ClearHud', "", %tag, 0 );

  // Clear the header
  messageClient( %client, 'SetScoreHudHeader', "", "" );

  // Send the subheader
  messageClient(%client, 'SetScoreHudSubheader', "", '<tab:15,205,310,390,500>\tTEAMS/PLAYERS\tSCORE\tKILLS\tWIN/LOSS\tBONUS');

  // Tracks the line of the score hud we're writing to
  %index = -1;


  // For every team in the game..

  for ( %iTeam = 1; %iTeam <= Game.numTeams; %iTeam++ )
  {
    // Send team name
    
    %teamPlayerCount = tsPlayerCountTeam( %iTeam );
    %teamPlayerCountPlural = %teamPlayerCount == 1 ? "" : "s";

    messageClient( %client, 'SetLineHud', "", %tag, %index++, '<tab:10, 310><spush><font:Univers Condensed:28>\t%1 (%2) <font:Univers Condensed:16>%3 Player%4<spop>', %game.getTeamName(%iTeam), $TeamScore[%iTeam], %teamPlayerCount, %teamPlayerCountPlural );
    messageClient( %client, 'SetLineHud', "", %tag, %index++, "");

    // Send team player list

    for ( %iPlayer = 0; %iPlayer < $TeamRank[%iTeam,count]; %iPlayer++ )
    {
      %cl = $TeamRank[%iTeam,%iPlayer];

      %clScore = %cl.score $= "" ? 0 : %cl.score;
      %clKills = %cl.kills $= "" ? 0 : %cl.kills;
      %clBonus = %cl.hatTricks $= "" ? 0 : %cl.hatTricks;
      %clWins = %cl.roundsWon $= "" ? 0 : %cl.roundsWon;
      %clLosses = %cl.roundsLost $= "" ? 0 : %cl.roundsLost;

      %score = %cl.score $= "" ? 0 : %cl.score;

      if ( %cl == %client )
        if ( %cl.isAlive )
          %clStyle = "<color:dcdcdc>";
        else
          %clStyle = "<color:dd7a7a>";
      else if ( %cl.isAlive )
        %clStyle = "";
      else
        %clStyle = "<color:f90202>";

      // For living players send a simple name
      if ( %client.team != 0 && %client.isAlive )
      {
         messageClient( %client, 'SetLineHud', "", %tag, %index++, '%5<tab:20, 450>\t<clip:200>%1</clip><rmargin:250><just:right>%2<rmargin:340><just:right>%3<rmargin:450><just:right>%4 / %6<rmargin:540><just:right>%7',
               %cl.name, %clScore, %clKills, %clWins, %clStyle, %clLosses, %clBonus );
      }
      // For observers, create an anchor around the player name so they can be observed
      else
      {
         messageClient( %client, 'SetLineHud', "", %tag, %index++, '%5<tab:20, 450>\t<clip:200><a:gamelink\t%6>%1</a></clip><rmargin:250><just:right>%2<rmargin:340><just:right>%3<rmargin:450><just:right>%4 / %8<rmargin:540><just:right>%7',
               %cl.name, %clScore, %clKills, %clWins, %clStyle, %cl, %clBonus, %clLosses );
      }
    }

    // Insert a blank line

    messageClient( %client, 'SetLineHud', "", %tag, %index++, "");

  }


  // Tack on the list of observers:
  %observerCount = 0;
  for ( %i = 0; %i < ClientGroup.getCount(); %i++ )
  {
    %cl = ClientGroup.getObject(%i);
    if ( %cl.team == 0 )
      %observerCount++;
  }

  if (%observerCount > 0)
  {
	messageClient(%client, 'SetLineHud', "", %tag, %index++, '<tab:10, 310><spush><font:Univers Condensed:22>\tOBSERVERS (%1)<rmargin:260><just:right>TIME<spop>', %observerCount);
//    %index++;
    for (%i = 0; %i < ClientGroup.getCount(); %i++)
    {
      %cl = ClientGroup.getObject(%i);
      //if this is an observer
      if (%cl.team == 0)
      {
        %obsTime = getSimTime() - %cl.observerStartTime;
        %obsTimeStr = %game.formatTime(%obsTime, false);
		messageClient( %client, 'SetLineHud', "", %tag, %index++, '<tab:20, 310>\t<clip:150>%1</clip><rmargin:260><just:right>%2',
		               %cl.name, %obsTimeStr );
//        %index++;
      }
    }
    messageClient( %client, 'SetLineHud', "", %tag, %index++, "");
  }

  // Arena Footer

  messageClient( %client, 'SetLineHud', "", %tag, %index++, "<tab:10, 310><spush><font:Univers Condensed:14><color:ffffff>\tTribes 2 Arena - 1.0 Final - http://www.planettribes.com/t2arena/<spop>" );
//  messageClient( %client, 'SetLineHud', "", %tag, %index++, "<tab:10, 310><spush><font:Univers Condensed:14><color:ffffff>\thttp://www.planettribes.com/t2arena/<spop>" );
  messageClient( %client, 'SetLineHud', "", %tag, %index++, "");

  // Clear the rest of HUD so we don't get old lines hanging around...
  messageClient( %client, 'ClearHud', "", %tag, %index );
}

//
// ArenaGame::processGameLink()
// Called when a client clicks an observer link in the score hud

function ArenaGame::processGameLink( %game, %client, %targetClient, %arg2, %arg3, %arg4, %arg5 )
{
   // The default behavior when clicking on a game link is to start observing that client

   if ( ( %client.team == 0 || !%client.isAlive ) && isObject( %targetClient ) && ( %targetClient.team != 0 ) )
   {
      if ( %targetClient.isAlive )
      {
         if ( %client == %targetClient )  // just in case (...)
            return;
            
         if ( $Host::TournamentMode && %client.team != 0 && %client.team != %targetClient.team )
         {
            messageClient( %client, 0, "\c2You can't observe players on other teams in tournament mode." );
            return;
         }

         %prevObsClient = %client.observeClient;
         serverCmdObserveClient( %client, %targetClient );

         if ( %client.team != 0 )
            %client.camera.getDataBlock().setMode( %client.camera, "arenaDeadFollow" );

         displayObserverHud( %client, %targetClient );
      }
      else if ( !%targetClient.isAIControlled() )
      {
         if ( %targetClient.team == 0 )
         {
            if ( %targetClient.camera.mode $= "ObserverFollow" )
               messageClient( %client, 0, "\c2" @ %targetClient.nameBase @ " is observing " @ getTaggedString( %targetClient.observeClient.name ) @ "." );
            else
               messageClient( %client, 0, "\c2" @ %targetClient.nameBase @ " is not observing anyone." );
         }
         else
         {
            if ( %targetClient.camera.mode $= "arenaDeadFollow" )
               messageClient( %client, 0, "\c2" @ %targetClient.nameBase @ " is observing " @ getTaggedString( %targetClient.observeClient.name ) @ "." );
            else
               messageClient( %client, 0, "\c2" @ %targetClient.nameBase @ " is not observing anyone." );
         }
      }
   }
}

//
// ArenaGame::sendDebriefing()
// Send the debriefing information at the end of a game

function ArenaGame::sendDebriefing( %game, %client )
{
   if ( %game.numTeams == 1 )
   {
      // Mission result:
      %winner = $TeamRank[0, 0];
      if ( %winner.score > 0 )
         messageClient( %client, 'MsgDebriefResult', "", '<just:center>%1 wins!', $TeamRank[0, 0].name );
      else
         messageClient( %client, 'MsgDebriefResult', "", '<just:center>Nobody wins.' );

      // Mission high scores

      if ( $Arena::Pref::TrackHighScores && Game.class $= "ArenaGame" )
      {

      if ( %game.newHighScoreFlag )
        messageClient( %client, 'MsgDebriefResult', "", '<spush><color:3cb4b4><font:univers condensed:18>%1 has set a NEW INDIVIDUAL RECORD for this mission with a score of %2!<spop>', $Arena::HighScores::Name[$currentMission], $Arena::HighScores::Score[$currentMission] );
      else if ( $Arena::HighScores::Name[$currentMission] !$= "" && $Arena::HighScores::Score[$currentMission] !$= "" )
        messageClient( %client, 'MsgDebriefResult', "", '<spush><color:3cb4b4><font:univers condensed:18>%1 holds the individual record for this mission with a score of %2.<spop>', $Arena::HighScores::Name[$currentMission], $Arena::HighScores::Score[$currentMission] );
      else
        messageClient( %client, 'MsgDebriefResult', "", '<spush><color:3cb4b4><font:univers condensed:18>There is no high score recorded for this mission.<spop>' );

      if ( %game.notEnoughHumansFlag )
        messageClient( %client, 'MsgDebriefResult', "", '<spush><color:3cb4b4><font:univers condensed:18>But there are not enough human players to set a new score.<spop>' );

      }

      // Player scores:
      %count = $TeamRank[0, count];
      messageClient( %client, 'MsgDebriefAddLine', "", '<spush><color:00dc00><font:univers condensed:18>PLAYER<lmargin%%:60>SCORE<lmargin%%:80>KILLS<spop>' );
      for ( %i = 0; %i < %count; %i++ )
      {
         %cl = $TeamRank[0, %i];
         if ( %cl.score $= "" )
            %score = 0;
         else
            %score = %cl.score;
         if ( %cl.kills $= "" )
            %kills = 0;
         else
            %kills = %cl.kills;
         messageClient( %client, 'MsgDebriefAddLine', "", '<lmargin:0><clip%%:60> %1</clip><lmargin%%:60><clip%%:20> %2</clip><lmargin%%:80><clip%%:20> %3', %cl.name, %score, %kills );
      }
   }
   else
   {
      %topScore = "";
      %topCount = 0;
      for ( %team = 1; %team <= %game.numTeams; %team++ )
      {
         if ( %topScore $= "" || $TeamScore[%team] > %topScore )
         {
            %topScore = $TeamScore[%team];
            %firstTeam = %team;
            %topCount = 1;
         }
         else if ( $TeamScore[%team] == %topScore )
         {
            %secondTeam = %team;
            %topCount++;
         }
      }

      // Mission result:
      if ( %topCount == 1 )
         messageClient( %client, 'MsgDebriefResult', "", '<just:center>Team %1 wins!', %game.getTeamName(%firstTeam) );
      else if ( %topCount == 2 )
         messageClient( %client, 'MsgDebriefResult', "", '<just:center>Team %1 and Team %2 tie!', %game.getTeamName(%firstTeam), %game.getTeamName(%secondTeam) );
      else
         messageClient( %client, 'MsgDebriefResult', "", '<just:center>The mission ended in a tie.' );


      // Mission high scores

      if ( $Arena::Pref::TrackHighScores && Game.class $= "ArenaGame" )
      {

      if ( %game.newHighScoreFlag )
        messageClient( %client, 'MsgDebriefResult', "", '<spush><color:3cb4b4><font:univers condensed:18>%1 has set a NEW INDIVIDUAL RECORD for this mission with a score of %2!<spop>', $Arena::HighScores::Name[$currentMission], $Arena::HighScores::Score[$currentMission] );
      else if ( $Arena::HighScores::Name[$currentMission] !$= "" && $Arena::HighScores::Score[$currentMission] !$= "" )
        messageClient( %client, 'MsgDebriefResult', "", '<spush><color:3cb4b4><font:univers condensed:18>%1 holds the individual record for this mission with a score of %2.<spop>', $Arena::HighScores::Name[$currentMission], $Arena::HighScores::Score[$currentMission] );
      else
        messageClient( %client, 'MsgDebriefResult', "", '<spush><color:3cb4b4><font:univers condensed:18>There is no high score recorded for this mission.<spop>' );

      if ( %game.notEnoughHumansFlag )
        messageClient( %client, 'MsgDebriefResult', "", '<spush><color:3cb4b4><font:univers condensed:18>But there are not enough human players here to set a new high score.<spop>' );

      }


      // Team scores:
      messageClient( %client, 'MsgDebriefAddLine', "", '<spush><color:00dc00><font:univers condensed:18>TEAM<lmargin%%:60>SCORE<spop>' );
      for ( %team = 1; %team - 1 < %game.numTeams; %team++ )
      {
         if ( $TeamScore[%team] $= "" )
            %score = 0;
         else
            %score = $TeamScore[%team];
         messageClient( %client, 'MsgDebriefAddLine', "", '<lmargin:0><clip%%:60> %1</clip><lmargin%%:60><clip%%:40> %2</clip>', %game.getTeamName(%team), %score );
      }

      // Player scores:
      messageClient( %client, 'MsgDebriefAddLine', "", '\n<lmargin:0><spush><color:00dc00><font:univers condensed:18>PLAYER<lmargin%%:40>TEAM<lmargin%%:70>SCORE<lmargin%%:87>KILLS<spop>' );
      for ( %team = 1; %team - 1 < %game.numTeams; %team++ )
         %count[%team] = 0;

      %notDone = true;
      while ( %notDone )
      {
         // Get the highest remaining score:
         %highScore = "";
         for ( %team = 1; %team <= %game.numTeams; %team++ )
         {
            if ( %count[%team] < $TeamRank[%team, count] && ( %highScore $= "" || $TeamRank[%team, %count[%team]].score > %highScore ) )
            {
               %highScore = $TeamRank[%team, %count[%team]].score;
               %highTeam = %team;
            }
         }

         // Send the debrief line:
         %cl = $TeamRank[%highTeam, %count[%highTeam]];
         %score = %cl.score $= "" ? 0 : %cl.score;
         %kills = %cl.kills $= "" ? 0 : %cl.kills;
         messageClient( %client, 'MsgDebriefAddLine', "", '<lmargin:0><clip%%:40> %1</clip><lmargin%%:40><clip%%:30> %2</clip><lmargin%%:70><clip%%:17> %3</clip><lmargin%%:87><clip%%:13> %4</clip>', %cl.name, %game.getTeamName(%cl.team), %score, %kills );

         %count[%highTeam]++;
         %notDone = false;
         for ( %team = 1; %team - 1 < %game.numTeams; %team++ )
         {
            if ( %count[%team] < $TeamRank[%team, count] )
            {
               %notDone = true;
               break;
            }
         }
      }
   }

   //now go through an list all the observers:
   %count = ClientGroup.getCount();
   %printedHeader = false;
   for (%i = 0; %i < %count; %i++)
   {
      %cl = ClientGroup.getObject(%i);
      if (%cl.team <= 0)
      {
         //print the header only if we actually find an observer
         if (!%printedHeader)
         {
            %printedHeader = true;
            messageClient(%client, 'MsgDebriefAddLine', "", '\n<lmargin:0><spush><color:00dc00><font:univers condensed:18>OBSERVERS<lmargin%%:60>SCORE<spop>');
         }

         //print out the client
         %score = %cl.score $= "" ? 0 : %cl.score;
         messageClient( %client, 'MsgDebriefAddLine', "", '<lmargin:0><clip%%:60> %1</clip><lmargin%%:60><clip%%:40> %2</clip>', %cl.name, %score);
      }
   }
}

//
// ArenaGame::sendGamePlayerPopupMenu()
// Add options for admins to switch players to teams >2

function ArenaGame::sendGamePlayerPopupMenu( %game, %client, %targetClient, %key )
{
  Parent::sendGamePlayerPopupMenu( %game, %client, %targetClient, %key );
  
  // TODO: this is disabled for now
  return;
  
  // we're just adding >2 team options, so quit if not >2 team
  if ( Game.numTeams <= 2 )
    return;
  
  // -- copied from default -- //
  
  if( !%targetClient.matchStartReady )
    return;

  %isAdmin = ( %client.isAdmin || %client.isSuperAdmin );

  %isTargetSelf = ( %client == %targetClient );
  %isTargetAdmin = ( %targetClient.isAdmin || %targetClient.isSuperAdmin );
  %isTargetBot = %targetClient.isAIControlled();
  %isTargetObserver = ( %targetClient.team == 0 );
  %outrankTarget = false;
  if ( %client.isSuperAdmin )
    %outrankTarget = !%targetClient.isSuperAdmin;
  else if ( %client.isAdmin )
    %outrankTarget = !%targetClient.isAdmin;
    
  // -- end copied from default -- //

  if ( %isAdmin && !isDemo() && ( %isTargetSelf || %outrankTarget ) && %game.numTeams > 2 )
  {
    if ( %isTargetObserver )
    {
      %action = %isTargetSelf ? "Join " : "Change to ";
      %str1 = %action @ getTaggedString( %game.getTeamName(1) );
      %str2 = %action @ getTaggedString( %game.getTeamName(2) );

      messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str1, 6 );
      messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str2, 7 );
      
      for ( %iTeam = 3; %iTeam <= Game.numTeams; %iTeam++ )
      {
        %msg = %action @ getTaggedString( %game.getTeamName(%iTeam) );
        messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %msg, 212 - 3 + %iTeam );
        
        // popup keys:   (completely arbitrary - but must sync with arenasupport)
        //  212 - add to team 3
        //  213 - add to team 4
        //  etc..
      }
    }
    else
    {
      %changeTo = %targetClient.team == 1 ? 2 : 1;
      
      // %changeTo is the index of the team that the base code will supply an entry for
      // we supply all the others
      
      for ( %iTeam = 1; %iTeam <= Game.numTeams; %iTeam++ )
      {
        if ( %iTeam != %changeTo && %iTeam != %targetClient.team )
        {
          if ( %iTeam == 1 )
            %caseId = 6;
          else if ( %iTeam == 2 )
            %caseId = 7;
          else
            %caseId = 212 - 3 + %iTeam;

          %msg = "Switch to " @ getTaggedString( %game.getTeamName(%iTeam) );
          messageClient( %client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str, %caseId );
        }
      }
    }
  }
}


// ========================================================================== //
// |                                                                        | //
// |  OUT OF BOUNDS DAMAGE                                                  | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// ArenaGame::enterMissionArea()
// Player reentered the mission area

function ArenaGame::enterMissionArea( %game, %playerData, %player )
{
   if( %player.getState() $= "Dead" )
      return;

   %player.client.outOfBounds = false;
   messageClient( %player.client, 'EnterMissionArea', '\c1You have returned to the mission area.' );
   logEcho( %player.client.nameBase@" (pl "@%player@"/cl "@%player.client@") entered mission area" );

   // Disable out of bounds kill
   cancel( %player.alertThread );
}

// ------------------------------------------------------------------ //
// ArenaGame::leaveMissionArea()
// Player left the mission area

function ArenaGame::leaveMissionArea( %game, %playerData, %player )
{
   if( %player.getState() $= "Dead" )
      return;

   %player.client.outOfBounds = true;
   messageClient( %player.client, 'MsgLeaveMissionArea', '\c1You have left the mission area. Return or take damage.~wfx/misc/warning_beep.wav' );
   logEcho( %player.client.nameBase@" (pl "@%player@"/cl "@%player.client@") left mission area" );

   // Schedule out of bounds kill
   %player.alertThread = %game.schedule( 7500, "MissionAreaDamage", %player );
}

// ------------------------------------------------------------------ //
// ArenaGame::MissionAreaDamage()
// Do damage to a player for being outside the mission area

function ArenaGame::MissionAreaDamage( %game, %player )
{
  if( %player.getState() !$= "Dead" )
  {
    %player.setDamageFlash( 0.1 );
    %prevHurt = %player.getDamageLevel();
    %player.setDamageLevel( %prevHurt + 0.09 );
    %player.alertThread = %game.schedule( 1000, "MissionAreaDamage", %player );
  }
  else
    %game.onClientKilled( %player.client, 0, $DamageType::OutOfBounds );
}


// ========================================================================== //
// |                                                                        | //
// |  GRACE PERIOD                                                          | //
// |                                                                        | //
// ========================================================================== //

// The grace period allows players to still spawn during the first few seconds of a round

// ------------------------------------------------------------------ //
// ArenaGame::setGracePeriod()
// Enable the grace period

function ArenaGame::setGracePeriod( %game )
{
  %game.gracePeriodActive = true;

  // Schedule Grace Period Clear

  if ( %game.gracePeriodClrSch !$= "" )
    cancel( %game.gracePeriodClrSch );

  %game.gracePeriodClrSch = %game.schedule( $Arena::Pref::GracePeriod * 1000, "clearGracePeriod" );
}

// ------------------------------------------------------------------ //
// ArenaGame::clearGracePeriod()
// Disable the grace period

function ArenaGame::clearGracePeriod( %game )
{
  cancel( %game.gracePeriodClrSch );

  %game.gracePeriodActive = false;
  %game.gracePeriodClrSch = "";
  
  
  // mark player's starting team for win/loss point purposes
  // see startRound() for more
  
  %clCount = ClientGroup.getCount();
  for ( %iClient = 0; %iClient < %clCount; %iClient++ )
  {
    %client = ClientGroup.getObject(%iClient);
    %client.arenaRoundStartTeam = %client.team;
  }
}

// ------------------------------------------------------------------ //
// ArenaGame::checkGracePeriod()
// Check whether the grace period is active (return true/false)

function ArenaGame::checkGracePeriod( %game )
{
  return %game.gracePeriodActive;
}


// ========================================================================== //
// |                                                                        | //
// |  COUNTDOWNS                                                            | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// ArenaGame::scheduleRoundCountdown()
// Schedule the announcer voice and messages counting down to the next round

function ArenaGame::scheduleRoundCountdown( %game, %timeMS )
{
  echo( "starting round countdown..." );
  
  UpdateClientTimes( %timeMS );

  //if ( %timeMS > 30000 )
  //  %game.notifyRoundStart( %timeMS );

  if( %timeMS >= 30000 )
    %game.rndthirtyCount = %game.schedule( %timeMS - 30000, "notifyRoundStart", 30000 );
  if( %timeMS >= 15000 )
    %game.rndfifteenCount = %game.schedule( %timeMS - 15000, "notifyRoundStart", 15000 );
  if( %timeMS >= 10000 )
    %game.rndtenCount = %game.schedule( %timeMS - 10000, "notifyRoundStart", 10000 );
  if( %timeMS >= 5000 )
    %game.rndfiveCount = %game.schedule( %timeMS - 5000, "notifyRoundStart", 5000 );
  if( %timeMS >= 4000 )
    %game.rndfourCount = %game.schedule( %timeMS - 4000, "notifyRoundStart", 4000 );
  if( %timeMS >= 3000 )
    %game.rndthreeCount = %game.schedule( %timeMS - 3000, "notifyRoundStart", 3000 );
  if( %timeMS >= 2000 )
    %game.rndtwoCount = %game.schedule( %timeMS - 2000, "notifyRoundStart", 2000 );
  if( %timeMS >= 1000 )
    %game.rndoneCount = %game.schedule( %timeMS - 1000, "notifyRoundStart", 1000 );

  %game.rndzeroCount = %game.schedule( %timeMS, "notifyRoundStart", 0 );
}

// ------------------------------------------------------------------ //
// ArenaGame::cancelRoundCountdown()
// Cancel any round countdown that's in progress

function ArenaGame::cancelRoundCountdown( %game )
{
  cancel( %game.rndthirtyCount );
  cancel( %game.rndfifteenCount );
  cancel( %game.rndtenCount );
  cancel( %game.rndfiveCount );
  cancel( %game.rndfourCount );
  cancel( %game.rndthreeCount );
  cancel( %game.rndtwoCount );
  cancel( %game.rndoneCount );
  cancel( %game.rndzeroCount );
}

// ------------------------------------------------------------------ //
// ArenaGame::notifyRoundStart()
// Show messages and play voice for %time seconds from the round start
// Called by schedule

function ArenaGame::notifyRoundStart( %game, %time )
{
  %seconds = mFloor( %time / 1000 );

  if ( %seconds > 2 )
    MessageAll( 'MsgMissionStart', "\c2Round starts in "@%seconds@" seconds.~wfx/misc/hunters_"@%seconds@".wav", %seconds );
  else if ( %seconds == 2 )
    MessageAll( 'MsgMissionStart', '\c2Round starts in 2 seconds.~wvoice/announcer/ann.match_begins.wav' );
  else if ( %seconds == 1 )
    MessageAll( 'MsgMissionStart', '\c2Round starts in 1 second.' );
  else if ( %seconds == 0 )
    MessageAll( 'MsgMissionStart', '\c2Round started.' );

  UpdateClientTimes( %time );
}

// ------------------------------------------------------------------ //
// ArenaGame::scheduleRoundEndCountdown()
// Schedule the announcer voice and messages counting down to the end of the round

function ArenaGame::scheduleRoundEndCountdown( %game, %timeMS )
{
  if( %timeMS >= 60000 )
    %game.rndendsixtyCount = %game.schedule( %timeMS - 60000, "notifyRoundEnd", 60000 );
  if( %timeMS >= 30000 )
    %game.rndendthirtyCount = %game.schedule( %timeMS - 30000, "notifyRoundEnd", 30000 );
  if( %timeMS >= 10000 )
    %game.rndendtenCount = %game.schedule( %timeMS - 10000, "notifyRoundEnd", 10000 );
  if( %timeMS >= 5000 )
    %game.rndendfiveCount = %game.schedule( %timeMS - 5000, "notifyRoundEnd", 5000 );
  if( %timeMS >= 4000 )
    %game.rndendfourCount = %game.schedule( %timeMS - 4000, "notifyRoundEnd", 4000 );
  if( %timeMS >= 3000 )
    %game.rndendthreeCount = %game.schedule( %timeMS - 3000, "notifyRoundEnd", 3000 );
  if( %timeMS >= 2000 )
    %game.rndendtwoCount = %game.schedule( %timeMS - 2000, "notifyRoundEnd", 2000 );
  if( %timeMS >= 1000 )
    %game.rndendoneCount = %game.schedule( %timeMS - 1000, "notifyRoundEnd", 1000 );
}

// ------------------------------------------------------------------ //
// ArenaGame::cancelRoundEndCountdown()
// Cancel any round end countdown that's in progress

function ArenaGame::cancelRoundEndCountdown( %game )
{
  cancel( %game.rndendsixtyCount );
  cancel( %game.rndendthirtyCount );
  cancel( %game.rndendtenCount );
  cancel( %game.rndendfiveCount );
  cancel( %game.rndendfourCount );
  cancel( %game.rndendthreeCount );
  cancel( %game.rndendtwoCount );
  cancel( %game.rndendoneCount );
}

// ------------------------------------------------------------------ //
// ArenaGame::notifyRoundEnd()
// Show messages and play voice for %time seconds from the round end
// Called by schedule

function ArenaGame::notifyRoundEnd( %game, %time )
{
  %seconds = mFloor( %time / 1000 );
  
  if ( %seconds > 1 )
    MessageAll( 'MsgMissionEnd', "\c2Round ends in "@%seconds@" seconds.~wfx/misc/hunters_"@%seconds@".wav", %seconds );
  else if ( %seconds == 1 )
    MessageAll( 'MsgMissionEnd', '\c2Round ends in 1 second.~wfx/misc/hunters_1.wav' );
    
  UpdateClientTimes( %time );
}


// ========================================================================== //
// |                                                                        | //
// |  ARENASUPPORT VERSIONING/HANDSHAKE/SETUP                               | //
// |                                                                        | //
// ========================================================================== //

$Arena::SupportTotal = 0;

//
// serverCmdArenaSupportHello()
// Handles a version response handshake from an arena support enabled client

function serverCmdArenaSupportHello( %client, %supportVersion, %prefShowWaypoints, %prefShowObservers )
{
  if ( $Arena::SupportTotalClients[%client.nameBase] $= "" )
  {
    $Arena::SupportTotal++;
    $Arena::SupportTotalClients[%client.nameBase] = true;
  }

  %client.arenaSupport = %supportVersion;
  
  if ( %prefShowWaypoints !$= "" )
    %client.arenaPrefShowWaypoints = %prefShowWaypoints;
    
  if ( %prefShowObservers !$= "" )
    %client.arenaPrefShowObservers = %prefShowObservers;

  TS_INFO( "client "@%client.nameBase@" is arenaSupport enabled (version "@%client.arenaSupport@")" );
}

//
// serverCmdArenaSetPrefs()
// Updates preference variables from the client

function serverCmdArenaSetPrefs( %client, %prefShowWaypoints, %prefShowObservers )
{
  if ( %prefShowWaypoints !$= "" )
  {
    if ( %client.arenaPrefShowWaypoints != %prefShowWaypoints )
      %bChanged = true;
      
    %client.arenaPrefShowWaypoints = %prefShowWaypoints;
    
    if ( %bChanged && !%prefShowWaypoints && %client.hasArenaWaypointTo )
        Game.waypointRevoke( %client );
  }

  if ( %prefShowObservers !$= "" )
    %client.arenaPrefShowObservers = %prefShowObservers;

  TS_INFO( "client "@%client.nameBase@" updated preferences" );
}

//
// serverCmdArenaDisableWaypoints()
// Obvious way to just set the waypoint pref to off

function serverCmdArenaDisableWaypoints( %client )
{
  if ( %client.arenaPrefShowWaypoints && %client.hasArenaWaypointTo )
    Game.waypointRevoke( %client );
    
  %client.arenaPrefShowWaypoints = false;
}



// ========================================================================== //
// |                                                                        | //
// |  SERVER INFORMATION COMMANDS                                           | //
// |                                                                        | //
// ========================================================================== //

// -------------------------------------------------------- //
// serverCmdArenaCountSupport()
// Displays to the client the number of clients on the server with arena support

function serverCmdArenaCountSupport( %client )
{
  %count = 0;
  %list = "";

  %clCount = ClientGroup.getCount();
  for ( %iCl = 0; %iCl < %clCount; %iCl++ )
  {
    %indexClient = ClientGroup.getObject( %iCl );

    if ( %indexClient.arenaSupport != 0 )
    {
      %count++;
      if ( %list !$= "" )
        %list = %list @ ", " @ %indexClient.nameBase ;
      else
        %list = %indexClient.nameBase;
    }
  }

  messageClient( %client, 0, "\c2Currently "@%count@" clients are arenaSupport enabled (With a total of "@$Arena::SupportTotal@" since the last server reset.)" );
  messageClient( %client, 0, "\c2These are the current clients: " @ %list );
}

// -------------------------------------------------------- //
// serverCmdArenaViewObservers()
// Displays to the client a list of the people observing them

function serverCmdArenaViewObservers( %client )
{
  %list = "";

  %clCount = ClientGroup.getCount();
  for ( %iCl = 0; %iCl < %clCount; %iCl++ )
  {
    %indexClient = ClientGroup.getObject( %iCl );

    // Check if this client has the caller client as an observer target
    // And also that they're actually in followcam mode
    if ( %indexClient.observeClient == %client
         && ( %client.camera.mode $= "observerFollow" || %client.camera.mode $= "arenaDeadFollow" )
         && ( %client.team == 0 || !%client.isAlive ) )
    {
      if ( %list !$= "" )
        %list = %list @ ", " @ %indexClient.nameBase;
      else
        %list = %indexClient.nameBase;
    }
  }

  if ( %list $= "" )
    messageClient( %client, 'ArenaObserver', "\c2There is nobody observing you.", %list );
  else
    messageClient( %client, 'ArenaObserver', "\c2The following clients are observing you: " @ %list, %list );
}

// -------------------------------------------------------- //
// serverCmdArenaGetHighScore()
// Get the high score for the current map

function serverCmdArenaGetHighScore( %client, %mapName )
{
  if ( !$Arena::Pref::TrackHighScores || Game.class !$= "ArenaGame" )
  {
    messageClient( %client, 0, "\c2This server doesn't track high scores." );
    return;
  }
  
  if ( %mapName $= "" )
  {
    %mapName = $currentMission;
    %thisThat = "this map";
  }
  else
    %thisThat = %mapName;

  if ( $Arena::HighScores::Score[%mapName] !$= ""
       && $Arena::HighScores::Score[%mapName] > 0 )
  {
    messageClient( %client, 'MsgArenaHighScore',
                   "\c2The high score for "@%thisThat@" is "@$Arena::HighScores::Score[%mapName]@" set by "@$Arena::HighScores::Name[%mapName],
                   $Arena::HighScores::Name[%mapName],
                   $Arena::HighScores::Score[%mapName] );
  }
  else
    messageClient( %client, 'MsgArenaHighScore', "\c2There is no individual high score recorded for "@%thisThat@".", "", 0 );
}

// -------------------------------------------------------- //
// serverCmdArenaGetAllHighScores()
// Dumps the high score file to the client (currently this is mostly for debugging)
// Includes spam protection since this may be hitting the disk

function serverCmdArenaGetAllHighScores( %client )
{
  if ( !$Arena::Pref::TrackHighScores || Game.class !$= "ArenaGame" )
  {
    messageClient( %client, 0, "\c2This server doesn't track high scores." );
    return;
  }

  if ( tsCheckCommandSpam( %client, 10 ) )
  {
    messageClient( %client, 0, "\c2You have executed a command too recently, try again later" );
    return;
  }
  
  TS_INFO( "client "@%client.nameBase@" requested dump of high scores file" );
  
  // --------
  
  %scoreFile = new FileObject();

  if ( %scoreFile.openForRead( "prefs/arenaHighScores.cs" ) )
  {
    while ( !%scoreFile.isEOF() )
      messageClient( %client, 0, %scoreFile.readLine() );

    %scoreFile.close();
  }
  else
    TS_ERROR( "ERROR! failed to open scores file" );
}


// ========================================================================== //
// |                                                                        | //
// |  SERVER ADMIN COMMANDS                                                 | //
// |                                                                        | //
// ========================================================================== //

// -------------------------------------------------------- //
// serverCmdArenaForceRoundEnd()
// Ends the current round in a tie and advances to the next one

function serverCmdArenaForceRoundEnd( %client, %teamIndex )
{
  if ( %teamIndex $= "" || %teamIndex < 0 || %teamIndex > Game.numTeams )
    %teamIndex = 0;

  if ( %client.isAdmin )
  {
    if ( Game.state !$= "play" )
    {
      messageClient( %client, 0, "\c2You may only use that command while a round is active." );
      return;
    }
    
    if ( %teamIndex == 0 )
    {
      messageAll( 0, "\c2"@%client.nameBase@" forced the round to end." );
      TS_INFO( %client.nameBase@" forced the round to end" );
    }
    else
    {
      %teamName = getTaggedString( Game.getTeamName( %teamIndex ) );
      
      messageAll( 0, "\c2"@%client.nameBase@" forced the round to end with a victory for "@%teamName );
      TS_INFO( %client.nameBase@" forced the round to end with a victory for "@%teamName );
    }

    Game.endRound( %teamIndex );
  }
  else
    messageClient( %client, 0, "\c2You must be an admin to use that command." );
}

// -------------------------------------------------------- //
// serverCmdArenaSetCurrentRoundLimit()
// Changes the round limit (this lasts only until the server changes maps)

function serverCmdArenaSetCurrentRoundLimit( %client, %newRoundLimit )
{
  if ( %client.isAdmin )
  {
    messageAll( 0, "\c2"@%client.nameBase@" changed the round limit for this map to "@%newRoundLimit@"." );
    TS_INFO( %client.nameBase@" changed the round limit for this map to "@%newRoundLimit );

    Game.roundLimit = %newRoundLimit;
  }
  else
    messageClient( %client, 0, "\c2You must be an admin to use that command." );
}

// -------------------------------------------------------- //
// serverCmdArenaSetCurrentTimeLimit()
// Changes the time limit (this lasts only until the server changes maps)

function serverCmdArenaSetCurrentTimeLimit( %client, %newTimeLimit )
{
  if ( %client.isAdmin )
  {
    %tlPlural = ( %newTimeLimit != 1 ? "s" : "" );
    
    messageAll( 0, "\c2"@%client.nameBase@" changed the time limit for this map to "@%newTimeLimit@" minute"@%tlPlural@". It will take effect next round." );
    TS_INFO( %client.nameBase@" changed the time limit for this map to "@%newTimeLimit );

    Game.adminTimeLimit = %newTimeLimit;
  }
  else
    messageClient( %client, 0, "\c2You must be an admin to use that command." );
}

//
// serverCmdArenaSetLaserBan()
// Toggle the laser rifle ban

function serverCmdArenaSetLaserBan( %client, %bEnabled )
{
  if ( %bEnabled $= "" )
    return;
    
  %bSet = !%bEnabled;
    
  if ( %client.isSuperAdmin )
  {
    $InvBanList[Arena, "SniperRifle"] = %bSet;
    $Arena::Pref::BanLaserRifle = %bSet;
  }
  else if ( %client.isAdmin )
    $InvBanList[Arena, "SniperRifle"] = %bSet;
}


// ========================================================================== //
// |                                                                        | //
// |  UTILITY                                                               | //
// |                                                                        | //
// ========================================================================== //

// ------------------------------------------------------------------ //
// arenaSetClientAlive()
// Mark a client as being alive (called on participants before a round)

function arenaSetClientAlive( %client )
{
  %client.isAlive = true;
  %client.stationUseCount = 0;
  
  %client.isFinalPlayer = false;
  %client.hasArenaWaypointTo = "";
}

// ------------------------------------------------------------------ //
// arenaSetClientDead()
// Mark a client as being dead (called on participants as they die)

function arenaSetClientDead( %client )
{
  %client.isAlive = false;
}

// ------------------------------------------------------------------ //
// arenaCountTeamLivingClients()
// Count the number of players on a team who are still alive

function arenaCountTeamLivingClients( %teamIndex )
{
  %clCount = ClientGroup.getCount();
  %returnCount = 0;
  
  for ( %i = 0; %i < %clCount; %i++ )
  {
    %client = ClientGroup.getObject( %i );
    
    if ( %client.team == %teamIndex && %client.isAlive )
      %returnCount++;
  }
  
  return %returnCount;
}


// ------------------------------------------------------------------ //
// arenaUpdateObjectiveHUD()
// Update the TeamState section of the Arena objective HUD

function arenaUpdateObjectiveHUD( %teamIndex ) // <- (Optional) update for only one team
{
  if ( %teamIndex !$= "" )
    // Update TeamState for one team
    messageAll( 'MsgArenaTeamState', "", %teamIndex, arenaCountTeamLivingClients( %teamIndex ), tsPlayerCountTeam( %teamIndex ) );
  else
    // Update TeamState for all teams
    for( %i = 1; %i <= Game.numTeams; %i++ )
      messageAll( 'MsgArenaTeamState', "", %i, arenaCountTeamLivingClients( %i ), tsPlayerCountTeam( %i ) );
}

