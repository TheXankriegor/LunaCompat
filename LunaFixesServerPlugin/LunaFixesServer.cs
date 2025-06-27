using LmpCommon.Enums;
using LmpCommon.Message.Interface;

using Server.Client;
using Server.Log;
using Server.Plugin;

namespace LunaFixesServerPlugin
{
    public class LunaFixesServer : ILmpPlugin
    {
        public void OnUpdate()
        {
            
        }

        public void OnServerStart()
        {
            LunaLog.Info($"Luna: OnServerStart");
        }

        public void OnServerStop()
        {
            LunaLog.Info($"Luna: OnServerStop");
        }

        public void OnClientConnect(ClientStructure client)
        {
            LunaLog.Info($"Luna: OnClientConnect");
        }

        public void OnClientAuthenticated(ClientStructure client)
        {
            LunaLog.Info($"Luna: OnClientAuthenticated");
        }

        public void OnClientDisconnect(ClientStructure client)
        {
            LunaLog.Info($"Luna: OnClientDisconnect");
        }

        public void OnMessageReceived(ClientStructure client, IClientMessageBase messageData)
        {
            if (messageData.MessageType == ClientMessageType.Vessel)
            {

            }
        }

        public void OnMessageSent(ClientStructure client, IServerMessageBase messageData)
        {
            if (messageData.MessageType == ServerMessageType.Vessel)
            {

            }
        }
    }
}
