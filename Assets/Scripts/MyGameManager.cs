using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChatClientExample
{
    public enum GameState
    {
        LOBBY,
        INTERMISSION,
        IN_GAME
    }

    public class MyGameManager
    {
        public GameState gameState;
        public Server owner;

        public SpawnPoint spawnPoints = new SpawnPoint();

        public MyGameManager(Server owner)
        {
            this.owner = owner;
        }

       
        public void GameUpdate()
        {
            CheckTeamStatus();

            switch (gameState)
            {
                case GameState.LOBBY://Check if teams are full


                    break;
                case GameState.INTERMISSION://Check if all players Ready for next round

                    ReadyCheck();

                    break;
                case GameState.IN_GAME:

                    if (CheckOutOfBounds(ServerSettings.teamRed) == true)//Check if round is over
                    {
                        Debug.Log("Team Red = OUT OF BOUNDS");
                        if (ServerSettings.activeZone > 0)
                        {
                            ServerSettings.activeZone = ServerSettings.activeZone - 1;
                            gameState = GameState.INTERMISSION;

                            SpawnPlayers();
                        }
                        else
                        {
                            //EndGame
                            gameState = GameState.LOBBY;
                            SetScore();
                            ClearGame();
                        }

                    }
                    if (CheckOutOfBounds(ServerSettings.teamBlue) == true)
                    {
                        Debug.Log("Team Blue = OUT OF BOUNDS");

                        if (ServerSettings.activeZone < 4)
                        {
                            ServerSettings.activeZone = ServerSettings.activeZone + 1;

                            gameState = GameState.INTERMISSION;

                            SpawnPlayers();
                        }
                        else
                        {
                            //EndGame
                            gameState = GameState.LOBBY;
                            SetScore();
                            ClearGame();
                        }

                    }
                    break;
            }
        }

        public void CheckTeamStatus()
        {
            switch (gameState)
            {
                case GameState.LOBBY:
                    //Check if teams are full
                    if (ServerSettings.redTeamPlayerCount == ServerSettings.maxTeamPlayerCount && ServerSettings.blueTeamPlayerCount == ServerSettings.maxTeamPlayerCount)
                    {
                        if (gameState == GameState.LOBBY)
                        {
                            //Spawn Players 
                            Debug.Log($"StartGame - TeamRedPlayerCount:{ServerSettings.redTeamPlayerCount} TeamBluePlayerCount:{ServerSettings.redTeamPlayerCount}");
                            SpawnPlayers();//start in zone 3 and 4
                            gameState = GameState.INTERMISSION;
                        }
                    }
                    break;

                case GameState.IN_GAME:
                    if (ServerSettings.redTeamPlayerCount != ServerSettings.maxTeamPlayerCount || ServerSettings.blueTeamPlayerCount != ServerSettings.maxTeamPlayerCount)
                    {
                        Debug.Log("Teams are not FULL");
                        gameState = GameState.INTERMISSION;

                        ClearGame();
                    }
                    break;
            }
            
        }
        public void ReadyCheck()
        {
            

            bool clientsReady()
            {
                if (ServerSettings.teamRed.Count == ServerSettings.maxTeamPlayerCount && ServerSettings.teamBlue.Count == ServerSettings.maxTeamPlayerCount)
                {
                    foreach (PlayerInfo player in ServerSettings.teamRed)
                    {
                        if (player != null && player.playerState != PlayerState.READY)
                        {
                            //Debug.Log($"Player from team Red: {player.clientID} is not Ready");
                            return false;
                        }
                    }
                    foreach (PlayerInfo player in ServerSettings.teamBlue)
                    {
                        if (player != null && player.playerState != PlayerState.READY)
                        {
                            //Debug.Log($"Player from team Blue: {player.clientID} is not Ready");
                            return false;
                        }
                    }

                    return true;
                }
                return false;
            }



            if (clientsReady() == true)
            {
                gameState = GameState.IN_GAME;
            }
        }
       

        public void SpawnPlayers()
        {
            //CleanUp
            EndRound();

            //Spawn Players on SERVER
            foreach (KeyValuePair<uint, PlayerInfo> player in owner.playerInfo)
            {
                SetPlayerState(owner, player.Value.clientID, (int)PlayerState.NOT_READY);
                uint networkID = NetworkManager.NextNetworkID;
                owner.playerInfo[player.Value.clientID].clientstate = ClientState.IN_GAME;

                Vector3 rot = new Vector3();

                if (player.Value.team == Team.RED)
                {
                    rot = new Vector3(0, 180, 0);
                }
                if (player.Value.team == Team.BLUE)
                {
                    rot = new Vector3(0, 0, 0);
                }
                GameObject newPlayer;
                if (owner.networkManager.SpawnWithID(NetworkSpawnObject.PLAYER, networkID, player.Value.clientID, (uint)player.Value.team, spawnPos(player.Value), rot, out newPlayer))
                {
                    NetworkPlayer playerInstance = newPlayer.GetComponent<NetworkPlayer>();
                    playerInstance.isServer = true;
                    playerInstance.isLocal = false;
                    playerInstance.networkID = networkID;

                    player.Value.networkID = playerInstance.networkID;
                    owner.playerInstances.Add(player.Value.connection, newPlayer.GetComponent<NetworkPlayer>());
                    player.Value.spawnPos = spawnPos(player.Value);
                    player.Value.spawnRot = rot;

                    if (player.Value.team == Team.RED)
                    {
                        player.Value.activeZone = ServerSettings.activeZone;
                    }
                    if (player.Value.team == Team.BLUE)
                    {
                        player.Value.activeZone = ServerSettings.activeZone + 1;
                    }

                    //Spawn player on Client
                    NetworkPlayerSpawnMessage spawnMsg = new NetworkPlayerSpawnMessage
                    {
                        networkID = networkID,
                        clientID = player.Value.clientID,
                        objectType = (uint)NetworkSpawnObject.PLAYER,
                        pos = player.Value.spawnPos,
                        rot = player.Value.spawnRot
                    };

                    owner.SendReply(player.Value.connection, spawnMsg);
                }
                else
                {
                    Debug.LogError("Could not spawn player instance");
                }

            }
            //Spawn Other players on Client
            foreach (KeyValuePair<uint, PlayerInfo> player in owner.playerInfo)
            {
                foreach (KeyValuePair<uint, PlayerInfo> networkPlayer in owner.playerInfo)
                {
                    if (player.Value.networkID == networkPlayer.Value.networkID)
                    {
                        //Debug.Log($"same key: {networkPlayer.Value.networkID} {player.Value.networkID}");
                        continue;
                    }

                    NetworkSpawnMessage clientMsg = new NetworkSpawnMessage
                    {
                        clientID = networkPlayer.Value.clientID,
                        networkID = networkPlayer.Value.networkID,
                        objectType = (uint)NetworkSpawnObject.PLAYER,
                        pos = networkPlayer.Value.spawnPos,
                        rot = networkPlayer.Value.spawnRot

                    };


                    owner.SendReply(player.Value.connection, clientMsg);
                }

            }

        }
        public Vector3 spawnPos(PlayerInfo info)
        {
            if (info.team == Team.RED)
            {
                Vector3 spawnPos = spawnPoints.zones[ServerSettings.activeZone].spot[info.teamPos];
                return spawnPos;

            }
            if (info.team == Team.BLUE)
            {
                Vector3 spawnPos = spawnPoints.zones[ServerSettings.activeZone + 1].spot[info.teamPos];
                return spawnPos;

            }

            Debug.Log("Invalid Spawn Position");
            return Vector3.zero;
        }

        public void HandleOutOfBounds(uint clientID, uint networkID)
        {
            //Destroy Object and set playerinfo.PlayerState.OUT_OF_BOUNDS
            SetPlayerState(owner, clientID, (int)PlayerState.OUT_OF_BOUNDS);
            
        }
        public bool CheckOutOfBounds(List<PlayerInfo> team)
        {
            bool outofbounds = true;

            if(gameState == GameState.IN_GAME)
            {
                for (int i = 0; i < team.Count; i++)
                {
                    if (team[i].playerState != PlayerState.OUT_OF_BOUNDS)
                    {
                        outofbounds = false;
                    }
                }
                return outofbounds;
            }
            return false;

        }
        public void SetPlayerState(Server serv, uint clientID, int state)
        {
            serv.playerInfo[clientID].playerState = (PlayerState)state;
            serv.server_UI.UpdateCard(serv.playerInfo[clientID]);

            ClientPlayerStateMessage msg = new ClientPlayerStateMessage
            {
                state = (PlayerState)state
            };
            serv.SendReply(serv.playerInfo[clientID].connection, msg);



        }
        public void EndRound()
        {
           foreach(KeyValuePair<uint,PlayerInfo> player in owner.playerInfo)
            {
                for (int i = 0; i < player.Value.objectList.Count; i++)
                {
                    if (player.Value.objectList[i] != null)
                    {
                        if (player.Value.objectList[i].type == NetworkSpawnObject.PLAYER)
                        {
                            owner.playerInstances.Remove(player.Value.connection);
                        }
                        owner.networkManager.DestroyWithID(player.Value.objectList[i].networkID);
                        
                        NetworkDestroyMessage msg = new NetworkDestroyMessage
                        {
                            networkID = player.Value.objectList[i].networkID
                        };
                        owner.SendBroadcast(msg);
                    }
                }

                
            }

        }
        public void ClearGame()
        {
            Debug.Log("Clearing Game");
            gameState = GameState.LOBBY;

            //Return players to Lobby, Leave team, Destroy player owned Objects
            foreach (KeyValuePair<uint,PlayerInfo> player in owner.playerInfo)
            {
                GameLobbyMessage msg = new GameLobbyMessage
                {
                    clientID = player.Value.clientID
                };

                Server.HandleClientLobbeyMessage(owner, player.Value.connection, msg);
            }

            //Reset Game
            ServerSettings.activeZone = 2;

        }
        public void SetScore()
        {
            foreach (KeyValuePair<uint,PlayerInfo> player in owner.playerInfo)
            {
                owner.networkBehavior.SetPlayerScore(1, player.Value.userID, player.Value.score);
            }
        }
    }




}

