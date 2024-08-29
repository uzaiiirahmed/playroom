﻿InsertCoin = async function (options, onLaunchCallBackName, gameObjectName) {
  function onLaunchCallBack() {
    unityInstance.SendMessage(gameObjectName, onLaunchCallBackName);
  }

  await Playroom.insertCoin(options, onLaunchCallBack);
};

OnPlayerJoin = function (gameObjectName) {
  Playroom.onPlayerJoin((player) => {
    console.log("Player joined: " + player.id);

    unityInstance.SendMessage(gameObjectName, "GetPlayerID", player.id);
  });
};

// States
SetState = function (key, value, reliable) {
  reliable = !!reliable;

  Playroom.setState(key, value, reliable);
};

GetState = function (key) {
  return JSON.stringify(Playroom.getState(key));
};

SetPlayerStateByPlayerId = function (playerId, key, value, reliable) {
  const players = window._multiplayer.getPlayers();

  reliable = !!reliable;

  if (typeof players !== "object" || players === null) {
    console.error('The "players" variable is not an object:', players);
    return null;
  }
  const playerState = players[playerId];

  if (!playerState) {
    console.error("Player with ID", playerId, "not found.");
    return null;
  }

  if (typeof playerState.setState === "function") {
    console.log(
      "Setting state for player",
      playerId,
      "key",
      key,
      "value",
      value,
      "reliable",
      reliable
    );

    playerState.setState(key, value, reliable);
  } else {
    console.error('The player state object does not have a "setState" method.');
    return null;
  }
};

GetPlayerStateByPlayerId = function (playerId, key) {
  const players = window._multiplayer.getPlayers();

  if (typeof players !== "object" || players === null) {
    console.error('The "players" variable is not an object:', players);
    return null;
  }

  const playerState = players[playerId];

  if (!playerState) {
    console.error("Player with ID", playerId, "not found.");
    return null;
  }

  if (typeof playerState.getState === "function") {
    try {
      var stateVal = playerState.getState(key);

      if (stateVal === undefined) {
        return null;
      }

      console.log(JSON.stringify(stateVal));

      return JSON.stringify(stateVal);
    } catch (error) {
      console.log("There was an error: " + error);
    }
  } else {
    console.error('The player state object does not have a "getState" method.');
    return null;
  }
};

GetRoomCode = function () {
  return Playroom.getRoomCode();
};

MyPlayer = function () {
  return Playroom.myPlayer().id;
};

IsHost = function () {
  console.log(Playroom.isHost());
  return Playroom.isHost();
};

IsStreamScreen = function () {
  return Playroom.isStreamScreen();
};

GetProfile = function (playerId) {
  const players = window._multiplayer.getPlayers();

  if (typeof players !== "object" || players === null) {
    console.error('The "players" variable is not an object:', players);
    return null;
  }

  const playerState = players[playerId];

  if (!playerState) {
    console.error("Player with ID", playerId, "not found.");
    return null;
  }

  if (typeof playerState.getProfile === "function") {
    const profile = playerState.getProfile();
    var returnStr = JSON.stringify(profile);

    return returnStr;
  } else {
    console.error(
      'The player state object does not have a "getProfile" method.'
    );
    return null;
  }
};

StartMatchmaking = async function () {
  await Playroom.startMatchmaking();
};

OnDisconnect = async function (callback) {
  Playroom.onDisconnect((e) => {
    console.log(`Disconnected!`, e.code, e.reason);
  });
};

WaitForState = async function (key, callback) {
  await Playroom.waitForState(key, callback);
};

WaitForPlayerState = async function (playerId, stateKey, onStateSetCallback) {
  if (!window.Playroom) {
    console.error(
      "Playroom library is not loaded. Please make sure to call InsertCoin first."
    );
    reject("Playroom library not loaded");
    return;
  }

  const players = window._multiplayer.getPlayers();

  if (typeof players !== "object" || players === null) {
    console.error('The "players" variable is not an object:', players);
    return null;
  }
  const playerState = players[playerId];

  if (!playerState) {
    console.error("Player with ID", playerId, "not found.");
    return null;
  }

  await Playroom.waitForPlayerState(playerState, stateKey, onStateSetCallback);
};

Kick = async function (playerID) {
  if (!window.Playroom) {
    console.error(
      "Playroom library is not loaded. Please make sure to call InsertCoin first."
    );
    reject("Playroom library not loaded");
    return;
  }

  const players = window._multiplayer.getPlayers();

  if (typeof players !== "object" || players === null) {
    console.error('The "players" variable is not an object:', players);
    return null;
  }
  const playerState = players[playerID];

  if (!playerState) {
    console.error("Player with ID", playerID, "not found.");
    return null;
  }

  await playerState.kick();
};

OnQuit = function (playerID) {
  if (!window.Playroom) {
    console.error(
      "Playroom library is not loaded. Please make sure to call InsertCoin first."
    );
    reject("Playroom library not loaded");
    return;
  }

  const players = window._multiplayer.getPlayers();

  if (typeof players !== "object" || players === null) {
    console.error('The "players" variable is not an object:', players);
    return null;
  }
  const playerState = players[playerID];

  if (!playerState) {
    console.error("Player with ID", playerID, "not found.");
    return null;
  }

  playerState.onQuit((state) => {
    console.log(`${state.id} quit!`);
  });
};

ResetPlayersStates = async function (keysToExclude) {
  console.log(keysToExclude);
  await Playroom.resetPlayersStates(keysToExclude);
};

ResetStates = async function (keysToExclude) {
  console.log(keysToExclude);
  await Playroom.resetStates(keysToExclude);
};
