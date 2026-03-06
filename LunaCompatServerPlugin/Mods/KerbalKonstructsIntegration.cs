using LmpCommon.Message.Data;

using LunaCompatCommon.Messages;
using LunaCompatCommon.Utils;

using Server.Client;
using Server.System;

namespace LunaCompatServerPlugin.Mods
{
    internal class KerbalKonstructsIntegration : ServersideModIntegration
    {
        #region Fields

        private ServerModMessageHandler _messageHandler;

        #endregion

        #region Properties

        public override string ModPrefix => "KerbalKonstructs";

        #endregion

        #region Public Methods

        public override void Setup(ServerModMessageHandler messageHandler)
        {
            _messageHandler = messageHandler;

            _messageHandler.OnCompatMessageReceived += OnCompatMessageReceived;
        }

        #endregion

        #region Non-Public Methods

        private void OnCompatMessageReceived(object sender, (ClientStructure Client, ModMsgData Data) e)
        {
            if (SerializationUtil.IsMessageOfType<KerbalKonstructRequestInstancesMessage>(e.Data.ModName))
            {
                // send all instances

                var basePath = Path.Combine(LunaCompatServer.GetLunaCompatBaseDirectory(), "KerbalKonstructs", "NewInstances");

                if (FileHandler.FolderExists(basePath))
                {
                    var files = Directory.GetFiles(basePath);

                    if (files.Any())
                    {
                        foreach (var i in files)
                        {
                            var instanceMsg = _messageHandler.CreateModMsgData(new KerbalKonstructChangeStaticInstanceMessage
                            {
                                PathName = Path.GetFileName(i),
                                Content = FileHandler.ReadFileText(i)
                            });
                            _messageHandler.SendCompatMessage(e.Client, instanceMsg);
                        }
                    }
                }
            }

            if (SerializationUtil.IsMessageOfType<KerbalKonstructChangeStaticInstanceMessage>(e.Data.ModName))
            {
                var message = SerializationUtil.Deserialize<KerbalKonstructChangeStaticInstanceMessage>(e.Data.Data);
                var basePath = Path.Combine(LunaCompatServer.GetLunaCompatBaseDirectory(), "KerbalKonstructs", "NewInstances");
                if (!FileHandler.FolderExists(basePath))
                    FileHandler.FolderCreate(basePath);
                var fileName = Path.GetFileName(message.PathName);
                var instancePath = Path.Combine(basePath, fileName);

                FileHandler.WriteToFile(instancePath, message.Content);
            }
        }

        #endregion
    }
}
