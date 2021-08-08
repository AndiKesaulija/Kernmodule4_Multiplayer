using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChatClientExample
{
    public static class ServerSettings
    {
        public static uint maxTeamPlayerCount = 1;

        public static uint redTeamPlayerCount;
        public static uint blueTeamPlayerCount;

        public static List<PlayerInfo> teamRed = new List<PlayerInfo>();
        public static List<PlayerInfo> teamBlue = new List<PlayerInfo>();

        public static uint activeZone = 2;

        public static void JoinTeam(Server serv, PlayerInfo player, Team target)
        {

            serv.playerInfo[player.clientID].team = target;

            if (target == Team.BLUE)
            {
                serv.playerInfo[player.clientID].teamPos = blueTeamPlayerCount;

                blueTeamPlayerCount++;
                teamBlue.Add(serv.playerInfo[player.clientID]);
            }
            else if (target == Team.RED)
            {
                serv.playerInfo[player.clientID].teamPos = redTeamPlayerCount;

                redTeamPlayerCount++;
                teamRed.Add(serv.playerInfo[player.clientID]);

            }
            else if (target == Team.SPECTATOR)
            {
                LeaveTeam(serv, player);
            }
            serv.server_UI.UpdateCard(serv.playerInfo[player.clientID]);

            UpdateClients(serv);

        }
        public static void LeaveTeam(Server serv, PlayerInfo player)
        {
            if (player.team == Team.RED)
            {
                redTeamPlayerCount = redTeamPlayerCount - 1;
                teamRed.Remove(serv.playerInfo[player.clientID]);
            }
            if (player.team == Team.BLUE)
            {
                blueTeamPlayerCount = blueTeamPlayerCount - 1;
                teamBlue.Remove(serv.playerInfo[player.clientID]);
            }

            serv.playerInfo[player.clientID].team = Team.SPECTATOR;
            serv.playerInfo[player.clientID].clientstate = ClientState.IN_LOBBY;

            serv.server_UI.UpdateCard(serv.playerInfo[player.clientID]);

            UpdateClients(serv);

        }

        public static void UpdateClients(Server serv)
        {
            //Update Clients
            ServerInfoMessage msg = new ServerInfoMessage();

            serv.SendBroadcast(msg);
        }
    }
}

