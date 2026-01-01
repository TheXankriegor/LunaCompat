using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunaCompatServerPlugin;

internal abstract class ServersideModIntegration
{
    #region Properties

    public abstract string ModPrefix { get; }

    #endregion

    #region Public Methods

    // replace server with some interface
    public abstract void Setup(LunaCompatServer messageHandler);

    #endregion
}